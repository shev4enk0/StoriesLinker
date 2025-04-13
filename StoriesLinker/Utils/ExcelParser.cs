using System;
using System.Collections.Concurrent;
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
        // Потокобезопасный кэш для Excel-словарей
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<ExcelParseKey, Dictionary<string, string>>> _cache
            = new ConcurrentDictionary<string, ConcurrentDictionary<ExcelParseKey, Dictionary<string, string>>>();

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

            var parseKey = new ExcelParseKey(keyColumnIndex, valueColumnIndex, startRow, sheetIndex);

            // Проверяем кэш
            if (useCache &&
                _cache.TryGetValue(filePath, out var fileCache) &&
                fileCache.TryGetValue(parseKey, out var cachedResult))
            {
                Console.WriteLine($"Используем кэшированные данные для файла {filePath}");
                return cachedResult;
            }

            // Запускаем асинхронную версию в фоновом потоке и ждем результат,
            // чтобы избежать deadlock в UI-потоке.
            // Используем Task.Run(...).Result как один из способов.
            try
            {
                // Передаем параметры явно в лямбду
                return Task.Run(async () => await ParseExcelToDictionaryAsync(filePath, keyColumnIndex, valueColumnIndex, startRow, sheetIndex, useCache)).Result;
            }
            catch (AggregateException ae)
            {
                // Если внутри Task возникло исключение, оно будет обернуто в AggregateException
                Console.WriteLine($"Ошибка при синхронном вызове асинхронного парсера: {ae.InnerException?.Message}");
                // Перебрасываем внутреннее исключение для сохранения типа
                throw ae.InnerException ?? ae;
            }
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

            var parseKey = new ExcelParseKey(keyColumnIndex, valueColumnIndex, startRow, sheetIndex);

            // Проверяем кэш
            if (useCache &&
                _cache.TryGetValue(filePath, out var fileCache) &&
                fileCache.TryGetValue(parseKey, out var cachedResult))
            {
                Console.WriteLine($"Используем кэшированные данные для файла {filePath}");
                return cachedResult;
            }

            var resultDict = new Dictionary<string, string>();
            var actualTimeout = timeout ?? _defaultTimeout;

            try
            {
                // Используем CancellationTokenSource для управления таймаутом
                using (var cts = new CancellationTokenSource(actualTimeout))
                {
                    await Task.Run(() => // Запускаем в фоновом потоке
                    {
                        try
                        {
                            using (var package = new ExcelPackage(new FileInfo(filePath)))
                            {
                                if (package.Workbook.Worksheets.Count == 0)
                                    throw new InvalidOperationException("Excel файл не содержит листов");

                                // Используем .First() если sheetIndex = 0 (по умолчанию), иначе используем индекс
                                ExcelWorksheet worksheet;
                                if (sheetIndex == 0)
                                {
                                    worksheet = package.Workbook.Worksheets.First(); // Безопаснее для стандартного случая
                                }
                                else if (sheetIndex > 0 && sheetIndex < package.Workbook.Worksheets.Count)
                                {
                                    worksheet = package.Workbook.Worksheets[sheetIndex];
                                }
                                else
                                {
                                    throw new ArgumentException($"Указанный индекс листа ({sheetIndex}) некорректен или выходит за пределы ({package.Workbook.Worksheets.Count} листов).");
                                }

                                int totalRows = worksheet.Dimension.End.Row;
                                int totalColumns = worksheet.Dimension.End.Column;

                                if (keyColumnIndex > totalColumns || valueColumnIndex > totalColumns)
                                    throw new ArgumentException($"Указанные индексы столбцов ({keyColumnIndex}, {valueColumnIndex}) выходят за пределы таблицы (всего столбцов: {totalColumns})");

                                Console.WriteLine($"Обработка файла {filePath}: ({totalRows} строк, {totalColumns} колонок)");

                                int processedRows = 0;
                                int skippedRows = 0;

                                for (int row = startRow; row <= totalRows; row++)
                                {
                                    cts.Token.ThrowIfCancellationRequested(); // Проверка отмены

                                    var keyCell = worksheet.Cells[row, keyColumnIndex];
                                    var valueCell = worksheet.Cells[row, valueColumnIndex];
                                    string key = keyCell?.Value?.ToString()?.Trim();
                                    string value = valueCell?.Value?.ToString()?.Trim();

                                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                                    {
                                        skippedRows++;
                                        continue;
                                    }

                                    // Используем ContainsKey и Add вместо TryAdd для совместимости
                                    if (!resultDict.ContainsKey(key))
                                    {
                                        resultDict.Add(key, value);
                                        processedRows++;
                                        if (processedRows % 1000 == 0) // Лог каждые 1000 строк
                                        {
                                            Console.WriteLine($"... обработано {processedRows} строк в {Path.GetFileName(filePath)}");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Обнаружен дублирующийся ключ: {key} в строке {row}");
                                    }
                                }

                                Console.WriteLine($"Файл {Path.GetFileName(filePath)}: Успешно обработано {processedRows} записей, пропущено {skippedRows}");
                            }
                        }
                        catch (OperationCanceledException) // Обработка таймаута
                        {
                            Console.WriteLine($"Превышено время ожидания ({actualTimeout.TotalSeconds} сек) при чтении Excel файла: {filePath}");
                            throw; // Перебрасываем для внешней обработки
                        }
                    }, cts.Token); // Передаем токен отмены
                }

                // Сохраняем в кэш, если включено и есть результат
                if (useCache && resultDict.Count > 0)
                {
                    var fileCacheDict = _cache.GetOrAdd(filePath, _ => new ConcurrentDictionary<ExcelParseKey, Dictionary<string, string>>());
                    fileCacheDict.TryAdd(parseKey, resultDict);
                }
            }
            catch (OperationCanceledException)
            {
                // Возвращаем пустой словарь при таймауте
                return new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка при обработке Excel файла {filePath}: {ex.Message}");
                Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
                return new Dictionary<string, string>(); // Возвращаем пустой словарь при ошибке
            }
            // finally - блок не нужен, так как SemaphoreSlim удален

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
            else if (_cache.TryRemove(filePath, out _))
            {
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
            // Синхронная версия вызывает асинхронную
            return ParseMultipleExcelsToDictionaryAsync(filePaths, keyColumnIndex, valueColumnIndex, useCache)
                .GetAwaiter().GetResult();
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
            // Используем ConcurrentDictionary для безопасного добавления из разных потоков
            var combinedResult = new ConcurrentDictionary<string, string>();
            var existingFiles = filePaths.Where(File.Exists).ToList();

            if (!existingFiles.Any())
            {
                Console.WriteLine("Предупреждение: не найдено ни одного существующего файла для обработки.");
                return new Dictionary<string, string>(combinedResult);
            }

            // Создаем задачи для парсинга каждого файла
            var tasks = existingFiles.Select(async filePath =>
            {
                try
                {
                    var fileDict = await ParseExcelToDictionaryAsync(filePath, keyColumnIndex, valueColumnIndex, startRow: 1, sheetIndex: 0, useCache: useCache);
                    foreach (var pair in fileDict)
                    {
                        // TryAdd потокобезопасен
                        if (!combinedResult.TryAdd(pair.Key, pair.Value))
                        {
                            Console.WriteLine($"Предупреждение: дублирующийся ключ '{pair.Key}' обнаружен при обработке файла {Path.GetFileName(filePath)} (возможно, из другого файла). Предыдущее значение сохранено.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке файла {filePath} в параллельной задаче: {ex.Message}");
                    // Ошибку обработали, задача не падает
                }
            });

            try
            {
                // Ожидаем завершения всех задач
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                // Эта ошибка маловероятна, так как ошибки ловятся внутри каждой задачи
                Console.WriteLine($"Непредвиденная ошибка при ожидании задач парсинга: {ex.Message}");
            }

            Console.WriteLine($"Завершено объединение данных из {existingFiles.Count} файлов. Итого записей: {combinedResult.Count}");
            // Возвращаем обычный словарь
            return new Dictionary<string, string>(combinedResult);
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