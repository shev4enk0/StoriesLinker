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
        private string _projectPath;

        private Dictionary<string, Dictionary<int, Dictionary<string, string>>> _savedXmlDicts;

        public LinkerBin(string projectPath)
        {
            _projectPath = projectPath;

            _savedXmlDicts = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();
        }

        /// <summary>
        /// Создает парсер для работы с форматом Articy X
        /// </summary>
        public ArticyXParser CreateArticyXParser()
        {
            return new ArticyXParser(this);
        }

        private Dictionary<string, string> XmlTableToDict(string path, int column = 1)
        {
            if (_savedXmlDicts.TryGetValue(path, out Dictionary<int, Dictionary<string, string>> columnsDict) && columnsDict.TryGetValue(column, out Dictionary<string, string> cachedDict))
            {
                return cachedDict;
            }

            Dictionary<string, string> nativeDict = new Dictionary<string, string>();

            using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(path)))
            {
                if (xlPackage.Workbook.Worksheets.Count == 0)
                {
                    throw new InvalidOperationException("The workbook contains no worksheets.");
                }
                ExcelWorksheet myWorksheet = xlPackage.Workbook.Worksheets.First();
                int totalRows = myWorksheet.Dimension.End.Row;
                int totalColumns = myWorksheet.Dimension.End.Column;

                for (int rowNum = 1; rowNum <= totalRows; rowNum++)
                {
                    ExcelRange firstRow = myWorksheet.Cells[rowNum, 1];
                    ExcelRange secondRow = myWorksheet.Cells[rowNum, column + 1];

                    string firstRowStr = firstRow?.Value != null
                                                ? firstRow.Value.ToString().Trim()
                                                : string.Empty;
                    string secondRowStr = secondRow?.Value != null
                                                 ? secondRow.Value.ToString().Trim()
                                                 : string.Empty;

                    // Пропускаем строки с пустым ключом или пустым значением
                    if (string.IsNullOrWhiteSpace(firstRowStr)) 
                        continue;

                    if (!nativeDict.ContainsKey(firstRowStr))
                    {
                        nativeDict.Add(firstRowStr, secondRowStr);
                    }
                    else
                    {
                        Console.WriteLine($"Обнаружен дублирующийся ключ: {firstRowStr}");
                    }
                }
            }

            if (!_savedXmlDicts.ContainsKey(path))
            {
                _savedXmlDicts[path] = new Dictionary<int, Dictionary<string, string>>();
            }
            _savedXmlDicts[path][column] = nativeDict;

            return nativeDict;
        }

        public Dictionary<string, string> GetNativeDict() { return XmlTableToDict(GetLocalizTablesPath(_projectPath)); }

        public AjFile GetParsedFlowJsonFile()
        {
            AjFile jsonObj;

            using (StreamReader r = new StreamReader(GetFlowJsonPath(_projectPath)))
            {
                string json = r.ReadToEnd();
                jsonObj = JsonConvert.DeserializeObject<AjFile>(json);
            }

            return jsonObj;
        }

        public AjLinkerMeta GetParsedMetaInputJsonFile()
        {
            AjLinkerMeta jsonObj = new AjLinkerMeta();

            jsonObj.Version = new BookVersionInfo();

            string metaXmlPath = _projectPath + @"\Raw\Meta.xlsx";

            Dictionary<string, string> nativeDict = new Dictionary<string, string>();

            using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(metaXmlPath)))
            {
                ExcelWorksheet myWorksheet = xlPackage.Workbook.Worksheets.First();
                int totalRows = myWorksheet.Dimension.End.Row;

                for (int rowNum = 2; rowNum <= totalRows; rowNum++)
                {
                    ExcelRange firstRow = myWorksheet.Cells[rowNum, 1];
                    ExcelRange secondRow = myWorksheet.Cells[rowNum, 2];

                    string fieldName = firstRow.Value.ToString();
                    string fieldValue = secondRow.Value.ToString();

                    string[] values;

                    switch (fieldName)
                    {
                        case "UniqueID":
                            jsonObj.UniqueId = fieldValue;
                            break;
                        case "SpritePrefix":
                            jsonObj.SpritePrefix = fieldValue;
                            break;
                        case "VersionBin":
                            jsonObj.Version.BinVersion = fieldValue;
                            break;
                        case "VersionPreview":
                            jsonObj.Version.PreviewVersion = fieldValue;
                            break;
                        case "VersionBaseResources":
                            jsonObj.Version.BaseResourcesVersion = fieldValue;
                            break;
                        case "StandartizedUI":
                            jsonObj.StandartizedUi = fieldValue == "1";
                            break;
                        case "UITextBlockFontSize":
                            jsonObj.UiTextBlockFontSize = int.Parse(fieldValue);
                            break;
                        case "UIChoiceBlockFontSize":
                            jsonObj.UiChoiceBlockFontSize = int.Parse(fieldValue);
                            break;
                        case "KarmaCurrency":
                            jsonObj.KarmaCurrency = fieldValue;
                            break;
                        case "KarmaBadBorder":
                            jsonObj.KarmaBadBorder = int.Parse(fieldValue);
                            break;
                        case "KarmaGoodBorder":
                            jsonObj.KarmaGoodBorder = int.Parse(fieldValue);
                            break;
                        case "KarmaTopLimit":
                            jsonObj.KarmaTopLimit = int.Parse(fieldValue);
                            break;
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
                        case "ExceptionsWeaponLayer":
                            jsonObj.ExceptionsWeaponLayer = fieldValue == "1";
                            break;
                        case "UITextPlateLimits":
                            values = fieldValue.Split(',');

                            jsonObj.UiTextPlateLimits = new List<int>();

                            foreach (string el in values)
                            {
                                jsonObj.UiTextPlateLimits.Add(int.Parse(el));
                            }

                            break;
                        case "UIPaintFirstLetterInRedException":
                            jsonObj.UiPaintFirstLetterInRedException = fieldValue == "1";
                            break;
                        case "UITextPlateOffset":
                            jsonObj.UiTextPlateOffset = int.Parse(fieldValue);
                            break;
                        case "UIOverridedTextColor":
                            jsonObj.UiOverridedTextColor = fieldValue == "1";
                            break;
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
                        case "WardrobeEnabled":
                            jsonObj.WardrobeEnabled = fieldValue == "1";
                            break;
                        case "MainHeroHasDifferentGenders":
                            jsonObj.MainHeroHasDifferentGenders = fieldValue == "1";
                            break;
                        case "MainHeroHasSplittedHairSprite":
                            jsonObj.MainHeroHasSplittedHairSprite = fieldValue == "1";
                            break;
                        case "CustomClothesCount":
                            jsonObj.CustomClothesCount = int.Parse(fieldValue);
                            break;
                        case "CustomHairsCount":
                            jsonObj.CustomHairCount = int.Parse(fieldValue);
                            break;
                    }
                }

                myWorksheet = xlPackage.Workbook.Worksheets[2];
                totalRows = myWorksheet.Dimension.End.Row;

                Func<object[], int> checkRow = CheckRow();

                List<AjMetaCharacterData> characters = new List<AjMetaCharacterData>();

                for (int rowNum = 2; rowNum <= totalRows; rowNum++)
                {
                    object[] cells = new object[]
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

                    AjMetaCharacterData ch = new AjMetaCharacterData();

                    ch.DisplayName = cells[0].ToString();
                    ch.ClothesVariableName = cells[1].ToString();
                    ch.AtlasFileName = cells[2].ToString();
                    ch.BaseNameInAtlas = cells[3].ToString();

                    characters.Add(ch);
                }

                jsonObj.Characters = characters;

                myWorksheet = xlPackage.Workbook.Worksheets[3];
                totalRows = myWorksheet.Dimension.End.Row;

                List<AjMetaLocationData> locations = new List<AjMetaLocationData>();

                for (int rowNum = 2; rowNum <= totalRows; rowNum++)
                {
                    object[] cells = new object[]
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

                    AjMetaLocationData loc = new AjMetaLocationData
                                              {
                                                  Id = int.Parse(cells[0].ToString()), DisplayName = cells[1].ToString(),
                                                  SpriteName = cells[2].ToString(),
                                                  SoundIdleName = cells[3].ToString()
                                              };

                    if (cells[4].ToString() == "1")
                    {
                        jsonObj.IntroLocation = rowNum - 1;
                    }

                    locations.Add(loc);
                }

                jsonObj.Locations = locations;
            }

            return jsonObj;
        }

        private static Func<object[], int> CheckRow()
        {
            Func<object[], int> checkRow = (cells) =>
                                             {
                                                 bool rowIsCompletelyEmpty = true;
                                                 bool rowHasEmptyField = false;

                                                 foreach (object cell in cells)
                                                 {
                                                     if (cell == null
                                                         || string.IsNullOrEmpty(cell.ToString().Trim()))
                                                     {
                                                         rowHasEmptyField = true;
                                                     }
                                                     else
                                                     {
                                                         rowIsCompletelyEmpty = false;
                                                     }
                                                 }

                                                 if (rowIsCompletelyEmpty)
                                                     return -1;
                                                 else if (rowHasEmptyField) return 0;

                                                 return 1;
                                             };
            return checkRow;
        }

        public Dictionary<string, AjObj> GetAricyBookEntities(AjFile ajfile, Dictionary<string, string> nativeDict)
        {
            Dictionary<string, AjObj> objectsList = new Dictionary<string, AjObj>();

            List<AjObj> models = ajfile.Packages[0].Models;

            Dictionary<string, int> chaptersIdNames = new Dictionary<string, int>();

            foreach (AjObj ns in models)
            {
                AjType type;

                switch (ns.Type)
                {
                    case "FlowFragment":
                        type = AjType.FlowFragment;
                        string value = Regex.Match(nativeDict[ns.Properties.DisplayName], @"\d+").Value;

                        int intValue = int.Parse(value);

                        chaptersIdNames.Add(ns.Properties.Id, intValue);
                        break;
                    case "Dialogue":
                        type = AjType.Dialogue;
                        break;
                    case "Entity":
                        type = AjType.Entity;
                        break;
                    case "DefaultSupportingCharacterTemplate":
                        type = AjType.Entity;
                        break;
                    case "DefaultMainCharacterTemplate":
                        type = AjType.Entity;
                        break;
                    case "Location":
                        type = AjType.Location;
                        break;
                    case "DialogueFragment":
                        type = AjType.DialogueFragment;
                        break;
                    case "Instruction":
                        type = AjType.Instruction;
                        break;
                    case "Condition":
                        type = AjType.Condition;
                        break;
                    case "Jump":
                        type = AjType.Jump;
                        break;
                    default:
                        type = AjType.Other;
                        break;
                }

                ns.EType = type;

                objectsList.Add(ns.Properties.Id, ns);
            }

            return objectsList;
        }

        private List<string> GetSortedChaptersList(Dictionary<string, AjObj> objList,
                                                   Dictionary<string, string> nativeDict)
        {
            List<string> chaptersIds = new List<string>();

            Dictionary<string, int> chaptersIdNames = new Dictionary<string, int>();

            foreach (KeyValuePair<string, AjObj> kobj in objList)
            {
                if (kobj.Value.EType != AjType.FlowFragment) continue;
                
                string value = Regex.Match(nativeDict[kobj.Value.Properties.DisplayName], @"\d+").Value;

                int intValue = int.Parse(value);

                chaptersIdNames.Add(kobj.Value.Properties.Id, intValue);
            }

            IOrderedEnumerable<KeyValuePair<string, int>> sortedChapterNames = from entry in chaptersIdNames orderby entry.Value ascending select entry;

            foreach (KeyValuePair<string, int> pair in sortedChapterNames)
            {
                chaptersIds.Add(pair.Key);
            }

            return chaptersIds;
        }

        private List<string>[] GetChaptersAndSubchaptersParentsIDs(List<string> chaptersIds,
                                                                   Dictionary<string, AjObj> objList)
        {
            List<List<string>> ids = new List<List<string>>();

            for (int i = 0; i < chaptersIds.Count; i++)
            {
                string chapterId = chaptersIds[i];

                ids.Add(new List<string>());
                ids[i].Add(chapterId);

                foreach (KeyValuePair<string, AjObj> kobj in objList)
                {
                    if (kobj.Value.EType != AjType.Dialogue) continue; //subchapter 
                    
                    string subchapterId = kobj.Value.Properties.Id;

                    string parent = kobj.Value.Properties.Parent;

                    while (true)
                    {
                        if (parent == chapterId)
                        {
                            ids[i].Add(subchapterId);
                            break;
                        }
                        else
                        {
                            if (objList.ContainsKey(parent))
                            {
                                parent = objList[parent].Properties.Parent;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return ids.ToArray();
        }

        public enum EChEmotion
        {
            Angry, //red
            Happy, //green
            Sad, //purple
            Surprised, //yellow
            IsntSetOrNeutral //blue
        }

        public bool GenerateLocalizTables()
        {
            if (Directory.Exists(_projectPath + @"\Localization"))
                Directory.Delete(_projectPath + @"\Localization", true);

            Directory.CreateDirectory(_projectPath + @"\Localization");
            Directory.CreateDirectory(_projectPath + @"\Localization\Russian");

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

            Func<AjColor, string> recognizeEmotion = (color) =>
                                                       {
                                                           EChEmotion emotion = EChEmotion.IsntSetOrNeutral;

                                                           Func<Color32, Color32, bool> colorsEquals = (a, b) =>
                                                                                                         {
                                                                                                             return
                                                                                                                 (Math
                                                                                                                      .Abs(a
                                                                                                                               .R
                                                                                                                           - b
                                                                                                                               .R)
                                                                                                                  < 20
                                                                                                                  && Math
                                                                                                                      .Abs(a
                                                                                                                               .G
                                                                                                                           - b
                                                                                                                               .G)
                                                                                                                  < 20
                                                                                                                  && Math
                                                                                                                      .Abs(a
                                                                                                                               .B
                                                                                                                           - b
                                                                                                                               .B)
                                                                                                                  < 20);
                                                                                                         };

                                                           Color32 fragColor = color.ToColor32();
                                                           Color32[] emotionsColor
                                                               = new Color32[]
                                                                 {
                                                                     new Color32(255, 0, 0, 0),
                                                                     new Color32(0, 110, 20, 0),
                                                                     new Color32(41, 6, 88, 0),
                                                                     new Color32(255, 134, 0, 0)
                                                                 };

                                                           for (int i = 0; i < emotionsColor.Length; i++)
                                                           {
                                                               if (colorsEquals(emotionsColor[i], fragColor))
                                                               {
                                                                   emotion = (EChEmotion)i;
                                                                   break;
                                                               }
                                                           }

                                                           return emotion.ToString();
                                                       };

            List<string>[] csparentsIds = GetChaptersAndSubchaptersParentsIDs(chaptersIds, objectsList);

            List<string> charactersIds = new List<string>();
            List<LocalizEntity> charactersLocalizIds = new List<LocalizEntity>();

            Dictionary<string, string> charactersNames = new Dictionary<string, string>();

            for (int i = 0; i < csparentsIds.Length; i++)
            {
                int chapterN = i + 1;

                List<LocalizEntity> forTranslating = new List<LocalizEntity>();
                List<LocalizEntity> nonTranslating = new List<LocalizEntity>();
                List<string> parentsIds = csparentsIds[i];

                foreach (KeyValuePair<string, AjObj> scobj in objectsList)
                {
                    if (parentsIds.Contains(scobj.Value.Properties.Parent))
                    {
                        AjObj dfobj = scobj.Value;

                        if (dfobj.EType != AjType.DialogueFragment) continue;

                        string chId = dfobj.Properties.Speaker;

                        if (!charactersIds.Contains(chId))
                        {
                            LocalizEntity entity = new LocalizEntity();

                            entity.LocalizId = objectsList[chId].Properties.DisplayName;

                            charactersIds.Add(chId);
                            charactersLocalizIds.Add(entity);

                            charactersNames.Add(chId, nativeDict[objectsList[chId].Properties.DisplayName]);
                        }


                        if (!string.IsNullOrEmpty(dfobj.Properties.Text))
                        {
                            LocalizEntity entity = new LocalizEntity();

                            entity.LocalizId = dfobj.Properties.Text;
                            entity.SpeakerDisplayName = charactersNames[chId];
                            entity.Emotion = recognizeEmotion(dfobj.Properties.Color);

                            forTranslating.Add(entity);
                        }

                        if (!string.IsNullOrEmpty(dfobj.Properties.MenuText))
                        {
                            LocalizEntity entity = new LocalizEntity();

                            entity.LocalizId = dfobj.Properties.MenuText;
                            entity.SpeakerDisplayName = charactersNames[chId];
                            entity.Emotion = recognizeEmotion(dfobj.Properties.Color);

                            forTranslating.Add(entity);
                        }

                        if (!string.IsNullOrEmpty(dfobj.Properties.StageDirections))
                        {
                            LocalizEntity entity = new LocalizEntity();

                            entity.LocalizId = dfobj.Properties.StageDirections;
                            entity.SpeakerDisplayName = "";

                            nonTranslating.Add(entity);
                        }
                    }
                }

                CreateLocalizTable(string.Format("Chapter_{0}_for_translating", chapterN),
                                   forTranslating,
                                   nativeDict);
                CreateLocalizTable(string.Format("Chapter_{0}_internal", chapterN), nonTranslating, nativeDict);
            }

            CreateLocalizTable(string.Format("CharacterNames"), charactersLocalizIds, nativeDict);

            return true;
        }

        private int _allWordsCount = 0;

        private void CreateLocalizTable(string name, List<LocalizEntity> ids, Dictionary<string, string> nativeDict)
        {
            int wordCount = 0;

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

                List<string> replacedIds = new List<string>();

                foreach (LocalizEntity item in ids)
                {
                    string id = item.LocalizId;

                    string value = nativeDict[id];

                    if (forTranslating && forLocalizatorsMode)
                    {
                        value = value.Replace("pname", "%pname%");
                        value = value.Replace("Pname", "%pname%");

                        if (!replacedIds.Contains(id))
                        {
                            List<string> repeatedValues = new List<string>();

                            foreach (KeyValuePair<string, string> pair in nativeDict)
                            {
                                if (pair.Value == value && pair.Key != id)
                                {
                                    repeatedValues.Add(pair.Key);
                                }
                            }

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

                    sheet.Cells[row, col].Value = item.LocalizId;

                    if (forLocalizatorsMode)
                    {
                        sheet.Cells[row, col + 1].Value = item.SpeakerDisplayName;
                        sheet.Cells[row, col + 2].Value = item.Emotion;
                    }

                    sheet.Cells[row, col + (forLocalizatorsMode ? 3 : 1)].Value = value;

                    if (forLocalizatorsMode && !replacedIds.Contains(id))
                    {
                        wordCount += CountWords(value);
                    }

                    row++;
                }

                byte[] bin = eP.GetAsByteArray();

                File.WriteAllBytes(_projectPath + @"\Localization\Russian\" + name + ".xlsx", bin);

                if (name.Contains("internal") || !forLocalizatorsMode) return;
                
                Console.WriteLine("Таблица " + name + " сгенерирована, количество слов: " + wordCount);

                _allWordsCount += wordCount;

                if (name.Contains("12")) Console.WriteLine("total count = " + _allWordsCount);
            }
        }

        public int CountWords(string text)
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


            // проверка на дубль имён персонажей и их имён в атласах

            for (int i = 0; i < meta.Characters.Count; i++)
            {
                AjMetaCharacterData cObj = meta.Characters[i];

                for (int j = 0; j < meta.Characters.Count; j++)
                {
                    if (i == j) continue;

                    AjMetaCharacterData aObj = meta.Characters[j];

                    if (cObj.DisplayName != aObj.DisplayName
                        && (cObj.BaseNameInAtlas != aObj.BaseNameInAtlas
                            || cObj.BaseNameInAtlas == "-"
                            || meta.UniqueId == "Shism_1")
                        && (cObj.ClothesVariableName != aObj.ClothesVariableName
                            || cObj.ClothesVariableName.Trim() == "-"))
                        continue;
                    
                    Form1.ShowMessage("Найдены дублирующиеся значения среди персонажей: " + aObj.DisplayName);

                    return false;
                }

                if (cObj.AtlasFileName.Contains("Sec_") || cObj.BaseNameInAtlas.Contains("Sec_"))
                {
                    if (cObj.AtlasFileName != cObj.BaseNameInAtlas)
                    {
                        Form1.ShowMessage("AtlasFileName и BaseNameInAtlas у второстепенных должны быть одинаковы: "
                                          + cObj.DisplayName);

                        return false;
                    }
                }

                int clothesNsIndex = ajfile.GlobalVariables.FindIndex(ns => ns.Namespace == "Clothes");

                bool state1 = clothesNsIndex != -1;
                bool state2 = ajfile.GlobalVariables[clothesNsIndex].Variables
                                     .FindIndex(v => v.Variable == cObj.ClothesVariableName) != -1;
                if (cObj.ClothesVariableName.Trim() == "-" || (state1 && state2))
                    continue;
                
                Form1.ShowMessage("В артиси не определена переменная с именем Clothes."
                                  + cObj.ClothesVariableName);

                return false;
            }


            // проверка на дубль имён и спрайтов локаций

            if (meta.UniqueId != "Pirates_1")
            {
                for (int i = 0; i < meta.Locations.Count; i++)
                {
                    AjMetaLocationData cObj = meta.Locations[i];

                    for (int j = 0; j < meta.Locations.Count; j++)
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

            AjAssetGridLinker gridLinker = new AjAssetGridLinker();

            Action<string> checkAddCh = CheckAddCh(nativeDict, objectsList, meta, gridLinker);

            Action<int> checkAddLocInt = CheckAddLocInt(meta, gridLinker);

            Action<string> checkAddLoc = CheckAddLoc(nativeDict, objectsList, meta, gridLinker);

            List<string> copiedChAtlasses = new List<string>();
            List<string> copiedLocSprites = new List<string>();
            List<string> copiedLocIdles = new List<string>();

            List<AjObj> sharedObjs = new List<AjObj>();

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
            {
                el.Aid = "fake_location_aid" + el.Id;
            }

            string translatedDataFolder = _projectPath + @"\TranslatedData\";

            bool multiLangOutput = Directory.Exists(translatedDataFolder);

            Dictionary<string, int> langsCols = new Dictionary<string, int> { { "Russian", multiLangOutput ? 3 : 1 } };

            if (multiLangOutput)
            {
                // Получаем все папки в директории TranslatedData
                string[] languageFolders = Directory.GetDirectories(translatedDataFolder);
                foreach (string folder in languageFolders)
                {
                    // Получаем имя папки (язык)
                    string language = Path.GetFileName(folder);
                    // Пропускаем папку Russian, так как она уже добавлена
                    if (language != "Russian")
                    {
                        langsCols.Add(language, 4);
                    }
                }
            }

            meta.ChaptersEntryPoints = new List<string>();

            AjGridAssetJson gridAssetFile = new AjGridAssetJson();

            Dictionary<string, Dictionary<string, string>> allDicts
                = new Dictionary<string, Dictionary<string, string>>();


            for (int i = 0; i < csparentsIds.Length; i++)
            {
                gridLinker.AddChapter();

                int chapterN = i + 1;
                List<string> parentsIds = csparentsIds[i];

                meta.ChaptersEntryPoints.Add(parentsIds[0]);

                List<AjObj> chapterObjs = new List<AjObj>();

                foreach (KeyValuePair<string, AjObj> pair in objectsList)
                {
                    if (!parentsIds.Contains(pair.Value.Properties.Parent)
                        && !parentsIds.Contains(pair.Value.Properties.Id))
                        continue;
                    
                    AjObj dfobj = pair.Value;

                    switch (dfobj.EType)
                    {
                        case AjType.DialogueFragment:
                        {
                            string chId = dfobj.Properties.Speaker;

                            checkAddCh(chId);
                            break;
                        }
                        case AjType.Dialogue:
                        {
                            List<string> attachments = dfobj.Properties.Attachments;

                            foreach (string el in attachments)
                            {
                                AjObj atObj = objectsList[el];

                                if (atObj.EType == AjType.Location)
                                    checkAddLoc(el);
                                else if (atObj.EType == AjType.Entity) checkAddCh(el);
                            }

                            break;
                        }
                        case AjType.Instruction:
                        {
                            string rawScript = dfobj.Properties.Expression;

                            if (rawScript.Contains("Location.loc"))
                            {
                                string[] scripts = rawScript.EscapeString()
                                                               .Replace("\\n", "")
                                                               .Replace("\\r", "")
                                                               .Split(';');

                                foreach (string uscript in scripts)
                                {
                                    if (!uscript.Contains("Location.loc")) continue;
                                    
                                    string[] parts = uscript.Split('=');
                                    int locId = int.Parse(parts[1].Trim());
                                    checkAddLocInt(locId);
                                }
                            }

                            break;
                        }
                    }

                    chapterObjs.Add(dfobj);
                }

                AjLinkerOutputChapterFlow flowJson = new AjLinkerOutputChapterFlow { Objects = chapterObjs };

                string chapterFolder
                    = tempFolder + getVersionName("chapter" + chapterN, meta.Version.BinVersion);

                Directory.CreateDirectory(chapterFolder);
                Directory.CreateDirectory(chapterFolder + @"\Resources");
                Directory.CreateDirectory(chapterFolder + @"\Strings");

                File.WriteAllText(chapterFolder + @"\Flow.json", JsonConvert.SerializeObject(flowJson));

                string[] chapterChs = gridLinker.GetCharactersNamesFromCurChapter();
                string[] locationsChs = gridLinker.GetLocationsNamesFromCurChapter();

                foreach (string el in chapterChs)
                {
                    AjMetaCharacterData ch = meta.Characters.Find(lch => lch.DisplayName.Trim() == el.Trim());

                    string atlasNameFiled = ch.AtlasFileName;

                    List<string> atlases = new List<string>();

                    if (!atlasNameFiled.Contains(","))
                    {
                        atlases.Add(atlasNameFiled);
                    }
                    else
                    {
                        string[] atlasStrs = atlasNameFiled.Split(',');

                        atlases.AddRange(atlasStrs.Where(t => !string.IsNullOrEmpty(t)));
                    }

                    foreach (string atlasFileName in atlases)
                    {
                        if (ch.BaseNameInAtlas == "-" 
                            || atlasFileName == "-" 
                            || copiedChAtlasses.Contains(atlasFileName))
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

                AjGridAssetChapterJson gridAssetChapter = new AjGridAssetChapterJson
                                                             {
                                                                 CharactersIDs
                                                                     = gridLinker.GetCharactersIDsFromCurChapter(),
                                                                 LocationsIDs = gridLinker.GetLocationsIDsFromCurChapter()
                                                             };

                gridAssetFile.Chapters.Add(gridAssetChapter);

                Dictionary<string, AjLocalizInJsonFile> origLangData = new Dictionary<string, AjLocalizInJsonFile>();

                // Получаем список всех известных языков из ключей _langs_cols
                List<string> knownLanguagesList = langsCols.Keys.ToList();

                // Передаем список языков в GenerateLjson при создании Func
                Func<string, string, string[], string, int, List<string>, string> generateLjson = GenerateLjson(allDicts, origLangData);

                string langOriginFolder = _projectPath + @"\Localization\Russian";

                Action<string, string> showLocalizError = ShowLocalizError();

                if (Form1.ONLY_ENGLISH_MODE)
                {
                    if (!langsCols.ContainsKey("English")) langsCols.Add("English", -1);
                     // Обновляем список известных языков, если добавили English
                    knownLanguagesList = langsCols.Keys.ToList();
                }

                foreach (KeyValuePair<string, int> langPair in langsCols)
                {
                    string lang = langPair.Key;
                    int colNum = langPair.Value;

                    bool nativeLang = lang == "Russian" || colNum == -1;

                    string langFolder
                        = (nativeLang ? langOriginFolder : _projectPath + @"\TranslatedData\" + lang);
                    string bookDescsPath = _projectPath + @"\Raw\BookDescriptions\" + lang + ".xlsx";

                    // Проверяем альтернативный путь для файла локализации
                    if (!File.Exists(bookDescsPath) && !nativeLang)
                    {
                        bookDescsPath = _projectPath + @"\TranslatedData\" + lang + @"\" + lang + ".xlsx";
                    }

                    Console.WriteLine("GENERATE TABLES FOR LANGUAGE: " + lang);

                    if (!Directory.Exists(langFolder)) continue;
                    
                    string[] langFiles = new string[]
                                           {
                                               string.Format(langFolder + @"\Chapter_{0}_for_translating.xlsx",
                                                             chapterN),
                                               string.Format(langOriginFolder + @"\Chapter_{0}_internal.xlsx",
                                                             chapterN)
                                           };

                    if (!File.Exists(langFiles[0])) break;

                    // Вызываем Func, передавая список языков
                    string correct = generateLjson(lang,
                                                      "chapter" + chapterN,
                                                      langFiles,
                                                      chapterFolder + @"\Strings\" + lang + ".json",
                                                      colNum != -1 ? colNum : 1,
                                                      knownLanguagesList);

                    if (!string.IsNullOrEmpty(correct))
                    {
                        showLocalizError(correct, "chapter" + chapterN);

                        //return false;
                    }

                    if (chapterN != 1) continue;
                        
                    string[] sharedLangFiles = new string[]
                                                  {
                                                      string.Format(langFolder + @"\CharacterNames.xlsx",
                                                                    chapterN),
                                                      bookDescsPath
                                                  };

                    Console.WriteLine("generate sharedstrings " + bookDescsPath);
                    
                    // Вызываем Func, передавая список языков
                    correct = generateLjson(lang,
                                               "sharedstrings",
                                               sharedLangFiles,
                                               binFolder + @"\SharedStrings\" + lang + ".json",
                                               colNum != -1 ? colNum : 1,
                                               knownLanguagesList);

                    string[] stringToPreviewFile = new string[] { bookDescsPath };

                    if (!string.IsNullOrEmpty(correct))
                    {
                        showLocalizError(correct, "sharedstrings");
                        return false;
                    }
                    
                    // Вызываем Func, передавая список языков
                    correct = generateLjson(lang,
                                               "previewstrings",
                                               stringToPreviewFile,
                                               previewFolder + @"\Strings\" + lang + ".json",
                                               colNum != -1 ? colNum : 1,
                                               knownLanguagesList);

                    if (string.IsNullOrEmpty(correct)) continue;
                            
                    showLocalizError(correct, "previewstrings");
                    return false;
                }
            }

            AjLinkerOutputBase baseJson = new AjLinkerOutputBase
                                            {
                                                GlobalVariables = ajfile.GlobalVariables, SharedObjs = sharedObjs
                                            };

            File.WriteAllText(binFolder + @"\Base.json", JsonConvert.SerializeObject(baseJson));
            File.WriteAllText(binFolder + @"\Meta.json", JsonConvert.SerializeObject(meta));
            File.WriteAllText(binFolder + @"\AssetsByChapters.json", JsonConvert.SerializeObject(gridAssetFile));

            string musicSourcePath = _projectPath + @"\Audio\Music";
            string musicTempPath = brFolder + @"\Music";
            if (!Directory.Exists(musicTempPath))
            {
                Directory.CreateDirectory(musicTempPath);
            }

            foreach (string srcPath in Directory.GetFiles(musicSourcePath))
            {
                //Copy the file from sourcepath and place into mentioned target path, 
                //Overwrite the file if same file is exist in target path
                File.Copy(srcPath, srcPath.Replace(musicSourcePath, musicTempPath), true);
            }

            string pcoversSourcePath = _projectPath + @"\Art\PreviewCovers";
            string pcoversTempPath = previewFolder + @"\Covers";
            if (!Directory.Exists(pcoversTempPath))
            {
                Directory.CreateDirectory(pcoversTempPath);
            }

            if (!File.Exists(pcoversSourcePath + @"\Russian\PreviewCover.png") /*||
                (_multi_lang_output && !File.Exists(pcoversSourcePath + @"\English\PreviewCover_English.png"))*/)
            {
                Form1.ShowMessage("Не все preview обложки присуствуют.");
                return false;
            }

            foreach (string dirPath in Directory.GetDirectories(pcoversSourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(pcoversSourcePath, pcoversTempPath));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(pcoversSourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(pcoversSourcePath, pcoversTempPath), true);

            string pbannersSourcePath = _projectPath + @"\Art\SliderBanners";

            if (!Directory.Exists(pbannersSourcePath)) return true;
            {
                string pbannersTempPath = previewFolder + @"\Banners";
                if (!Directory.Exists(pbannersTempPath))
                {
                    Directory.CreateDirectory(pbannersTempPath);
                }

                foreach (string dirPath in Directory.GetDirectories(pbannersSourcePath, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(pbannersSourcePath, pbannersTempPath));

                //Copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(pbannersSourcePath, "*.*", SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace(pbannersSourcePath, pbannersTempPath), true);
            }

            return true;
        }

        private static Func<string, string, string> GetVersionName()
        {
            Func<string, string, string> getVersionName = (folderName, version) => char.ToUpper(folderName[0])
                                                                                   + folderName.Substring(1);
            return getVersionName;
        }

        private static Action<string> CheckAddCh(Dictionary<string, string> nativeDict,
                                                 Dictionary<string, AjObj> objectsList,
                                                 AjLinkerMeta meta,
                                                 AjAssetGridLinker gridLinker)
        {
            Action<string> checkAddCh = aid =>
                                           {
                                               string dname = nativeDict[objectsList[aid].Properties.DisplayName];

                                               if (meta.Characters.Find(l => l.DisplayName == dname) == null)
                                               {
                                                   Form1.ShowMessage("В таблице нет персонажа с именем " + dname);

                                                   throw new Exception("В таблице нет персонажа с именем " + dname);
                                               }

                                               if (!gridLinker.IsChExist(dname))
                                                   gridLinker.AddCharacter(dname, aid);
                                           };
            return checkAddCh;
        }

        private static Action<int> CheckAddLocInt(AjLinkerMeta meta, AjAssetGridLinker gridLinker)
        {
            Action<int> checkAddLocInt = intId =>
                                             {
                                                 AjMetaLocationData mdata
                                                     = meta.Locations.Find(chf => chf.Id == intId);

                                                 if (!gridLinker.IsLocExist(mdata.DisplayName))
                                                     gridLinker.AddLocation(mdata.DisplayName, mdata.Aid);
                                             };
            return checkAddLocInt;
        }

        private static Action<string> CheckAddLoc(Dictionary<string, string> nativeDict,
                                                  Dictionary<string, AjObj> objectsList,
                                                  AjLinkerMeta meta,
                                                  AjAssetGridLinker gridLinker)
        {
            Action<string> checkAddLoc = aid =>
                                            {
                                                string dname
                                                    = nativeDict[objectsList[aid].Properties.DisplayName];

                                                if (meta.Locations.Find(l => l.DisplayName == dname) == null)
                                                {
                                                    Form1.ShowMessage("В таблице нет локации с именем " + dname);

                                                    throw new Exception("В таблице нет локации с именем " + dname);
                                                }

                                                if (!gridLinker.IsLocExist(dname))
                                                    gridLinker.AddLocation(dname, objectsList[aid].Properties.Id);
                                            };
            return checkAddLoc;
        }

        private static Action<string, string> ShowLocalizError()
        {
            Action<string, string> showLocalizError = (missingKey, fileGroupId) =>
                                                         {
                                                             // Добавляем уточнение, что ключ отсутствует в данных для этой группы файлов
                                                             Form1.ShowMessage($"Ошибка мультиязыкового вывода: Ключ '{missingKey}' отсутствует или пуст в данных для группы файлов '{fileGroupId}'");
                                                         };
            return showLocalizError;
        }

        private Func<string, string, string[], string, int, List<string>, string> GenerateLjson(Dictionary<string, Dictionary<string, string>> allDicts,
                                                                                  Dictionary<string, AjLocalizInJsonFile> origLangData)
        {
            return (language, id, inPaths, outputPath, colN, knownLanguages) =>
                   {
                       if (!allDicts.TryGetValue(language, out Dictionary<string, string> allStrings))
                       {
                           allStrings = new Dictionary<string, string>();
                           allDicts[language] = allStrings;
                       }

                       AjLocalizInJsonFile jsonData = GetXmlFile(inPaths, colN, knownLanguages);
                       bool origLang = !origLangData.ContainsKey(id);

                       if (origLang) origLangData[id] = jsonData;

                       AjLocalizInJsonFile origJsonData = origLangData[id];
                       if (origLang) jsonData = GetXmlFile(inPaths, colN, knownLanguages);

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
                                   if (!jsonData.Data.TryGetValue(linkId, out string linkedValue)
                                       && !allStrings.TryGetValue(linkId, out linkedValue))
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

        private bool
            IsTranslationIncomplete(string translatedValue, string origValue, bool origLang, string jsonDataValue)
        {
            return string.IsNullOrEmpty(translatedValue.Trim())
                   || (origValue.Trim() == translatedValue.Trim()
                       && !origLang
                       && origValue.Length > 10
                       && !origValue.Contains("*SystemLinkTo*")
                       && !origValue.Contains("NextChoiceIsTracked")
                       && !jsonDataValue.Contains("StageDirections")
                       && !string.IsNullOrEmpty(jsonDataValue.Replace(".", "").Trim())
                       && !origValue.ToLower().Contains("%pname%"));
        }

        private string CheckLocalizationIssues(AjLocalizInJsonFile origJsonData,
                                               AjLocalizInJsonFile jsonData,
                                               bool origLang)
        {
            foreach (KeyValuePair<string, string> pair in origJsonData.Data)
            {
                // Проверяем только наличие ключа, игнорируем пустые значения
                if (!jsonData.Data.ContainsKey(pair.Key))
                    return pair.Key;
            }

            return string.Empty;
        }


        private AjLocalizInJsonFile GetXmlFile(string[] pathsToXmls, int defaultColumn, List<string> knownLanguages)
        {
            Dictionary<string, string> total = new Dictionary<string, string>();
            var knownLanguagesSet = new HashSet<string>(knownLanguages ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

            Console.WriteLine("\n=== Начало обработки файлов локализации ===");
            foreach (string path in pathsToXmls)
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"ВНИМАНИЕ: Файл не найден: {path}");
                    continue;
                }

                Console.WriteLine($"\nОбработка файла: {path}");
                Dictionary<string, string> fileDict = null;
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);

                try
                {
                    // Определяем, является ли файл файлом описания книги
                    bool isBookDescription = path.Contains("BookDescriptions");
                    bool isTranslatedData = path.Contains("TranslatedData");

                    if (isBookDescription && knownLanguagesSet.Contains(fileNameWithoutExtension))
                    {
                        // Для файлов из BookDescriptions используем логику D->B
                        Console.WriteLine($"Применяем логику D->B для файла описания книги: {path}");
                        
                        // Получаем данные из колонок D и B
                        Dictionary<string, string> dictD = XmlTableToDict(path, 3).Where(x => !string.IsNullOrWhiteSpace(x.Value))
                                                                                  .ToDictionary(x => x.Key, x => x.Value.Trim());
                        Dictionary<string, string> dictB = XmlTableToDict(path, 1).Where(x => !string.IsNullOrWhiteSpace(x.Value))
                                                                                  .ToDictionary(x => x.Key, x => x.Value.Trim());

                        // Выводим статистику по непустым значениям
                        Console.WriteLine($"Количество непустых значений в колонке D: {dictD.Count}");
                        Console.WriteLine($"Количество непустых значений в колонке B: {dictB.Count}");

                        // Создаем итоговый словарь, приоритет отдаем значениям из колонки D
                        fileDict = new Dictionary<string, string>();
                        
                        // Сначала добавляем все непустые значения из D
                        foreach (KeyValuePair<string, string> pair in dictD)
                        {
                            fileDict[pair.Key] = pair.Value;
                        }
                        
                        // Добавляем значения из B только если ключа нет в D
                        foreach (KeyValuePair<string, string> pair in dictB)
                        {
                            if (!fileDict.ContainsKey(pair.Key))
                            {
                                fileDict[pair.Key] = pair.Value;
                            }
                        }

                        // Выводим итоговую статистику
                        Console.WriteLine($"Итоговое количество уникальных непустых значений: {fileDict.Count}");
                    }
                    else if (isTranslatedData)
                    {
                        // Для файлов из TranslatedData используем колонку E
                        Console.WriteLine($"Применяем логику колонки E для переведенного файла: {path}");
                        fileDict = XmlTableToDict(path, 4); // Колонка E
                    }
                    else
                    {
                        // Для остальных файлов используем стандартную логику
                        Console.WriteLine($"Применяем стандартную логику для колонки {defaultColumn}: {path}");
                        fileDict = XmlTableToDict(path, defaultColumn);
                    }

                    if (fileDict != null)
                    {
                        foreach (KeyValuePair<string, string> pair in fileDict.Where(p => p.Key != "ID"))
                        {
                            if (!total.ContainsKey(pair.Key))
                            {
                                total.Add(pair.Key, pair.Value);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке файла {path}: {ex.Message}");
                    throw;
                }
            }

            AjLocalizInJsonFile jsonFile = new AjLocalizInJsonFile();
            jsonFile.Data = total;

            return jsonFile;
        }

        private AjLocalizInJsonFile WriteJsonFile(AjLocalizInJsonFile jsonFile, string pathToJson)
        {
            File.WriteAllText(pathToJson, JsonConvert.SerializeObject(jsonFile));

            return jsonFile;
        }

        public AjLocalizInJsonFile ConvertXmlToJson(string[] pathsToXmls, string pathToJson, int column)
        {
            Dictionary<string, string> total = new Dictionary<string, string>();

            foreach (string el in pathsToXmls)
            {
                Dictionary<string, string> fileDict = XmlTableToDict(el, column);

                foreach (KeyValuePair<string, string> pair in fileDict.Where(pair => pair.Key != "ID"))
                {
                    total.Add(pair.Key, pair.Value);
                }
            }

            AjLocalizInJsonFile jsonFile = new AjLocalizInJsonFile();
            jsonFile.Data = total;

            File.WriteAllText(pathToJson, JsonConvert.SerializeObject(jsonFile));

            return jsonFile;
        }

        public static string GetLocalizTablesPath(string projPath)
        {
            string path = projPath + @"\Raw\loc_All objects_en.xlsx";

            if (!File.Exists(path)) path = projPath + @"\Raw\loc_All objects_ru.xlsx";

            return path;
        }

        public static string GetFlowJsonPath(string projPath) { return projPath + @"\Raw\Flow.json"; }

        public static string GetMetaJsonPath(string projPath) { return projPath + @"\Raw\Meta.json"; }
    }
}