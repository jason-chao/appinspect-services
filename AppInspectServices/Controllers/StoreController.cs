using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AppInspectDataModels;
using MongoDB.Driver;
using CsvHelper;
using System.Text;
using System.Globalization;
using System.Text.Json;
using System.Text.Unicode;
using System.Text.Encodings.Web;
using MongoDB.Bson;

namespace AppInspectServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoreController : ControllerBase
    {
        private readonly AppInspectData appInspectData;
        public StoreController(AppInspectData appInspectData)
        {
            this.appInspectData = appInspectData;
        }

        [HttpGet]
        public IEnumerable<AppInspectTask> GetMostRecentUserQueries()
        {
            DateTime timeFrame = DateTime.UtcNow.AddDays(-7);
            return appInspectData.Tasks.AsQueryable().Where(t => t.Action == AppInspectConstants.TaskActions.query_googleplay
                                                                && t.Created >= timeFrame
                                                                && t.Requester != "127.0.0.1")
                                                        .OrderByDescending(t => t.Created).ToList();
        }


        [HttpGet("search/{searchTermBase64}")]
        public IEnumerable<AppInspectTask> SearchQueries(string searchTermBase64)
        {
            string? searchTerm = Utility.DecodeBase64(searchTermBase64);

            if (!string.IsNullOrEmpty(searchTerm))
                return appInspectData.Tasks.AsQueryable().Where(t => t.Action == AppInspectConstants.TaskActions.query_googleplay
                                                                && t.Requester != "127.0.0.1"
                                                                && (t.Arguments.Contains(searchTerm) || t.Results!.Contains(searchTerm)))
                                                        .OrderByDescending(t => t.Created).ToList();

            return new List<AppInspectTask>();
        }

        [HttpGet("{taskId}/download/csv")]
        public FileResult? GetQueryResultsInCSV(string taskId)
        {
            var result = appInspectData.GetStoreQueryRecords(taskId);

            var csvRowList = new List<dynamic>();

            if (result != null)
            {
                if (result.Any())
                {
                    var task = appInspectData.Tasks.AsQueryable().First(t => t.Id == taskId);

                    result.ForEach(storeResult =>
                    {
                        var csvRow = Utility.MergeObjects(new object[] {
                            Utility.FlattenObject(storeResult, null, null),
                            new TaskInfoCSVRow()
                            {
                                TaskId = task.Id,
                                QueryArguments = Utility.ConvertJsonToYaml(task.Arguments),
                                Executed = task.Completed
                            }
                        });

                        csvRowList.Add(csvRow);
                    });

                    var textWriter = new StringWriter();
                    var csvWriter = new CsvWriter(textWriter, Utility.GetCsvWriterConfiguration());
                    Utility.ConfigureCSVWriter(ref csvWriter);

                    csvWriter.WriteRecords(csvRowList);

                    return File(Encoding.UTF8.GetBytes(textWriter.ToString()), "text/csv", $"appinspect_storequery_{task.Id}.csv");
                }
            }

            return null;
        }

        [HttpGet("{taskId}/download/json")]
        public FileResult? GetQueryResultsInJson(string taskId)
        {
            var result = appInspectData.GetStoreQueryRecords(taskId);

            if (result != null)
            {
                if (result.Any())
                {
                    var task = appInspectData.Tasks.AsQueryable().First(t => t.Id == taskId);

                    var resultObject = new { taskId = task.Id, queryArguments = task.Arguments, executed = task.Completed, Records = result };

                    return File(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(resultObject, new JsonSerializerOptions() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) })), "application/json", $"appinspect_storequery_{task.Id}.json");
                }
            }

            return null;
        }

        [HttpGet("appdetails/{appId}/download/csv")]
        public FileResult? GetAppDetailsInCSV(string appId)
        {
            var csvRowList = appInspectData.AppStoreRecords.AsQueryable().Where(sr => sr.AppId == appId).ToList()
                .Select(sr =>
                Utility.MergeObjects(new object[] {
                    Utility.FlattenObject(JsonSerializer.Deserialize<AppStoreQueryRecord>(JsonSerializer.Serialize(BsonTypeMapper.MapToDotNetValue(sr.Details!)!)!)!, null, null),
                    new AppStoreRecordMetadata ()
                        {
                            Retrieved = sr.Retrieved,
                            StoreLocale_Country = sr.StoreCountry,
                            StoreLocale_Language = sr.StoreLanguage
                        }}
                )).ToList();

            if (csvRowList != null)
            {
                if (csvRowList.Any())
                {
                    var textWriter = new StringWriter();
                    var csvWriter = new CsvWriter(textWriter, Utility.GetCsvWriterConfiguration());
                    Utility.ConfigureCSVWriter(ref csvWriter);

                    csvWriter.WriteRecords(csvRowList);

                    return File(Encoding.UTF8.GetBytes(textWriter.ToString()), "text/csv", $"appinspect_appdetails_{appId}.csv");
                }
            }

            return null;
        }


        [HttpGet("appdetails/{appId}/download/json")]
        public FileResult? GetAppDetailsInJson(string appId)
        {
            var results = appInspectData.AppStoreRecords.AsQueryable().Where(sr => sr.AppId == appId).ToList()
                .Select(sr => new AppStoreRecordDownloadResult { AppId = sr.AppId, Details = BsonTypeMapper.MapToDotNetValue(sr.Details), StoreCountry = sr.StoreCountry, StoreLanguage = sr.StoreLanguage, Retrieved = sr.Retrieved }).ToList();

            if (results != null)
            {
                if (results.Any())
                {
                    return File(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(results, new JsonSerializerOptions() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) })), "application/json", $"appinspect_appdetails_{appId}.json");
                }
            }

            return null;
        }

        [HttpGet("appdetails/record/{recordId}/download/json")]
        public FileResult? GetAppDetailsRecordInJson(string recordId)
        {
            var storeRecord = appInspectData.AppStoreRecords.AsQueryable().FirstOrDefault(sr => sr.Id == recordId);

            if (storeRecord != null)
            {
                dynamic detailsObject = BsonTypeMapper.MapToDotNetValue(storeRecord.Details);

                return File(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(detailsObject, new JsonSerializerOptions() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) })), "application/json", $"appinspect_appdetails_record_{recordId}.json");
            }

            return null;
        }
    }

}
