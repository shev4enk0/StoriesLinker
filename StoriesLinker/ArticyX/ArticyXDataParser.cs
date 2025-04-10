using System;
using System.Collections.Generic; // Добавлено для Dictionary
using System.IO;

namespace StoriesLinker.ArticyX // Оставляем в пространстве имен ArticyX
{
    public class ArticyXDataParser : IArticyDataParser // Реализуем обновленный интерфейс
    {
        private readonly JsonObjectsParser _objectsParser;
        private readonly JsonLocalizationParser _localizationParser;
        private readonly string _projectPath;

        // Можно добавить опциональный LinkerBin, если он нужен объектам ArticyX
        // public ArticyXDataParser(string projectPath, LinkerBin linker = null)
        public ArticyXDataParser(string projectPath)
        {
             if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
            {
                throw new ArgumentException("Некорректный путь к проекту", nameof(projectPath));
            }
            _projectPath = projectPath;
            // Передаем projectPath в конструкторы парсеров
            // Если JsonObjectsParser требует LinkerBin, его нужно передать сюда
            _objectsParser = new JsonObjectsParser(projectPath); // Или new JsonObjectsParser(linker, projectPath);
            _localizationParser = new JsonLocalizationParser(projectPath);
        }

        /// <summary>
        /// Парсит данные Articy:Draft X (JSON файлы)
        /// </summary>
        /// <returns>Кортеж с объектом AjFile и словарем локализации.</returns>
        public (AjFile ParsedData, Dictionary<string, string> Localization) ParseData()
        {
            AjFile articyData = null;
            Dictionary<string, string> localizationDict = new Dictionary<string, string>();

            try
            {
                // Парсим основные объекты Articy X
                // Метод ParseArticyX в JsonObjectsParser уже читает нужные файлы
                articyData = _objectsParser.ParseArticyX();

                // Если articyData == null после парсинга, возможно, стоит обработать это как ошибку
                if (articyData == null)
                {
                     Console.WriteLine("Парсинг объектов Articy X вернул null.");
                     // Можно вернуть пустой результат или выбросить исключение
                }

                Console.WriteLine("Успешно распарсены объекты Articy X.");
            }
            catch (FileNotFoundException ex)
            {
                 Console.WriteLine($"Ошибка: Не найдены файлы данных Articy X: {ex.Message}");
                 articyData = null; // Устанавливаем в null при ошибке
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге объектов Articy X: {ex.Message}");
                articyData = null; // Устанавливаем в null при ошибке
            }

            try
            {
                 // Загружаем локализацию из папки JSON_X
                 // Конструктор JsonLocalizationParser уже пытается загрузить дефолтный файл
                 // Если нужно загрузить все файлы из папки:
                 _localizationParser.LoadLocalizationFromFolder();

                // Получаем словарь локализации в нужном формате <string, string>
                localizationDict = _localizationParser.GetLocalizationTextDictionary();

                Console.WriteLine($"Успешно загружена локализация Articy X, записей: {localizationDict.Count}");
            }
             catch (FileNotFoundException ex)
            {
                 Console.WriteLine($"Ошибка: Не найдены файлы локализации Articy X: {ex.Message}");
                 // Продолжаем без локализации, возвращаем пустой словарь
                 localizationDict = new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке локализации Articy X: {ex.Message}");
                 // Продолжаем без локализации, возвращаем пустой словарь
                 localizationDict = new Dictionary<string, string>();
            }

            // Возвращаем результат, даже если articyData == null или локализация пуста
            return (articyData, localizationDict);
        }
    }
}
