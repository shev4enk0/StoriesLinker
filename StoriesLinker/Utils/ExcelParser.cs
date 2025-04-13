using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;

namespace StoriesLinker.Utils
{
    /// <summary>
    /// Утилитарный класс для парсинга Excel файлов
    /// </summary>
    public static class ExcelParser
    {
        /// <summary>
        /// Парсит Excel файл и возвращает словарь на основе указанных столбцов
        /// </summary>
        /// <param name="filePath">Путь к Excel файлу</param>
        /// <param name="keyColumnIndex">Индекс столбца ключа (начиная с 1)</param>
        /// <param name="valueColumnIndex">Индекс столбца значения (начиная с 1)</param>
        /// <param name="startRow">Начальная строка для парсинга (по умолчанию 1)</param>
        /// <param name="sheetIndex">Индекс листа в Excel файле (по умолчанию 0)</param>
        /// <returns>Словарь ключ-значение</returns>
        public static Dictionary<string, string> ParseExcelToDictionary(
            string filePath,
            int keyColumnIndex = 1,
            int valueColumnIndex = 2,
            int startRow = 1,
            int sheetIndex = 0)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel файл не найден: {filePath}");

            var resultDict = new Dictionary<string, string>();

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                        throw new InvalidOperationException("Excel файл не содержит листов");

                    ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetIndex];

                    // Получаем размеры данных
                    int totalRows = worksheet.Dimension.End.Row;
                    int totalColumns = worksheet.Dimension.End.Column;

                    if (keyColumnIndex > totalColumns || valueColumnIndex > totalColumns)
                        throw new ArgumentException($"Указанные индексы столбцов ({keyColumnIndex}, {valueColumnIndex}) выходят за пределы таблицы (всего столбцов: {totalColumns})");

                    // Логируем информацию о файле
                    Console.WriteLine($"Обработка файла {filePath}:");
                    Console.WriteLine($"- Всего строк: {totalRows}");
                    Console.WriteLine($"- Всего столбцов: {totalColumns}");
                    Console.WriteLine($"- Столбец ключа: {keyColumnIndex}");
                    Console.WriteLine($"- Столбец значения: {valueColumnIndex}");

                    // Проходим по всем строкам и формируем словарь
                    int processedRows = 0;
                    int skippedRows = 0;

                    for (int row = startRow; row <= totalRows; row++)
                    {
                        var keyCell = worksheet.Cells[row, keyColumnIndex];
                        var valueCell = worksheet.Cells[row, valueColumnIndex];

                        string key = keyCell?.Value?.ToString();
                        string value = valueCell?.Value?.ToString();

                        // Пропускаем строки с пустыми ключами или значениями
                        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                        {
                            skippedRows++;
                            continue;
                        }

                        // Проверяем на дубликаты ключей
                        if (!resultDict.ContainsKey(key))
                        {
                            resultDict.Add(key, value);
                            processedRows++;
                        }
                        else
                        {
                            Console.WriteLine($"Обнаружен дублирующийся ключ: {key} в строке {row}");
                        }
                    }

                    Console.WriteLine($"Успешно обработано {processedRows} записей");
                    if (skippedRows > 0)
                        Console.WriteLine($"Пропущено {skippedRows} строк с пустыми ключами или значениями");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке Excel файла: {ex.Message}");
                throw;
            }

            return resultDict;
        }

        /// <summary>
        /// Парсит несколько Excel файлов и объединяет их в один словарь
        /// </summary>
        /// <param name="filePaths">Список путей к Excel файлам</param>
        /// <param name="keyColumnIndex">Индекс столбца ключа (начиная с 1)</param>
        /// <param name="valueColumnIndex">Индекс столбца значения (начиная с 1)</param>
        /// <returns>Объединенный словарь</returns>
        public static Dictionary<string, string> ParseMultipleExcelsToDictionary(
            IEnumerable<string> filePaths,
            int keyColumnIndex = 1,
            int valueColumnIndex = 2)
        {
            var resultDict = new Dictionary<string, string>();

            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Предупреждение: файл не найден - {filePath}");
                    continue;
                }

                try
                {
                    var fileDict = ParseExcelToDictionary(filePath, keyColumnIndex, valueColumnIndex);

                    // Объединяем словари
                    foreach (var pair in fileDict)
                    {
                        if (!resultDict.ContainsKey(pair.Key))
                        {
                            resultDict.Add(pair.Key, pair.Value);
                        }
                        else
                        {
                            Console.WriteLine($"Предупреждение: дублирующийся ключ '{pair.Key}' найден в файле {filePath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке файла {filePath}: {ex.Message}");
                }
            }

            return resultDict;
        }

        /// <summary>
        /// Ищет Excel файлы по заданным путям и паттернам
        /// </summary>
        /// <param name="basePath">Базовый путь для поиска</param>
        /// <param name="searchPatterns">Паттерны поиска (например, "*.xlsx")</param>
        /// <returns>Список найденных файлов</returns>
        public static List<string> FindExcelFiles(string basePath, params string[] searchPatterns)
        {
            var files = new List<string>();

            if (!Directory.Exists(basePath))
            {
                Console.WriteLine($"Папка не найдена: {basePath}");
                return files;
            }

            foreach (var pattern in searchPatterns)
            {
                files.AddRange(Directory.GetFiles(basePath, pattern, SearchOption.TopDirectoryOnly));
            }

            return files;
        }
    }
}