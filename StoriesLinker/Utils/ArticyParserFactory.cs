using System;
using System.IO;
using StoriesLinker.Interfaces;
using StoriesLinker.Articy3;
using StoriesLinker.ArticyX;

namespace StoriesLinker.Utils
{
    /// <summary>
    /// Фабрика для создания подходящего парсера данных Articy на основе содержимого проекта
    /// </summary>
    public static class ArticyParserFactory
    {
        /// <summary>
        /// Создает парсер данных Articy (3 или X) на основе анализа структуры проекта
        /// </summary>
        /// <param name="projectPath">Путь к папке проекта</param>
        /// <returns>Реализация IArticyDataParser для подходящей версии Articy</returns>
        public static IArticyDataParser CreateParser(string projectPath)
        {
            // Проверяем существование пути проекта
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
            {
                throw new ArgumentException("Некорректный путь к проекту", nameof(projectPath));
            }

            bool isArticyX = DetectArticyXFormat(projectPath);

            Console.WriteLine($"Определен формат проекта: {(isArticyX ? "Articy:Draft X" : "Articy:Draft 3")}");

            // Создаем подходящий парсер
            if (isArticyX)
            {
                return new ArticyXDataParser(projectPath);
            }
            else
            {
                return new Articy3DataParser(projectPath);
            }
        }

        /// <summary>
        /// Определяет, является ли проект форматом Articy X
        /// </summary>
        /// <param name="projectPath">Путь к папке проекта</param>
        /// <returns>true, если проект в формате Articy X, иначе false</returns>
        private static bool DetectArticyXFormat(string projectPath)
        {
            // Проверяем характерные для Articy X файлы и папки
            string jsonXFolder = Path.Combine(projectPath, "Raw", "JSON_X");
            if (Directory.Exists(jsonXFolder))
            {
                return true;
            }

            // Если не нашли признаков Articy X, проверяем наличие файлов Articy 3
            string flowJsonPath = Path.Combine(projectPath, "Raw", "Flow.json");
            string locEnPath = Path.Combine(projectPath, "Raw", "loc_All objects_en.xlsx");
            string locRuPath = Path.Combine(projectPath, "Raw", "loc_All objects_ru.xlsx");

            if (File.Exists(flowJsonPath) && (File.Exists(locEnPath) || File.Exists(locRuPath)))
            {
                return false;
            }

            // Если не можем однозначно определить, по умолчанию возвращаем Articy 3
            Console.WriteLine("Внимание: Не удалось однозначно определить формат Articy. По умолчанию используем Articy 3.");
            return false;
        }
    }
}