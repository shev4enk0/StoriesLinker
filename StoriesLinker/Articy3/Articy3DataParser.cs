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
        // Кэш для Excel-словарей, аналогично LinkerBin
        private readonly Dictionary<string, Dictionary<int, Dictionary<string, string>>> _savedXmlDicts = new();
        private readonly Dictionary<string, Dictionary<string, string>> _cachedLocalizationDict = new();
        private readonly Dictionary<string, ArticyExportData> _cachedFlowJson = new();
        private readonly Dictionary<string, Dictionary<string, Model>> _cachedBookEntities = new();
        private readonly Dictionary<string, Dictionary<string, string>> _cachedEntitiesNativeDict = new();
        private readonly Dictionary<string, Dictionary<string, LocalizationEntry>> _cachedLocalizationData = new();
        private readonly Dictionary<string, Dictionary<string, string>> _cachedTranslations = new();
        private readonly StringPool _stringPool = new();
        private readonly SemaphoreSlim _excelLock = new(1, 1);
        private readonly TimeSpan _excelTimeout = TimeSpan.FromSeconds(30);

        public Articy3DataParser(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
            {
                throw new ArgumentException("Некорректный путь к проекту", nameof(projectPath));
            }
            _projectPath = projectPath;

            // В версии 4.5.2.1 нет необходимости устанавливать LicenseContext
            Console.WriteLine("Articy3DataParser: Используется EPPlus для чтения Excel");
        }

        /// <summary>
        /// Парсит данные Articy:Draft 3 (Flow.json и loc_*.xlsx)
        /// </summary>
        /// <returns>Кортеж с объектом AjFile и словарем локализации.</returns>
        public ArticyExportData ParseData()
        {
            ArticyExportData articyExportData = new();
            Dictionary<string, string> localizationDict = new Dictionary<string, string>();
            
            try
            {
                articyExportData = ParseFlowJsonFileInternal();
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Ошибка: Файл Flow.json не найден по пути: {ex.FileName}");
                // Возвращаем null для AjFile, если он не найден
                articyExportData = null;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Ошибка парсинга Flow.json: {ex.Message}");
                // Возвращаем null для AjFile при ошибке парсинга
                articyExportData = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Непредвиденная ошибка при парсинге Flow.json: {ex.Message}");
                articyExportData = null; // Возвращаем null для AjFile при других ошибках
            }

            try
            {
                articyExportData.NativeMap = GetLocalizationDictionaryInternal();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке словаря локализации Articy 3: {ex.Message}");
                // Продолжаем выполнение, даже если локализация не загрузилась
            }

            // Возвращаем результат, даже если ajFile равен null
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

            if (File.Exists(pathEn)) return pathEn;
            if (File.Exists(pathRu)) return pathRu;

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
        /// Получает словарь локализации из Excel файла (адаптировано из LinkerBin)
        /// </summary>
        private Dictionary<string, string> GetLocalizationDictionaryInternal()
        {
            string path = GetLocalizationTablesPathInternal();
            if (string.IsNullOrEmpty(path)) // Проверяем, найден ли файл
            {
                Console.WriteLine("Файл локализации Articy 3 (.xlsx) не найден в папке Raw.");
                return new Dictionary<string, string>(); // Возвращаем пустой словарь
            }

            // Используем ConvertExcelToDictionaryInternal с путем к файлу
            return ConvertExcelToDictionaryInternal(path);
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

        /// <summary>
        /// Преобразует Excel таблицу в словарь ключ-значение (адаптировано из LinkerBin)
        /// </summary>
        private Dictionary<string, string> ConvertExcelToDictionaryInternal(string path, int column = 1)
        {
            if (_savedXmlDicts.TryGetValue(path, out var columnsDict) &&
                columnsDict.TryGetValue(column, out var cachedDict))
            {
                Console.WriteLine($"Используем кэшированные данные для файла {path}, колонка {column}");
                return cachedDict;
            }

            Dictionary<string, string> nativeDict = new Dictionary<string, string>();

            try
            {
                using (var cts = new CancellationTokenSource(_excelTimeout))
                {
                    var task = Task.Run(() =>
                    {
                        using (var xlPackage = new ExcelPackage(new FileInfo(path)))
                        {
                            if (xlPackage.Workbook.Worksheets.Count == 0)
                            {
                                Console.WriteLine($"Ошибка: В файле {path} нет рабочих листов");
                                throw new InvalidOperationException("The workbook contains no worksheets.");
                            }

                            ExcelWorksheet myWorksheet = xlPackage.Workbook.Worksheets.First();
                            int totalRows = myWorksheet.Dimension.End.Row;
                            int totalColumns = myWorksheet.Dimension.End.Column;

                            Console.WriteLine($"Обработка файла {path}:");
                            Console.WriteLine($"- Всего строк: {totalRows}");
                            Console.WriteLine($"- Всего колонок: {totalColumns}");
                            Console.WriteLine($"- Целевая колонка: {column}");

                            for (var rowNum = 1; rowNum <= totalRows; rowNum++)
                            {
                                ExcelRange firstRow = myWorksheet.Cells[rowNum, 1];
                                ExcelRange secondRow = myWorksheet.Cells[rowNum, column + 1];

                                string firstRowStr = firstRow?.Value != null
                                                    ? firstRow.Value.ToString().Trim()
                                                    : string.Empty;
                                string secondRowStr = secondRow?.Value != null
                                                    ? secondRow.Value.ToString().Trim()
                                                    : string.Empty;

                                if (string.IsNullOrWhiteSpace(firstRowStr) || string.IsNullOrWhiteSpace(secondRowStr))
                                {
                                    if (rowNum == 1)
                                    {
                                        Console.WriteLine($"Предупреждение: Пустые значения в заголовке строки {rowNum}");
                                    }
                                    continue;
                                }

                                if (!nativeDict.ContainsKey(firstRowStr))
                                {
                                    nativeDict.Add(firstRowStr, secondRowStr);
                                }
                                else
                                {
                                    Console.WriteLine($"Предупреждение: Дублирующийся ключ '{firstRowStr}' в строке {rowNum}");
                                }
                            }

                            Console.WriteLine($"Успешно обработано {nativeDict.Count} записей");
                        }
                    }, cts.Token);

                    task.Wait(cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Ошибка: Превышено время ожидания при чтении Excel файла: {path}");
                return new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке Excel файла '{path}': {ex.Message}");
                Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
                return new Dictionary<string, string>();
            }

            if (!_savedXmlDicts.ContainsKey(path))
            {
                _savedXmlDicts[path] = new Dictionary<int, Dictionary<string, string>>();
            }
            _savedXmlDicts[path][column] = nativeDict;

            return nativeDict;
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
