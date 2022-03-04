using System.Text.Json.Serialization;

namespace AppInspectDataModels
{
    public class ApkAnalysisRecord
    {
        [JsonPropertyName("app_id")]
        public string AppId { get; set; } = string.Empty;

        [JsonPropertyName("version_name")]
        public string? VersionName { get; set; }

        [JsonPropertyName("version_code")]
        public long? VersionCode { get; set; }

        [JsonPropertyName("apk_sha256")]
        public string? APK_SHA256 { get; set; }

        [JsonPropertyName("paths")]
        public List<string>? Paths { get; set; }

        [JsonPropertyName("inferred_developers")]
        public List<string>? InferredDevelopers { get; set; }

        [JsonPropertyName("classes")]
        public List<string>? Classes { get; set; }

        [JsonPropertyName("permissions")]
        public List<string>? Permissions { get; set; }

        [JsonPropertyName("protection_levels")]
        public List<string>? ProtectionLevels { get; set; }

        [JsonPropertyName("trackers")]
        public List<string>? Trackers { get; set; }

        [JsonPropertyName("tracker_list")]
        public string? TrackerList { get; set; }

        [JsonPropertyName("domains")]
        public List<string>? Domains { get; set; }

        [JsonPropertyName("urls")]
        public List<string>? Urls { get; set; }

        [JsonPropertyName("lines")]
        public List<string>? Lines { get; set; }


        [JsonIgnore]
        public int? FileCount { get { if (Paths != null) return Paths.Count(); else return null; } }

        [JsonIgnore]
        public int? InferredDeveloperCount { get { if (InferredDevelopers != null) return InferredDevelopers.Count(); else return null; } }

        [JsonIgnore]
        public int? TrackerCount { get { if (Trackers != null) return Trackers.Count(); else return null; } }

        [JsonIgnore]
        public int? DomainCount { get { if (Domains != null) return Domains.Count(); else return null; } }

        [JsonIgnore]
        public int? UrlCount { get { if (Urls != null) return Urls.Count(); else return null; } }

        [JsonIgnore]
        private Dictionary<string, string>? _permissionProtectionLevels;

        [JsonIgnore]
        public Dictionary<string, string>? PermissionProtectionLevels
        {
            get
            {
                if (_permissionProtectionLevels == null)
                {
                    if (Permissions != null && ProtectionLevels != null)
                    {
                        if (Permissions.Count() == ProtectionLevels.Count())
                            _permissionProtectionLevels = FormattingUtility.CreateDictionary(Permissions, ProtectionLevels);
                    }
                }

                return _permissionProtectionLevels;
            }
        }

        [JsonIgnore]
        public List<string>? PermissionsNormal
        {
            get
            {
                if (PermissionProtectionLevels != null)
                    return FormattingUtility.GetKeysOfValuesContaining(PermissionProtectionLevels, "normal");
                else
                    return null;
            }
        }

        [JsonIgnore]
        public List<string>? PermissionsDangerous
        {
            get
            {
                if (PermissionProtectionLevels != null)
                    return FormattingUtility.GetKeysOfValuesContaining(PermissionProtectionLevels, "dangerous");
                else
                    return null;
            }
        }

        [JsonIgnore]
        public List<string>? PermissionsSignature
        {
            get
            {
                if (PermissionProtectionLevels != null)
                    return FormattingUtility.GetKeysOfValuesContaining(PermissionProtectionLevels, "signature");
                else
                    return null;
            }
        }

        [JsonIgnore]
        public List<string>? PermissionsOther
        {
            get
            {
                if (PermissionProtectionLevels != null)
                    return FormattingUtility.GetKeysOfValuesNotContaining(PermissionProtectionLevels, new List<string> { "dangerous", "normal", "signature" });
                else
                    return null;
            }
        }

        [JsonIgnore]
        public int? PermissionCount { get { if (Permissions != null) return Permissions.Count(); else return null; } }

        [JsonIgnore]
        public int? PermissionNormalCount { get { if (PermissionsNormal != null) return PermissionsNormal.Count(); else return null; } }

        [JsonIgnore]
        public int? PermissionDangerousCount { get { if (PermissionsDangerous != null) return PermissionsDangerous.Count(); else return null; } }

        [JsonIgnore]
        public int? PermissionSignatureCount { get { if (PermissionsSignature != null) return PermissionsSignature.Count(); else return null; } }

        [JsonIgnore]
        public int? PermissionOtherCount { get { if (PermissionsOther != null) return PermissionsOther.Count(); else return null; } }
    }

    public class GrouppedApkAnalysisRecord
    {
        public string AppId { get; set; } = string.Empty;
        public string? Title { get; set; }
        public List<ApkAnalysisRecord> ApkRecords { get; set; } = new List<ApkAnalysisRecord>();
    }

    /*
    public class ApkAnalysisRecordCSVRow
    {
        public ApkAnalysisRecordCSVRow(ApkAnalysisRecord baseRecord)
        {
            var properties = baseRecord.GetType().GetProperties();

            properties.ToList().ForEach(property =>
            {
                var isPresent = this.GetType().GetProperty(property.Name);
                if (isPresent != null)
                {
                    var value = baseRecord.GetType().GetProperty(property.Name)!.GetValue(baseRecord, null);

                    if (value != null)
                    {
                        var targetProperty = this.GetType().GetProperty(property.Name);

                        if (targetProperty != null)
                        {
                            if (value.GetType() == typeof(List<string>))
                            {
                                var stringList = (List<string>)value;

                                bool valueIsSet = false;

                                if (property.Name == "Classes")
                                {
                                    stringList = stringList.Distinct().ToList();
                                }
                                else if (property.Name == "Lines")
                                {
                                    if (baseRecord.Classes != null)
                                    {
                                        string text = FormattingUtility.GetLinesByPathsString(baseRecord.Classes, stringList);

                                        if (!string.IsNullOrEmpty(text))
                                        {
                                            targetProperty.SetValue(this, text, null);
                                            valueIsSet = true;
                                        }
                                    }
                                }

                                if (!valueIsSet)
                                    targetProperty.SetValue(this, string.Join(Environment.NewLine, stringList), null);
                            }
                            else
                            {
                                targetProperty.SetValue(this, value, null);
                            }
                        }
                    }
                }
            });
        }

        public string TaskId { get; set; } = string.Empty;
        public DateTime? Executed { get; set; }
        public string? QueryArguments { get; set; }

        // a copy of the properties of ApkAnalysisRecord below with the type of list<string> changed to string
        public string AppId { get; set; } = string.Empty;
        public string? VersionName { get; set; }
        public long? VersionCode { get; set; }
        public string? APK_SHA256 { get; set; }
        public int? FileCount { get; set; }
        public string? Paths { get; set; }
        public int? InferredDeveloperCount { get; set; }
        public string? InferredDevelopers { get; set; }
        public string? Classes { get; set; }
        public int? TrackerCount { get; set; }
        public string? Trackers { get; set; }
        public int? DomainCount { get; set; }
        public string? Domains { get; set; }
        public string? Urls { get; set; }
        public int? UrlCount { get; set; }
        public int? PermissionCount { get; set; }
        public string? Permissions { get; set; }
        public int? PermissionNormalCount { get; set; }
        public int? PermissionDangerousCount { get; set; }
        public int? PermissionSignatureCount { get; set; }
        public int? PermissionOtherCount { get; set; }
        public string? PermissionsNormal { get; set; }
        public string? PermissionsDangerous { get; set; }
        public string? PermissionsSignature { get; set; }
        public string? PermissionsOther { get; set; }
    }
    */
}
