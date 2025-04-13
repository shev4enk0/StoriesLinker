using System;
using System.Collections.Generic; // Добавлено для Dictionary
using System.IO;
using Newtonsoft.Json;
using StoriesLinker.Interfaces;

namespace StoriesLinker.ArticyX // Оставляем в пространстве имен ArticyX
{
    public class ArticyXDataParser : IArticyDataParser // Реализуем обновленный интерфейс
    {
        private readonly string _projectPath;
        private readonly JsonLocalizationParser _localizationParser;

        public ArticyXDataParser(string projectPath)
        {
            _projectPath = projectPath;
            _localizationParser = new JsonLocalizationParser(projectPath);
        }

        /// <summary>
        /// Парсит данные Articy:Draft X (JSON файлы)
        /// </summary>
        /// <returns>Объект ArticyExportData с данными и локализацией.</returns>
        public ArticyExportData ParseData()
        {
            try
            {
                if (!File.Exists(_projectPath))
                {
                    Console.WriteLine($"Файл проекта не найден: {_projectPath}");
                    return null;
                }

                string jsonContent = File.ReadAllText(_projectPath);
                var exportData = JsonConvert.DeserializeObject<ArticyExportData>(jsonContent);

                if (exportData == null)
                {
                    Console.WriteLine("Не удалось десериализовать данные проекта");
                    return null;
                }

                // Загружаем локализацию
                var localization = _localizationParser.GetLocalizationTextDictionary();
                exportData.NativeMap = localization;

                return exportData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге данных: {ex.Message}");
                return null;
            }
        }
    }
}
