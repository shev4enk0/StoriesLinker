using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace StoriesLinker.ArticyX
{
    // Вспомогательные классы для десериализации JSON локализации Articy X
    // (Оставлены для возможного использования, но текущий ParseLocalization их не использует напрямую)
    public class LocalizationEntryX // Переименовано, чтобы избежать конфликта с внутренним классом Articy3DataParser
    {
        public string Text { get; set; }
        public string Context { get; set; }
    }
    public class LocalizationLanguageDataX // Переименовано
    {
        public LocalizationEntryX Ru { get; set; }
    }

    /// <summary>
    /// Парсер для файлов локализации JSON из Articy X.
    /// </summary>
    public class JsonLocalizationParser
    {
        private readonly string _localizationFilePath; // Храним путь к файлу
        private Dictionary<string, string> _parsedLocalizations; // Кэш результата

        // Константы вынесены для ясности
        private const string JSON_FOLDER_NAME = "JSON_X";
        private const string LOCALIZATION_FILE_PATTERN = "package_*_localization.json";

        /// <summary>
        /// Создает экземпляр парсера для конкретного проекта Articy X.
        /// Находит путь к файлу локализации при создании.
        /// </summary>
        /// <param name="projectPath">Путь к корневой папке проекта.</param>
        public JsonLocalizationParser(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath), "Путь к проекту не может быть пустым.");
            }

            string jsonFolder = Path.Combine(projectPath, "Raw", JSON_FOLDER_NAME);
            if (!Directory.Exists(jsonFolder))
            {
                // Можно просто установить путь в null или выбросить исключение
                Console.WriteLine($"Предупреждение: Папка {jsonFolder} не найдена.");
                _localizationFilePath = null;
                return;
                // throw new DirectoryNotFoundException($"Папка {jsonFolder} не найдена.");
            }

            try
            {
                // Ищем файл при создании экземпляра
                _localizationFilePath = Directory.GetFiles(jsonFolder, LOCALIZATION_FILE_PATTERN).FirstOrDefault();
                if (string.IsNullOrEmpty(_localizationFilePath))
                {
                    Console.WriteLine($"Предупреждение: Файл локализации по паттерну '{LOCALIZATION_FILE_PATTERN}' не найден в {jsonFolder}.");
                }
                else
                {
                    Console.WriteLine($"Найден файл локализации: {Path.GetFileName(_localizationFilePath)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске файла локализации в {jsonFolder}: {ex.Message}");
                _localizationFilePath = null; // Устанавливаем в null при ошибке
            }
        }

        /// <summary>
        /// Парсит найденный файл локализации.
        /// Использует внутренний кэш для предотвращения повторного парсинга.
        /// </summary>
        /// <returns>Словарь локализации (ключ-текст) или пустой словарь, если файл не найден или произошла ошибка.</returns>
        public Dictionary<string, string> ParseLocalization()
        {
            // Если результат уже кэширован, возвращаем его
            if (_parsedLocalizations != null)
            {
                Console.WriteLine("Используем кэшированную локализацию.");
                return _parsedLocalizations;
            }

            // Если путь к файлу не был найден при инициализации
            if (string.IsNullOrEmpty(_localizationFilePath))
            {
                Console.WriteLine("Парсинг невозможен: путь к файлу локализации не определен.");
                _parsedLocalizations = new Dictionary<string, string>(); // Кэшируем пустой результат
                return _parsedLocalizations;
            }

            Console.WriteLine($"Начинаю парсинг файла локализации: {Path.GetFileName(_localizationFilePath)}");
            try
            {
                if (!File.Exists(_localizationFilePath))
                {
                    Console.WriteLine($"Файл локализации не найден по пути: {_localizationFilePath}");
                    _parsedLocalizations = new Dictionary<string, string>();
                    return _parsedLocalizations;
                }

                string jsonContent = File.ReadAllText(_localizationFilePath);

                // Используем оригинальный подход с DeserializeObject<Dictionary<string, LocalizationLanguageData>>
                // если ваш JSON имеет такую структуру
                var rawData = JsonConvert.DeserializeObject<Dictionary<string, LocalizationLanguageDataX>>(jsonContent);
                var resultDict = new Dictionary<string, string>();

                if (rawData != null)
                {
                    foreach (var entry in rawData)
                    {
                        // Проверяем наличие русского текста
                        if (entry.Value?.Ru?.Text != null)
                        {
                            resultDict[entry.Key] = entry.Value.Ru.Text;
                        }
                    }
                    Console.WriteLine($"Успешно спарсено {resultDict.Count} записей локализации.");
                }
                else
                {
                    Console.WriteLine("Не удалось десериализовать данные локализации или файл пуст.");
                }

                _parsedLocalizations = resultDict; // Кэшируем результат
                return _parsedLocalizations;

                /* // Альтернативный вариант, если JSON имеет вид { "key": "value" }
                var localizationData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                _parsedLocalizations = localizationData ?? new Dictionary<string, string>();
                Console.WriteLine($"Успешно спарсено {_parsedLocalizations.Count} записей локализации.");
                return _parsedLocalizations;
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге локализации из файла '{_localizationFilePath}': {ex.Message}");
                _parsedLocalizations = new Dictionary<string, string>(); // Кэшируем пустой результат при ошибке
                return _parsedLocalizations;
            }
        }

        // Статические методы FindDefaultLocalizationFile и ParseLocalization(filePath) больше не нужны
        // public static string FindDefaultLocalizationFile(string projectPath) { ... }
        // public static Dictionary<string, string> ParseLocalization(string filePath) { ... }
    }
}