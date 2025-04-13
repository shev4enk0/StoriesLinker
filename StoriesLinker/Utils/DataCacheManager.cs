using System;
using System.Collections.Generic;
using StoriesLinker.Models;
using System.Threading.Tasks;

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
        /// <param name="duration">Длительность хранения в кэше (по умолчанию 1 час)</param>
        /// <returns>Кэшированные или вновь созданные данные</returns>
        public static T TryGetOrCreate<T>(string key, Func<T> createFunc, TimeSpan? duration = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));

            if (HasCache(key))
            {
                return GetCache<T>(key);
            }

            var data = createFunc();
            SetCache(key, data, duration);
            return data;
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
        /// Получает значение из NativeMap кэшированных данных ArticyExportData
        /// </summary>
        /// <param name="cacheKey">Ключ кэша ArticyExportData</param>
        /// <param name="nativeKey">Ключ в NativeMap</param>
        /// <returns>Значение из NativeMap или null, если не найдено</returns>
        public static string GetNativeMapValue(string cacheKey, string nativeKey)
        {
            var data = GetCache<ArticyExportData>(cacheKey);
            if (data?.NativeMap == null)
                return null;

            return data.NativeMap.TryGetValue(nativeKey, out var value) ? value : null;
        }

        /// <summary>
        /// Устанавливает значение в NativeMap кэшированных данных ArticyExportData
        /// </summary>
        /// <param name="cacheKey">Ключ кэша ArticyExportData</param>
        /// <param name="nativeKey">Ключ в NativeMap</param>
        /// <param name="value">Значение для установки</param>
        public static void SetNativeMapValue(string cacheKey, string nativeKey, string value)
        {
            var data = GetCache<ArticyExportData>(cacheKey);
            if (data?.NativeMap == null)
                return;

            data.NativeMap[nativeKey] = value;
        }

        /// <summary>
        /// Получает или создает кэшированные данные Articy X
        /// </summary>
        /// <param name="projectPath">Путь к проекту</param>
        /// <param name="createFunc">Функция создания данных</param>
        /// <returns>Кэшированные или вновь созданные данные Articy X</returns>
        public static ArticyExportData GetOrCreateArticyXData(string projectPath, Func<ArticyExportData> createFunc)
        {
            string cacheKey = $"articy_x_{projectPath}";
            return TryGetOrCreate(cacheKey, createFunc);
        }

        /// <summary>
        /// Получает или создает кэшированные данные Articy 3
        /// </summary>
        /// <param name="projectPath">Путь к проекту</param>
        /// <param name="createFunc">Функция создания данных</param>
        /// <returns>Кэшированные или вновь созданные данные Articy 3</returns>
        public static ArticyExportData GetOrCreateArticy3Data(string projectPath, Func<ArticyExportData> createFunc)
        {
            string cacheKey = $"articy_3_{projectPath}";
            return TryGetOrCreate(cacheKey, createFunc);
        }

        /// <summary>
        /// Получает или создает кэшированные данные локализации Articy X
        /// </summary>
        /// <param name="projectPath">Путь к проекту</param>
        /// <param name="createFunc">Функция создания данных локализации</param>
        /// <returns>Кэшированные или вновь созданные данные локализации</returns>
        public static Dictionary<string, string> GetOrCreateArticyXLocalization(string projectPath, Func<Dictionary<string, string>> createFunc)
        {
            string cacheKey = $"articy_x_localization_{projectPath}";
            return TryGetOrCreate(cacheKey, createFunc);
        }

        /// <summary>
        /// Получает или создает кэшированные данные локализации Articy 3
        /// </summary>
        /// <param name="projectPath">Путь к проекту</param>
        /// <param name="createFunc">Функция создания данных локализации</param>
        /// <returns>Кэшированные или вновь созданные данные локализации</returns>
        public static Dictionary<string, string> GetOrCreateArticy3Localization(string projectPath, Func<Dictionary<string, string>> createFunc)
        {
            string cacheKey = $"articy_3_localization_{projectPath}";
            return TryGetOrCreate(cacheKey, createFunc);
        }

        /// <summary>
        /// Очищает кэш для конкретного проекта Articy X
        /// </summary>
        /// <param name="projectPath">Путь к проекту</param>
        public static void ClearArticyXCache(string projectPath)
        {
            string dataKey = $"articy_x_{projectPath}";
            string localizationKey = $"articy_x_localization_{projectPath}";

            RemoveCache(dataKey);
            RemoveCache(localizationKey);
        }

        /// <summary>
        /// Очищает кэш для конкретного проекта Articy 3
        /// </summary>
        /// <param name="projectPath">Путь к проекту</param>
        public static void ClearArticy3Cache(string projectPath)
        {
            string dataKey = $"articy_3_{projectPath}";
            string localizationKey = $"articy_3_localization_{projectPath}";

            RemoveCache(dataKey);
            RemoveCache(localizationKey);
        }
    }
}