using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AppInspectDataModels;
using AppInspectServices.Models;
using CsvHelper;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Text;
using System.Globalization;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using CsvHelper.Configuration;

namespace AppInspectServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalysisController : ControllerBase
    {
        private readonly AppInspectData appInspectData;
        private readonly ServicesSettings serviceSettings;

        public AnalysisController(AppInspectData appInspectData, ServicesSettings serviceSettings)
        {
            this.appInspectData = appInspectData;
            this.serviceSettings = serviceSettings;
        }

        [HttpGet]
        public IEnumerable<AppInspectTask> GetMostRecentUserAnalyses()
        {
            DateTime timeFrame = DateTime.UtcNow.AddDays(-7);
            return appInspectData.Tasks.AsQueryable().Where(t => t.Action == AppInspectConstants.TaskActions.analyse_apks
                                                                && t.Created >= timeFrame
                                                                && t.Requester != "127.0.0.1")
                                                        .OrderByDescending(t => t.Created).ToList();
        }

        [HttpGet("search/{searchTermBase64}")]
        public IEnumerable<AppInspectTask> SearchQueries(string searchTermBase64)
        {
            string? searchTerm = Utility.DecodeBase64(searchTermBase64);

            if (!string.IsNullOrEmpty(searchTerm))
                return appInspectData.Tasks.AsQueryable().Where(t => t.Action == AppInspectConstants.TaskActions.analyse_apks
                                                                && t.Requester != "127.0.0.1"
                                                                && (t.Arguments.Contains(searchTerm) || t.Results!.Contains(searchTerm)))
                                                        .OrderByDescending(t => t.Created).ToList();

            return new List<AppInspectTask>();
        }


        [HttpGet("{taskId}")]
        public ApkAnalysisResults? GetAnalysisResults(string taskId)
        {
            return appInspectData.GetApkAnalysisResults(taskId);
        }

        [HttpGet("{taskId}/download/json")]
        public FileResult? GetAnalysisResultsInJson(string taskId)
        {
            var result = appInspectData.GetApkAnalysisResults(taskId);

            if (result != null)
            {
                return File(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result, new JsonSerializerOptions() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) })), "application/json", $"appinspect_apkanalysis_{result.TaskId}.json");
            }

            return null;
        }

        [HttpGet("{taskId}/download/csv")]
        public FileResult? GetAnalysisResultsInCSV(string taskId)
        {
            var result = appInspectData.GetApkAnalysisResults(taskId);

            var csvRowList = new List<dynamic>();

            if (result != null)
            {
                result.Records.ForEach(appRecord =>
                {
                    appRecord.ApkRecords.ForEach(apkRecord =>
                    {
                        var csvRow = Utility.MergeObjects(new object[] { Utility.FlattenObject(apkRecord, new string[] { "Lines", "PermissionProtectionLevels", "_permissionProtectionLevels" }, new string [] { "Classes" }),
                        new TaskInfoCSVRow()
                            {
                                TaskId = result.TaskId,
                                //QueryArguments = result.ArgumentsPrettifed,
                                Executed = result.Completed
                            }
                        });

                        csvRowList.Add(csvRow);
                    });
                });

                StringWriter textWriter = new StringWriter();
                var csvWriter = new CsvWriter(textWriter, Utility.GetCsvWriterConfiguration());
                Utility.ConfigureCSVWriter(ref csvWriter);

                csvWriter.WriteRecords(csvRowList);

                return File(Encoding.UTF8.GetBytes(textWriter.ToString()), "text/csv", $"appinspect_apkanalysis_{result.TaskId}.csv");
            }

            return null;
        }
    }
}
