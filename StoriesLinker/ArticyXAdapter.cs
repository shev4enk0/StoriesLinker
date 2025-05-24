using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;

namespace StoriesLinker
{
    /// <summary>
    /// Адаптер для работы с данными Articy X, создающий файлы в формате Articy 3
    /// </summary>
    public class ArticyXAdapter
    {
        private readonly string _projectPath;
        private readonly string _baseLanguage;
        private Dictionary<string, string> _localizationDict; // ключ -> текст

        public ArticyXAdapter(string projectPath, string baseLanguage = "Russian")
        {
            _projectPath = projectPath;
            _baseLanguage = baseLanguage;
            _localizationDict = new Dictionary<string, string>();
        }

        /// <summary>
        /// Проверяет, является ли проект Articy X
        /// </summary>
        public static bool IsArticyXProject(string projectPath)
        {
            return Directory.Exists(Path.Combine(projectPath, "Raw", "X"));
        }

        /// <summary>
        /// Генерирует ключ локализации для готового текста на основе TechnicalName
        /// </summary>
        private string GenerateLocalizationKey(string text, string suffix, string technicalName)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Если это уже ключ локализации - возвращаем как есть
            if (IsLocalizationKey(text))
                return text;

            // Используем TechnicalName объекта + суффикс
            string newKey = $"{technicalName}.{suffix}";

            // Сохраняем соответствие для локализации
            if (!_localizationDict.ContainsKey(newKey))
            {
                _localizationDict[newKey] = text;
                Console.WriteLine($"Создан ключ {newKey} для текста: {text.Substring(0, Math.Min(50, text.Length))}...");
            }

            return newKey;
        }

        /// <summary>
        /// Проверяет, является ли строка ключом локализации
        /// </summary>
        private bool IsLocalizationKey(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            // Ключи локализации содержат точку и начинаются с букв
            return text.Contains(".") && 
                   (text.StartsWith("DFr_") || text.StartsWith("Ntt_") || text.StartsWith("Loc_") || text.StartsWith("FFr_"));
        }

        /// <summary>
        /// Конвертирует данные Articy X в формат Articy 3 (AjFile)
        /// </summary>
        public AjFile ConvertToArticy3Format()
        {
            Console.WriteLine("Создаем файл Flow.json из данных Articy X...");

            var ajFile = new AjFile
            {
                GlobalVariables = LoadGlobalVariables(),
                Packages = new List<AjPackage>()
            };

            // Загружаем объекты из пакета и конвертируем тексты в ключи
            var package = LoadPackageObjects();
            ajFile.Packages.Add(package);

            Console.WriteLine($"Загружено {package.Models.Count} объектов из Articy X");
            Console.WriteLine($"Сгенерировано {_localizationDict.Count} ключей локализации");

            return ajFile;
        }

        /// <summary>
        /// Создает Excel файл локализации из данных Articy X + созданных ключей
        /// </summary>
        public void CreateLocalizationExcelFile()
        {
            Console.WriteLine("Создаем файл локализации Excel из данных Articy X...");
            
            // Загружаем существующие данные локализации из Articy X
            var existingLocalizationData = LoadExistingLocalizationData();
            
            // Объединяем с нашими сгенерированными ключами
            var combinedData = new Dictionary<string, string>(existingLocalizationData);
            foreach (var kvp in _localizationDict)
            {
                combinedData[kvp.Key] = kvp.Value;
            }

            string outputPath = Path.Combine(_projectPath, "Raw", $"loc_All objects_{GetLanguageCode()}.xlsx");

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Localization");

                int row = 1;
                foreach (var kvp in combinedData)
                {
                    worksheet.Cells[row, 1].Value = kvp.Key;   // Ключ локализации
                    worksheet.Cells[row, 2].Value = kvp.Value; // Переведенный текст
                    row++;
                }

                var fileInfo = new FileInfo(outputPath);
                package.SaveAs(fileInfo);
            }

            Console.WriteLine($"Создан файл локализации: {outputPath}");
            Console.WriteLine($"Записано {combinedData.Count} записей локализации ({_localizationDict.Count} новых)");
        }

        /// <summary>
        /// Загружает глобальные переменные из Articy X
        /// </summary>
        private List<AjNamespace> LoadGlobalVariables()
        {
            string globalVarsPath = Path.Combine(_projectPath, "Raw", "X", "global_variables.json");
            
            if (!File.Exists(globalVarsPath))
                return new List<AjNamespace>();

            string json = File.ReadAllText(globalVarsPath);
            var data = JsonConvert.DeserializeObject<ArticyXGlobalVariables>(json);

            return data.GlobalVariables.Select(ns => new AjNamespace
            {
                Namespace = ns.Namespace,
                Description = ns.Description,
                Variables = ns.Variables.Select(v => new AjVariable
                {
                    Variable = v.Variable,
                    Type = v.Type,
                    Value = v.Value,
                    Description = v.Description
                }).ToList()
            }).ToList();
        }

        /// <summary>
        /// Загружает объекты пакета из Articy X и конвертирует тексты в ключи
        /// </summary>
        private AjPackage LoadPackageObjects()
        {
            string manifestPath = Path.Combine(_projectPath, "Raw", "X", "manifest.json");
            string manifest = File.ReadAllText(manifestPath);
            var manifestData = JObject.Parse(manifest);

            // Получаем информацию о пакете из манифеста
            var packageInfo = manifestData["Packages"][0];
            string objectsFileName = packageInfo["Files"]["Objects"]["FileName"].ToString();
            
            string objectsPath = Path.Combine(_projectPath, "Raw", "X", objectsFileName);
            string objectsJson = File.ReadAllText(objectsPath);
            var objectsData = JObject.Parse(objectsJson);

            var package = new AjPackage
            {
                Name = packageInfo["Name"].ToString(),
                Description = packageInfo["Description"].ToString(),
                IsDefaultPackage = packageInfo["IsDefaultPackage"].ToObject<bool>(),
                Models = new List<AjObj>()
            };

            // Преобразуем объекты, заменяя готовые тексты на ключи
            var objects = objectsData["Objects"].ToArray();
            foreach (var obj in objects)
            {
                var ajObj = ConvertToAjObj(obj);
                if (ajObj != null)
                {
                    package.Models.Add(ajObj);
                }
            }

            return package;
        }

        /// <summary>
        /// Конвертирует объект из формата Articy X в AjObj, заменяя тексты на ключи
        /// </summary>
        private AjObj ConvertToAjObj(JToken objToken)
        {
            try
            {
                var properties = objToken["Properties"];
                string objectType = objToken["Type"].ToString();
                
                var ajObj = new AjObj
                {
                    Type = objectType,
                    Properties = new AjObjProps
                    {
                        TechnicalName = properties["TechnicalName"]?.ToString(),
                        Id = properties["Id"]?.ToString(),
                        Parent = properties["Parent"]?.ToString(),
                        ExternalId = properties["ExternalId"]?.ToString(),
                        ShortId = properties["ShortId"]?.ToString(),
                        Speaker = properties["Speaker"]?.ToString(),
                        Expression = properties["Expression"]?.ToString(),
                        Target = properties["Target"]?.ToString(),
                        TargetPin = properties["TargetPin"]?.ToString(),
                        Attachments = properties["Attachments"]?.ToObject<List<string>>() ?? new List<string>()
                    }
                };

                // Обрабатываем текстовые поля в зависимости от типа объекта
                if (objectType == "DialogueFragment")
                {
                    // Для DialogueFragment заменяем готовые тексты на ключи
                    ajObj.Properties.DisplayName = ConvertTextToKey(properties["DisplayName"]?.ToString(), "DisplayName", ajObj.Properties.TechnicalName);
                    ajObj.Properties.Text = ConvertTextToKey(properties["Text"]?.ToString(), "Text", ajObj.Properties.TechnicalName);
                    ajObj.Properties.MenuText = ConvertTextToKey(properties["MenuText"]?.ToString(), "PreviewText", ajObj.Properties.TechnicalName);
                    ajObj.Properties.StageDirections = ConvertTextToKey(properties["StageDirections"]?.ToString(), "StageDirections", ajObj.Properties.TechnicalName);
                }
                else
                {
                    // Для других типов оставляем как есть (они уже содержат ключи)
                    ajObj.Properties.DisplayName = properties["DisplayName"]?.ToString();
                    ajObj.Properties.Text = properties["Text"]?.ToString();
                    ajObj.Properties.MenuText = properties["MenuText"]?.ToString();
                    ajObj.Properties.StageDirections = properties["StageDirections"]?.ToString();
                }

                // Обработка цвета
                if (properties["Color"] != null)
                {
                    var color = properties["Color"];
                    ajObj.Properties.Color = new AjColor
                    {
                        R = color["R"]?.ToObject<float>() ?? 0f,
                        G = color["G"]?.ToObject<float>() ?? 0f,
                        B = color["B"]?.ToObject<float>() ?? 0f,
                        A = color["A"]?.ToObject<float>() ?? 1f
                    };
                }

                // Обработка пинов
                if (properties["InputPins"] != null)
                {
                    ajObj.Properties.InputPins = ConvertPins(properties["InputPins"]);
                }

                if (properties["OutputPins"] != null)
                {
                    ajObj.Properties.OutputPins = ConvertPins(properties["OutputPins"]);
                }

                return ajObj;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка преобразования объекта: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Конвертирует готовый текст в ключ локализации, если нужно
        /// </summary>
        private string ConvertTextToKey(string text, string suffix, string technicalName)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Если это уже ключ локализации - оставляем как есть
            if (IsLocalizationKey(text))
                return text;

            // Если это готовый текст - создаем ключ
            return GenerateLocalizationKey(text, suffix, technicalName);
        }

        /// <summary>
        /// Конвертирует пины из формата Articy X
        /// </summary>
        private List<AjPin> ConvertPins(JToken pinsToken)
        {
            var pins = new List<AjPin>();
            
            foreach (var pin in pinsToken)
            {
                var ajPin = new AjPin
                {
                    Text = pin["Text"]?.ToString(),
                    Id = pin["Id"]?.ToString(),
                    Owner = pin["Owner"]?.ToString(),
                    Connections = new List<AjConnection>()
                };

                if (pin["Connections"] != null)
                {
                    foreach (var connection in pin["Connections"])
                    {
                        ajPin.Connections.Add(new AjConnection
                        {
                            Label = connection["Label"]?.ToString(),
                            TargetPin = connection["TargetPin"]?.ToString(),
                            Target = connection["Target"]?.ToString()
                        });
                    }
                }

                pins.Add(ajPin);
            }

            return pins;
        }

        /// <summary>
        /// Загружает существующие данные локализации из Articy X
        /// </summary>
        private Dictionary<string, string> LoadExistingLocalizationData()
        {
            string manifestPath = Path.Combine(_projectPath, "Raw", "X", "manifest.json");
            string manifest = File.ReadAllText(manifestPath);
            var manifestData = JObject.Parse(manifest);

            // Получаем информацию о файле локализации
            var packageInfo = manifestData["Packages"][0];
            string localizationFileName = packageInfo["Files"]["Texts"]["FileName"].ToString();
            
            string localizationPath = Path.Combine(_projectPath, "Raw", "X", localizationFileName);
            string localizationJson = File.ReadAllText(localizationPath);
            var localizationData = JObject.Parse(localizationJson);

            var result = new Dictionary<string, string>();
            string langCode = GetLanguageCode().ToLower();

            foreach (var kvp in localizationData)
            {
                string key = kvp.Key;
                var value = kvp.Value;

                // Ищем текст для нужного языка
                if (value[langCode] != null && value[langCode]["Text"] != null)
                {
                    result[key] = value[langCode]["Text"].ToString();
                }
                else
                {
                    // Если нет текста для нужного языка, берем первый доступный
                    var firstLang = value.Children().FirstOrDefault();
                    if (firstLang != null && firstLang["Text"] != null)
                    {
                        result[key] = firstLang["Text"].ToString();
                    }
                    else
                    {
                        result[key] = key; // Используем ключ как значение по умолчанию
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Получает код языка для файлов
        /// </summary>
        private string GetLanguageCode()
        {
            switch (_baseLanguage.ToLower())
            {
                case "russian": return "ru";
                case "english": return "en";
                case "polish": return "pl";
                case "deutsch": case "german": return "de";
                case "french": return "fr";
                case "spanish": return "es";
                case "japan": case "japanese": return "jp";
                default: return "ru";
            }
        }
    }

    // Вспомогательные классы для десериализации JSON Articy X
    [Serializable]
    public class ArticyXGlobalVariables
    {
        public List<ArticyXNamespace> GlobalVariables { get; set; }
    }

    [Serializable]
    public class ArticyXNamespace
    {
        public string Namespace { get; set; }
        public string Description { get; set; }
        public List<ArticyXVariable> Variables { get; set; }
    }

    [Serializable]
    public class ArticyXVariable
    {
        public string Variable { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }
} 