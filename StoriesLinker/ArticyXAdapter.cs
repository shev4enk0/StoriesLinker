using Newtonsoft.Json.Linq;

namespace StoriesLinker
{
    /// <summary>
    /// Адаптер для работы с данными Articy X, создающий файлы в формате Articy 3
    /// </summary>
    public class ArticyXAdapter(string projectPath, string baseLanguage = "Russian")
    {
        private readonly Dictionary<string, string> _localizationDict = new(); // ключ -> текст

        /// <summary>
        /// Проверяет, является ли проект Articy X
        /// </summary>
        public static bool IsArticyXProject(string projectPath) => 
            Directory.Exists(Path.Combine(projectPath, "Raw", "X"));

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
            if (_localizationDict.TryAdd(newKey, text))
            {
                Console.WriteLine(ConsoleMessages.ProcessingLocalizationKey(newKey, text.Substring(0, Math.Min(50, text.Length))));
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
            Console.WriteLine(ConsoleMessages.ProcessingArticyFile());

            var ajFile = new AjFile
            {
                GlobalVariables = LoadGlobalVariables(),
                Packages = []
            };

            // Загружаем объекты из пакета и конвертируем тексты в ключи
            var package = LoadPackageObjects();
            ajFile.Packages.Add(package);

            Console.WriteLine(ConsoleMessages.ArticyObjectsProcessed(package.Models.Count));
            Console.WriteLine(ConsoleMessages.LocalizationKeysGenerated(_localizationDict.Count));

            return ajFile;
        }

        /// <summary>
        /// Создает Excel файл локализации из данных Articy X + созданных ключей
        /// </summary>
        public void CreateLocalizationExcelFile()
        {
            Console.WriteLine(ConsoleMessages.CreatingLocalizationExcel());

            // Загружаем существующие данные локализации
            var existingData = LoadExistingLocalizationData();
            
            // Объединяем с новыми ключами
            var combinedData = new Dictionary<string, string>(existingData);
            foreach (var kvp in _localizationDict)
            {
                if (!combinedData.ContainsKey(kvp.Key))
                {
                    combinedData[kvp.Key] = kvp.Value;
                }
            }

            // Создаем Excel файл
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Localization");
                
            // Заголовки
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Text";
                
            int row = 2;
            foreach (var kvp in combinedData.OrderBy(x => x.Key))
            {
                worksheet.Cells[row, 1].Value = kvp.Key;
                worksheet.Cells[row, 2].Value = kvp.Value;
                row++;
            }

            // Сохраняем файл
            string outputPath = Path.Combine(projectPath, "Raw", "loc_All objects_" + GetLanguageCode() + ".xlsx");
            package.SaveAs(new FileInfo(outputPath));
                
            Console.WriteLine(ConsoleMessages.LocalizationFileCreated(outputPath));
            Console.WriteLine(ConsoleMessages.LocalizationEntriesWritten(combinedData.Count, _localizationDict.Count));
        }

        /// <summary>
        /// Загружает глобальные переменные из Articy X
        /// </summary>
        private List<AjNamespace> LoadGlobalVariables()
        {
            string globalVarsPath = Path.Combine(projectPath, "Raw", "X", "global_variables.json");
            
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
            string manifestPath = Path.Combine(projectPath, "Raw", "X", "manifest.json");
            string manifest = File.ReadAllText(manifestPath);
            var manifestData = JObject.Parse(manifest);

            // Получаем информацию о пакете из манифеста
            var packageInfo = manifestData["Packages"][0];
            string objectsFileName = packageInfo["Files"]["Objects"]["FileName"].ToString();
            
            string objectsPath = Path.Combine(projectPath, "Raw", "X", objectsFileName);
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
                    var ajColor = new AjColor
                    {
                        R = color["r"]?.ToObject<float>() ?? 0f,
                        G = color["g"]?.ToObject<float>() ?? 0f,
                        B = color["b"]?.ToObject<float>() ?? 0f,
                        A = color["a"]?.ToObject<float>() ?? 1f
                    };
                    
                    // Устанавливаем цвет и автоматически обновляем эмоцию
                    ajObj.Properties.SetColorAndUpdateEmotion(ajColor);
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
                Console.WriteLine(ConsoleMessages.ObjectConversionError(ex.Message));
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
            string manifestPath = Path.Combine(projectPath, "Raw", "X", "manifest.json");
            string manifest = File.ReadAllText(manifestPath);
            var manifestData = JObject.Parse(manifest);

            // Получаем информацию о файле локализации
            var packageInfo = manifestData["Packages"][0];
            string localizationFileName = packageInfo["Files"]["Texts"]["FileName"].ToString();
            
            string localizationPath = Path.Combine(projectPath, "Raw", "X", localizationFileName);
            string localizationJson = File.ReadAllText(localizationPath);
            var localizationData = JObject.Parse(localizationJson);

            var result = new Dictionary<string, string>();
            string langCode = GetLanguageCode().ToLower();

            foreach (var kvp in localizationData)
            {
                string key = kvp.Key;
                var value = kvp.Value;

                // Проверяем, что value является JObject, а не JProperty
                if (value.Type != JTokenType.Object)
                {
                    continue;
                }

                var valueObj = value as JObject;
                if (valueObj == null)
                {
                    continue;
                }

                // Ищем текст для нужного языка
                if (valueObj[langCode] != null && valueObj[langCode]["Text"] != null)
                {
                    result[key] = valueObj[langCode]["Text"].ToString();
                }
                else
                {
                    // Если нет текста для нужного языка, берем первый доступный
                    var firstLangProperty = valueObj.Properties().FirstOrDefault();
                    if (firstLangProperty != null && firstLangProperty.Value["Text"] != null)
                    {
                        result[key] = firstLangProperty.Value["Text"].ToString();
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
            switch (baseLanguage.ToLower())
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