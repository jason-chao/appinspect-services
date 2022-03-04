using AppInspectDataModels;
using AppInspectServices.Hubs;
using AppInspectServices.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace AppInspectServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppController : ControllerBase
    {
        private readonly AppInspectData appInspectData;
        private readonly ServicesSettings serviceSettings;
        private readonly IHubContext<AppInspectRPCHub> rpcHub;


        public AppController(AppInspectData appInspectData, ServicesSettings serviceSettings, IHubContext<AppInspectRPCHub> rpcHub)
        {
            this.appInspectData = appInspectData;
            this.serviceSettings = serviceSettings;
            this.rpcHub = rpcHub;
        }


        [HttpGet("{appId}")]
        public AppEntryAllRecords? getAppEntryResult(string appId)
        {
            if (!FormattingUtility.IsAppIdValid(appId))
                return null;

            var appEntry = appInspectData.AppEntires.AsQueryable().FirstOrDefault(ae => ae.Id == appId);

            if (appEntry != null)
            {
                var appEntryResult = new AppEntryAllRecords() { Id = appEntry.Id, Title = appEntry.Title };

                appEntryResult.FileRecords = appInspectData.AppFileRecords.AsQueryable().Where(fr => fr.AppId == appId).OrderByDescending(fr => fr.VersionCode).ToList();
                appEntryResult.AppStoreRecords = appInspectData.AppStoreRecords.AsQueryable().Where(sr => sr.AppId == appId).OrderByDescending(sr => sr.Retrieved).ToList().Select(sr => new { baseRecord = sr, detailsInJson = JsonSerializer.Serialize(BsonTypeMapper.MapToDotNetValue(sr.Details)) })
                    .Select(r => new AppStoreRecordResult { RecordId = r.baseRecord.Id, Retrieved = r.baseRecord.Retrieved, StoreCountry = r.baseRecord.StoreCountry, StoreLanguage = r.baseRecord.StoreLanguage, PrettifiedDetails = Utility.ConvertJsonToYaml(r.detailsInJson), Url = JsonSerializer.Deserialize<AppStoreQueryRecord>(r.detailsInJson)!.Url! }).ToList();

                return appEntryResult;
            }

            return null;
        }

        [HttpGet("{appId}/{fileHash}/textcontent/{relativePathBase64}")]
        public FileResult? downloadExtractedApkFileTextContent(string appId, string fileHash, string relativePathBase64)
        {
            string? relativePath = Utility.DecodeBase64(relativePathBase64);
            if (!string.IsNullOrEmpty(relativePath))
                return getApkFileTextContent(appId, fileHash, relativePath, null);
            else
                return null;
        }

        [HttpGet("{appId}/{fileHash}/textcontent/byclass/{classNameBase64}")]
        public FileResult? downloadExtractedApkFileTextContentByClass(string appId, string fileHash, string classNameBase64)
        {
            string? className = Utility.DecodeBase64(classNameBase64);
            if (!string.IsNullOrEmpty(className))
                return getApkFileTextContent(appId, fileHash, null, className);
            else
                return null;
        }

        private FileResult? getApkFileTextContent(string appId, string fileHash, string? relativePath, string? className)
        {
            var apkFileRecord = appInspectData.AppFileRecords.AsQueryable().FirstOrDefault(fr => fr.AppId == appId && fr.SHA256 == fileHash);

            if (apkFileRecord != null)
            {
                string parquetDirectory = Path.Combine(serviceSettings.APKParquetPath, apkFileRecord.ParquetBasename!, "content_mode=text");
                var apkParquet = new ApkParquet(parquetDirectory);
                string? content = apkParquet.GetContentText(fileHash, relativePath, className);

                if (!string.IsNullOrEmpty(content))
                {
                    string defaultFilename = "apkcontent.txt";

                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        defaultFilename = relativePath.Replace("/", "_").Replace("\\", "_");
                    }
                    else if (!string.IsNullOrEmpty(className))
                    {
                        defaultFilename = className + ".smali";
                    }

                    return File(Encoding.UTF8.GetBytes(content), "text/plain", defaultFilename);
                }
            }

            return null;
        }

        [HttpGet("download/apk/{apkBaseFilename}")]
        public FileStreamResult? downloadApk(string apkBaseFilename)
        {
            var fullFilePath = Path.Combine(serviceSettings.APKArchivePath, apkBaseFilename);
            var fileRecord = appInspectData.AppFileRecords.AsQueryable().FirstOrDefault(fr => fr.APKBaseFilenameInArchive == apkBaseFilename);

            var downloadFilename = apkBaseFilename;

            if (fileRecord != null)
            {
                downloadFilename = $"{fileRecord.AppId}-{fileRecord.VersionName}.apk";
            }

            if (System.IO.File.Exists(fullFilePath))
            {
                FileStream fileStream = System.IO.File.OpenRead(fullFilePath);
                return new FileStreamResult(fileStream, "application/vnd.android.package-archive") { FileDownloadName = downloadFilename };
            }

            return null;
        }

        [HttpPost("archive")]
        public List<AppArchiveSearchResult> searchArchive(AppArchiveSearch search)
        {
            List<string> resultSetAppIds = new List<string>();

            var query = appInspectData.AppEntires.AsQueryable();

            if (search.AppIds != null)
            {
                if (search.AppIds.Any())
                {
                    search.AppIds = search.AppIds.Select(a => a.Trim().ToLower()).Distinct().ToList();

                    foreach (var appId in search.AppIds)
                    {
                        var matchingAppIds = query.Where(entry => entry.Id.ToLower().Contains(appId)).Select(entry => entry.Id).ToList();
                        if (matchingAppIds.Any())
                            resultSetAppIds.AddRange(matchingAppIds);
                    }
                }
            }

            if (search.Title != null)
            {
                var fragements = search.Title.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                foreach (var fragement in fragements)
                {
                    var searchTerm = fragement.ToLower();
                    var matchingAppIds = query.Where(entry => entry.Title.ToLower().Contains(searchTerm)).Select(entry => entry.Id).ToList();
                    if (matchingAppIds.Any())
                        resultSetAppIds.AddRange(matchingAppIds);
                }
            }

            resultSetAppIds = resultSetAppIds.Distinct().ToList();

            var resultAppEntires = new List<AppArchiveSearchResult>();

            if (resultSetAppIds.Any())
            {
                foreach (var appId in resultSetAppIds)
                {
                    var appEntry = appInspectData.AppEntires.AsQueryable().Where(ae => ae.Id == appId).FirstOrDefault();

                    if (appEntry == null)
                        continue;

                    var result = new AppArchiveSearchResult() { AppId = appId, Title = appEntry.Title };

                    result.ApkFileCount = appInspectData.AppFileRecords.AsQueryable().Where(afr => afr.AppId == appId).Count();

                    result.StoreQueryCount = appInspectData.AppStoreRecords.AsQueryable().Where(afr => afr.AppId == appId).Count();

                    if (result.ApkFileCount > 0)
                    {
                        result.LatestVersionName = appInspectData.AppFileRecords.AsQueryable().Where(afr => afr.AppId == appId).OrderByDescending(afr => afr.VersionCode).Select(afr => afr.VersionName).FirstOrDefault();
                    }

                    resultAppEntires.Add(result);
                }
            }

            return resultAppEntires;
        }


        [HttpPost("apkretrival")]
        public async Task<List<string>> batchApkRetrieval([FromBody] List<string> appIds)
        {
            var cleanAppIds = appIds.Select(a => a.Trim()).Distinct().ToList();

            if (cleanAppIds.Any())
            {
                await appInspectData.CreateAPKRetrievalTasks(cleanAppIds);
                await pushPendingTasksToAllWorkers();
            }

            return cleanAppIds;
        }

        [HttpPost("apkupload")]
        [RequestSizeLimit(2147483648)] // (1024 bytes ^ 3) x 2 = 2 GB -> a comfortable margin for uploading 10 APKs 
        public async Task<ActionResult<Dictionary<string, string>>> UploadAPKFiles([FromForm] IEnumerable<IFormFile> files)
        {
            Dictionary<string, string> uploadResults = new();

            foreach (var file in files)
            {
                string tempBaseFilename = $"{DateTime.UtcNow.ToString("u").Replace(":", String.Empty).Replace("-", String.Empty).Replace(" ", String.Empty)}_{Guid.NewGuid()}.apk";
                string targetFilePath = Path.Combine(serviceSettings.APKUploadPath, tempBaseFilename);

                await using FileStream fs = new(targetFilePath, FileMode.Create);
                await file.CopyToAsync(fs);

                uploadResults.Add(file.FileName, tempBaseFilename);
                await appInspectData.CreateAPKConversionTask(tempBaseFilename);
            }


            if (uploadResults.Keys.Any())
            {
                await pushPendingTasksToAllWorkers();
            }

            return uploadResults;
        }


        private async Task pushPendingTasksToAllWorkers()
        {
            // Same as PushPendingTasksToAllWorkers in AppInspectRPCHub (SignalR methods cannot be invoked directly from this controller)

            var pendingTasks = appInspectData.GetOrderedPendingLists().ToList();

            if (pendingTasks.Any())
            {
                //await rpcHub.Clients.All.SendAsync(AppInspectConstants.RPCClientMethods.PendingTasks, JsonSerializer.Serialize(pendingTasks));
                await rpcHub.Clients.Group(AppInspectConstants.RPCGroups.Workers).SendAsync(AppInspectConstants.RPCClientMethods.PendingTasks, JsonSerializer.Serialize(pendingTasks));
            }
        }


        [HttpPost("inarchive")]
        public InArchiveCheckResults InArchiveCheck([FromBody] List<string> appIds)
        {
            var response = new InArchiveCheckResults();
            var cleanAppIds = appIds.Select(a => a.Trim()).Distinct().ToList();
            response.ParquetFilenames = appInspectData.GetParquetNamesByAppIds(cleanAppIds).ToList();
            response.AppIdsNotInArchive = cleanAppIds.Where(a => !response.ParquetFilenames.Any(pf => pf.StartsWith(a))).ToList();
            return response;
        }

        [HttpGet("mostrecent/{number}")]
        public List<string>? GetMostRecentApps(int number)
        {
            return appInspectData.AppFileRecords.AsQueryable().OrderByDescending(r => r.Created).Select(r => r.AppId).Take(number).ToList().ToList();
        }
    }
}
