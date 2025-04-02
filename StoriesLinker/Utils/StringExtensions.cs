using System;
using System.Text.RegularExpressions;

namespace StoriesLinker
{
    public static class StringExtensions
    {
        public static string EscapeString(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return Regex.Replace(input, @"[^\w\s-]", "_");
        }
    }
} 