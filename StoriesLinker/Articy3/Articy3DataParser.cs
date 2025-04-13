using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OfficeOpenXml;
using StoriesLinker.Interfaces;
using StoriesLinker.Utils;

namespace StoriesLinker.Articy3
{
    public class Articy3DataParser : IArticyDataParser
    {
        private readonly string _projectPath;
        // Удаляем ненужные кэши, т.к. ExcelParser управляет своим
        // private readonly Dictionary<string, Dictionary<int, Dictionary<string, string>>> _savedXmlDicts = new();
        // private readonly Dictionary<string, Dictionary<string, string>> _cachedLocalizationDict = new();
        private readonly Dictionary<string, ArticyExportData> _cachedFlowJson = new();
        private readonly Dictionary<string, Dictionary<string, Model>> _cachedBookEntities = new();
        private readonly Dictionary<string, Dictionary<string, string>> _cachedEntitiesNativeDict = new();
        private readonly Dictionary<string, Dictionary<string, LocalizationEntry>> _cachedLocalizationData = new();
        private readonly Dictionary<string, Dictionary<string, string>> _cachedTranslations = new();
        private readonly StringPool _stringPool = new();
        // private readonly SemaphoreSlim _excelLock = new(1, 1); // Больше не нужен
        private readonly TimeSpan _excelTimeout = TimeSpan.FromSeconds(60); // Увеличим таймаут для ExcelParser

        public Articy3DataParser(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
            {
                throw new ArgumentException("Некорректный путь к проекту", nameof(projectPath));
            }
            _projectPath = projectPath;
            Console.WriteLine("Articy3DataParser: Инициализирован.");
        }

        /// <summary>
        /// Парсит данные Articy:Draft 3 (Flow.json и loc_*.xlsx)
        /// </summary>
        /// <returns>Кортеж с объектом AjFile и словарем локализации.</returns>
        public ArticyExportData ParseData()
        {
            ArticyExportData articyExportData = null; // Инициализируем null

            try
            {
                articyExportData = ParseFlowJsonFileInternal();
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Ошибка: Файл Flow.json не найден по пути: {ex.FileName}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Ошибка парсинга Flow.json: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Непредвиденная ошибка при парсинге Flow.json: {ex.Message} \n{ex.StackTrace}");
            }

            // Если Flow.json не удалось спарсить, нет смысла продолжать
            if (articyExportData == null)
            {
                Console.WriteLine("Не удалось загрузить Flow.json, дальнейший парсинг невозможен.");
                return null; // Возвращаем null, если базовые данные не загружены
            }

            try
            {
                // Получаем словарь локализации через ExcelParser
                articyExportData.NativeMap = GetLocalizationDictionaryInternal();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке словаря локализации Articy 3: {ex.Message} \n{ex.StackTrace}");
                // Оставляем NativeMap null или пустым, если локализация не загрузилась
                articyExportData.NativeMap = articyExportData.NativeMap ?? new Dictionary<string, string>();
            }

            return articyExportData;
        }

        // --- Вспомогательные методы, адаптированные из LinkerBin ---

        /// <summary>
        /// Получает путь к таблицам локализации Articy 3
        /// </summary>
        private string GetLocalizationTablesPathInternal()
        {
            // Предполагаем, что стандартные имена файлов такие же, как в LinkerBin
            string pathEn = Path.Combine(_projectPath, "Raw", "loc_All objects_en.xlsx");
            string pathRu = Path.Combine(_projectPath, "Raw", "loc_All objects_ru.xlsx");

            // Выводим полные пути для отладки
            Console.WriteLine($"Поиск таблиц локализации в путях:");
            Console.WriteLine($"EN: {pathEn}");
            Console.WriteLine($"RU: {pathRu}");

            if (File.Exists(pathRu)) return pathRu; // Приоритет русскому файлу
            if (File.Exists(pathEn)) return pathEn;

            // Возвращаем пустую строку или null, если файлы не найдены, чтобы обозначить проблему
            return null; // Или можно выбросить FileNotFoundException
        }

        /// <summary>
        /// Получает путь к Flow.json Articy 3
        /// </summary>
        private string GetFlowJsonPathInternal()
        {
            // Предполагаем стандартный путь
            return Path.Combine(_projectPath, "Raw", "Flow.json");
        }

        /// <summary>
        /// Получает словарь локализации, используя ExcelParser
        /// </summary>
        private Dictionary<string, string> GetLocalizationDictionaryInternal()
        {
            string path = GetLocalizationTablesPathInternal();
            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("Файл локализации Articy 3 (.xlsx) не найден в папке Raw.");
                return new Dictionary<string, string>();
            }

            Console.WriteLine($"Используем ExcelParser для чтения: {path}");
            try
            {
                // Вызов ExcelParser с нужными параметрами и таймаутом
                // KeyColumnIndex = 1 (первый столбец)
                // ValueColumnIndex = 2 (второй столбец)
                return ExcelParser.ParseExcelToDictionary(
                    path,
                    keyColumnIndex: 1,
                    valueColumnIndex: 2,
                    useCache: true // Включаем кэширование ExcelParser
                                   // timeout передается в асинхронную версию, которую вызывает эта
                );
            }
            catch (Exception ex)
            {
                // Логируем ошибку, если ExcelParser не справился
                Console.WriteLine($"Ошибка при использовании ExcelParser для файла '{path}': {ex.Message}");
                Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
                return new Dictionary<string, string>(); // Возвращаем пустой словарь при ошибке
            }
        }

        /// <summary>
        /// Парсит Flow.json файл (адаптировано из LinkerBin)
        /// </summary>
        private ArticyExportData ParseFlowJsonFileInternal()
        {
            string flowJsonPath = GetFlowJsonPathInternal();
            if (!File.Exists(flowJsonPath))
            {
                // Выбрасываем исключение, если файл не найден
                throw new FileNotFoundException("Файл Flow.json не найден", flowJsonPath);
            }

            using var r = new StreamReader(flowJsonPath);
            string json = r.ReadToEnd();
            // При ошибке десериализации будет выброшено исключение JsonException
            return JsonConvert.DeserializeObject<ArticyExportData>(json);
        }

        private class StringPool
        {
            private readonly HashSet<string> _strings = new();

            public string Intern(string str)
            {
                if (string.IsNullOrEmpty(str)) return str;

                if (_strings.TryGetValue(str, out string existing)) return existing;

                _strings.Add(str);
                return str;
            }
        }

        private class LocalizationEntry
        {
            public string Text { get; set; }
            public string SpeakerDisplayName { get; set; }
            public string Emotion { get; set; }
            public bool IsInternal { get; set; }
        }
    }
}
