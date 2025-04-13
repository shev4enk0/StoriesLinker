using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace StoriesLinker.Utils
{
    /// <summary>
    /// Утилитарный класс для парсинга Excel файлов
    /// </summary>
    public static class ExcelParser
    {
        // Кэш для Excel-словарей по пути файла, колонке и листу
        private static readonly Dictionary<string, Dictionary<ExcelParseKey, Dictionary<string, string>>> _cache
            = new Dictionary<string, Dictionary<ExcelParseKey, Dictionary<string, string>>>();

        // Семафор для предотвращения одновременных операций чтения
        private static readonly SemaphoreSlim _excelLock = new SemaphoreSlim(1, 1);

        // Таймаут по умолчанию для операций Excel (30 секунд)
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Ключ для кэширования результатов Excel
        /// </summary>
        private struct ExcelParseKey : IEquatable<ExcelParseKey>
        {
            public int KeyColumn { get; }
            public int ValueColumn { get; }
            public int StartRow { get; }
            public int SheetIndex { get; }

            public ExcelParseKey(int keyColumn, int valueColumn, int startRow, int sheetIndex)
            {
                KeyColumn = keyColumn;
                ValueColumn = valueColumn;
                StartRow = startRow;
                SheetIndex = sheetIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is ExcelParseKey key && Equals(key);
            }

            public bool Equals(ExcelParseKey other)
            {
                return KeyColumn == other.KeyColumn &&
                       ValueColumn == other.ValueColumn &&
                       StartRow == other.StartRow &&
                       SheetIndex == other.SheetIndex;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + KeyColumn.GetHashCode();
                    hash = hash * 23 + ValueColumn.GetHashCode();
                    hash = hash * 23 + StartRow.GetHashCode();
                    hash = hash * 23 + SheetIndex.GetHashCode();
                    return hash;
                }
            }
        }

        /// <summary>
        /// Парсит Excel файл и возвращает словарь на основе указанных столбцов
        /// </summary>
        /// <param name="filePath">Путь к Excel файлу</param>
        /// <param name="keyColumnIndex">Индекс столбца ключа (начиная с 1)</param>
        /// <param name="valueColumnIndex">Индекс столбца значения (начиная с 1)</param>
        /// <param name="startRow">Начальная строка для парсинга (по умолчанию 1)</param>
        /// <param name="sheetIndex">Индекс листа в Excel файле (по умолчанию 0)</param>
        /// <param name="useCache">Использовать кэширование (по умолчанию true)</param>
        /// <returns>Словарь ключ-значение</returns>
        public static Dictionary<string, string> ParseExcelToDictionary(
            string filePath,
            int keyColumnIndex = 1,
            int valueColumnIndex = 2,
            int startRow = 1,
            int sheetIndex = 0,
            bool useCache = true)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel файл не найден: {filePath}");

            // Проверяем кэш, если включено кэширование
            if (useCache)
            {
                var key = new ExcelParseKey(keyColumnIndex, valueColumnIndex, startRow, sheetIndex);
                if (_cache.TryGetValue(filePath, out var fileCache) && fileCache.TryGetValue(key, out var cachedResult))
                {
                    Console.WriteLine($"Используем кэшированные данные для файла {filePath}");
                    return cachedResult;
                }
            }

            // Синхронная версия метода использует асинхронную с ожиданием результата
            return ParseExcelToDictionaryAsync(filePath, keyColumnIndex, valueColumnIndex, startRow, sheetIndex, useCache)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Асинхронно парсит Excel файл и возвращает словарь на основе указанных столбцов
        /// </summary>
        /// <param name="filePath">Путь к Excel файлу</param>
        /// <param name="keyColumnIndex">Индекс столбца ключа (начиная с 1)</param>
        /// <param name="valueColumnIndex">Индекс столбца значения (начиная с 1)</param>
        /// <param name="startRow">Начальная строка для парсинга (по умолчанию 1)</param>
        /// <param name="sheetIndex">Индекс листа в Excel файле (по умолчанию 0)</param>
        /// <param name="useCache">Использовать кэширование (по умолчанию true)</param>
        /// <param name="timeout">Таймаут операции (по умолчанию 30 секунд)</param>
        /// <returns>Словарь ключ-значение</returns>
        public static async Task<Dictionary<string, string>> ParseExcelToDictionaryAsync(
            string filePath,
            int keyColumnIndex = 1,
            int valueColumnIndex = 2,
            int startRow = 1,
            int sheetIndex = 0,
            bool useCache = true,
            TimeSpan? timeout = null)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel файл не найден: {filePath}");

            // Проверяем кэш, если включено кэширование
            if (useCache)
            {
                var key = new ExcelParseKey(keyColumnIndex, valueColumnIndex, startRow, sheetIndex);
                if (_cache.TryGetValue(filePath, out var fileCache) && fileCache.TryGetValue(key, out var cachedResult))
                {
                    Console.WriteLine($"Используем кэшированные данные для файла {filePath}");
                    return cachedResult;
                }
            }

            var resultDict = new Dictionary<string, string>();
            var actualTimeout = timeout ?? _defaultTimeout;

            // Блокируем доступ к Excel для предотвращения одновременных операций
            await _excelLock.WaitAsync();
            try
            {
                using (var cts = new CancellationTokenSource(actualTimeout))
                {
                    await Task.Run(async () =>
                    {
                        try
                        {
                            using (var package = new ExcelPackage(new FileInfo(filePath)))
                            {
                                if (package.Workbook.Worksheets.Count == 0)
                                    throw new InvalidOperationException("Excel файл не содержит листов");

                                if (sheetIndex >= package.Workbook.Worksheets.Count)
                                    throw new ArgumentException($"Индекс листа {sheetIndex} выходит за пределы количества листов {package.Workbook.Worksheets.Count}");

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
                                    // Проверяем токен отмены
                                    cts.Token.ThrowIfCancellationRequested();

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
                        catch (OperationCanceledException)
                        {
                            Console.WriteLine($"Превышено время ожидания ({actualTimeout.TotalSeconds} сек) при чтении Excel файла: {filePath}");
                            throw;
                        }
                    }, cts.Token);
                }

                // Сохраняем в кэш, если включено кэширование
                if (useCache && resultDict.Count > 0)
                {
                    var key = new ExcelParseKey(keyColumnIndex, valueColumnIndex, startRow, sheetIndex);
                    if (!_cache.ContainsKey(filePath))
                    {
                        _cache[filePath] = new Dictionary<ExcelParseKey, Dictionary<string, string>>();
                    }
                    _cache[filePath][key] = resultDict;
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Ошибка: Превышено время ожидания при чтении Excel файла: {filePath}");
                return new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке Excel файла: {ex.Message}");
                Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
                return new Dictionary<string, string>();
            }
            finally
            {
                _excelLock.Release();
            }

            return resultDict;
        }

        /// <summary>
        /// Очищает кэш Excel-словарей
        /// </summary>
        /// <param name="filePath">Путь к файлу для очистки (если null, очищает весь кэш)</param>
        public static void ClearCache(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                _cache.Clear();
                Console.WriteLine("Весь кэш Excel-словарей очищен");
            }
            else if (_cache.ContainsKey(filePath))
            {
                _cache.Remove(filePath);
                Console.WriteLine($"Кэш для файла {filePath} очищен");
            }
        }

        /// <summary>
        /// Парсит несколько Excel файлов и объединяет их в один словарь
        /// </summary>
        /// <param name="filePaths">Список путей к Excel файлам</param>
        /// <param name="keyColumnIndex">Индекс столбца ключа (начиная с 1)</param>
        /// <param name="valueColumnIndex">Индекс столбца значения (начиная с 1)</param>
        /// <param name="useCache">Использовать кэширование</param>
        /// <returns>Объединенный словарь</returns>
        public static Dictionary<string, string> ParseMultipleExcelsToDictionary(
            IEnumerable<string> filePaths,
            int keyColumnIndex = 1,
            int valueColumnIndex = 2,
            bool useCache = true)
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
                    var fileDict = ParseExcelToDictionary(
                        filePath,
                        keyColumnIndex,
                        valueColumnIndex,
                        useCache: useCache);

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

        // Вспомогательный класс для результатов обработки файла
        private class FileResult
        {
            public string FilePath { get; set; }
            public Dictionary<string, string> Result { get; set; }
            public bool Success { get; set; }
        }

        /// <summary>
        /// Асинхронно парсит несколько Excel файлов и объединяет их в один словарь
        /// </summary>
        /// <param name="filePaths">Список путей к Excel файлам</param>
        /// <param name="keyColumnIndex">Индекс столбца ключа (начиная с 1)</param>
        /// <param name="valueColumnIndex">Индекс столбца значения (начиная с 1)</param>
        /// <param name="useCache">Использовать кэширование</param>
        /// <returns>Объединенный словарь</returns>
        public static async Task<Dictionary<string, string>> ParseMultipleExcelsToDictionaryAsync(
            IEnumerable<string> filePaths,
            int keyColumnIndex = 1,
            int valueColumnIndex = 2,
            bool useCache = true)
        {
            var resultDict = new Dictionary<string, string>();
            var existingFiles = filePaths.Where(File.Exists).ToList();

            if (existingFiles.Count == 0)
            {
                Console.WriteLine("Предупреждение: не найдено ни одного существующего файла");
                return resultDict;
            }

            // Создаем задачи для всех файлов
            List<Task<FileResult>> fileTasks = new List<Task<FileResult>>();

            foreach (string filePath in existingFiles)
            {
                Task<FileResult> task = ParseExcelToDictionaryAsync(filePath, keyColumnIndex, valueColumnIndex, useCache: useCache)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            Console.WriteLine($"Ошибка при обработке файла {filePath}: {t.Exception?.InnerException?.Message}");
                            return new FileResult
                            {
                                FilePath = filePath,
                                Result = new Dictionary<string, string>(),
                                Success = false
                            };
                        }
                        return new FileResult
                        {
                            FilePath = filePath,
                            Result = t.Result,
                            Success = true
                        };
                    });
                fileTasks.Add(task);
            }

            // Ждем завершения всех задач
            FileResult[] results = await Task.WhenAll(fileTasks);

            // Объединяем результаты успешных задач
            foreach (FileResult result in results)
            {
                if (!result.Success) continue;

                foreach (var pair in result.Result)
                {
                    if (!resultDict.ContainsKey(pair.Key))
                    {
                        resultDict.Add(pair.Key, pair.Value);
                    }
                    else
                    {
                        Console.WriteLine($"Предупреждение: дублирующийся ключ '{pair.Key}' найден в файле {result.FilePath}");
                    }
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