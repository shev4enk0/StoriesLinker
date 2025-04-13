using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace StoriesLinker.ArticyX
{
    public class LocalizationEntry
    {
        public string Text { get; set; }
        public string Context { get; set; }
    }

    public class LocalizationLanguageData
    {
        public LocalizationEntry Ru { get; set; }
    }

    public class JsonLocalizationParser
    {
        private readonly Dictionary<string, string> _localizations = new();
        private const string JSON_FOLDER_NAME = "JSON_X";
        private const string LOCALIZATION_FILE_PATTERN = "package_*_localization.json";
        private readonly string _defaultPath;
        private readonly string _defaultLocalizationFile;

        public JsonLocalizationParser(string defaultPath = null)
        {
            if (string.IsNullOrEmpty(defaultPath)) return;

            string jsonFolder = Path.Combine(defaultPath, "Raw", JSON_FOLDER_NAME);
            _defaultPath = jsonFolder;

            if (!Directory.Exists(jsonFolder)) return;

            string[] files = Directory.GetFiles(jsonFolder, LOCALIZATION_FILE_PATTERN);
            _defaultLocalizationFile = files.FirstOrDefault();
            if (_defaultLocalizationFile == null) return;

            try
            {
                string jsonContent = File.ReadAllText(_defaultLocalizationFile);
                ParseLocalizationData(jsonContent);
                Console.WriteLine($"Автоматически загружен файл локализации: {Path.GetFileName(_defaultLocalizationFile)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при автоматической загрузке файла локализации: {ex.Message}");
            }
        }

        public Dictionary<string, string> GetLocalizationTextDictionary() => _localizations;

        private Dictionary<string, string> ParseLocalizationData(string jsonContent)
        {
            try
            {
                var rawData = JsonConvert.DeserializeObject<Dictionary<string, LocalizationLanguageData>>(jsonContent);

                foreach (KeyValuePair<string, LocalizationLanguageData> entry in rawData)
                {
                    if (entry.Value.Ru == null) continue;

                    string key = entry.Key;
                    _localizations[key] = entry.Value.Ru.Text;
                }

                return _localizations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге локализации: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Парсит файл локализации из JSON
        /// </summary>
        /// <param name="filePath">Путь к файлу локализации</param>
        /// <returns>Словарь локализации</returns>
        public static Dictionary<string, string> ParseLocalization(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Файл локализации не найден: {filePath}");
                    return new Dictionary<string, string>();
                }

                string jsonContent = File.ReadAllText(filePath);
                var localizationData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);

                return localizationData ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге локализации: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }
    }
}