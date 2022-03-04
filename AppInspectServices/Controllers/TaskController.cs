using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AppInspectDataModels;
using MongoDB.Driver;
using AppInspectServices.Models;

namespace AppInspectServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ServicesSettings serviceSettings;
        private readonly AppInspectData appInspectData;
        public TaskController(AppInspectData appInspectData, ServicesSettings serviceSettings)
        {
            this.appInspectData = appInspectData;
            this.serviceSettings = serviceSettings;
        }

        [HttpGet]
        public IEnumerable<AppInspectTask> GetMostRecentTasks()
        {
            DateTime twentyFourHoursBefore = DateTime.UtcNow.AddDays(-12);
            return appInspectData.Tasks.AsQueryable().Where(t => t.Created >= twentyFourHoursBefore).OrderByDescending(t => t.Created).Take(1000).ToList();
        }

        [HttpGet("{taskId?}")]
        public AppInspectTask? GetTaskById(string taskId)
        {
            var task = appInspectData.Tasks.AsQueryable().FirstOrDefault(t => t.Id == taskId);
            return task;
        }

        [HttpPost("{taskId?}")]
        public bool UploadTaskResult(string taskId, [FromForm]string result)
        {
            var clientIPAddress = TryGetClientIPAddress();

            if (clientIPAddress != null)
            {
                if (serviceSettings.WorkerAddresses.Contains(clientIPAddress.ToString()))
                {
                    var task = appInspectData.Tasks.AsQueryable().FirstOrDefault(t => t.Id == taskId && t.Results == null && t.Error == null && t.Completed == null && t.Worker == clientIPAddress.ToString());

                    if ((task != null)&&(!string.IsNullOrEmpty(result)))
                    {
                        string resultsHash = Utility.GetSHA256(result);

                        appInspectData.Tasks
                            .UpdateOneAsync(Builders<AppInspectTask>.Filter.Eq(t => t.Id, taskId),
                                Builders<AppInspectTask>.Update
                                    .Set(t => t.Results, result)
                                    .Set(t => t.Results_SHA256, resultsHash));

                        return true;
                    }
                }
            }

            return false;
        }

        private System.Net.IPAddress? TryGetClientIPAddress()
        {
            return HttpContext.Connection.RemoteIpAddress;
        }
    }
}
