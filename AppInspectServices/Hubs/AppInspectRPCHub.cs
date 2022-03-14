using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

using AppInspectDataModels;
using AppInspectServices.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace AppInspectServices.Hubs
{
    public class AppInspectRPCHub : Hub
    {
        private ServicesSettings serviceSettings;
        private AppInspectData appInspectData;
        private ILogger logger;

        public AppInspectRPCHub(ServicesSettings servicesSettings, AppInspectData appInspectData, ILogger<AppInspectRPCHub> logger)
        {
            this.serviceSettings = servicesSettings;
            this.appInspectData = appInspectData;
            this.logger = logger;
        }

        private System.Net.IPAddress? TryGetClientIPAddress()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext != null)
                return httpContext.Connection.RemoteIpAddress;
            return null;
        }

        public async Task RegisterWorker()
        {
            var clientIPAddress = TryGetClientIPAddress();
            if (clientIPAddress != null)
            {
                if (!serviceSettings.WorkerAddresses.Contains(clientIPAddress.ToString()))
                {
                    await Clients.Client(Context.ConnectionId).SendAsync(AppInspectConstants.RPCClientMethods.WorkerRegistered, false);
                    return;
                }
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, AppInspectConstants.RPCGroups.Workers);
            await Clients.Client(Context.ConnectionId).SendAsync(AppInspectConstants.RPCClientMethods.WorkerRegistered, true);
        }
       

        public async Task SubmitTask(string taskAction, string argumentsJsonString)
        {
            if (AppInspectConstants.Exists(taskAction, typeof(AppInspectConstants.TaskActions)))
            {
                var task = new AppInspectTask()
                {
                    Id = Guid.NewGuid().ToString(),
                    Action = taskAction,
                    Arguments = argumentsJsonString,
                    RequesterConnectionId = Context.ConnectionId,
                    Created = DateTime.UtcNow,
                    Requester = TryGetClientIPAddress()?.ToString()
                };

                await appInspectData.Tasks.InsertOneAsync(task);

                await PushPendingTasksToAllWorkers();

                await Clients.Client(Context.ConnectionId).SendAsync("TaskCreated", task.Id);
            }
        }

        public async Task PushPendingTasksToAllWorkers()
        {
            var pendingTasks = appInspectData.GetOrderedPendingLists().ToList();

            if (pendingTasks.Any())
                await Clients.Group(AppInspectConstants.RPCGroups.Workers).SendAsync(AppInspectConstants.RPCClientMethods.PendingTasks,
                    JsonSerializer.Serialize(pendingTasks));
        }

        public async Task GetPendingTasks()
        {
            var pendingTasks = appInspectData.GetOrderedPendingLists().ToList();

            if (pendingTasks.Any())
                await Clients.Client(Context.ConnectionId).SendAsync(AppInspectConstants.RPCClientMethods.PendingTasks,
                    JsonSerializer.Serialize(pendingTasks));
        }

        public async Task TakeupTasks(string taskIdListJsonString)
        {
            var taskIds = JsonArray.Parse(taskIdListJsonString);

            if (taskIds != null)
            {
                foreach (var taskId in taskIds.AsArray().Select(id => id!.ToString()))
                {
                    if (!appInspectData.Tasks.AsQueryable().Any(t => t.Id == taskId && t.Started == null && t.WorkerConnectionId == null && t.Worker == null))
                        continue;

                    var workerIPAddress = TryGetClientIPAddress()?.ToString();

                    // necessnary to put multiple filter conditions to avoid high concurrency problem - multiple workers may ask to take up the same task at almost the same time
                    await appInspectData.Tasks
                        .UpdateOneAsync(Builders<AppInspectTask>.Filter.Eq(t => t.Id, taskId) &
                                        Builders<AppInspectTask>.Filter.Eq(t => t.Started, null) &
                                        Builders<AppInspectTask>.Filter.Eq(t => t.WorkerConnectionId, null) &
                                        Builders<AppInspectTask>.Filter.Eq(t => t.Worker, null),
                                        Builders<AppInspectTask>.Update
                                            .Set(t => t.Started, DateTime.UtcNow)
                                            .Set(t => t.WorkerConnectionId, Context.ConnectionId)
                                            .Set(t => t.Worker, workerIPAddress));

                    var task = appInspectData.Tasks.AsQueryable().Where(t => t.Id == taskId && t.Worker == workerIPAddress && t.WorkerConnectionId == Context.ConnectionId).FirstOrDefault();
                    if (task != null)
                        await Clients.Client(Context.ConnectionId).SendAsync("AssignTask", task.Id, task.Action, task.Arguments, task.BasePriority);
                }
            }
        }

        private async Task notifyRequesterForTaskStatusChange(string taskId)
        {
            var task = appInspectData.Tasks.AsQueryable().First(t => t.Id == taskId);
            if (task.RequesterConnectionId != null) {
                await Clients.Client(task.RequesterConnectionId!).SendAsync("TaskStatusChanged", task.Id);
            }
        }

        public async Task CompleteTask(string taskId, string? errorMessage)
        {
           await appInspectData.Tasks
            .UpdateOneAsync(Builders<AppInspectTask>.Filter.Eq(t => t.Id, taskId),
                            Builders<AppInspectTask>.Update.Set(t => t.Completed, DateTime.UtcNow)
                            .Set(t => t.Error, errorMessage));

            if (appInspectData.Tasks.AsQueryable().Any(t => t.Id == taskId && t.RequesterConnectionId != null))
            {
                await notifyRequesterForTaskStatusChange(taskId);
            }

            await PushPendingTasksToAllWorkers();

            await HandleAllTaskResults();

            //await HandleTaskResult(taskId);
        }

        public async Task HandleAllTaskResults()
        {
            foreach (string taskId in appInspectData.Tasks.AsQueryable()
                                            .Where(t => t.Completed.HasValue && t.Results != null && t.ResultsHandled == null)
                                            .OrderBy(t => t.Completed).Select(t => t.Id))
            {
                await HandleTaskResult(taskId);
            };
        }

        public async Task HandleTaskResult(string taskId)
        {
            try
            {
                var task = appInspectData.Tasks.AsQueryable().Where(t => t.Id == taskId && t.Completed.HasValue && t.Results != null && t.ResultsHandled == null).FirstOrDefault();

                if (task == null)
                    return;

                switch (task.Action)
                {
                    case AppInspectConstants.TaskActions.query_googleplay:
                        {
                            var requestArgs = JsonSerializer.Deserialize<AppStoreQueryTaskArguments>(task.Arguments);// JsonObject.Parse(task.Arguments);
                            if (requestArgs != null)
                            {
                                switch (requestArgs.QueryMethod)
                                {
                                    case "list":
                                    case "search":
                                    case "developer":
                                    case "similar":
                                        {
                                            var apps = JsonArray.Parse(task.Results!);
                                            if (apps == null)
                                                break;
                                            var appIdList = apps.AsArray().Where(a => a!["appId"] != null).Select(a => Convert.ToString(a!["appId"]!)).ToList();
                                            var queryObject = JsonObject.Parse(requestArgs.QueryInJson);
                                            await appInspectData.CreateGooglePlayAppDetailsQueries(appIdList!, (string?)queryObject!["country"], (string?)queryObject["lang"], requestArgs.AutomaticAPKRetrieval);
                                            await PushPendingTasksToAllWorkers();
                                        }
                                        break;
                                    case "app":
                                        {
                                            await appInspectData.HandleGooglePlayAppDetailsResult(task.Results!, task.Arguments, task.Completed, requestArgs.AutomaticAPKRetrieval);

                                            if (requestArgs.AutomaticAPKRetrieval)
                                            {
                                                await PushPendingTasksToAllWorkers();
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                        break;
                    case AppInspectConstants.TaskActions.retrieve_apk:
                        {
                            var apkInfo = JsonSerializer.Deserialize<APKFileInfo>(task.Results!);//JsonObject.Parse(task.Results!);
                            if (apkInfo != null)
                            {
                                //string? apkBaseFilename = (string?)apkInfo["base_filename"];
                                if (!string.IsNullOrEmpty(apkInfo.BaseFilename))
                                {
                                    await appInspectData.CreateAPKConversionTask(apkInfo.BaseFilename);
                                    await PushPendingTasksToAllWorkers();
                                };
                            }
                        }
                        break;
                    case AppInspectConstants.TaskActions.convert_and_move_apk:
                        {
                            await appInspectData.HandleAPKConversionResult(task.Results!);
                            await PushPendingTasksToAllWorkers();
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"HandleTaskResult : {ex.Message} {ex.Source} {ex.StackTrace}";
                logger.LogError(errorMessage);
            }
            finally
            {
                await appInspectData.MarkTaskResultsHandled(taskId);
            }
        }
    }
}
