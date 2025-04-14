using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // Для JObject
using System.Linq;

namespace StoriesLinker.ArticyX
{
    // --- Вспомогательные классы для десериализации Articy X JSON --- 
    public class RootObject
    {
        public List<ArticyXObject> Objects { get; set; }
    }

    public class ArticyXObject
    {
        public string Type { get; set; }
        // Используем JObject для гибкости, т.к. набор свойств разный
        public JObject Properties { get; set; }
    }
    // Класс ArticyXProperties больше не нужен в явном виде, т.к. используем JObject
    // public class ArticyXProperties { ... }

    // --- Основной класс парсера --- 
    public class JsonObjectsParser
    {
        private readonly LinkerBin _linker;
        private const string JSON_FOLDER_NAME = "JSON_X";
        private const string FILE_PATTERN_NAME = "package_*_objects.json";
        private const string GLOBAL_VARS_FILE_NAME = "global_variables.json";
        private string _defaultPath;
        private readonly string _flowJsonPath;
        private readonly string _globalVarsJsonPath;

        /// <summary>
        /// Создает парсер Articy X с указанным путем к проекту
        /// </summary>
        /// <param name="projectPath">Путь к корневой папке проекта</param>
        public JsonObjectsParser(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                Console.WriteLine("Предупреждение: Путь к проекту не указан, JsonObjectsParser не инициализирован.");
                return;
            }

            string jsonFolder = Path.Combine(projectPath, "Raw", JSON_FOLDER_NAME);
            _defaultPath = jsonFolder;

            if (!Directory.Exists(jsonFolder))
            {
                Console.WriteLine($"Предупреждение: Папка {jsonFolder} не найдена, JsonObjectsParser не инициализирован.");
                return;
            }

            // Ищем файл объектов по паттерну
            try
            {
                string[] objectFiles = Directory.GetFiles(jsonFolder, FILE_PATTERN_NAME);
                _flowJsonPath = objectFiles.FirstOrDefault(); // Берем первый найденный файл
                if (string.IsNullOrEmpty(_flowJsonPath))
                {
                    Console.WriteLine($"Предупреждение: Файл объектов по паттерну '{FILE_PATTERN_NAME}' не найден в {jsonFolder}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске файла объектов по паттерну '{FILE_PATTERN_NAME}' в {jsonFolder}: {ex.Message}");
                _flowJsonPath = null;
            }

            // Формируем путь к файлу глобальных переменных
            _globalVarsJsonPath = Path.Combine(jsonFolder, GLOBAL_VARS_FILE_NAME);

            // Проверяем наличие обоих файлов (если файл объектов был найден)
            bool flowFileExists = !string.IsNullOrEmpty(_flowJsonPath) && File.Exists(_flowJsonPath);
            bool globalVarsFileExists = File.Exists(_globalVarsJsonPath);

            if (!flowFileExists || !globalVarsFileExists)
            {
                Console.WriteLine("Предупреждение: Один или оба необходимых файла Articy X не найдены:");
                Console.WriteLine($"- Файл объектов (паттерн {FILE_PATTERN_NAME}): {(flowFileExists ? Path.GetFileName(_flowJsonPath) : "НЕ НАЙДЕН")}");
                Console.WriteLine($"- Файл глоб. переменных ({GLOBAL_VARS_FILE_NAME}): {(globalVarsFileExists ? GLOBAL_VARS_FILE_NAME : "НЕ НАЙДЕН")}");
                // Можно не прерывать инициализацию, а позволить методу ParseArticyX обработать отсутствие файлов
                return;
            }

            try
            {
                Console.WriteLine($"Найдены файлы Articy X в папке: {jsonFolder}");
                Console.WriteLine($"- Flow файл: {Path.GetFileName(_flowJsonPath)}");
                Console.WriteLine($"- Global Variables файл: {Path.GetFileName(_globalVarsJsonPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при логировании найденных файлов Articy X: {ex.Message}");
            }
        }

        /// <summary>
        /// Парсит файлы Articy X и преобразует их в формат AJ
        /// </summary>
        /// <param name="customFlowJsonPath">Опциональный путь к JSON с нодами и связями</param>
        /// <param name="customGlobalVarsJsonPath">Опциональный путь к JSON с глобальными переменными</param>
        /// <returns>Объект AJFile, совместимый с существующей структурой</returns>
        public ArticyExportData ParseArticyX(string customFlowJsonPath = null, string customGlobalVarsJsonPath = null)
        {
            string flowJsonPath = customFlowJsonPath ?? _flowJsonPath;
            string globalVarsJsonPath = customGlobalVarsJsonPath ?? _globalVarsJsonPath;

            if (string.IsNullOrEmpty(flowJsonPath) || string.IsNullOrEmpty(globalVarsJsonPath))
            {
                // Добавим проверку, найдены ли пути в конструкторе
                if (string.IsNullOrEmpty(_flowJsonPath) || string.IsNullOrEmpty(_globalVarsJsonPath))
                    throw new InvalidOperationException("Не удалось найти пути к файлам Articy X при инициализации парсера.");
                else
                    throw new ArgumentException("Пути к файлам не указаны в параметрах метода, а пути по умолчанию не найдены.");
            }

            if (!File.Exists(flowJsonPath))
            {
                throw new FileNotFoundException($"Файл объектов не найден: {flowJsonPath}");
            }
            if (!File.Exists(globalVarsJsonPath))
            {
                throw new FileNotFoundException($"Файл глобальных переменных не найден: {globalVarsJsonPath}");
            }

            // --- Парсинг --- 
            ArticyExportData parsedData = null;
            ArticyExportData globalVarsData = null;
            try
            {
                Console.WriteLine($"Читаем файл объектов: {Path.GetFileName(flowJsonPath)}");
                string flowJsonContent = File.ReadAllText(flowJsonPath);
                parsedData = ParseArticyXObjects(flowJsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка при парсинге файла объектов '{Path.GetFileName(flowJsonPath)}': {ex.Message}\n{ex.StackTrace}");
                throw; // Перебрасываем исключение, т.к. без объектов продолжать нельзя
            }

            try
            {
                Console.WriteLine($"Читаем файл глоб. переменных: {Path.GetFileName(globalVarsJsonPath)}");
                string globalVarsJsonContent = File.ReadAllText(globalVarsJsonPath);
                globalVarsData = ParseArticyXGlobalVariables(globalVarsJsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка при парсинге файла глоб. переменных '{Path.GetFileName(globalVarsJsonPath)}': {ex.Message}\n{ex.StackTrace}");
                throw; // Перебрасываем
            }

            // --- Объединение данных --- 
            if (parsedData != null && globalVarsData != null)
            {
                parsedData.GlobalVariables = globalVarsData.GlobalVariables; // Добавляем переменные
                Console.WriteLine($"Данные успешно объединены. Загружено моделей: {parsedData.Packages?.FirstOrDefault()?.Models?.Count ?? 0}, Глоб. переменных: {parsedData.GlobalVariables?.Count ?? 0}");
                return parsedData;
            }
            else
            {
                // Эта ветка маловероятна из-за throw выше, но для полноты
                Console.WriteLine("Ошибка: Не удалось получить данные из одного или обоих файлов.");
                return null;
            }
        }

        /// <summary>
        /// Парсит основной файл с объектами из Articy X
        /// </summary>
        private ArticyExportData ParseArticyXObjects(string jsonContent)
        {
            ArticyExportData articyData = new ArticyExportData();
            articyData.Packages = new List<Package>
            {
                new Package
                {
                    Name = "Default",
                    Description = "Imported from Articy X",
                    IsDefaultPackage = true,
                    Models = new List<Model>() // Инициализируем список!
                }
            };

            try
            {
                // Десериализуем в корневой объект
                var rootObject = JsonConvert.DeserializeObject<RootObject>(jsonContent);

                if (rootObject == null || rootObject.Objects == null)
                {
                    Console.WriteLine("Ошибка: Корневой объект JSON пуст или не содержит поля 'Objects'.");
                    // Пытаемся десериализовать как массив (для обратной совместимости, если вдруг формат изменится)
                    try
                    {
                        var objectsList = JsonConvert.DeserializeObject<List<ArticyXObject>>(jsonContent);
                        if (objectsList != null)
                        {
                            Console.WriteLine("JSON десериализован как массив объектов.");
                            ProcessArticyXObjectList(objectsList, articyData.Packages[0].Models);
                        }
                        else
                        {
                            Console.WriteLine("Не удалось десериализовать JSON ни как объект, ни как массив.");
                        }
                    }
                    catch (Exception exArray)
                    {
                        Console.WriteLine($"Ошибка при попытке десериализации JSON как массива: {exArray.Message}");
                    }
                    return articyData; // Возвращаем то, что успели сделать (пустой пакет)
                }

                // Обрабатываем список объектов
                Console.WriteLine($"Найдено {rootObject.Objects.Count} объектов в JSON.");
                ProcessArticyXObjectList(rootObject.Objects, articyData.Packages[0].Models);
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"Ошибка десериализации JSON объектов: {jsonEx.Message}");
                // Попытка десериализации как массива (на случай корневого массива)
                try
                {
                    var objectsList = JsonConvert.DeserializeObject<List<ArticyXObject>>(jsonContent);
                    if (objectsList != null)
                    {
                        Console.WriteLine("JSON десериализован как массив объектов (после ошибки). ");
                        ProcessArticyXObjectList(objectsList, articyData.Packages[0].Models);
                    }
                    else
                    {
                        Console.WriteLine("Не удалось десериализовать JSON ни как объект, ни как массив.");
                    }
                }
                catch (Exception exArray)
                {
                    Console.WriteLine($"Ошибка при попытке десериализации JSON как массива: {exArray.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Непредвиденная ошибка при парсинге объектов Articy X: {ex.Message}\n{ex.StackTrace}");
            }

            Console.WriteLine($"Добавлено {articyData.Packages[0].Models.Count} моделей в пакет.");
            return articyData;
        }

        /// <summary>
        /// Обрабатывает список десериализованных объектов Articy X и преобразует их в Models.
        /// </summary>
        private void ProcessArticyXObjectList(List<ArticyXObject> objects, List<Model> targetModelList)
        {
            if (objects == null || targetModelList == null) return;

            foreach (var articyXObj in objects)
            {
                if (articyXObj == null || articyXObj.Properties == null)
                {
                    Console.WriteLine("Пропущен объект: отсутствует сам объект или его свойства.");
                    continue;
                }

                Model model = new Model
                {
                    // Присваиваем строковый тип
                    Type = articyXObj.Type,
                    // Сразу пытаемся десериализовать JObject в ModelProperty
                    Properties = articyXObj.Properties?.ToObject<ModelProperty>(new JsonSerializer { MissingMemberHandling = MissingMemberHandling.Ignore })
                                 ?? new ModelProperty() // Если Properties null или ошибка, создаем пустой
                };

                try
                {
                    // Проверка обязательного поля ID (все еще нужна)
                    if (string.IsNullOrEmpty(model.Properties.Id))
                    {
                        Console.WriteLine($"Пропущен объект типа '{articyXObj.Type}' из-за отсутствия ID.");
                        continue;
                    }

                    // Проверка типа (все еще полезна)
                    if (model.TypeEnum == TypeEnum.Other)
                    {
                        Console.WriteLine($"Предупреждение: Неизвестный тип объекта '{articyXObj.Type}' для ID {model.Properties.Id}.");
                    }

                    targetModelList.Add(model);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке объекта (ID: {model.Properties?.Id ?? "??"}, Type: {model.Type ?? "??"}): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Парсит файл с глобальными переменными из Articy X
        /// </summary>
        private ArticyExportData ParseArticyXGlobalVariables(string jsonContent)
        {
            // ... (код остается без изменений) ...
            try
            {
                // Пытаемся десериализовать старую структуру { "GlobalVariables": [...] }
                var globalVarsJson = JsonConvert.DeserializeObject<Dictionary<string, List<GlobalVariable>>>(jsonContent);
                if (globalVarsJson != null && globalVarsJson.ContainsKey("GlobalVariables"))
                {
                    return new ArticyExportData
                    {
                        GlobalVariables = globalVarsJson["GlobalVariables"],
                        Packages = new List<Package>()
                    };
                }
            }
            catch (JsonException) { /* Игнорируем ошибку десериализации старого формата */ }

            try
            {
                // Пытаемся десериализовать новую структуру, где корневой элемент - это список
                var globalVarsList = JsonConvert.DeserializeObject<List<GlobalVariable>>(jsonContent);
                if (globalVarsList != null)
                {
                    return new ArticyExportData
                    {
                        GlobalVariables = globalVarsList,
                        Packages = new List<Package>()
                    };
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Ошибка десериализации JSON глобальных переменных как списка: {ex.Message}");
                throw; // Если и это не сработало, значит формат неизвестен
            }

            // Если ни один формат не подошел
            Console.WriteLine("Не удалось определить формат JSON для глобальных переменных.");
            return new ArticyExportData { GlobalVariables = new List<GlobalVariable>(), Packages = new List<Package>() };
        }
    }
}