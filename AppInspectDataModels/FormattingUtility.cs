using System.Text.RegularExpressions;

namespace AppInspectDataModels
{
    public static class FormattingUtility
    {
        static private Regex? _appIdCheckRegex;

        // with metadata specifier
        // ^([a-z][a-z0-9_]*(\.[a-z0-9_]+)+[0-9a-z_])(((==|>=|>|<=|<)[a-z0-9]+)(\:)([a-z0-9]+))?
        static public Regex AppIdMatchingRegex
        {
            get { if (_appIdCheckRegex == null) _appIdCheckRegex = new Regex(@"^[a-z][a-z0-9_]*(\.[a-z0-9_]+)+[0-9a-z_]$", RegexOptions.Compiled | RegexOptions.IgnoreCase); return _appIdCheckRegex; }
        }

        static public bool IsAppIdValid(string appId)
        {
            return AppIdMatchingRegex.IsMatch(appId);
        }

        static public List<string> GetKeysOfValuesNotContaining(Dictionary<string, string> dictionary, List<string> values)
        {
            List<string> keysWithoutMatchingValues = new List<string>();

            foreach (var key in dictionary.Keys)
            {
                bool matched = false;
                foreach (var value in values)
                {
                    if (dictionary[key].Contains(value))
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                    keysWithoutMatchingValues.Add(key);
            }

            return keysWithoutMatchingValues;
        }

        static public List<string> GetKeysOfValuesContaining(Dictionary<string, string> dictionary, string value)
        {
            List<string> keysWithMatchingValues = new List<string>();

            foreach (var key in dictionary.Keys)
            {
                if (dictionary[key].Contains(value))
                    keysWithMatchingValues.Add(key);
            }

            return keysWithMatchingValues;
        }

        static public Dictionary<string, string> CreateDictionary(List<string> keys, List<string> values)
        {
            var dictionary = new Dictionary<string, string>();

            if (keys.Count() == values.Count())
            {
                for (int i = 0; i < keys.Count(); i++)
                {
                    if (!dictionary.ContainsKey(keys[i]))
                    {
                        dictionary.Add(keys[i], values[i]);
                    }
                }
            }

            return dictionary;
        }

        static public string GetLinesByPathsString(List<string> paths, List<string> lines)
        {
            Dictionary<string, List<string>> linesByClasses = new Dictionary<string, List<string>>();

            if (paths.Count() != lines.Count())
                return string.Empty;

            for (int i = 0; i < paths.Count(); i++)
            {
                if (!linesByClasses.ContainsKey(paths[i]))
                {
                    linesByClasses.Add(paths[i], new List<string> { lines[i] });
                }
                else
                {
                    linesByClasses[paths[i]].Add(lines[i]);
                }
            }

            string displayText = string.Empty;

            foreach (var key in linesByClasses.Keys)
            {
                string pathDisplayText = key + ":" + Environment.NewLine;

                foreach (var line in linesByClasses[key])
                {
                    pathDisplayText += line + Environment.NewLine;
                }

                displayText += pathDisplayText + Environment.NewLine;
            }

            return displayText;
        }
    }
}
