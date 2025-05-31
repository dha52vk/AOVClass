using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AovClass
{
    class LanguageWrapper
    {
        Dictionary<string, string> languageMap;

        public LanguageWrapper(string s)
        {
            languageMap = new Dictionary<string, string>();

            string[] lines = Regex.Split(s, "\\r?\\n|\\r");
            foreach (string line in lines)
            {
                string[] split = line.Split(" = ");
                languageMap[split[0]] = split[1];
            }
        }

        public string? GetValue(string key)
        {
            if (languageMap.ContainsKey(key))
            {
                return languageMap[key];
            }
            else
            {
                return null;
            }
        }
    }
}
