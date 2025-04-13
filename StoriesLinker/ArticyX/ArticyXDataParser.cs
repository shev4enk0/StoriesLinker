using System;
using System.Collections.Generic; // Добавлено для Dictionary
using System.IO;
using StoriesLinker.Interfaces;

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
        public ArticyExportData ParseData()
        {
            ArticyExportData articyData = null;

            try
            {
                articyData = _objectsParser.ParseArticyX();

                if (articyData == null)
                {
                    Console.WriteLine("Парсинг объектов Articy X вернул null.");
                }

                Console.WriteLine("Успешно распарсены объекты Articy X.");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Ошибка: Не найдены файлы данных Articy X: {ex.Message}");
                articyData = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге объектов Articy X: {ex.Message}");
                articyData = null;
            }

            return articyData;
        }
    }
}
