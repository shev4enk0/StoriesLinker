using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace StoriesLinker
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

    public class LocalizationParser
    {
        private Dictionary<string, LocalizationEntry> _localizations = new Dictionary<string, LocalizationEntry>();
        private const string JSON_FOLDER_NAME = "JSON_X";
        private const string LOCALIZATION_FILE_PATTERN = "package_*_localization.json";
        private string _defaultPath;
        private string _defaultLocalizationFile;

        public LocalizationParser(string defaultPath = null)
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

        public string GetDefaultLocalizationFilePath()
        {
            return _defaultLocalizationFile;
        }

        public void LoadLocalizationFromFolder(string rootFolderPath = null)
        {
            try
            {
                string pathToUse = rootFolderPath ?? _defaultPath;

                if (string.IsNullOrEmpty(pathToUse))
                {
                    throw new ArgumentException("Путь не указан ни в параметре метода, ни в конструкторе");
                }

                if (!Directory.Exists(pathToUse))
                {
                    throw new DirectoryNotFoundException($"Папка {pathToUse} не найдена");
                }

                string[] localizationFiles = Directory.GetFiles(pathToUse, LOCALIZATION_FILE_PATTERN);

                if (localizationFiles.Length == 0)
                {
                    throw new FileNotFoundException($"Файлы локализации не найдены в папке {pathToUse}");
                }

                foreach (string filePath in localizationFiles)
                {
                    string jsonContent = File.ReadAllText(filePath);
                    ParseLocalizationData(jsonContent);
                    Console.WriteLine($"Загружен файл локализации: {Path.GetFileName(filePath)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке файлов локализации: {ex.Message}");
            }
        }

        public Dictionary<string, LocalizationEntry> ParseLocalizationData(string jsonContent)
        {
            try
            {
                var rawData = JsonConvert.DeserializeObject<Dictionary<string, LocalizationLanguageData>>(jsonContent);
                
                foreach (KeyValuePair<string, LocalizationLanguageData> entry in rawData)
                {
                    if (entry.Value.Ru != null)
                    {
                        string key = entry.Key;
                        _localizations[key] = entry.Value.Ru;
                    }
                }

                return _localizations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге локализации: {ex.Message}");
                return null;
            }
        }

        public LocalizationEntry GetLocalization(string key)
        {
            return _localizations.TryGetValue(key, out LocalizationEntry entry) ? entry : null;
        }

        public void Clear()
        {
            _localizations.Clear();
        }
    }
} 