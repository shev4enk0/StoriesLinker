using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace StoriesLinker.ArticyX
{
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
            if (string.IsNullOrEmpty(projectPath)) return;

            string jsonFolder = Path.Combine(projectPath, "Raw", JSON_FOLDER_NAME);
            _defaultPath = jsonFolder;

            if (!Directory.Exists(jsonFolder)) return;

            _flowJsonPath = Path.Combine(jsonFolder, FILE_PATTERN_NAME);
            _globalVarsJsonPath = Path.Combine(jsonFolder, GLOBAL_VARS_FILE_NAME);

            if (!File.Exists(_flowJsonPath) || !File.Exists(_globalVarsJsonPath)) return;

            try
            {
                Console.WriteLine($"Найдены файлы Articy X в папке: {jsonFolder}");
                Console.WriteLine($"Flow файл: {Path.GetFileName(_flowJsonPath)}");
                Console.WriteLine($"Global Variables файл: {Path.GetFileName(_globalVarsJsonPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при инициализации ArticyXParser: {ex.Message}");
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
                throw new ArgumentException("Пути к файлам не указаны ни в параметрах метода, ни в конструкторе");
            }

            if (!File.Exists(flowJsonPath) || !File.Exists(globalVarsJsonPath))
            {
                throw new FileNotFoundException($"Один или оба файла не найдены:\nFlow: {flowJsonPath}\nGlobalVars: {globalVarsJsonPath}");
            }

            string flowJsonContent = File.ReadAllText(flowJsonPath);
            string globalVarsJsonContent = File.ReadAllText(globalVarsJsonPath);

            var flowFile = ParseArticyXObjects(flowJsonContent);
            var globalVarsFile = ParseArticyXGlobalVariables(globalVarsJsonContent);

            // Объединяем данные
            flowFile.GlobalVariables = globalVarsFile.GlobalVariables;

            return flowFile;
        }

        /// <summary>
        /// Парсит основной файл с нодами и связями из Articy X
        /// </summary>
        private ArticyExportData ParseArticyXObjects(string jsonContent)
        {
            ArticyExportData jsonObj = new ArticyExportData();

            try
            {
                // Парсим основной файл с нодами
                var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

                // Создаем дефолтный пакет для совместимости с существующей структурой
                jsonObj.Packages =
                [
                    new Package
                    {
                        Name = "Default",
                        Description = "Imported from Articy X",
                        IsDefaultPackage = true,
                        Models = []
                    }
                ];

                // Проверяем, есть ли массив Objects
                if (jsonData.ContainsKey("Objects") && jsonData["Objects"] is Newtonsoft.Json.Linq.JArray objectsArray)
                {
                    Console.WriteLine($"Найдено {objectsArray.Count} объектов в массиве Objects");
                }

            }
            catch (JsonReaderException ex)
            {
                // Если не удалось десериализовать как Dictionary, пробуем как массив
                try
                {
                    Console.WriteLine("Не удалось десериализовать как Dictionary, пробуем как массив...");
                    var nodesArray = JsonConvert.DeserializeObject<List<object>>(jsonContent);

                    // Создаем дефолтный пакет для совместимости с существующей структурой
                    jsonObj.Packages =
                    [
                        new Package
                        {
                            Name = "Default",
                            Description = "Imported from Articy X",
                            IsDefaultPackage = true,
                            Models = []
                        }
                    ];

                }
                catch (Exception arrayEx)
                {
                    Console.WriteLine($"Ошибка при парсинге JSON как массива: {arrayEx.Message}");
                    throw;
                }
            }

            return jsonObj;
        }

        /// <summary>
        /// Парсит файл с глобальными переменными из Articy X
        /// </summary>
        private ArticyExportData ParseArticyXGlobalVariables(string jsonContent)
        {
            var globalVarsJson = JsonConvert.DeserializeObject<Dictionary<string, List<GlobalVariable>>>(jsonContent);

            return new ArticyExportData
            {
                GlobalVariables = globalVarsJson["GlobalVariables"],
                Packages = [] // Пустой список пакетов, так как в этом файле их нет
            };
        }
    }
}