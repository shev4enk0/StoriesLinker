using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.Text;

namespace StoriesLinker.Converter
{
    // Классы для Articy 3
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class FlowProject
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class FlowGlobalVariable
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class FlowObject
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("technicalName")]
        public string TechnicalName { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }

        [JsonProperty("position")]
        public Dictionary<string, object> Position { get; set; }

        [JsonProperty("connections")]
        public List<FlowConnection> Connections { get; set; }

        [JsonProperty("pins")]
        public List<FlowPin> Pins { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class FlowConnection
    {
        [JsonProperty("sourceId")]
        public string SourceId { get; set; }

        [JsonProperty("targetId")]
        public string TargetId { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class FlowPin
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class FlowHierarchyNode
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("technicalName")]
        public string TechnicalName { get; set; }

        [JsonProperty("children")]
        public List<FlowHierarchyNode> Children { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Flow
    {
        [JsonProperty("project")]
        public FlowProject Project { get; set; }

        [JsonProperty("globalVariables")]
        public Dictionary<string, FlowGlobalVariable> GlobalVariables { get; set; }

        [JsonProperty("objects")]
        public List<FlowObject> Objects { get; set; }

        [JsonProperty("hierarchy")]
        public FlowHierarchyNode Hierarchy { get; set; }
    }

    // Классы для Articy X
    public class ArticyXManifestSettings
    {
        public string ExportVersion { get; set; }
    }

    public class ArticyXManifestProject
    {
        public string Name { get; set; }
        public string DetailName { get; set; }
        public string TechnicalName { get; set; }
    }

    public class ArticyXManifest
    {
        public ArticyXManifestSettings Settings { get; set; }
        public ArticyXManifestProject Project { get; set; }
    }

    public class ArticyXGlobalVariable
    {
        public string Namespace { get; set; }
        public string Variable { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }

    public class ArticyXGlobalVariables
    {
        public List<ArticyXGlobalVariable> GlobalVariables { get; set; }
    }

    public class ArticyXObject
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string TechnicalName { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public Dictionary<string, object> Position { get; set; }
        public List<ArticyXConnection> Connections { get; set; }
        public List<ArticyXPin> Pins { get; set; }
    }

    public class ArticyXConnection
    {
        public string SourceId { get; set; }
        public string TargetId { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }

    public class ArticyXPin
    {
        public string Id { get; set; }
        public string ObjectId { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }

    public class ArticyXHierarchy
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string TechnicalName { get; set; }
        public List<ArticyXHierarchy> Children { get; set; }
    }

    public class ArticyXToArticy3Converter
    {
        private readonly string _sourceDirectory;
        private readonly string _outputFile;
        private Dictionary<string, string> _localizationMap;

        public ArticyXToArticy3Converter(string sourceDirectory, string outputFile)
        {
            _sourceDirectory = sourceDirectory;
            _outputFile = outputFile;
            _localizationMap = new Dictionary<string, string>();
        }

        public void Convert()
        {
            try
            {
                Console.WriteLine("=== Начало конвертации из Articy X в Articy 3 ===");
                Console.WriteLine($"Исходная директория: {_sourceDirectory}");
                Console.WriteLine($"Выходной файл: {_outputFile}");

                // Читаем и парсим все файлы
                var manifest = ReadAndParseManifest();
                var globalVars = ReadAndParseGlobalVariables();
                var hierarchy = ReadAndParseHierarchy();
                var objects = ReadAndParseObjects();
                _localizationMap = ReadAndParseLocalization();

                Console.WriteLine("\nПодготовка данных для сериализации...");

                var json = new StringBuilder();
                json.AppendLine("{");

                // Project
                Console.WriteLine("Сериализация Project...");
                json.AppendLine("  \"project\": {");
                json.AppendLine($"    \"name\": {JsonConvert.SerializeObject(manifest.Project.Name)},");
                json.AppendLine($"    \"author\": {JsonConvert.SerializeObject(manifest.Project.DetailName)},");
                json.AppendLine($"    \"version\": {JsonConvert.SerializeObject(manifest.Settings.ExportVersion)}");
                json.AppendLine("  },");

                // GlobalVariables
                Console.WriteLine("Сериализация GlobalVariables...");
                json.AppendLine("  \"globalVariables\": {");
                var firstVar = true;
                foreach (var variable in globalVars.GlobalVariables)
                {
                    if (!firstVar) json.AppendLine(",");
                    firstVar = false;
                    var fullName = $"{variable.Namespace}.{variable.Variable}";
                    json.AppendLine($"    {JsonConvert.SerializeObject(fullName)}: {{");
                    json.AppendLine($"      \"type\": {JsonConvert.SerializeObject(variable.Type)},");
                    json.AppendLine($"      \"defaultValue\": {JsonConvert.SerializeObject(variable.Value ?? GetDefaultValue(variable.Type))},");
                    json.AppendLine($"      \"description\": {JsonConvert.SerializeObject(variable.Description ?? "")}");
                    json.Append("    }");
                }
                json.AppendLine();
                json.AppendLine("  },");

                // Objects
                Console.WriteLine("Сериализация Objects...");
                json.AppendLine("  \"objects\": [");
                var firstObj = true;
                foreach (var obj in objects)
                {
                    try
                    {
                        if (!firstObj) json.AppendLine(",");
                        firstObj = false;
                        json.AppendLine("    {");
                        json.AppendLine($"      \"id\": {JsonConvert.SerializeObject(obj.Id)},");
                        json.AppendLine($"      \"type\": {JsonConvert.SerializeObject(obj.Type)},");
                        json.AppendLine($"      \"technicalName\": {JsonConvert.SerializeObject(obj.TechnicalName)}");

                        if (obj.Properties?.Count > 0)
                        {
                            json.AppendLine(",");
                            json.AppendLine($"      \"properties\": {JsonConvert.SerializeObject(obj.Properties)}");
                        }

                        if (obj.Position?.Count > 0)
                        {
                            json.AppendLine(",");
                            json.AppendLine($"      \"position\": {JsonConvert.SerializeObject(obj.Position)}");
                        }

                        if (obj.Connections?.Count > 0)
                        {
                            json.AppendLine(",");
                            json.AppendLine("      \"connections\": [");
                            var firstConn = true;
                            foreach (var conn in obj.Connections)
                            {
                                if (!string.IsNullOrEmpty(conn.SourceId) && !string.IsNullOrEmpty(conn.TargetId))
                                {
                                    if (!firstConn) json.AppendLine(",");
                                    firstConn = false;
                                    json.AppendLine("        {");
                                    json.AppendLine($"          \"sourceId\": {JsonConvert.SerializeObject(conn.SourceId)},");
                                    json.AppendLine($"          \"targetId\": {JsonConvert.SerializeObject(conn.TargetId)}");
                                    if (conn.Properties?.Count > 0)
                                    {
                                        json.AppendLine(",");
                                        json.AppendLine($"          \"properties\": {JsonConvert.SerializeObject(conn.Properties)}");
                                    }
                                    json.Append("        }");
                                }
                            }
                            json.AppendLine();
                            json.AppendLine("      ]");
                        }

                        if (obj.Pins?.Count > 0)
                        {
                            json.AppendLine(",");
                            json.AppendLine("      \"pins\": [");
                            var firstPin = true;
                            foreach (var pin in obj.Pins)
                            {
                                if (!string.IsNullOrEmpty(pin.Id) && !string.IsNullOrEmpty(pin.ObjectId))
                                {
                                    if (!firstPin) json.AppendLine(",");
                                    firstPin = false;
                                    json.AppendLine("        {");
                                    json.AppendLine($"          \"id\": {JsonConvert.SerializeObject(pin.Id)},");
                                    json.AppendLine($"          \"objectId\": {JsonConvert.SerializeObject(pin.ObjectId)}");
                                    if (pin.Properties?.Count > 0)
                                    {
                                        json.AppendLine(",");
                                        json.AppendLine($"          \"properties\": {JsonConvert.SerializeObject(pin.Properties)}");
                                    }
                                    json.Append("        }");
                                }
                            }
                            json.AppendLine();
                            json.AppendLine("      ]");
                        }

                        json.Append("    }");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при сериализации объекта {obj.Id}: {ex.Message}");
                    }
                }
                json.AppendLine();
                json.AppendLine("  ]");

                // Hierarchy
                if (hierarchy != null)
                {
                    Console.WriteLine("Сериализация Hierarchy...");
                    json.AppendLine(",");
                    json.AppendLine("  \"hierarchy\": ");
                    SerializeHierarchy(hierarchy, json, 2);
                }

                json.AppendLine();
                json.AppendLine("}");

                Console.WriteLine("\nСохранение результата...");
                File.WriteAllText(_outputFile, json.ToString());

                Console.WriteLine($"\nКонвертация завершена успешно!");
                Console.WriteLine($"Результат сохранен в файл: {_outputFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nОшибка при конвертации: {ex.Message}");
                Console.WriteLine($"Тип ошибки: {ex.GetType().Name}");
                Console.WriteLine($"Стек вызовов:\n{ex.StackTrace}");
                throw;
            }
        }

        private void SerializeHierarchy(ArticyXHierarchy hierarchy, StringBuilder json, int indent)
        {
            if (hierarchy == null) return;

            var spaces = new string(' ', indent * 2);
            json.AppendLine($"{spaces}{{");
            json.AppendLine($"{spaces}  \"id\": {JsonConvert.SerializeObject(hierarchy.Id)},");
            json.AppendLine($"{spaces}  \"type\": {JsonConvert.SerializeObject(hierarchy.Type)},");
            json.AppendLine($"{spaces}  \"technicalName\": {JsonConvert.SerializeObject(hierarchy.TechnicalName)}");

            if (hierarchy.Children?.Count > 0)
            {
                json.AppendLine($"{spaces}  ,\"children\": [");
                var firstChild = true;
                foreach (var child in hierarchy.Children.Where(c => c != null))
                {
                    if (!firstChild) json.AppendLine(",");
                    firstChild = false;
                    SerializeHierarchy(child, json, indent + 2);
                }
                json.AppendLine();
                json.AppendLine($"{spaces}  ]");
            }

            json.Append($"{spaces}}}");
        }

        private ArticyXManifest ReadAndParseManifest()
        {
            var path = Path.Combine(_sourceDirectory, "manifest.json");
            Console.WriteLine($"Чтение manifest.json: {File.Exists(path)}");
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<ArticyXManifest>(json);
        }

        private ArticyXGlobalVariables ReadAndParseGlobalVariables()
        {
            var path = Path.Combine(_sourceDirectory, "global_variables.json");
            Console.WriteLine($"Чтение global_variables.json: {File.Exists(path)}");
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<ArticyXGlobalVariables>(json);
        }

        private ArticyXHierarchy ReadAndParseHierarchy()
        {
            var path = Path.Combine(_sourceDirectory, "hierarchy.json");
            Console.WriteLine($"Чтение hierarchy.json: {File.Exists(path)}");
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<ArticyXHierarchy>(json);
        }

        private List<ArticyXObject> ReadAndParseObjects()
        {
            var path = Path.Combine(_sourceDirectory, "package_0100000000000102_objects.json");
            Console.WriteLine($"Чтение objects.json: {File.Exists(path)}");
            
            try
            {
                var jsonText = File.ReadAllText(path);
                Console.WriteLine($"Размер файла: {jsonText.Length:N0} байт");

                // Сначала попробуем прочитать как JToken для анализа структуры
                var token = JToken.Parse(jsonText);
                Console.WriteLine($"Тип корневого элемента: {token.Type}");

                if (token is JObject rootObject)
                {
                    Console.WriteLine("\nАнализ структуры objects.json:");
                    foreach (var prop in rootObject.Properties())
                    {
                        Console.WriteLine($"- Свойство: {prop.Name}, Тип: {prop.Value?.Type}");
                        if (prop.Value?.Type == JTokenType.Array)
                        {
                            var array = prop.Value as JArray;
                            Console.WriteLine($"  Количество элементов: {array?.Count ?? 0}");
                            if (array?.FirstOrDefault() is JObject firstObj)
                            {
                                Console.WriteLine("\nСтруктура первого объекта в массиве:");
                                foreach (var objProp in firstObj.Properties())
                                {
                                    var value = objProp.Value?.ToString() ?? "null";
                                    if (value.Length > 100) value = value.Substring(0, 100) + "...";
                                    Console.WriteLine($"  - {objProp.Name}: {objProp.Value?.Type}, Значение: {value}");
                                }
                            }
                        }
                    }

                    var objects = new List<ArticyXObject>();
                    JArray objectsArray = null;

                    // Пробуем найти массив объектов
                    if (rootObject["Objects"] is JArray directObjects)
                    {
                        Console.WriteLine("\nНайден прямой массив Objects");
                        objectsArray = directObjects;
                    }
                    else if (rootObject["Package"]?["Objects"] is JArray packageObjects)
                    {
                        Console.WriteLine("\nНайдены объекты в Package.Objects");
                        objectsArray = packageObjects;
                    }

                    if (objectsArray != null)
                    {
                        Console.WriteLine($"\nНачинаем обработку {objectsArray.Count} объектов");
                        int processed = 0, withId = 0;

                        foreach (var item in objectsArray)
                        {
                            processed++;
                            try
                            {
                                if (!(item is JObject obj))
                                {
                                    Console.WriteLine($"Пропущен элемент {processed}: не является объектом");
                                    continue;
                                }

                                // Создаем словарь всех свойств объекта
                                var allProps = obj.Properties()
                                    .ToDictionary(p => p.Name, p => p.Value);

                                // Получаем Properties как JObject
                                var properties = new Dictionary<string, object>();
                                var propsObj = allProps.ContainsKey("Properties") ? allProps["Properties"] as JObject : null;
                                if (propsObj != null)
                                {
                                    foreach (var prop in propsObj.Properties())
                                    {
                                        properties[prop.Name] = prop.Value.ToObject<object>();
                                    }
                                }

                                // Извлекаем Id из Properties или генерируем новый
                                var id = (propsObj?["Id"]?.ToString()) ?? 
                                        (allProps.ContainsKey("Id") ? allProps["Id"]?.ToString() : null) ??
                                        $"obj_{processed}";

                                // Извлекаем тип
                                var type = (allProps.ContainsKey("Type") ? allProps["Type"]?.ToString() : null) ?? "Unknown";

                                // Извлекаем техническое имя из Properties
                                var techName = (propsObj?["TechnicalName"]?.ToString()) ?? 
                                             (allProps.ContainsKey("TechnicalName") ? allProps["TechnicalName"]?.ToString() : null) ?? 
                                             id;

                                // Извлекаем позицию из Properties
                                var position = new Dictionary<string, object>();
                                var positionObj = propsObj?["Position"] as JObject;
                                if (positionObj != null)
                                {
                                    position["x"] = positionObj["x"]?.ToObject<float>() ?? 0f;
                                    position["y"] = positionObj["y"]?.ToObject<float>() ?? 0f;
                                }

                                // Обрабатываем пины и связи
                                var connections = new List<ArticyXConnection>();
                                var pins = new List<ArticyXPin>();

                                // Входные пины
                                var inputPins = propsObj?["InputPins"] as JArray;
                                if (inputPins != null)
                                {
                                    foreach (JObject pinObj in inputPins)
                                    {
                                        var pinId = pinObj["Id"]?.ToString();
                                        if (!string.IsNullOrEmpty(pinId))
                                        {
                                            pins.Add(new ArticyXPin
                                            {
                                                Id = pinId,
                                                ObjectId = id,
                                                Properties = new Dictionary<string, object>
                                                {
                                                    ["Type"] = "Input",
                                                    ["Text"] = pinObj["Text"]?.ToString() ?? ""
                                                }
                                            });

                                            // Обрабатываем связи пина
                                            var pinConnections = pinObj["Connections"] as JArray;
                                            if (pinConnections != null)
                                            {
                                                foreach (JObject conn in pinConnections)
                                                {
                                                    var targetId = conn["Target"]?.ToString();
                                                    if (!string.IsNullOrEmpty(targetId))
                                                    {
                                                        connections.Add(new ArticyXConnection
                                                        {
                                                            SourceId = id,
                                                            TargetId = targetId,
                                                            Properties = new Dictionary<string, object>
                                                            {
                                                                ["SourcePin"] = pinId,
                                                                ["TargetPin"] = conn["TargetPin"]?.ToString(),
                                                                ["Label"] = conn["Label"]?.ToString() ?? ""
                                                            }
                                                        });
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                // Выходные пины
                                var outputPins = propsObj?["OutputPins"] as JArray;
                                if (outputPins != null)
                                {
                                    foreach (JObject pinObj in outputPins)
                                    {
                                        var pinId = pinObj["Id"]?.ToString();
                                        if (!string.IsNullOrEmpty(pinId))
                                        {
                                            pins.Add(new ArticyXPin
                                            {
                                                Id = pinId,
                                                ObjectId = id,
                                                Properties = new Dictionary<string, object>
                                                {
                                                    ["Type"] = "Output",
                                                    ["Text"] = pinObj["Text"]?.ToString() ?? ""
                                                }
                                            });

                                            // Обрабатываем связи пина
                                            var pinConnections = pinObj["Connections"] as JArray;
                                            if (pinConnections != null)
                                            {
                                                foreach (JObject conn in pinConnections)
                                                {
                                                    var targetId = conn["Target"]?.ToString();
                                                    if (!string.IsNullOrEmpty(targetId))
                                                    {
                                                        connections.Add(new ArticyXConnection
                                                        {
                                                            SourceId = id,
                                                            TargetId = targetId,
                                                            Properties = new Dictionary<string, object>
                                                            {
                                                                ["SourcePin"] = pinId,
                                                                ["TargetPin"] = conn["TargetPin"]?.ToString(),
                                                                ["Label"] = conn["Label"]?.ToString() ?? ""
                                                            }
                                                        });
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                var articyObj = new ArticyXObject
                                {
                                    Id = id,
                                    Type = type,
                                    TechnicalName = techName,
                                    Properties = properties,
                                    Position = position,
                                    Connections = connections,
                                    Pins = pins
                                };

                                objects.Add(articyObj);
                                withId++;

                                if (processed == 1)
                                {
                                    Console.WriteLine("\nПервый сконвертированный объект:");
                                    Console.WriteLine($"Id: {articyObj.Id}");
                                    Console.WriteLine($"Type: {articyObj.Type}");
                                    Console.WriteLine($"TechnicalName: {articyObj.TechnicalName}");
                                    Console.WriteLine("Properties:");
                                    foreach (var prop in articyObj.Properties.Take(5))
                                    {
                                        Console.WriteLine($"  {prop.Key}: {prop.Value}");
                                    }
                                    Console.WriteLine("Position:");
                                    foreach (var pos in articyObj.Position)
                                    {
                                        Console.WriteLine($"  {pos.Key}: {pos.Value}");
                                    }
                                }
                                else if (processed % 100 == 0)
                                {
                                    Console.WriteLine($"Обработано {processed} объектов");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка при обработке объекта {processed}:");
                                Console.WriteLine($"- Сообщение: {ex.Message}");
                                if (processed == 1)
                                {
                                    Console.WriteLine($"- Стек: {ex.StackTrace}");
                                    Console.WriteLine($"- Объект: {item.ToString(Formatting.None)}");
                                }
                            }
                        }

                        Console.WriteLine($"\nИтоги обработки objects.json:");
                        Console.WriteLine($"- Всего объектов: {processed}");
                        Console.WriteLine($"- Успешно обработано: {withId}");

                        return objects;
                    }
                    else
                    {
                        throw new Exception("Не найден массив объектов в JSON");
                    }
                }
                else
                {
                    throw new Exception($"Некорректная структура JSON: ожидался объект, получен {token.Type}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nОшибка при чтении objects.json:");
                Console.WriteLine($"- Тип: {ex.GetType().Name}");
                Console.WriteLine($"- Сообщение: {ex.Message}");
                Console.WriteLine($"- Стек: {ex.StackTrace}");
                throw;
            }
        }

        private Dictionary<string, string> ReadAndParseLocalization()
        {
            var path = Path.Combine(_sourceDirectory, "package_0100000000000102_localization.json");
            Console.WriteLine($"Чтение localization.json: {File.Exists(path)}");
            
            var localization = new Dictionary<string, string>();
            using (var file = File.OpenText(path))
            using (var reader = new JsonTextReader(file))
            {
                var serializer = new JsonSerializer();
                var rootObject = serializer.Deserialize<JObject>(reader);

                foreach (var prop in rootObject)
                {
                    if (prop.Value is JObject textObj && 
                        textObj[""] is JObject emptyObj &&
                        emptyObj["Text"] != null)
                    {
                        localization[prop.Key] = emptyObj["Text"].Value<string>();
                    }
                }
            }

            return localization;
        }

        private Dictionary<string, FlowGlobalVariable> ConvertGlobalVariables(ArticyXGlobalVariables vars)
        {
            var result = new Dictionary<string, FlowGlobalVariable>();
            foreach (var variable in vars.GlobalVariables)
            {
                var fullName = $"{variable.Namespace}.{variable.Variable}";
                result[fullName] = new FlowGlobalVariable
                {
                    Type = variable.Type,
                    DefaultValue = variable.Value ?? GetDefaultValue(variable.Type),
                    Description = variable.Description ?? ""
                };
            }
            return result;
        }

        private string GetDefaultValue(string type)
        {
            return type switch
            {
                "Integer" => "0",
                "Float" => "0.0",
                "Boolean" => "False",
                "String" => "",
                _ => null
            };
        }

        private string GetLocalizedText(string key)
        {
            return _localizationMap.TryGetValue(key, out var text) ? text : key;
        }
    }
} 

