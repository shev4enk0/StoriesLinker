using System;
using System.Text.RegularExpressions;

namespace StoriesLinker
{
    public static class StringExtensions
    {
        public static string EscapeString(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            // Заменяем специальные символы на их экранированные версии
            return Regex.Replace(str, @"[\\\""']", match => "\\" + match.Value);
        }
    }
} 