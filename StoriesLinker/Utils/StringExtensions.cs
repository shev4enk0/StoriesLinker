using System.Text.RegularExpressions;

namespace StoriesLinker
{
    public static class StringExtensions
    {
        public static string EscapeString(this string str)
        {
            return string.IsNullOrEmpty(str) 
                       ? str 
                       : // Заменяем специальные символы на их экранированные версии
                       Regex.Replace(str, @"[\\\""']", match => "\\" + match.Value);
        }
    }
} 