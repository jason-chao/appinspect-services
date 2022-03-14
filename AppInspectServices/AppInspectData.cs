using AppInspectServices.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using AppInspectDataModels;

namespace AppInspectServices
{
    public class AppInspectData
    {
        private DatabaseSettings dbSettings;
        private MongoClient dbClient;
        private IMongoDatabase db;
        private IMongoCollection<AppInspectTask> tasks;
        private IMongoCollection<AppEntry> appEntires;
        private IMongoCollection<AppFileRecord> appFileRecords;
        private IMongoCollection<AppStoreRecord> appStoreRecords;

        //public IMongoDatabase DB { get { return db; }  }
        public IMongoCollection<AppInspectTask> Tasks { get { return tasks; } }
        public IMongoCollection<AppEntry> AppEntires { get { return appEntires; } }
        public IMongoCollection<AppFileRecord> AppFileRecords { get { return appFileRecords; } }
        public IMongoCollection<AppStoreRecord> AppStoreRecords { get { return appStoreRecords; } }

        public AppInspectData(DatabaseSettings settings)
        {
            dbSettings = settings;
            dbClient = new MongoClient(dbSettings.ConnectionString);
            db = dbClient.GetDatabase(dbSettings.DatabaseName);
            tasks = db.GetCollection<AppInspectTask>(dbSettings.TaskCollectionName);
            appEntires = db.GetCollection<AppEntry>(dbSettings.AppEntryCollectionName);
            appFileRecords = db.GetCollection<AppFileRecord>(dbSettings.AppFileRecordCollectionName);
            appStoreRecords = db.GetCollection<AppStoreRecord>(dbSettings.AppStoreRecordCollectionName);
        }

        public IEnumerable<string> GetParquetNamesByAppIds(List<string> appIds)
        {
            var resultParquetNames = new List<string>();

            var matchingAppFileRecords = AppFileRecords.AsQueryable().Where(afr => appIds.Contains(afr.AppId)).ToList();

            foreach (var parquetBasename in matchingAppFileRecords.Where(rfr => appIds.Contains(rfr.AppId) && rfr.ParquetBasename != null).GroupBy(fr => fr.ParquetBasename, (parquetBasename, records) => parquetBasename))
            {
                if (!string.IsNullOrEmpty(parquetBasename))
                    resultParquetNames.Add(parquetBasename);
            }

            return resultParquetNames.Distinct();
        }

        public IEnumerable<AppInspectTask> GetPendingTasks()
        {
            return Tasks.AsQueryable().Where(t => t.Started == null);
        }

        public IEnumerable<dynamic> GetOrderedPendingLists()
        {
            var pendingTasks = GetPendingTasks();
            var pendingTaskHeaders = pendingTasks
                .Select(t => new { Id = t.Id, Action = t.Action, Created = t.Created, Priority = t.BasePriority })
                .OrderBy(t => t.Priority).ThenBy(t => t.Created);

            return pendingTaskHeaders.Select(t => t as dynamic);
        }

        public async Task MarkTaskResultsHandled(string taskId)
        {
            await Tasks.UpdateOneAsync(Builders<AppInspectTask>.Filter.Eq(t => t.Id, taskId), Builders<AppInspectTask>.Update.Set(t => t.ResultsHandled, DateTime.UtcNow));
        }

        public async Task CreateNewAppEntry(string appId, string title)
        {
            var newAppEntry = new AppEntry()
            {
                Id = appId,
                Title = title,
            };

            await AppEntires.InsertOneAsync(newAppEntry);
        }

        public async Task HandleGooglePlayAppDetailsResult(string appDetailsResult, string requestArguments, DateTime? retrieved = null, bool autoRetrieveApks = false)
        {
            var resultObj = JsonObject.Parse(appDetailsResult);

            string? title = (string?)resultObj!["title"];
            string? appId = (string?)resultObj!["appId"];

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(appId))
                return;

            var requestObj = JsonSerializer.Deserialize<AppStoreQueryTaskArguments>(requestArguments); //JsonObject.Parse(requestArguments);

            string? country = null;
            string? language = null;

            if (requestObj != null)
            {
                var queryObj = JsonObject.Parse(requestObj.QueryInJson);
                if (queryObj != null)
                {
                    country = (string?)queryObj!["country"];
                    language = (string?)queryObj!["lang"];
                }
            }

            if (!AppEntires.AsQueryable().Any(a => a.Id == appId))
            {
                await CreateNewAppEntry(appId!, title);
            }

            string resultHash = Utility.GetSHA256(appDetailsResult);

            var appEntry = AppEntires.AsQueryable().First(a => a.Id == appId);

            /*var matchingAppStoreRecord = AppStoreRecords.AsQueryable().Where(r => r.AppId == appId && r.JsonSHA256 == resultHash).FirstOrDefault();

            if (matchingAppStoreRecord != null)
            {
                if (!matchingAppStoreRecord.Locales.Any(l => l.Country == country && l.Language == language))
                {
                    await AppStoreRecords.UpdateOneAsync(Builders<AppStoreRecord>.Filter.Eq(r => r.Id, matchingAppStoreRecord.Id),
                                                            Builders<AppStoreRecord>.Update.Push<AppStoreRecord.Locale>(r => r.Locales, new AppStoreRecord.Locale() { Country = country, Language = language }));
                }
            }
            else*/

            if (!AppStoreRecords.AsQueryable().Any(r => r.AppId == appId && r.JsonSHA256 == resultHash))
            {
                var appDetailBson = BsonDocument.Parse(appDetailsResult);

                if (!retrieved.HasValue)
                    retrieved = DateTime.UtcNow;

                var newAppStoreRecord = new AppStoreRecord()
                {
                    Id = Guid.NewGuid().ToString(),
                    AppId = appId,
                    StoreName = "Google Play",
                    //Locales = new List<AppStoreRecord.Locale>() { new AppStoreRecord.Locale() { Country = country, Language = language } },
                    StoreCountry = country,
                    StoreLanguage = language,
                    Details = appDetailBson,
                    JsonSHA256 = resultHash,
                    Retrieved = retrieved.Value
                };

                await AppStoreRecords.InsertOneAsync(newAppStoreRecord);

            }

            // update the title if the record is different from the new result
            if (title != appEntry.Title)
            {
                await AppEntires.UpdateOneAsync(Builders<AppEntry>.Filter.Eq(a => a.Id, appId),
                                            Builders<AppEntry>.Update.Set(a => a.Title, title));
            }

            // create a task to retrieve the APK if the version name is not in file records
            if (autoRetrieveApks)
            {
                var appDetails = JsonSerializer.Deserialize<AppStoreQueryRecord>(appDetailsResult);

                if (appDetails != null)
                {
                    if (appDetails.VersionName != null)
                    {
                        if (!AppFileRecords.AsQueryable().Any(fr => fr.AppId == appDetails.AppId && fr.VersionName == appDetails.VersionName))
                        {
                            if (!ShouldPreventAPKRetrieval(appDetails.AppId!))
                            {
                                await CreateAPKRetrievalTasks(new string[] { appDetails.AppId! });
                            }
                        }
                    }
                }
            }
        }

        public bool ShouldPreventAPKRetrieval(string appId)
        {
            var aPointInTime = DateTime.UtcNow.AddHours(0 - dbSettings.PreventRepeatedAPKRetrievalInHours);
            bool hasRecentedCreatedFileRecord = AppFileRecords.AsQueryable().Any(fr => fr.AppId == appId && fr.Created >= aPointInTime);
            bool hasRecentedCreatedRetrievalTask = Tasks.AsQueryable().Any(t => t.Action == AppInspectConstants.TaskActions.retrieve_apk && t.Arguments.Contains(appId) && t.Created >= aPointInTime);
            return (hasRecentedCreatedFileRecord && hasRecentedCreatedRetrievalTask);
        }

        public async Task CreateAPKRetrievalTasks(IEnumerable<string> appIds, int priority = 800)
        {
            List<AppInspectTask> newTasks = new List<AppInspectTask>();

            foreach (var appId in appIds)
            {
                if (!ShouldPreventAPKRetrieval(appId))
                {
                    var newTask = new AppInspectTask()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Action = AppInspectConstants.TaskActions.retrieve_apk,
                        Arguments = JsonSerializer.Serialize(new { appid = appId }),
                        Requester = "127.0.0.1",
                        Created = DateTime.UtcNow,
                        BasePriority = priority
                    };

                    newTasks.Add(newTask);
                }
            }

            if (newTasks.Any())
                await Tasks.InsertManyAsync(newTasks);
        }

        public async Task CreateAPKConversionTask(string apkFilename, int priority = 700)
        {
            var taskArguments = new APKConversionTaskArguments() { APKFilename = apkFilename };

            var newTask = new AppInspectTask()
            {
                Id = Guid.NewGuid().ToString(),
                Action = AppInspectConstants.TaskActions.convert_and_move_apk,
                Arguments = JsonSerializer.Serialize(taskArguments),
                Requester = "127.0.0.1",
                Created = DateTime.UtcNow,
                BasePriority = priority
            };

            await Tasks.InsertOneAsync(newTask);
        }


        public async Task HandleAPKConversionResult(string apkConversionResult)
        {
            var conversionInfo = JsonSerializer.Deserialize<APKConversionInfo>(apkConversionResult);

            if (conversionInfo == null)
                return;

            if (string.IsNullOrEmpty(conversionInfo.BasicInfo.AppId))
                return;

            if (!AppEntires.AsQueryable().Any(a => a.Id == conversionInfo.BasicInfo.AppId))
            {
                await CreateNewAppEntry(conversionInfo.BasicInfo.AppId, conversionInfo.BasicInfo.AppId);
                await CreateGooglePlayAppDetailsQueries(new List<string> { conversionInfo.BasicInfo.AppId }, null, null, false);
            }

            if (AppFileRecords.AsQueryable().Any(r => r.AppId == conversionInfo.BasicInfo.AppId && r.SHA256 == conversionInfo.BasicInfo.APK_SHA256))
                return;

            var newFileReocrd = new AppFileRecord()
            {
                Id = Guid.NewGuid().ToString(),
                AppId = conversionInfo.BasicInfo.AppId,
                SHA256 = conversionInfo.BasicInfo.APK_SHA256,
                VersionCode = conversionInfo.BasicInfo.VersionCode,
                VersionName = conversionInfo.BasicInfo.VersionName,
                Created = DateTime.UtcNow,
                APKBaseFilenameInArchive = conversionInfo.ArchivedAPKBaseFilename,
                ParquetBasename = conversionInfo.ParquetBasename,
                SignatureVerified = conversionInfo.SignatureVerified,
                SignedByCertificate = conversionInfo.SignedByCertificate
            };

            await AppFileRecords.InsertOneAsync(newFileReocrd);
        }

        public List<AppStoreQueryRecord>? GetStoreQueryRecords(string taskId)
        {
            var task = Tasks.AsQueryable().FirstOrDefault(t => t.Id == taskId);

            if (task == null)
                return null;

            AppStoreQueryTaskArguments? queryTaskArguments;
            var appStoreListResults = new List<AppStoreQueryRecord>();

            if (!string.IsNullOrEmpty(task.Arguments))
            {
                queryTaskArguments = JsonSerializer.Deserialize<AppStoreQueryTaskArguments>(task.Arguments);
                if (queryTaskArguments != null)
                {
                    if (!string.IsNullOrEmpty(task.Results))
                    {
                        switch (queryTaskArguments.QueryMethod)
                        {
                            case "list":
                            case "search":
                            case "developer":
                            case "similar":
                                {
                                    appStoreListResults = JsonSerializer.Deserialize<List<AppStoreQueryRecord>>(task.Results);
                                }
                                break;
                            case "app":
                                {
                                    var appDetails = JsonSerializer.Deserialize<AppStoreQueryRecord>(task.Results);
                                    if (appDetails != null)
                                        appStoreListResults.Add(appDetails);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            return appStoreListResults;
        }

        public ApkAnalysisResults? GetApkAnalysisResults(string taskId)
        {
            var task = Tasks.AsQueryable().FirstOrDefault(t => t.Id == taskId);

            if (task == null)
                return null;

            if (task.Action != AppInspectConstants.TaskActions.analyse_apks)
                return null;

            var taskArguments = JsonObject.Parse(task.Arguments);

            if (taskArguments == null)
                return null;

            var result = new ApkAnalysisResults() { TaskId = taskId, TaskName = (string)taskArguments["task_name"]!, Completed = task.Completed, Started = task.Started, ErrorMessage = task.Error };

            string? argumentsInYaml = Utility.ConvertJsonToYaml(task.Arguments);

            if (argumentsInYaml != null)
                result.ArgumentsPrettifed = argumentsInYaml;
            else
                result.ArgumentsPrettifed = taskArguments.ToJsonString(new JsonSerializerOptions() { WriteIndented = true });

            if (task.Results == null)
                return result;

            var workerResult = JsonSerializer.Deserialize<ApkAnalysisWorkerResults>(task.Results);

            if (workerResult == null)
                return result;

            // the output from the workers is per APK basis.  necessnary to group different versions of APKs of the same app by app id.

            result.OutputApks = workerResult.Total;
            result.Records = workerResult.Records.GroupBy(r => r.AppId, (appid, records) => new GrouppedApkAnalysisRecord() { AppId = appid, ApkRecords = records.OrderByDescending(ar => ar.VersionCode).ToList() }).ToList();
            result.OutputApps = result.Records.Count();
            result.OutputApkLimit = workerResult.OutputLimit;

            var appIdList = result.Records.Select(r => r.AppId).ToList();

            // add app titles to the result set

            var appTitleQuery = AppEntires.AsQueryable().Where(ae => appIdList.Contains(ae.Id)).Select(ae => new { Id = ae.Id, Title = ae.Title });

            foreach (var appTitle in appTitleQuery)
            {
                if (appTitle.Id == appTitle.Title)
                    continue;

                foreach (var record in result.Records)
                {
                    if (record.AppId == appTitle.Id)
                    {
                        record.Title = appTitle.Title;
                        break;
                    }
                }
            }


            if (result.TaskName == "task_code_scan") // aggregate inferred developers in the case of code scan
            {
                var allInferredDevelopers = new List<string>();

                foreach (var workerRecord in workerResult.Records)
                {
                    if (workerRecord.InferredDevelopers != null)
                        allInferredDevelopers.AddRange(workerRecord.InferredDevelopers);
                }

                allInferredDevelopers = allInferredDevelopers.Distinct().ToList();

                result.TotalInferredDevelopers = allInferredDevelopers.Count();
                result.InferredDevelopers = allInferredDevelopers;
            }
            else if (new string[] { "task_tracker_domain_scan", "task_tracker_classname_scan" }.Contains(result.TaskName)) // aggregate trackers in the case of tracker-domain scan and tracker-classname scan
            {
                var allTrackers = new List<string>();

                foreach (var workerRecord in workerResult.Records)
                {
                    if (workerRecord.Trackers != null)
                        allTrackers.AddRange(workerRecord.Trackers);
                }

                allTrackers = allTrackers.Distinct().ToList();

                result.TotalTrackers = allTrackers.Count();
                result.Trackers = allTrackers;
            }

            return result;
        }

        public async Task CreateGooglePlayAppDetailsQueries(List<string> appIdList, string? country = null, string? lang = null, bool autoRetrieveApks = false)
        {
            List<AppInspectTask> newTasks = new List<AppInspectTask>();

            foreach (var appId in appIdList)
            {
                if (string.IsNullOrEmpty(appId))
                    continue;

                var queryObject = new JsonObject();
                queryObject["appId"] = appId;
                if (country != null)
                    queryObject["country"] = country;
                if (lang != null)
                    queryObject["lang"] = lang;

                var taskArgument = new AppStoreQueryTaskArguments()
                {
                    QueryMethod = "app",
                    QueryInJson = queryObject.ToJsonString(),
                    AutomaticAPKRetrieval = autoRetrieveApks,
                };

                newTasks.Add(new AppInspectTask()
                {
                    Id = Guid.NewGuid().ToString(),
                    Action = AppInspectConstants.TaskActions.query_googleplay,
                    Arguments = JsonSerializer.Serialize(taskArgument),
                    Requester = "127.0.0.1",
                    Created = DateTime.UtcNow,
                    BasePriority = 700
                });
            }

            if (newTasks.Any())
                await Tasks.InsertManyAsync(newTasks);
        }
    }
}
