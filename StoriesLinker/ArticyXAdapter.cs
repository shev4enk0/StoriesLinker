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
    /// Адаптер для работы с данными Articy X, преобразующий их в формат Articy 3
    /// </summary>
    public class ArticyXAdapter
    {
        private readonly string _projectPath;
        private readonly string _baseLanguage;

        public ArticyXAdapter(string projectPath, string baseLanguage = "Russian")
        {
            _projectPath = projectPath;
            _baseLanguage = baseLanguage;
        }

        /// <summary>
        /// Проверяет, является ли проект Articy X
        /// </summary>
        public static bool IsArticyXProject(string projectPath)
        {
            return Directory.Exists(Path.Combine(projectPath, "Raw", "JSON_X"));
        }

        /// <summary>
        /// Конвертирует данные Articy X в формат Articy 3 (AjFile)
        /// </summary>
        public AjFile ConvertToArticy3Format()
        {
            var ajFile = new AjFile
            {
                GlobalVariables = LoadGlobalVariables(),
                Packages = new List<AjPackage>()
            };

            // Загружаем объекты из пакета
            var package = LoadPackageObjects();
            ajFile.Packages.Add(package);

            return ajFile;
        }

        /// <summary>
        /// Создает Excel файл локализации из JSON данных Articy X
        /// </summary>
        public void CreateLocalizationExcelFile()
        {
            var localizationData = LoadLocalizationData();
            string outputPath = Path.Combine(_projectPath, "Raw", $"loc_All objects_{GetLanguageCode()}.xlsx");

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Localization");

                int row = 1;
                foreach (var kvp in localizationData)
                {
                    worksheet.Cells[row, 1].Value = kvp.Key;
                    worksheet.Cells[row, 2].Value = kvp.Value;
                    row++;
                }

                var fileInfo = new FileInfo(outputPath);
                package.SaveAs(fileInfo);
            }
        }

        /// <summary>
        /// Загружает глобальные переменные из Articy X
        /// </summary>
        private List<AjNamespace> LoadGlobalVariables()
        {
            string globalVarsPath = Path.Combine(_projectPath, "Raw", "JSON_X", "global_variables.json");
            
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
        /// Загружает объекты пакета из Articy X
        /// </summary>
        private AjPackage LoadPackageObjects()
        {
            string manifestPath = Path.Combine(_projectPath, "Raw", "JSON_X", "manifest.json");
            string manifest = File.ReadAllText(manifestPath);
            var manifestData = JObject.Parse(manifest);

            // Получаем информацию о пакете из манифеста
            var packageInfo = manifestData["Packages"][0];
            string objectsFileName = packageInfo["Files"]["Objects"]["FileName"].ToString();
            
            string objectsPath = Path.Combine(_projectPath, "Raw", "JSON_X", objectsFileName);
            string objectsJson = File.ReadAllText(objectsPath);
            var objectsData = JObject.Parse(objectsJson);

            var package = new AjPackage
            {
                Name = packageInfo["Name"].ToString(),
                Description = packageInfo["Description"].ToString(),
                IsDefaultPackage = packageInfo["IsDefaultPackage"].ToObject<bool>(),
                Models = new List<AjObj>()
            };

            // Преобразуем объекты
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
        /// Конвертирует объект из формата Articy X в AjObj
        /// </summary>
        private AjObj ConvertToAjObj(JToken objToken)
        {
            try
            {
                var properties = objToken["Properties"];
                
                var ajObj = new AjObj
                {
                    Type = objToken["Type"].ToString(),
                    Properties = new AjObjProps
                    {
                        TechnicalName = properties["TechnicalName"]?.ToString(),
                        Id = properties["Id"]?.ToString(),
                        DisplayName = properties["DisplayName"]?.ToString(),
                        Parent = properties["Parent"]?.ToString(),
                        Text = properties["Text"]?.ToString(),
                        ExternalId = properties["ExternalId"]?.ToString(),
                        ShortId = properties["ShortId"]?.ToString(),
                        MenuText = properties["MenuText"]?.ToString(),
                        StageDirections = properties["StageDirections"]?.ToString(),
                        Speaker = properties["Speaker"]?.ToString(),
                        Expression = properties["Expression"]?.ToString(),
                        Target = properties["Target"]?.ToString(),
                        TargetPin = properties["TargetPin"]?.ToString(),
                        Attachments = properties["Attachments"]?.ToObject<List<string>>() ?? new List<string>()
                    }
                };

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
        /// Загружает данные локализации из Articy X
        /// </summary>
        private Dictionary<string, string> LoadLocalizationData()
        {
            string manifestPath = Path.Combine(_projectPath, "Raw", "JSON_X", "manifest.json");
            string manifest = File.ReadAllText(manifestPath);
            var manifestData = JObject.Parse(manifest);

            // Получаем информацию о файле локализации
            var packageInfo = manifestData["Packages"][0];
            string localizationFileName = packageInfo["Files"]["Texts"]["FileName"].ToString();
            
            string localizationPath = Path.Combine(_projectPath, "Raw", "JSON_X", localizationFileName);
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