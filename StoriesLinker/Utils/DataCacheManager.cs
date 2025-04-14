using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StoriesLinker.ArticyX;
using StoriesLinker.Interfaces;
// using StoriesLinker.Models;

namespace StoriesLinker.Utils
{
    /// <summary>
    /// Менеджер кэширования данных для оптимизации производительности
    /// </summary>
    public static class DataCacheManager
    {
        private static Dictionary<string, object> _cache = new Dictionary<string, object>();
        private static Dictionary<string, DateTime> _cacheTimestamps = new Dictionary<string, DateTime>();
        private static TimeSpan _defaultCacheDuration = TimeSpan.FromHours(1);

        /// <summary>
        /// Определяет тип проекта (Articy X или Articy 3)
        /// </summary>
        public static bool IsArticyX { get; private set; }

        /// <summary>
        /// Добавляет или обновляет данные в кэше
        /// </summary>
        /// <param name="key">Ключ кэша</param>
        /// <param name="data">Данные для кэширования</param>
        /// <param name="duration">Длительность хранения в кэше (по умолчанию 1 час)</param>
        public static void SetCache(string key, object data, TimeSpan? duration = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            _cache[key] = data;
            _cacheTimestamps[key] = DateTime.Now.Add(duration ?? _defaultCacheDuration);
        }

        /// <summary>
        /// Получает данные из кэша
        /// </summary>
        /// <typeparam name="T">Тип данных</typeparam>
        /// <param name="key">Ключ кэша</param>
        /// <returns>Кэшированные данные или null, если данные отсутствуют или устарели</returns>
        public static T GetCache<T>(string key)
        {
            if (string.IsNullOrEmpty(key) || !_cache.ContainsKey(key))
                return default;

            if (_cacheTimestamps[key] < DateTime.Now)
            {
                RemoveCache(key);
                return default;
            }

            return (T)_cache[key];
        }

        /// <summary>
        /// Удаляет данные из кэша
        /// </summary>
        /// <param name="key">Ключ кэша</param>
        public static void RemoveCache(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            _cache.Remove(key);
            _cacheTimestamps.Remove(key);
        }

        /// <summary>
        /// Очищает весь кэш
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
            _cacheTimestamps.Clear();
        }

        /// <summary>
        /// Проверяет наличие данных в кэше
        /// </summary>
        /// <param name="key">Ключ кэша</param>
        /// <returns>True, если данные есть и они актуальны</returns>
        public static bool HasCache(string key)
        {
            if (string.IsNullOrEmpty(key) || !_cache.ContainsKey(key))
                return false;

            return _cacheTimestamps[key] >= DateTime.Now;
        }

        /// <summary>
        /// Пытается получить данные из кэша, если их нет - создает и кэширует
        /// </summary>
        /// <typeparam name="T">Тип данных</typeparam>
        /// <param name="key">Ключ кэша</param>
        /// <param name="createFunc">Функция создания данных, если их нет в кэше</param>
        /// <param name="data">Полученные или созданные данные</param>
        /// <param name="isArticyX">True если это Articy X, False если Articy 3</param>
        /// <param name="duration">Длительность хранения в кэше (по умолчанию 1 час)</param>
        /// <returns>True если данные были получены или созданы успешно</returns>
        public static bool TryGetOrCreate<T>(string key, Func<T> createFunc, out T data, out bool isArticyX, TimeSpan? duration = null)
        {
            data = default;
            isArticyX = false;

            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));

            if (HasCache(key))
            {
                data = GetCache<T>(key);
                isArticyX = IsArticyX;
                return true;
            }

            try
            {
                data = createFunc();
                if (data != null)
                {
                    SetCache(key, data, duration);
                    isArticyX = IsArticyX;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании данных: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Асинхронная версия TryGetOrCreate
        /// </summary>
        public static async Task<T> TryGetOrCreateAsync<T>(string key, Func<Task<T>> createFunc, TimeSpan? duration = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));

            if (HasCache(key))
            {
                return GetCache<T>(key);
            }

            var data = await createFunc();
            SetCache(key, data, duration);
            return data;
        }

        /// <summary>
        /// Получает данные Articy, используя кэширование и парсеры
        /// </summary>
        /// <param name="projectPath">Путь к проекту</param>
        /// <param name="data">Полученные данные Articy</param>
        /// <param name="isArticyX">True если это Articy X, False если Articy 3</param>
        /// <returns>True если данные были получены успешно</returns>
        public static bool TryGetArticyData(string projectPath, out ArticyExportData data, out bool isArticyX)
        {
            data = null;
            isArticyX = false;

            // Создаем парсер для определения типа проекта
            IArticyDataParser parser = ArticyParserFactory.CreateParser(projectPath);
            if (parser == null)
                return false;

            isArticyX = parser is ArticyXDataParser;
            IsArticyX = isArticyX;
            string cacheKey = $"articy_{projectPath}";

            return TryGetOrCreate(cacheKey, () => parser.ParseData(), out data, out _);
        }

        /// <summary>
        /// Очищает кэш для конкретного проекта
        /// </summary>
        /// <param name="projectPath">Путь к проекту</param>
        public static void ClearProjectCache(string projectPath)
        {
            string cacheKey = $"articy_{projectPath}";
            RemoveCache(cacheKey);
        }

        /// <summary>
        /// Пытается получить данные LinkerBin из кэша, если их нет - создает и кэширует
        /// </summary>
        /// <param name="projectPath">Путь к проекту</param>
        /// <param name="data">Полученные данные LinkerBin</param>
        /// <returns>True если данные были получены успешно</returns>
        public static bool TryGetLinkerBin(string projectPath, out LinkerBin data)
        {
            if (string.IsNullOrEmpty(projectPath))
                throw new ArgumentNullException(nameof(projectPath));

            string cacheKey = $"linkerbin_{projectPath}";

            // Передаем лямбда-выражение для создания LinkerBin только при необходимости
            return TryGetOrCreate(cacheKey, () => new LinkerBin(projectPath), out data, out _); // Игнорируем isArticyX
        }
    }
}