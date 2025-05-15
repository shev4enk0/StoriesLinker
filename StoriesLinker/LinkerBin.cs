using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using OfficeOpenXml;

namespace StoriesLinker
{
    public class LinkerBin
    {
        #region Поля и инициализация

        private string _projectPath;
        private string _baseLanguage; // Базовый язык локализации
        private Dictionary<string, Dictionary<string, string>> _savedXMLDicts;
        private int _allWordsCount = 0;

        public LinkerBin(string projectPath)
        {
            _projectPath = projectPath;
            _savedXMLDicts = new Dictionary<string, Dictionary<string, string>>();
            
            // Автоматически определяем язык по найденному файлу
            DetermineBaseLanguage();
        }

        #endregion

        #region Работа с языками и локализацией

        // Метод для определения базового языка на основе найденного файла локализации
        private void DetermineBaseLanguage()
        {
            string locFilePath = GetLocalizTablesPath(_projectPath);
            
            if (locFilePath.Contains("loc_All objects_ru.xlsx"))
                _baseLanguage = "Russian";
            else if (locFilePath.Contains("loc_All objects_en.xlsx"))
                _baseLanguage = "English";
            else if (locFilePath.Contains("loc_All objects_pl.xlsx"))
                _baseLanguage = "Polish";
            else if (locFilePath.Contains("loc_All objects_de.xlsx"))
                _baseLanguage = "Deutsch";
            else if (locFilePath.Contains("loc_All objects_fr.xlsx"))
                _baseLanguage = "French";
            else if (locFilePath.Contains("loc_All objects_es.xlsx"))
                _baseLanguage = "Spanish";
            else if (locFilePath.Contains("loc_All objects_jp.xlsx"))
                _baseLanguage = "Japan";
            else
                _baseLanguage = "Russian"; // По умолчанию русский
            
            Console.WriteLine($"Определен базовый язык: {_baseLanguage} на основе файла {locFilePath}");
        }
        
        // Метод для ручной установки базового языка
        public void SetBaseLanguage(string language)
        {
            _baseLanguage = language;
        }

        // Получение пути к основному файлу локализации
        public static string GetLocalizTablesPath(string projPath)
        {
            // Пробуем варианты файлов
            string[] langVariants = { "en", "ru", "pl", "de", "fr", "es", "jp" };
            string path = "";
            
            foreach (string lang in langVariants)
            {
                path = projPath + $@"\Raw\loc_All objects_{lang}.xlsx";
                if (File.Exists(path)) 
                    return path;
            }
            
            // Если ничего не найдено, возвращаем путь к русскому файлу
            return projPath + @"\Raw\loc_All objects_ru.xlsx";
        }

        // Получение пути к файлу потока
        public static string GetFlowJsonPath(string projPath) => projPath + @"\Raw\Flow.json";

        // Получение пути к файлу метаданных
        public static string GetMetaJsonPath(string projPath) => projPath + @"\Raw\Meta.json";

        #endregion

        #region Работа с Excel таблицами

        // Преобразование Excel-таблицы в словарь
        private Dictionary<string, string> XMLTableToDict(string path, int column = 1)
        {
            if (_savedXMLDicts.TryGetValue(path, out Dictionary<string, string> dict)) return dict;

            var nativeDict = new Dictionary<string, string>();

            using (var xlPackage = new ExcelPackage(new FileInfo(path)))
            {
                ExcelWorksheet myWorksheet = xlPackage.Workbook.Worksheets.First();
                int totalRows = myWorksheet.Dimension.End.Row;
                int totalColumns = myWorksheet.Dimension.End.Column;

                for (var rowNum = 1; rowNum <= totalRows; rowNum++)
                {
                    ExcelRange firstRow = myWorksheet.Cells[rowNum, 1];
                    ExcelRange secondRow = myWorksheet.Cells[rowNum, column + 1];

                    string firstRowStr = firstRow != null && firstRow.Value != null
                                                ? firstRow.Value.ToString()
                                                : "";
                    string secondRowStr = secondRow != null && secondRow.Value != null
                                                 ? secondRow.Value.ToString()
                                                 : " ";

                    if (string.IsNullOrEmpty(firstRowStr)) continue;

                    if (!nativeDict.ContainsKey(firstRowStr))
                        nativeDict.Add(firstRowStr, secondRowStr);
                    else
                        Console.WriteLine("double key critical error " + firstRowStr);
                }
            }

            _savedXMLDicts.Add(path, nativeDict);

            return nativeDict;
        }

        // Специальная версия для обработки BookDescriptions для основного языка
        private Dictionary<string, string> XMLTableToDictBookDesc(string path)
        {
            if (_savedXMLDicts.TryGetValue(path, out Dictionary<string, string> dict)) return dict;

            var nativeDict = new Dictionary<string, string>();

            using (var xlPackage = new ExcelPackage(new FileInfo(path)))
            {
                ExcelWorksheet myWorksheet = xlPackage.Workbook.Worksheets.First();
                int totalRows = myWorksheet.Dimension.End.Row;
                int totalColumns = myWorksheet.Dimension.End.Column;

                for (var rowNum = 1; rowNum <= totalRows; rowNum++)
                {
                    ExcelRange firstRow = myWorksheet.Cells[rowNum, 1];  // Колонка A (ID)
                    ExcelRange columnD = myWorksheet.Cells[rowNum, 4];   // Колонка D
                    ExcelRange columnB = myWorksheet.Cells[rowNum, 2];   // Колонка B

                    string firstRowStr = firstRow != null && firstRow.Value != null
                                                ? firstRow.Value.ToString()
                                                : "";
                                                
                    if (string.IsNullOrEmpty(firstRowStr)) continue;

                    // Проверяем сначала колонку D, если пусто, берем из B
                    string valueStr;
                    if (columnD != null && columnD.Value != null && !string.IsNullOrWhiteSpace(columnD.Value.ToString()))
                    {
                        valueStr = columnD.Value.ToString();
                    }
                    else
                    {
                        valueStr = columnB != null && columnB.Value != null
                                      ? columnB.Value.ToString()
                                      : " ";
                    }

                    if (!nativeDict.ContainsKey(firstRowStr))
                        nativeDict.Add(firstRowStr, valueStr);
                    else
                        Console.WriteLine("double key critical error " + firstRowStr);
                }
            }

            _savedXMLDicts.Add(path, nativeDict);

            return nativeDict;
        }

        // Получение основного словаря локализации
        public Dictionary<string, string> GetNativeDict() => XMLTableToDict(GetLocalizTablesPath(_projectPath));

        #endregion

        #region Работа с JSON файлами

        public AjFile GetParsedFlowJsonFile()
        {
            AjFile jsonObj;

            using (var r = new StreamReader(GetFlowJsonPath(_projectPath)))
            {
                string json = r.ReadToEnd();
                jsonObj = JsonConvert.DeserializeObject<AjFile>(json);
            }

            return jsonObj;
        }

        public AjLinkerMeta GetParsedMetaInputJsonFile()
        {
            var jsonObj = new AjLinkerMeta { Version = new BookVersionInfo() };

            string metaXMLPath = _projectPath + @"\Raw\Meta.xlsx";

            using (var xlPackage = new ExcelPackage(new FileInfo(metaXMLPath)))
            {
                ExcelWorksheet myWorksheet = xlPackage.Workbook.Worksheets.First();
                int totalRows = myWorksheet.Dimension.End.Row;

                for (var rowNum = 2; rowNum <= totalRows; rowNum++)
                {
                    ExcelRange firstRow = myWorksheet.Cells[rowNum, 1];
                    ExcelRange secondRow = myWorksheet.Cells[rowNum, 2];

                    var fieldName = firstRow.Value.ToString();
                    var fieldValue = secondRow.Value.ToString();

                    string[] values;

                    switch (fieldName)
                    {
                        case "UniqueID": jsonObj.UniqueId = fieldValue; break;
                        case "SpritePrefix": jsonObj.SpritePrefix = fieldValue; break;
                        case "VersionBin": jsonObj.Version.BinVersion = fieldValue; break;
                        case "VersionPreview": jsonObj.Version.PreviewVersion = fieldValue; break;
                        case "VersionBaseResources": jsonObj.Version.BaseResourcesVersion = fieldValue; break;
                        case "StandartizedUI": jsonObj.StandartizedUi = fieldValue == "1"; break;
                        case "UITextBlockFontSize": jsonObj.UiTextBlockFontSize = int.Parse(fieldValue); break;
                        case "UIChoiceBlockFontSize": jsonObj.UiChoiceBlockFontSize = int.Parse(fieldValue); break;
                        case "KarmaCurrency": jsonObj.KarmaCurrency = fieldValue; break;
                        case "KarmaBadBorder": jsonObj.KarmaBadBorder = int.Parse(fieldValue); break;
                        case "KarmaGoodBorder": jsonObj.KarmaGoodBorder = int.Parse(fieldValue); break;
                        case "KarmaTopLimit": jsonObj.KarmaTopLimit = int.Parse(fieldValue); break;
                        case "CurrenciesInOrderOfUI":
                            jsonObj.CurrenciesInOrderOfUi = new List<string>(fieldValue.Split(','));
                            break;
                        case "RacesList":
                            jsonObj.RacesList = fieldValue != "-"
                                                      ? new List<string>(fieldValue.Split(','))
                                                      : new List<string>();
                            break;
                        case "ClothesSpriteNames":
                            jsonObj.ClothesSpriteNames = new List<string>(fieldValue.Split(','));
                            break;
                        case "UndefinedClothesFuncVariant":
                            jsonObj.UndefinedClothesFuncVariant = int.Parse(fieldValue);
                            break;
                        case "ExceptionsWeaponLayer": jsonObj.ExceptionsWeaponLayer = fieldValue == "1"; break;
                        case "UITextPlateLimits":
                            values = fieldValue.Split(',');

                            jsonObj.UiTextPlateLimits = new List<int>();

                            foreach (string el in values) jsonObj.UiTextPlateLimits.Add(int.Parse(el));

                            break;
                        case "UIPaintFirstLetterInRedException":
                            jsonObj.UiPaintFirstLetterInRedException = fieldValue == "1";
                            break;
                        case "UITextPlateOffset": jsonObj.UiTextPlateOffset = int.Parse(fieldValue); break;
                        case "UIOverridedTextColor": jsonObj.UiOverridedTextColor = fieldValue == "1"; break;
                        case "UITextColor":
                            values = fieldValue.Split(',');

                            jsonObj.UiTextColor = new List<int>();

                            foreach (string el in values) jsonObj.UiTextColor.Add(int.Parse(el));

                            break;
                        case "UIBlockedTextColor":
                            values = fieldValue.Split(',');

                            jsonObj.UiBlockedTextColor = new List<int>();

                            foreach (string el in values) jsonObj.UiBlockedTextColor.Add(int.Parse(el));

                            break;
                        case "UIChNameTextColor":
                            values = fieldValue.Split(',');

                            jsonObj.UiChNameTextColor = new List<int>();

                            foreach (string el in values) jsonObj.UiChNameTextColor.Add(int.Parse(el));

                            break;
                        case "UIOutlineColor":
                            values = fieldValue.Split(',');

                            jsonObj.UiOutlineColor = new List<int>();

                            foreach (string el in values) jsonObj.UiOutlineColor.Add(int.Parse(el));

                            break;
                        case "UIResTextColor":
                            values = fieldValue.Split(',');

                            jsonObj.UiResTextColor = new List<int>();

                            foreach (string el in values) jsonObj.UiResTextColor.Add(int.Parse(el));

                            break;
                        case "WardrobeEnabled": jsonObj.WardrobeEnabled = fieldValue == "1"; break;
                        case "MainHeroHasDifferentGenders":
                            jsonObj.MainHeroHasDifferentGenders = fieldValue == "1";
                            break;
                        case "MainHeroHasSplittedHairSprite":
                            jsonObj.MainHeroHasSplittedHairSprite = fieldValue == "1";
                            break;
                        case "CustomClothesCount": jsonObj.CustomClothesCount = int.Parse(fieldValue); break;
                        case "CustomHairsCount": jsonObj.CustomHairCount = int.Parse(fieldValue); break;
                    }
                }

                myWorksheet = xlPackage.Workbook.Worksheets[2];
                totalRows = myWorksheet.Dimension.End.Row;

                Func<object[], int> checkRow = CheckRow();

                var characters = new List<AjMetaCharacterData>();

                for (var rowNum = 2; rowNum <= totalRows; rowNum++)
                {
                    var cells = new object[]
                                 {
                                     myWorksheet.Cells[rowNum, 1].Value,
                                     myWorksheet.Cells[rowNum, 2].Value,
                                     myWorksheet.Cells[rowNum, 3].Value,
                                     myWorksheet.Cells[rowNum, 4].Value
                                 };

                    int chResult = checkRow(cells);

                    switch (chResult)
                    {
                        case -1: continue;
                        case 0: return null;
                    }

                    var ch = new AjMetaCharacterData
                             {
                                 DisplayName = cells[0].ToString(),
                                 ClothesVariableName = cells[1].ToString(),
                                 AtlasFileName = cells[2].ToString(),
                                 BaseNameInAtlas = cells[3].ToString()
                             };

                    characters.Add(ch);
                }

                jsonObj.Characters = characters;

                myWorksheet = xlPackage.Workbook.Worksheets[3];
                totalRows = myWorksheet.Dimension.End.Row;

                var locations = new List<AjMetaLocationData>();

                for (var rowNum = 2; rowNum <= totalRows; rowNum++)
                {
                    var cells = new object[]
                                 {
                                     myWorksheet.Cells[rowNum, 1].Value,
                                     myWorksheet.Cells[rowNum, 2].Value,
                                     myWorksheet.Cells[rowNum, 3].Value,
                                     myWorksheet.Cells[rowNum, 4].Value,
                                     myWorksheet.Cells[rowNum, 5].Value
                                 };

                    int chResult = checkRow(cells);

                    switch (chResult)
                    {
                        case -1: continue;
                        case 0: return null;
                    }

                    var loc = new AjMetaLocationData
                               {
                                   Id = int.Parse(cells[0].ToString()),
                                   DisplayName = cells[1].ToString(),
                                   SpriteName = cells[2].ToString(),
                                   SoundIdleName = cells[3].ToString()
                               };

                    if (cells[4].ToString() == "1") jsonObj.IntroLocation = rowNum - 1;

                    locations.Add(loc);
                }

                jsonObj.Locations = locations;
            }

            return jsonObj;
        }

        // Проверка строки в Excel таблице
        private static Func<object[], int> CheckRow()
        {
            int Row(object[] cells)
            {
                var rowIsCompletelyEmpty = true;
                var rowHasEmptyField = false;

                foreach (object cell in cells)
                    if (cell == null || string.IsNullOrEmpty(cell.ToString().Trim()))
                        rowHasEmptyField = true;
                    else
                        rowIsCompletelyEmpty = false;

                if (rowIsCompletelyEmpty)
                    return -1;
                else if (rowHasEmptyField) return 0;

                return 1;
            }

            return Row;
        }

        private AjLocalizInJsonFile GetXMLFile(string[] pathsToXmls, int column)
        {
            var total = new Dictionary<string, string>();

            foreach (string el in pathsToXmls)
            {
                Dictionary<string, string> fileDict;
                
                // Проверяем, является ли файл описанием книги для основного языка
                if (el.Contains("BookDescriptions") && el.Contains(_baseLanguage))
                {
                    // Для основного языка используем специальный метод чтения BookDescriptions
                    fileDict = XMLTableToDictBookDesc(el);
                }
                else
                {
                    // Для других файлов используем стандартный метод
                    fileDict = XMLTableToDict(el, column);
                }

                foreach (KeyValuePair<string, string> pair in fileDict.Where(pair => pair.Key != "ID"))
                    total.Add(pair.Key, pair.Value);
            }

            var jsonFile = new AjLocalizInJsonFile { Data = total };

            return jsonFile;
        }

        private AjLocalizInJsonFile WriteJsonFile(AjLocalizInJsonFile jsonFile, string pathToJson)
        {
            File.WriteAllText(pathToJson, JsonConvert.SerializeObject(jsonFile));

            return jsonFile;
        }

        public AjLocalizInJsonFile ConvertXMLToJson(string[] pathsToXmls, string pathToJson, int column)
        {
            var total = new Dictionary<string, string>();

            foreach (string el in pathsToXmls)
            {
                Dictionary<string, string> fileDict = XMLTableToDict(el, column);

                foreach (KeyValuePair<string, string> pair in fileDict.Where(pair => pair.Key != "ID"))
                    total.Add(pair.Key, pair.Value);
            }

            var jsonFile = new AjLocalizInJsonFile();
            jsonFile.Data = total;

            File.WriteAllText(pathToJson, JsonConvert.SerializeObject(jsonFile));

            return jsonFile;
        }

        #endregion

        #region Работа с сущностями и главами

        public Dictionary<string, AjObj> GetAricyBookEntities(AjFile ajfile, Dictionary<string, string> nativeDict)
        {
            var objectsList = new Dictionary<string, AjObj>();

            List<AjObj> models = ajfile.Packages[0].Models;

            foreach (AjObj ns in models)
            {
                objectsList.Add(ns.Properties.Id, ns);
            }

            return objectsList;
        }

        private List<string> GetSortedChaptersList(Dictionary<string, AjObj> objList,
                                                   Dictionary<string, string> nativeDict)
        {
            var chaptersIds = new List<string>();

            var chaptersIDNames = new Dictionary<string, int>();

            foreach (KeyValuePair<string, AjObj> kobj in objList)
            {
                if (kobj.Value.EType != AjType.FlowFragment) continue;

                string value = Regex.Match(nativeDict[kobj.Value.Properties.DisplayName], @"\d+").Value;

                int intValue = int.Parse(value);

                chaptersIDNames.Add(kobj.Value.Properties.Id, intValue);
            }

            IOrderedEnumerable<KeyValuePair<string, int>> sortedChapterNames =
                from entry in chaptersIDNames orderby entry.Value ascending select entry;

            foreach (KeyValuePair<string, int> pair in sortedChapterNames) chaptersIds.Add(pair.Key);

            return chaptersIds;
        }

        private List<string>[] GetChaptersAndSubchaptersParentsIDs(List<string> chaptersIds,
                                                                   Dictionary<string, AjObj> objList)
        {
            var ids = new List<List<string>>();

            for (var i = 0; i < chaptersIds.Count; i++)
            {
                string chapterID = chaptersIds[i];

                ids.Add(new List<string>());
                ids[i].Add(chapterID);

                foreach (KeyValuePair<string, AjObj> kobj in objList)
                {
                    if (kobj.Value.EType != AjType.Dialogue) continue; //subchapter 

                    string subchapterID = kobj.Value.Properties.Id;

                    string parent = kobj.Value.Properties.Parent;

                    while (true)
                        if (parent == chapterID)
                        {
                            ids[i].Add(subchapterID);
                            break;
                        }
                        else
                        {
                            if (objList.TryGetValue(parent, out AjObj value))
                                parent = value.Properties.Parent;
                            else
                                break;
                        }
                }
            }

            return ids.ToArray();
        }

        private enum EChEmotion
        {
            Angry, //red
            Happy, //green
            Sad, //purple
            Surprised, //yellow
            IsntSetOrNeutral //blue
        }

        #endregion

        #region Генерация таблиц локализации

        /// <summary>
        /// Генерирует таблицы локализации на основе данных книги
        /// </summary>
        public bool GenerateLocalizTables()
        {
            if (Directory.Exists(_projectPath + @"\Localization"))
                Directory.Delete(_projectPath + @"\Localization", true);

            Directory.CreateDirectory(_projectPath + @"\Localization");
            Directory.CreateDirectory(_projectPath + @"\Localization\" + _baseLanguage);

            AjFile ajfile = GetParsedFlowJsonFile();

            Dictionary<string, string> nativeDict = GetNativeDict();
            Dictionary<string, AjObj> objectsList = GetAricyBookEntities(ajfile, nativeDict);

            List<string> chaptersIds = GetSortedChaptersList(objectsList, nativeDict);

            if (chaptersIds.Count < Form1.AvailableChapters)
            {
                Form1.ShowMessage("Глав в книге меньше введённого количества");

                return false;
            }

            chaptersIds.RemoveRange(Form1.AvailableChapters, chaptersIds.Count - Form1.AvailableChapters);

            string RecognizeEmotion(AjColor color)
            {
                var emotion = EChEmotion.IsntSetOrNeutral;

                bool ColorsEquals(Color32 a, Color32 b) =>
                    Math.Abs(a.R - b.R) < 20 && Math.Abs(a.G - b.G) < 20 && Math.Abs(a.B - b.B) < 20;

                var fragColor = color.ToColor32();
                var emotionsColor = new Color32[]
                                    {
                                        new Color32(255, 0, 0, 0),
                                        new Color32(0, 110, 20, 0),
                                        new Color32(41, 6, 88, 0),
                                        new Color32(255, 134, 0, 0)
                                    };

                for (var i = 0; i < emotionsColor.Length; i++)
                {
                    if (!ColorsEquals(emotionsColor[i], fragColor)) continue;
                    
                    emotion = (EChEmotion)i;
                    break;
                }

                return emotion.ToString();
            }

            List<string>[] csparentsIds = GetChaptersAndSubchaptersParentsIDs(chaptersIds, objectsList);

            var charactersIds = new List<string>();
            var charactersLocalizIds = new List<LocalizEntity>();

            var charactersNames = new Dictionary<string, string>();

            for (var i = 0; i < csparentsIds.Length; i++)
            {
                int chapterN = i + 1;

                var forTranslating = new List<LocalizEntity>();
                var nonTranslating = new List<LocalizEntity>();
                List<string> parentsIds = csparentsIds[i];

                foreach (KeyValuePair<string, AjObj> scobj in objectsList)
                    if (parentsIds.Contains(scobj.Value.Properties.Parent))
                    {
                        AjObj dfobj = scobj.Value;

                        if (dfobj.EType != AjType.DialogueFragment) continue;

                        string chID = dfobj.Properties.Speaker;

                        if (!charactersIds.Contains(chID))
                        {
                            var entity = new LocalizEntity();

                            entity.LocalizID = objectsList[chID].Properties.DisplayName;

                            charactersIds.Add(chID);
                            charactersLocalizIds.Add(entity);

                            charactersNames.Add(chID, nativeDict[objectsList[chID].Properties.DisplayName]);
                        }


                        if (!string.IsNullOrEmpty(dfobj.Properties.Text))
                        {
                            var entity = new LocalizEntity
                                         {
                                             LocalizID = dfobj.Properties.Text, SpeakerDisplayName = charactersNames[chID],
                                             Emotion = RecognizeEmotion(dfobj.Properties.Color)
                                         };

                            forTranslating.Add(entity);
                        }

                        if (!string.IsNullOrEmpty(dfobj.Properties.MenuText))
                        {
                            var entity = new LocalizEntity
                                         {
                                             LocalizID = dfobj.Properties.MenuText, SpeakerDisplayName = charactersNames[chID],
                                             Emotion = RecognizeEmotion(dfobj.Properties.Color)
                                         };

                            forTranslating.Add(entity);
                        }

                        if (!string.IsNullOrEmpty(dfobj.Properties.StageDirections))
                        {
                            var entity = new LocalizEntity
                                         {
                                             LocalizID = dfobj.Properties.StageDirections, SpeakerDisplayName = ""
                                         };

                            nonTranslating.Add(entity);
                        }
                    }

                CreateLocalizTable($"Chapter_{chapterN}_for_translating",
                                   forTranslating,
                                   nativeDict);
                CreateLocalizTable($"Chapter_{chapterN}_internal", nonTranslating, nativeDict);
            }

            CreateLocalizTable(string.Format("CharacterNames"), charactersLocalizIds, nativeDict);

            return true;
        }

        /// <summary>
        /// Создает таблицу локализации в формате Excel
        /// </summary>
        private void CreateLocalizTable(string name, List<LocalizEntity> ids, Dictionary<string, string> nativeDict)
        {
            var wordCount = 0;

            using (var eP = new ExcelPackage())
            {
                bool forTranslating = name.Contains("for_translating");
                ExcelWorksheet sheet = eP.Workbook.Worksheets.Add("Data");

                bool forLocalizatorsMode = Form1.FOR_LOCALIZATORS_MODE;

                var row = 1;
                var col = 1;

                sheet.Cells[row, col].Value = "ID";

                if (forLocalizatorsMode)
                {
                    sheet.Cells[row, col + 1].Value = "Speaker";
                    sheet.Cells[row, col + 2].Value = "Emotion";
                }

                sheet.Cells[row, col + (forLocalizatorsMode ? 3 : 1)].Value = "Text";

                row++;

                var replacedIds = new List<string>();

                foreach (LocalizEntity item in ids)
                {
                    string id = item.LocalizID;

                    string value = nativeDict[id];

                    if (forTranslating && forLocalizatorsMode)
                    {
                        value = value.Replace("pname", "%pname%");
                        value = value.Replace("Pname", "%pname%");

                        if (!replacedIds.Contains(id))
                        {
                            var repeatedValues = new List<string>();

                            foreach (KeyValuePair<string, string> pair in nativeDict)
                                if (pair.Value == value && pair.Key != id)
                                    repeatedValues.Add(pair.Key);

                            if (repeatedValues.Count == 1 || (repeatedValues.Count > 1 && value.Contains("?")))
                            {
                                foreach (string el in repeatedValues)
                                {
                                    nativeDict[el] = "*SystemLinkTo*" + id + "*";
                                    replacedIds.Add(el);
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(value.Trim())) continue;

                    sheet.Cells[row, col].Value = item.LocalizID;

                    if (forLocalizatorsMode)
                    {
                        sheet.Cells[row, col + 1].Value = item.SpeakerDisplayName;
                        sheet.Cells[row, col + 2].Value = item.Emotion;
                    }

                    sheet.Cells[row, col + (forLocalizatorsMode ? 3 : 1)].Value = value;

                    if (forLocalizatorsMode && !replacedIds.Contains(id)) wordCount += CountWords(value);

                    row++;
                }

                byte[] bin = eP.GetAsByteArray();

                File.WriteAllBytes(_projectPath + @"\Localization\" + _baseLanguage + @"\" + name + ".xlsx", bin);

                if (name.Contains("internal") || !forLocalizatorsMode) return;

                Console.WriteLine("Таблица " + name + " сгенерирована, количество слов: " + wordCount);

                _allWordsCount += wordCount;

                if (name.Contains("12")) Console.WriteLine("total count = " + _allWordsCount);
            }
        }

        /// <summary>
        /// Подсчет слов в тексте
        /// </summary>
        private int CountWords(string text)
        {
            int wordCount = 0, index = 0;

            // skip whitespace until first word
            while (index < text.Length && char.IsWhiteSpace(text[index])) index++;

            while (index < text.Length)
            {
                // check if current char is part of a word
                while (index < text.Length && !char.IsWhiteSpace(text[index])) index++;

                wordCount++;

                // skip whitespace until next word
                while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
            }

            return wordCount;
        }

        #endregion

        #region Генерация выходных файлов

        /// <summary>
        /// Генерирует папки с выходными файлами для игры
        /// </summary>
        public bool GenerateOutputFolder()
        {
            Form1.ShowMessage("Начинаем...");

            string tempFolder = _projectPath + @"\Temp\";

            AjFile ajfile = GetParsedFlowJsonFile();
            AjLinkerMeta meta = GetParsedMetaInputJsonFile();

            if (meta == null)
            {
                Form1.ShowMessage("Таблица содержит пустые поля в листе Characters или Locations.");

                return false;
            }

            // Проверка персонажей
            if (!CheckCharacters(meta, ajfile))
                return false;

            // Проверка локаций
            if (!CheckLocations(meta))
                return false;

            Func<string, string, string> getVersionName = GetVersionName();

            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);

            Directory.CreateDirectory(tempFolder);

            string binFolder = tempFolder + getVersionName("bin", meta.Version.BinVersion);
            string brFolder = tempFolder + getVersionName("baseResources", meta.Version.BaseResourcesVersion);
            string previewFolder = tempFolder + getVersionName("preview", meta.Version.PreviewVersion);

            Directory.CreateDirectory(previewFolder);
            Directory.CreateDirectory(previewFolder + @"\Covers");
            Directory.CreateDirectory(previewFolder + @"\Strings");

            Directory.CreateDirectory(binFolder);
            Directory.CreateDirectory(binFolder + @"\SharedStrings");
            Directory.CreateDirectory(brFolder);
            Directory.CreateDirectory(brFolder + @"\UI");
            Directory.CreateDirectory(brFolder + @"\Music");

            Dictionary<string, string> nativeDict = GetNativeDict();
            Dictionary<string, AjObj> objectsList = GetAricyBookEntities(ajfile, nativeDict);

            List<string> chaptersIds = GetSortedChaptersList(objectsList, nativeDict);

            if (chaptersIds.Count < Form1.AvailableChapters)
            {
                Form1.ShowMessage("Глав в книге меньше введённого количества");

                return false;
            }

            chaptersIds.RemoveRange(Form1.AvailableChapters, chaptersIds.Count - Form1.AvailableChapters);

            List<string>[] csparentsIds = GetChaptersAndSubchaptersParentsIDs(chaptersIds, objectsList);

            var gridLinker = new AjAssetGridLinker();

            Action<string> checkAddCh = CheckAddCh(nativeDict, objectsList, meta, gridLinker);

            Action<int> checkAddLocINT = CheckAddLocINT(meta, gridLinker);

            Action<string> checkAddLoc = CheckAddLoc(nativeDict, objectsList, meta, gridLinker);

            var copiedChAtlasses = new List<string>();
            var copiedLocSprites = new List<string>();
            var copiedLocIdles = new List<string>();

            var sharedObjs = new List<AjObj>();

            ProcessSharedObjects(objectsList, nativeDict, meta, sharedObjs);

            string translatedDataFolder = _projectPath + @"\TranslatedData\";

            bool multiLangOutput = Directory.Exists(translatedDataFolder);

            var langsCols = new Dictionary<string, int> { { _baseLanguage, multiLangOutput ? 3 : 1 } };

            if (multiLangOutput)
            {
                // Определение доступных языков по наличию папок в TranslatedData
                foreach (string langDir in Directory.GetDirectories(translatedDataFolder))
                {
                    string langName = Path.GetFileName(langDir);
                    if (langName != _baseLanguage && !langsCols.ContainsKey(langName))
                    {
                        langsCols.Add(langName, 4);
                        Console.WriteLine($"Добавлен язык для локализации: {langName}");
                    }
                }
            }

            meta.ChaptersEntryPoints = new List<string>();

            var gridAssetFile = new AjGridAssetJson();

            var allDicts = new Dictionary<string, Dictionary<string, string>>();

            // Обработка глав
            ProcessChapters(csparentsIds, 
                            gridLinker, 
                            meta, 
                            objectsList, 
                            parentsIds => ProcessChapterObjects(parentsIds, objectsList, checkAddCh, checkAddLoc, checkAddLocINT), 
                            tempFolder, 
                            getVersionName, 
                            allDicts, 
                            copiedChAtlasses, 
                            copiedLocSprites, 
                            copiedLocIdles, 
                            gridAssetFile, 
                            langsCols
            );

            // Запись общих файлов
            WriteSharedFiles(binFolder, brFolder, previewFolder, ajfile, meta, gridAssetFile, sharedObjs);

            // Копирование ресурсов
            CopyResources(musicSourcePath: _projectPath + @"\Audio\Music", 
                          musicTempPath: brFolder + @"\Music", 
                          pcoversSourcePath: _projectPath + @"\Art\PreviewCovers", 
                          pcoversTempPath: previewFolder + @"\Covers", 
                          pbannersSourcePath: _projectPath + @"\Art\SliderBanners", 
                          previewFolder: previewFolder
            );

            return true;
        }

        #endregion

        #region Вспомогательные методы

        // Проверка персонажей
        private bool CheckCharacters(AjLinkerMeta meta, AjFile ajfile)
        {
            for (var i = 0; i < meta.Characters.Count; i++)
            {
                AjMetaCharacterData cObj = meta.Characters[i];

                for (var j = 0; j < meta.Characters.Count; j++)
                {
                    if (i == j) continue;

                    AjMetaCharacterData aObj = meta.Characters[j];

                    if (cObj.DisplayName != aObj.DisplayName &&
                        (cObj.BaseNameInAtlas != aObj.BaseNameInAtlas ||
                         cObj.BaseNameInAtlas == "-" ||
                         meta.UniqueId == "Shism_1") &&
                        (cObj.ClothesVariableName != aObj.ClothesVariableName ||
                         cObj.ClothesVariableName.Trim() == "-"))
                        continue;

                    Form1.ShowMessage("Найдены дублирующиеся значения среди персонажей: " + aObj.DisplayName);

                    return false;
                }

                if (cObj.AtlasFileName.Contains("Sec_") || cObj.BaseNameInAtlas.Contains("Sec_"))
                {
                    if (cObj.AtlasFileName != cObj.BaseNameInAtlas)
                    {
                        Form1.ShowMessage("AtlasFileName и BaseNameInAtlas у второстепенных должны быть одинаковы: " +
                                          cObj.DisplayName);

                        return false;
                    }
                }

                int clothesNsIndex = ajfile.GlobalVariables.FindIndex(ns => ns.Namespace == "Clothes");

                bool state1 = clothesNsIndex != -1;
                bool state2 = ajfile.GlobalVariables[clothesNsIndex]
                                     .Variables
                                     .FindIndex(v => v.Variable == cObj.ClothesVariableName) !=
                              -1;
                if (cObj.ClothesVariableName.Trim() == "-" || (state1 && state2)) continue;

                Form1.ShowMessage("В артиси не определена переменная с именем Clothes." + cObj.ClothesVariableName);

                return false;
            }
            return true;
        }

        // Проверка локаций
        private bool CheckLocations(AjLinkerMeta meta)
        {
            if (meta.UniqueId != "Pirates_1")
            {
                for (var i = 0; i < meta.Locations.Count; i++)
                {
                    AjMetaLocationData cObj = meta.Locations[i];

                    for (var j = 0; j < meta.Locations.Count; j++)
                    {
                        if (i == j) continue;

                        AjMetaLocationData aObj = meta.Locations[j];

                        if (cObj.DisplayName != aObj.DisplayName && cObj.SpriteName != aObj.SpriteName)
                            continue;

                        Form1.ShowMessage("Найдены дублирующиеся значения среди локаций: " + aObj.DisplayName);
                        return false;
                    }
                }
            }
            return true;
        }

        // Обработка общих объектов
        private void ProcessSharedObjects(Dictionary<string, AjObj> objectsList, Dictionary<string, string> nativeDict, AjLinkerMeta meta, List<AjObj> sharedObjs)
        {
            foreach (KeyValuePair<string, AjObj> pair in objectsList)
            {
                if (pair.Value.EType != AjType.Entity && pair.Value.EType != AjType.Location) continue;

                string dname = nativeDict[pair.Value.Properties.DisplayName];

                if (pair.Value.EType == AjType.Entity)
                {
                    int index = meta.Characters.FindIndex(ch => ch.DisplayName == dname);

                    if (index != -1) meta.Characters[index].Aid = pair.Key;
                }
                else
                {
                    int index = meta.Locations.FindIndex(loc => loc.DisplayName == dname);

                    if (index != -1) meta.Locations[index].Aid = pair.Key;
                }

                sharedObjs.Add(pair.Value);
            }

            foreach (AjMetaLocationData el in meta.Locations.Where(el => string.IsNullOrEmpty(el.Aid)))
                el.Aid = "fake_location_aid" + el.Id;
        }

        // Обработка объектов в главе
        private void ProcessChapterObjects(List<string> parentsIds, Dictionary<string, AjObj> objectsList, 
                                          Action<string> checkAddCh, Action<string> checkAddLoc, Action<int> checkAddLocINT)
        {
            foreach (KeyValuePair<string, AjObj> pair in objectsList)
            {
                if (!parentsIds.Contains(pair.Value.Properties.Parent) &&
                    !parentsIds.Contains(pair.Value.Properties.Id))
                    continue;

                AjObj dfobj = pair.Value;

                switch (dfobj.EType)
                {
                    case AjType.DialogueFragment:
                    {
                        string chID = dfobj.Properties.Speaker;

                        checkAddCh(chID);
                        break;
                    }
                    case AjType.Dialogue:
                    {
                        List<string> attachments = dfobj.Properties.Attachments;

                        foreach (string el in attachments)
                        {
                            AjObj atObj = objectsList[el];

                            switch (atObj.EType)
                            {
                                case AjType.Location: checkAddLoc(el); break;
                                case AjType.Entity: checkAddCh(el); break;
                            }
                        }

                        break;
                    }
                    case AjType.Instruction:
                    {
                        string rawScript = dfobj.Properties.Expression;

                        if (rawScript.Contains("Location.loc"))
                        {
                            string[] scripts = rawScript.Replace("\\n", "")
                                                           .Replace("\\r", "")
                                                           .Split(';');

                            foreach (string uscript in scripts)
                            {
                                if (!uscript.Contains("Location.loc")) continue;

                                string[] parts = uscript.Split('=');
                                int locID = int.Parse(parts[1].Trim());
                                checkAddLocINT(locID);
                            }
                        }

                        break;
                    }
                }
            }
        }

        // Обработка глав
        private void ProcessChapters(List<string>[] csparentsIds, AjAssetGridLinker gridLinker, AjLinkerMeta meta, 
                                     Dictionary<string, AjObj> objectsList, Action<List<string>> processChapterObjects,
                                     string tempFolder, Func<string, string, string> getVersionName,
                                     Dictionary<string, Dictionary<string, string>> allDicts, 
                                     List<string> copiedChAtlasses, List<string> copiedLocSprites, List<string> copiedLocIdles,
                                     AjGridAssetJson gridAssetFile, Dictionary<string, int> langsCols)
        {
            // Обработка по главам
            for (var i = 0; i < csparentsIds.Length; i++)
            {
                gridLinker.AddChapter();

                int chapterN = i + 1;
                List<string> parentsIds = csparentsIds[i];

                meta.ChaptersEntryPoints.Add(parentsIds[0]);

                var chapterObjs = new List<AjObj>();

                // Обработка объектов в главе
                foreach (KeyValuePair<string, AjObj> pair in objectsList)
                {
                    if (!parentsIds.Contains(pair.Value.Properties.Parent) &&
                        !parentsIds.Contains(pair.Value.Properties.Id))
                        continue;

                    chapterObjs.Add(pair.Value);
                }

                processChapterObjects(parentsIds);

                // Создание файлов главы
                var flowJson = new AjLinkerOutputChapterFlow { Objects = chapterObjs };

                string chapterFolder = tempFolder + getVersionName("chapter" + chapterN, meta.Version.BinVersion);
                string binFolder = tempFolder + getVersionName("bin", meta.Version.BinVersion);
                string previewFolder = tempFolder + getVersionName("preview", meta.Version.PreviewVersion);

                Directory.CreateDirectory(chapterFolder);
                Directory.CreateDirectory(chapterFolder + @"\Resources");
                Directory.CreateDirectory(chapterFolder + @"\Strings");

                File.WriteAllText(chapterFolder + @"\Flow.json", JsonConvert.SerializeObject(flowJson));

                // Копирование ресурсов
                CopyChapterResources(chapterN, gridLinker, meta, chapterFolder, copiedChAtlasses, copiedLocSprites, copiedLocIdles);

                // Создание таблиц локализации
                CreateLocalizationTables(chapterN, gridLinker, meta, chapterFolder, allDicts, langsCols, gridAssetFile, binFolder, previewFolder);
            }
        }

        // Создание таблиц локализации
        private void CreateLocalizationTables(int chapterN, AjAssetGridLinker gridLinker, AjLinkerMeta meta, 
                                              string chapterFolder, Dictionary<string, Dictionary<string, string>> allDicts,
                                              Dictionary<string, int> langsCols, AjGridAssetJson gridAssetFile,
                                              string binFolder, string previewFolder)
        {
            var gridAssetChapter = new AjGridAssetChapterJson
                                      {
                                          CharactersIDs = gridLinker.GetCharactersIDsFromCurChapter(),
                                          LocationsIDs = gridLinker.GetLocationsIDsFromCurChapter()
                                      };

            gridAssetFile.Chapters.Add(gridAssetChapter);

            var origLangData = new Dictionary<string, AjLocalizInJsonFile>();

            Func<string, string, string[], string, int, string> generateLjson =
                GenerateLjson(allDicts, origLangData);

            string langOriginFolder = _projectPath + @"\Localization\" + _baseLanguage;

            Action<string, string> showLocalizError = ShowLocalizError();

            if (Form1.ONLY_ENGLISH_MODE)
                if (!langsCols.ContainsKey("English"))
                    langsCols.Add("English", -1);

            foreach (KeyValuePair<string, int> langPair in langsCols)
            {
                string lang = langPair.Key;
                int colNum = langPair.Value;

                bool nativeLang = lang == _baseLanguage || colNum == -1;

                string langFolder = nativeLang ? langOriginFolder : _projectPath + @"\TranslatedData\" + lang;
                string bookDescsPath;
                
                if (nativeLang)
                {
                    // Для основного языка ищем файл в папке Raw/BookDescriptions
                    bookDescsPath = _projectPath + @"\Raw\BookDescriptions\" + lang + ".xlsx";
                }
                else
                {
                    // Для дополнительных языков проверяем, есть ли файл в папке с переводом
                    string translatedBookPath = _projectPath + @"\TranslatedData\" + lang + @"\" + lang + ".xlsx";
                    
                    if (File.Exists(translatedBookPath))
                    {
                        bookDescsPath = translatedBookPath;
                    }
                    else
                    {
                        // Если файла в папке перевода нет, используем файл из основной папки
                        bookDescsPath = _projectPath + @"\Raw\BookDescriptions\" + lang + ".xlsx";
                    }
                }

                Console.WriteLine("GENERATE TABLES FOR LANGUAGE: " + lang);

                if (!Directory.Exists(langFolder)) continue;

                var langFiles = new string[]
                                  {
                                      string.Format(langFolder + @"\Chapter_{0}_for_translating.xlsx", chapterN),
                                      string.Format(langOriginFolder + @"\Chapter_{0}_internal.xlsx", chapterN)
                                  };

                if (!File.Exists(langFiles[0])) break;

                string correct = generateLjson(lang,
                                                  "chapter" + chapterN,
                                                  langFiles,
                                                  chapterFolder + @"\Strings\" + lang + ".json",
                                                  colNum != -1 ? colNum : 1);

                if (!string.IsNullOrEmpty(correct))
                {
                    showLocalizError(correct, "chapter" + chapterN);
                }

                if (chapterN != 1) continue;

                var sharedLangFiles = new string[]
                                         {
                                             string.Format(langFolder + @"\CharacterNames.xlsx", chapterN),
                                             bookDescsPath
                                         };

                Console.WriteLine("generate sharedstrings " + bookDescsPath);

                correct = generateLjson(lang,
                                           "sharedstrings",
                                           sharedLangFiles,
                                           binFolder + @"\SharedStrings\" + lang + ".json",
                                           colNum != -1 ? colNum : 1);

                var stringToPreviewFile = new string[] { bookDescsPath };

                if (!string.IsNullOrEmpty(correct))
                {
                    showLocalizError(correct, "sharedstrings");
                    throw new Exception("Ошибка при генерации sharedstrings");
                }

                correct = generateLjson(lang,
                                           "previewstrings",
                                           stringToPreviewFile,
                                           previewFolder + @"\Strings\" + lang + ".json",
                                           colNum != -1 ? colNum : 1);

                if (!string.IsNullOrEmpty(correct))
                {
                    showLocalizError(correct, "previewstrings");
                    throw new Exception("Ошибка при генерации previewstrings");
                }
            }
        }

        // Запись общих файлов
        private void WriteSharedFiles(string binFolder, string brFolder, string previewFolder, AjFile ajfile, 
                                     AjLinkerMeta meta, AjGridAssetJson gridAssetFile, List<AjObj> sharedObjs)
        {
            var baseJson = new AjLinkerOutputBase
                             {
                                 GlobalVariables = ajfile.GlobalVariables, 
                                 SharedObjs = sharedObjs
                             };

            File.WriteAllText(binFolder + @"\Base.json", JsonConvert.SerializeObject(baseJson));
            File.WriteAllText(binFolder + @"\Meta.json", JsonConvert.SerializeObject(meta));
            File.WriteAllText(binFolder + @"\AssetsByChapters.json", JsonConvert.SerializeObject(gridAssetFile));
        }

        // Копирование ресурсов
        private void CopyResources(string musicSourcePath, string musicTempPath, 
                                  string pcoversSourcePath, string pcoversTempPath,
                                  string pbannersSourcePath, string previewFolder)
        {
            // Копирование музыки
            if (!Directory.Exists(musicTempPath)) Directory.CreateDirectory(musicTempPath);

            foreach (string srcPath in Directory.GetFiles(musicSourcePath))
                File.Copy(srcPath, srcPath.Replace(musicSourcePath, musicTempPath), true);

            // Копирование обложек
            if (!Directory.Exists(pcoversTempPath)) Directory.CreateDirectory(pcoversTempPath);

            if (!File.Exists(pcoversSourcePath + @"\Russian\PreviewCover.png"))
            {
                Form1.ShowMessage("Не все preview обложки присуствуют.");
                throw new Exception("Отсутствуют preview обложки");
            }

            foreach (string dirPath in Directory.GetDirectories(pcoversSourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(pcoversSourcePath, pcoversTempPath));

            foreach (string newPath in Directory.GetFiles(pcoversSourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(pcoversSourcePath, pcoversTempPath), true);

            // Копирование баннеров
            if (Directory.Exists(pbannersSourcePath))
            {
                string pbannersTempPath = previewFolder + @"\Banners";
                if (!Directory.Exists(pbannersTempPath)) Directory.CreateDirectory(pbannersTempPath);

                foreach (string dirPath in Directory.GetDirectories(pbannersSourcePath, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(pbannersSourcePath, pbannersTempPath));

                foreach (string newPath in Directory.GetFiles(pbannersSourcePath, "*.*", SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace(pbannersSourcePath, pbannersTempPath), true);
            }
        }

        // Получение имени версии
        private static Func<string, string, string> GetVersionName()
        {
            string VersionName(string folderName, string version) => char.ToUpper(folderName[0]) + folderName.Substring(1);
            return VersionName;
        }

        // Добавление персонажа
        private static Action<string> CheckAddCh(Dictionary<string, string> nativeDict,
                                                 Dictionary<string, AjObj> objectsList,
                                                 AjLinkerMeta meta,
                                                 AjAssetGridLinker gridLinker)
        {
            void AddCh(string aid)
            {
                string dname = nativeDict[objectsList[aid].Properties.DisplayName];

                if (meta.Characters.Find(l => l.DisplayName == dname) == null)
                {
                    Form1.ShowMessage("В таблице нет персонажа с именем " + dname);

                    throw new Exception("В таблице нет персонажа с именем " + dname);
                }

                if (!gridLinker.IsChExist(dname)) gridLinker.AddCharacter(dname, aid);
            }

            return AddCh;
        }

        // Добавление локации по ID
        private static Action<int> CheckAddLocINT(AjLinkerMeta meta, AjAssetGridLinker gridLinker)
        {
            void AddLocINT(int intID)
            {
                AjMetaLocationData mdata = meta.Locations.Find(chf => chf.Id == intID);

                if (!gridLinker.IsLocExist(mdata.DisplayName)) gridLinker.AddLocation(mdata.DisplayName, mdata.Aid);
            }

            return AddLocINT;
        }

        // Добавление локации
        private static Action<string> CheckAddLoc(Dictionary<string, string> nativeDict,
                                                  Dictionary<string, AjObj> objectsList,
                                                  AjLinkerMeta meta,
                                                  AjAssetGridLinker gridLinker)
        {
            void AddLoc(string aid)
            {
                string dname = nativeDict[objectsList[aid].Properties.DisplayName];

                if (meta.Locations.Find(l => l.DisplayName == dname) == null)
                {
                    Form1.ShowMessage("В таблице нет локации с именем " + dname);

                    throw new Exception("В таблице нет локации с именем " + dname);
                }

                if (!gridLinker.IsLocExist(dname)) gridLinker.AddLocation(dname, objectsList[aid].Properties.Id);
            }

            return AddLoc;
        }

        // Показ ошибки локализации
        private static Action<string, string> ShowLocalizError()
        {
            void LocalizError(string cell, string fileID)
            {
                Form1.ShowMessage("Ошибка мультиязыкового вывода: " + cell + " в файле " + fileID);
            }

            return LocalizError;
        }

        // Генерация JSON локализации
        private Func<string, string, string[], string, int, string> GenerateLjson(
            Dictionary<string, Dictionary<string, string>> allDicts,
            Dictionary<string, AjLocalizInJsonFile> origLangData)
        {
            return (language, id, inPaths, outputPath, colN) =>
            {
                if (!allDicts.TryGetValue(language, out Dictionary<string, string> allStrings))
                {
                    allStrings = new Dictionary<string, string>();
                    allDicts[language] = allStrings;
                }

                AjLocalizInJsonFile jsonData = GetXMLFile(inPaths, colN);
                bool origLang = !origLangData.ContainsKey(id);

                if (origLang) origLangData[id] = jsonData;

                AjLocalizInJsonFile origJsonData = origLangData[id];
                if (origLang) jsonData = GetXMLFile(inPaths, colN);

                if (Form1.FOR_LOCALIZATORS_MODE)
                {
                    Console.WriteLine($"start {id} {allStrings.Count}");
                    foreach (KeyValuePair<string, string> pair in origJsonData.Data)
                    {
                        string origValue = pair.Value.Trim();
                        if (!jsonData.Data.TryGetValue(pair.Key, out string translatedValue))
                        {
                            Console.WriteLine($"String with ID {pair.Key} not found");
                            continue;
                        }

                        translatedValue = translatedValue.Replace("Pname", "pname");

                        if (!allStrings.ContainsKey(pair.Key)) allStrings[pair.Key] = translatedValue;

                        if (origValue.Contains("*SystemLinkTo*"))
                        {
                            string linkId = origValue.Split('*')[2];
                            if (!jsonData.Data.TryGetValue(linkId, out string linkedValue) &&
                                !allStrings.TryGetValue(linkId, out linkedValue))
                            {
                                Console.WriteLine($"String with {linkId} is not found");
                                continue;
                            }

                            jsonData.Data[pair.Key] = linkedValue;
                            translatedValue = linkedValue;
                        }

                        if (IsTranslationIncomplete(translatedValue,
                                                    origValue,
                                                    origLang,
                                                    jsonData.Data[pair.Key]))
                            Console.WriteLine($"String with ID {pair.Key} isn't translated");
                    }
                }

                string localizationIssue = CheckLocalizationIssues(origJsonData, jsonData, origLang);
                WriteJsonFile(jsonData, outputPath);

                return localizationIssue;
            };
        }

        // Проверка неполного перевода
        private bool IsTranslationIncomplete(string translatedValue, string origValue, bool origLang, string jsonDataValue) =>
            string.IsNullOrEmpty(translatedValue.Trim()) ||
            (/*origValue.Trim() == translatedValue.Trim() &&*/
             !origLang &&
             origValue.Length > 10 &&
             !origValue.Contains("*SystemLinkTo*") &&
             !origValue.Contains("NextChoiceIsTracked") &&
             !jsonDataValue.Contains("StageDirections") &&
             !string.IsNullOrEmpty(jsonDataValue.Replace(".", "").Trim()) &&
             !origValue.ToLower().Contains("%pname%"));

        // Проверка проблем локализации
        private string CheckLocalizationIssues(AjLocalizInJsonFile origJsonData,
                                               AjLocalizInJsonFile jsonData,
                                               bool origLang)
        {
            foreach (KeyValuePair<string, string> pair in origJsonData.Data)
                if (!jsonData.Data.ContainsKey(pair.Key) ||
                    string.IsNullOrEmpty(jsonData.Data[pair.Key].Trim()) /*||
                    (jsonData.Data[pair.Key] == pair.Value && !Form1.ONLY_ENGLISH_MODE && !origLang)*/)
                    return pair.Key;

            return string.Empty;
        }

        // Копирование ресурсов глав
        private void CopyChapterResources(int chapterN, AjAssetGridLinker gridLinker, AjLinkerMeta meta, string chapterFolder,
                                         List<string> copiedChAtlasses, List<string> copiedLocSprites, List<string> copiedLocIdles)
        {
            string[] chapterChs = gridLinker.GetCharactersNamesFromCurChapter();
            string[] locationsChs = gridLinker.GetLocationsNamesFromCurChapter();

            foreach (string el in chapterChs)
            {
                AjMetaCharacterData ch = meta.Characters.Find(lch => lch.DisplayName.Trim() == el.Trim());

                string atlasNameFiled = ch.AtlasFileName;

                var atlases = new List<string>();

                if (!atlasNameFiled.Contains(","))
                    atlases.Add(atlasNameFiled);
                else
                {
                    string[] atlasStrs = atlasNameFiled.Split(',');

                    atlases.AddRange(atlasStrs.Where(t => !string.IsNullOrEmpty(t)));
                }

                foreach (string atlasFileName in atlases)
                {
                    if (ch.BaseNameInAtlas == "-" ||
                        atlasFileName == "-" ||
                        copiedChAtlasses.Contains(atlasFileName))
                        continue;

                    copiedChAtlasses.Add(atlasFileName);

                    if (!atlasFileName.Contains("Sec_"))
                    {
                        File.Copy(string.Format(_projectPath + @"\Art\Characters\{0}.png", atlasFileName),
                                  string.Format(chapterFolder + @"\Resources\{0}.png", atlasFileName));
                        File.Copy(string.Format(_projectPath + @"\Art\Characters\{0}.tpsheet", atlasFileName),
                                  string.Format(chapterFolder + @"\Resources\{0}.tpsheet", atlasFileName));
                    }
                    else
                    {
                        string fileName = atlasFileName;
                        fileName = fileName.Replace("Sec_", meta.SpritePrefix);

                        File.Copy(string.Format(_projectPath + @"\Art\Characters\Secondary\{0}.png", fileName),
                                  string.Format(chapterFolder + @"\Resources\{0}.png", atlasFileName));
                    }
                }
            }

            foreach (string el in locationsChs)
            {
                AjMetaLocationData loc = meta.Locations.Find(lloc => lloc.DisplayName == el);

                if (!copiedLocSprites.Contains(loc.SpriteName))
                {
                    copiedLocSprites.Add(loc.SpriteName);

                    File.Copy(string.Format(_projectPath + @"\Art\Locations\{0}.png", loc.SpriteName),
                              string.Format(chapterFolder + @"\Resources\{0}.png", loc.SpriteName));
                }


                if (loc.SoundIdleName == "-" || copiedLocIdles.Contains(loc.SoundIdleName)) continue;

                copiedLocIdles.Add(loc.SoundIdleName);
                File.Copy(string.Format(_projectPath + @"\Audio\Idles\{0}.mp3", loc.SoundIdleName),
                          string.Format(chapterFolder + @"\Resources\{0}.mp3", loc.SoundIdleName));
            }
        }

        #endregion
    }
}