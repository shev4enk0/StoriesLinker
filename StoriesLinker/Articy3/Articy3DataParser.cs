using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OfficeOpenXml; // Необходимо добавить ссылку на EPPlus

namespace StoriesLinker.Articy3 // Помещаем в отдельное пространство имен для Articy 3
{
    public class Articy3DataParser : IArticyDataParser
    {
        private readonly string _projectPath;
        // Кэш для Excel-словарей, аналогично LinkerBin
        private readonly Dictionary<string, Dictionary<int, Dictionary<string, string>>> _savedXmlDicts = new();

        public Articy3DataParser(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
            {
                throw new ArgumentException("Некорректный путь к проекту", nameof(projectPath));
            }
            _projectPath = projectPath;
            // Установка контекста лицензии для EPPlus 5+
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Или LicenseContext.Commercial, если у вас есть лицензия
        }

        /// <summary>
        /// Парсит данные Articy:Draft 3 (Flow.json и loc_*.xlsx)
        /// </summary>
        /// <returns>Кортеж с объектом AjFile и словарем локализации.</returns>
        public (AjFile ParsedData, Dictionary<string, string> Localization) ParseData()
        {
            AjFile ajFile = null;
            Dictionary<string, string> localizationDict = new Dictionary<string, string>();

            try
            {
                localizationDict = GetLocalizationDictionaryInternal();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке словаря локализации Articy 3: {ex.Message}");
                // Продолжаем выполнение, даже если локализация не загрузилась
            }

            try
            {
                ajFile = ParseFlowJsonFileInternal();
            }
            catch (FileNotFoundException ex)
            {
                 Console.WriteLine($"Ошибка: Файл Flow.json не найден по пути: {ex.FileName}");
                 // Возвращаем null для AjFile, если он не найден
                 ajFile = null;
            }
            catch (JsonException ex)
            {
                 Console.WriteLine($"Ошибка парсинга Flow.json: {ex.Message}");
                 // Возвращаем null для AjFile при ошибке парсинга
                 ajFile = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Непредвиденная ошибка при парсинге Flow.json: {ex.Message}");
                ajFile = null; // Возвращаем null для AjFile при других ошибках
            }

            // Возвращаем результат, даже если ajFile равен null
            return (ajFile, localizationDict);
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
        private AjFile ParseFlowJsonFileInternal()
        {
            string flowJsonPath = GetFlowJsonPathInternal();
            if (!File.Exists(flowJsonPath))
            {
                 // Выбрасываем исключение, если файл не найден
                 throw new FileNotFoundException("Файл Flow.json не найден", flowJsonPath);
            }

            using (var r = new StreamReader(flowJsonPath))
            {
                string json = r.ReadToEnd();
                // При ошибке десериализации будет выброшено исключение JsonException
                return JsonConvert.DeserializeObject<AjFile>(json);
            }
        }

        /// <summary>
        /// Преобразует Excel таблицу в словарь ключ-значение (адаптировано из LinkerBin)
        /// </summary>
        private Dictionary<string, string> ConvertExcelToDictionaryInternal(string path, int column = 1)
        {
            // Проверка кэша
             if (_savedXmlDicts.TryGetValue(path, out var columnsDict) && columnsDict.TryGetValue(column, out var cachedDict))
             {
                 return cachedDict;
             }

            var nativeDict = new Dictionary<string, string>();

            try
            {
                using (var xlPackage = new ExcelPackage(new FileInfo(path)))
                {
                    if (xlPackage.Workbook.Worksheets.Count == 0)
                    {
                        Console.WriteLine($"Предупреждение: Рабочая книга Excel по пути '{path}' не содержит листов.");
                        return nativeDict; // Возвращаем пустой словарь
                    }

                    ExcelWorksheet myWorksheet = xlPackage.Workbook.Worksheets.First(); // Берем первый лист
                    int totalRows = myWorksheet.Dimension?.End.Row ?? 0; // Проверка на null для Dimension

                    if (totalRows == 0)
                    {
                         Console.WriteLine($"Предупреждение: Лист в файле '{path}' пуст.");
                         return nativeDict;
                    }


                    for (var rowNum = 1; rowNum <= totalRows; rowNum++) // Обычно данные с 1 или 2 строки
                    {
                        // Ключ в колонке 1 (A), значение в колонке column + 1 (B, C, и т.д.)
                        ExcelRange firstRow = myWorksheet.Cells[rowNum, 1];
                        ExcelRange secondRow = myWorksheet.Cells[rowNum, column + 1];

                        // Используем ?. для безопасного доступа к Value и ?? для значения по умолчанию
                        string firstRowStr = firstRow?.Value?.ToString()?.Trim() ?? string.Empty;
                        string secondRowStr = secondRow?.Value?.ToString()?.Trim() ?? string.Empty;

                        // Пропускаем строки с пустым ключом или значением, если это не заголовок (можно добавить проверку rowNum > 1)
                        if (rowNum > 1 && (string.IsNullOrWhiteSpace(firstRowStr) || string.IsNullOrWhiteSpace(secondRowStr))) continue;
                         // Пропускаем заголовок, если он есть
                         if (rowNum == 1 && (firstRowStr.Equals("ID", StringComparison.OrdinalIgnoreCase))) continue;


                        if (!nativeDict.ContainsKey(firstRowStr))
                        {
                            nativeDict.Add(firstRowStr, secondRowStr);
                        }
                        else
                        {
                            Console.WriteLine($"Обнаружен дублирующийся ключ в файле '{Path.GetFileName(path)}': {firstRowStr}");
                        }
                    }
                }
            }
             catch (FileNotFoundException)
             {
                  Console.WriteLine($"Ошибка: Файл Excel не найден по пути: {path}");
                  return new Dictionary<string, string>(); // Возвращаем пустой словарь
             }
             catch (IOException ex)
             {
                  Console.WriteLine($"Ошибка чтения файла Excel '{path}': {ex.Message}");
                  // Можно выбросить исключение дальше или вернуть пустой словарь
                  return new Dictionary<string, string>();
             }
            catch (Exception ex) // Ловим другие возможные исключения при работе с Excel
            {
                 Console.WriteLine($"Ошибка при обработке Excel файла '{path}': {ex.Message}");
                 return new Dictionary<string, string>(); // Возвращаем пустой словарь
            }


            // Сохранение в кэш
            if (!_savedXmlDicts.ContainsKey(path))
            {
                _savedXmlDicts[path] = new Dictionary<int, Dictionary<string, string>>();
            }
            _savedXmlDicts[path][column] = nativeDict;

            return nativeDict;
        }
    }
}
