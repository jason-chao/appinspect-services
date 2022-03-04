using Parquet;
using Parquet.Data;

namespace AppInspectServices
{
    public class ApkParquet
    {
        private string parquetDirectory = string.Empty;
        private string[] parquetFiles = new string[] { };

        public ApkParquet(string parquetDirectory)
        {
            this.parquetDirectory = parquetDirectory;

            if (Directory.Exists(this.parquetDirectory))
            {
                this.parquetFiles = Directory.EnumerateFiles(this.parquetDirectory, "*.parquet").ToArray();

                if (!parquetFiles.Any())
                    throw new Exception("Parquet files do not exist");

            }
            else
            {
                throw new Exception("Directory does not exist");
            }
        }

        public string? GetContentText(string apkSHA256, string? relativePath, string? className)
        {
            if (string.IsNullOrEmpty(relativePath) && string.IsNullOrEmpty(className))
                throw new Exception("Either relative path or class name is required");

            string? classNameEndingPath = null;

            if (className != null)
            {
                classNameEndingPath = className.Replace(".", "/") + ".smali";
            }

            foreach (var file in this.parquetFiles)
            {
                using (Stream fileStream = System.IO.File.OpenRead(file))
                {
                    using (var parquetReader = new ParquetReader(fileStream))
                    {
                        DataField[] dataFields = parquetReader.Schema.GetDataFields();

                        var searchByColumnNames = new string[] { "apk_sha256", "relative_path" };
                        var searchByDataFields = dataFields.Where(df => searchByColumnNames.Contains(df.Name)).ToArray();

                        var textContentField = dataFields.First(df => df.Name == "content_text");

                        for (int i = 0; i < parquetReader.RowGroupCount; i++)
                        {
                            using (ParquetRowGroupReader groupReader = parquetReader.OpenRowGroupReader(i))
                            {
                                var columns = searchByDataFields.Select(groupReader.ReadColumn).ToArray();

                                string[] fileHashes = (string[])columns[0].Data;
                                string[] relativePaths = (string[])columns[1].Data;

                                for (var rowIndex = 0; rowIndex < relativePaths.Length; rowIndex++)
                                {
                                    bool matching = false;

                                    if (fileHashes[rowIndex] != apkSHA256)
                                        continue;

                                    if (!string.IsNullOrEmpty(relativePath))
                                    {
                                        if (relativePaths[rowIndex] == relativePath)
                                            matching = true;
                                    }
                                    else if (!string.IsNullOrEmpty(classNameEndingPath))
                                    {
                                        if (relativePaths[rowIndex].EndsWith(classNameEndingPath))
                                            matching = true;
                                    }

                                    if (matching)
                                    {
                                        var contentColumns = new DataField[] { textContentField }.Select(groupReader.ReadColumn).ToArray();
                                        string[] contents = (string[])contentColumns[0].Data;

                                        return contents[rowIndex];
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
