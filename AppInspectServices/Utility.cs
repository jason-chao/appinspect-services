using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Dynamic;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace AppInspectServices
{
    static public class Utility
    {
        public static dynamic MergeObjects(IEnumerable<object> baseObjects)
        {
            dynamic dynamicObj = new ExpandoObject();
            var dynamicDict = (IDictionary<string, object>)dynamicObj;

            foreach (var baseObject in baseObjects)
            {
                if (baseObject.GetType() == typeof(ExpandoObject))
                {
                    var baseDynamicObj = (IDictionary<string, object>)baseObject;
                    foreach(var key in baseDynamicObj.Keys)
                    {
                        dynamicDict[key] = baseDynamicObj[key];
                    }
                }
                else
                {
                    foreach (var property in baseObject.GetType().GetProperties())
                    {
                        if (property.CanRead)
                        {
                            var value = property.GetValue(baseObject);

                            if (value != null)
                                dynamicDict[property.Name] = value;
                        }
                    }
                }
            }

            return dynamicObj;
        }

        public static dynamic FlattenObject(object baseObject, string[]? ignoreProperties, string[]? distinctListProperties)
        {
            dynamic dynamicObj = new ExpandoObject();
            IDictionary<string, object> dynamicDict = (IDictionary<string, object>)dynamicObj;

            var collectionTypes = new Type[] { typeof(List<string>), typeof(string[]) };

            foreach (var property in baseObject.GetType().GetProperties())
            {
                if (ignoreProperties != null)
                {
                    if (ignoreProperties.Contains(property.Name))
                        continue;
                }

                if (property.CanRead)
                {
                    var value = property.GetValue(baseObject);

                    if (value != null)
                    {
                        if (collectionTypes.Contains(value.GetType()))
                        {
                            var stringList = (List<string>)value;

                            if (distinctListProperties != null)
                            {
                                if (distinctListProperties.Contains(property.Name))
                                {
                                    stringList = stringList.Distinct().ToList();
                                }
                            }

                            dynamicDict[property.Name] = string.Join(Environment.NewLine, stringList);
                        }
                        else
                        {
                            dynamicDict[property.Name] = value;
                        }
                    }
                }

            }

            return dynamicObj;
        }


        public static dynamic ConvertToDynamic(object baseObject)
        {
            dynamic dynamicObj = new ExpandoObject();
            IDictionary<string, object> dynamicDict = (IDictionary<string, object>)dynamicObj;

            foreach (var property in baseObject.GetType().GetProperties())
            {
                if (property.CanRead)
                {
                    var value = property.GetValue(baseObject);

                    if (value != null)
                        dynamicDict[property.Name] = value;
                }
            }

            return dynamicObj;
        }

        public static string EncodeBase64(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string? DecodeBase64(string base64EncodedData)
        {
            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch { }

            return null;
        }

        static public string GetSHA256(string clearText)
        {
            return Convert.ToHexString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(clearText))).ToLower();
        }

        static public string? ConvertJsonToYaml(string jsonString)
        {
            var expConverter = new ExpandoObjectConverter();
            dynamic? deserializedObject = JsonConvert.DeserializeObject<ExpandoObject>(jsonString, expConverter);

            if (deserializedObject != null)
            {
                var serializer = new YamlDotNet.Serialization.Serializer();
                return serializer.Serialize(deserializedObject);
            }

            return null;
        }

        static public void ConfigureCSVWriter(ref CsvWriter csvWriter)
        {
            csvWriter.Context.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] { "u" };
            csvWriter.Context.TypeConverterOptionsCache.GetOptions<DateTime?>().Formats = new[] { "u" };
        }

        static public CsvConfiguration GetCsvWriterConfiguration()
        {
            return new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Encoding = Encoding.UTF8,
                ShouldQuote = (args) => true
            };
        }
    }
}
