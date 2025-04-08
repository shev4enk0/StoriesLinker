using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace StoriesLinker
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
        /// Создает парсер Articy X с указанным экземпляром LinkerBin
        /// </summary>
        /// <param name="linker">Экземпляр LinkerBin для работы с проектом</param>
        /// <param name="defaultPath">Опциональный путь к корневой папке проекта</param>
        public JsonObjectsParser(LinkerBin linker, string defaultPath = null)
        {
            _linker = linker;
            if (string.IsNullOrEmpty(defaultPath)) return;
            
            string jsonFolder = Path.Combine(defaultPath, "Raw", JSON_FOLDER_NAME);
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

        public string GetFlowJsonPath()
        {
            return _flowJsonPath;
        }

        public string GetGlobalVarsJsonPath()
        {
            return _globalVarsJsonPath;
        }

        /// <summary>
        /// Получает объекты Articy X из найденных файлов
        /// </summary>
        /// <param name="nativeDict">Словарь с нативными значениями для локализации</param>
        /// <returns>Словарь объектов Articy X</returns>
        public Dictionary<string, AjObj> GetArticyXObjects(Dictionary<string, string> nativeDict = null)
        {
            if (string.IsNullOrEmpty(_flowJsonPath) || string.IsNullOrEmpty(_globalVarsJsonPath))
            {
                Console.WriteLine("Ошибка: Пути к файлам Articy X не найдены");
                return new Dictionary<string, AjObj>();
            }

            if (!File.Exists(_flowJsonPath) || !File.Exists(_globalVarsJsonPath))
            {
                Console.WriteLine($"Ошибка: Файлы Articy X не найдены:\nFlow: {_flowJsonPath}\nGlobalVars: {_globalVarsJsonPath}");
                return new Dictionary<string, AjObj>();
            }

            try
            {
                Console.WriteLine("Начинаем парсинг файлов Articy X...");
                
                // Парсим файлы Articy X
                var ajFile = ParseArticyX();
                
                // Если LinkerBin не предоставлен, возвращаем пустой словарь
                if (_linker == null)
                {
                    Console.WriteLine("Предупреждение: LinkerBin не предоставлен, возвращаем пустой словарь объектов");
                    return new Dictionary<string, AjObj>();
                }
                
                // Если словарь локализации не предоставлен, получаем его из LinkerBin
                if (nativeDict == null)
                {
                    nativeDict = _linker.GetLocalizationDictionary();
                }
                
                // Получаем объекты Articy X
                var objects = _linker.ExtractBookEntities(ajFile, nativeDict);
                
                Console.WriteLine($"Успешно получено {objects.Count} объектов Articy X");
                return objects;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении объектов Articy X: {ex.Message}");
                return new Dictionary<string, AjObj>();
            }
        }

        /// <summary>
        /// Парсит файлы Articy X и преобразует их в формат AJ
        /// </summary>
        /// <param name="customFlowJsonPath">Опциональный путь к JSON с нодами и связями</param>
        /// <param name="customGlobalVarsJsonPath">Опциональный путь к JSON с глобальными переменными</param>
        /// <returns>Объект AJFile, совместимый с существующей структурой</returns>
        public AjFile ParseArticyX(string customFlowJsonPath = null, string customGlobalVarsJsonPath = null)
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
        private AjFile ParseArticyXObjects(string jsonContent)
        {
            AjFile jsonObj = new AjFile();
            
            try
            {
                // Парсим основной файл с нодами
                var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
                
                // Создаем дефолтный пакет для совместимости с существующей структурой
                jsonObj.Packages = new List<AjPackage>
                {
                    new AjPackage
                    {
                        Name = "Default",
                        Description = "Imported from Articy X",
                        IsDefaultPackage = true,
                        Models = new List<AjObj>()
                    }
                };

                // Проверяем, есть ли массив Objects
                if (jsonData.ContainsKey("Objects") && jsonData["Objects"] is Newtonsoft.Json.Linq.JArray objectsArray)
                {
                    Console.WriteLine($"Найдено {objectsArray.Count} объектов в массиве Objects");
                    
                    // Парсим каждый объект из массива Objects
                    foreach (var obj in objectsArray)
                    {
                        try
                        {
                            var nodeObj = JsonConvert.DeserializeObject<AjObj>(obj.ToString());
                            // Устанавливаем EType в соответствии с Type
                            switch (nodeObj.Type)
                            {
                                case "FlowFragment":
                                    nodeObj.EType = AjType.FlowFragment;
                                    break;
                                case "Dialogue":
                                    nodeObj.EType = AjType.Dialogue;
                                    break;
                                case "Entity":
                                case "DefaultSupportingCharacterTemplate":
                                case "DefaultMainCharacterTemplate":
                                    nodeObj.EType = AjType.Entity;
                                    break;
                                case "Location":
                                    nodeObj.EType = AjType.Location;
                                    break;
                                case "DialogueFragment":
                                    nodeObj.EType = AjType.DialogueFragment;
                                    break;
                                case "Instruction":
                                    nodeObj.EType = AjType.Instruction;
                                    break;
                                case "Condition":
                                    nodeObj.EType = AjType.Condition;
                                    break;
                                case "Jump":
                                    nodeObj.EType = AjType.Jump;
                                    break;
                                default:
                                    nodeObj.EType = AjType.Other;
                                    break;
                            }
                            jsonObj.Packages[0].Models.Add(nodeObj);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка при парсинге объекта: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // Если нет массива Objects, пробуем парсить как Dictionary
                    foreach (var node in jsonData)
                    {
                        try
                        {
                            var nodeObj = JsonConvert.DeserializeObject<AjObj>(node.Value.ToString());
                            // Устанавливаем EType в соответствии с Type
                            switch (nodeObj.Type)
                            {
                                case "FlowFragment":
                                    nodeObj.EType = AjType.FlowFragment;
                                    break;
                                case "Dialogue":
                                    nodeObj.EType = AjType.Dialogue;
                                    break;
                                case "Entity":
                                case "DefaultSupportingCharacterTemplate":
                                case "DefaultMainCharacterTemplate":
                                    nodeObj.EType = AjType.Entity;
                                    break;
                                case "Location":
                                    nodeObj.EType = AjType.Location;
                                    break;
                                case "DialogueFragment":
                                    nodeObj.EType = AjType.DialogueFragment;
                                    break;
                                case "Instruction":
                                    nodeObj.EType = AjType.Instruction;
                                    break;
                                case "Condition":
                                    nodeObj.EType = AjType.Condition;
                                    break;
                                case "Jump":
                                    nodeObj.EType = AjType.Jump;
                                    break;
                                default:
                                    nodeObj.EType = AjType.Other;
                                    break;
                            }
                            jsonObj.Packages[0].Models.Add(nodeObj);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка при парсинге элемента Dictionary: {ex.Message}");
                        }
                    }
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
                    jsonObj.Packages = new List<AjPackage>
                    {
                        new AjPackage
                        {
                            Name = "Default",
                            Description = "Imported from Articy X",
                            IsDefaultPackage = true,
                            Models = new List<AjObj>()
                        }
                    };

                    // Парсим каждый объект из массива
                    foreach (var node in nodesArray)
                    {
                        try
                        {
                            var nodeObj = JsonConvert.DeserializeObject<AjObj>(node.ToString());
                            // Устанавливаем EType в соответствии с Type
                            switch (nodeObj.Type)
                            {
                                case "FlowFragment":
                                    nodeObj.EType = AjType.FlowFragment;
                                    break;
                                case "Dialogue":
                                    nodeObj.EType = AjType.Dialogue;
                                    break;
                                case "Entity":
                                case "DefaultSupportingCharacterTemplate":
                                case "DefaultMainCharacterTemplate":
                                    nodeObj.EType = AjType.Entity;
                                    break;
                                case "Location":
                                    nodeObj.EType = AjType.Location;
                                    break;
                                case "DialogueFragment":
                                    nodeObj.EType = AjType.DialogueFragment;
                                    break;
                                case "Instruction":
                                    nodeObj.EType = AjType.Instruction;
                                    break;
                                case "Condition":
                                    nodeObj.EType = AjType.Condition;
                                    break;
                                case "Jump":
                                    nodeObj.EType = AjType.Jump;
                                    break;
                                default:
                                    nodeObj.EType = AjType.Other;
                                    break;
                            }
                            jsonObj.Packages[0].Models.Add(nodeObj);
                        }
                        catch (Exception innerEx)
                        {
                            Console.WriteLine($"Ошибка при парсинге элемента массива: {innerEx.Message}");
                        }
                    }
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
        private AjFile ParseArticyXGlobalVariables(string jsonContent)
        {
            var globalVarsJson = JsonConvert.DeserializeObject<Dictionary<string, List<AjNamespace>>>(jsonContent);
            
            return new AjFile
            {
                GlobalVariables = globalVarsJson["GlobalVariables"],
                Packages = new List<AjPackage>() // Пустой список пакетов, так как в этом файле их нет
            };
        }
    }
} 