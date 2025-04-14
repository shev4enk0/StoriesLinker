using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StoriesLinker.Interfaces;

namespace StoriesLinker.ArticyX
{
    public class ArticyXDataParser : IArticyDataParser
    {
        private readonly string _projectPath;
        private readonly JsonObjectsParser _objectsParser;
        private readonly JsonLocalizationParser _localizationParser;

        // Оставляем только нужную константу
        private const string JSON_FOLDER_NAME = "JSON_X";

        public ArticyXDataParser(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(Path.Combine(projectPath, "Raw", JSON_FOLDER_NAME)))
            {
                throw new ArgumentException($"Некорректный путь к проекту Articy X или отсутствует папка Raw/{JSON_FOLDER_NAME}: {projectPath}");
            }
            _projectPath = projectPath;
            _objectsParser = new JsonObjectsParser(_projectPath);
            _localizationParser = new JsonLocalizationParser(_projectPath);
        }

        /// <summary>
        /// Парсит данные Articy:Draft X, используя JsonObjectsParser и JsonLocalizationParser.
        /// </summary>
        /// <returns>Объект ArticyExportData или null в случае ошибки.</returns>
        public ArticyExportData ParseData()
        {
            ArticyExportData exportData = null;
            Dictionary<string, string> localization = null;

            // --- Парсинг основного JSON --- 
            try
            {
                Console.WriteLine("Начинаю парсинг объектов Articy X...");
                exportData = _objectsParser.ParseArticyX();
                if (exportData == null)
                {
                    Console.WriteLine("Ошибка: JsonObjectsParser вернул null.");
                    return null; // Критическая ошибка, не можем продолжать
                }
                Console.WriteLine("Парсинг объектов Articy X завершен.");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Ошибка: Не найдены файлы объектов Articy X: {ex.Message}");
                return null; // Не можем продолжать без основных данных
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка при парсинге объектов Articy X: {ex.Message}\n{ex.StackTrace}");
                return null; // Не можем продолжать при критической ошибке
            }

            // --- Парсинг локализации --- 
            try
            {
                // Используем метод экземпляра для парсинга локализации
                localization = _localizationParser.ParseLocalization();
                if (localization == null) // На всякий случай, хотя метод должен вернуть пустой словарь
                {
                    Console.WriteLine("Парсер локализации вернул null, инициализируем пустым словарем.");
                    localization = new Dictionary<string, string>();
                }
            }
            catch (Exception ex)
            {
                // Эта ошибка не должна возникать, т.к. ParseLocalization обрабатывает свои исключения,
                // но добавим для надежности.
                Console.WriteLine($"Непредвиденная ошибка при вызове парсера локализации: {ex.Message}");
                localization = new Dictionary<string, string>();
            }

            exportData.NativeMap = localization;
            Console.WriteLine($"Загружено {exportData.NativeMap.Count} записей локализации.");

            return exportData;
        }
    }
}
