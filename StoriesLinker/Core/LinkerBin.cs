using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using OfficeOpenXml;
using StoriesLinker.Interfaces;
using StoriesLinker.Utils;

namespace StoriesLinker
{
    public class LinkerBin(string projectPath)
    {
        private readonly string _projectPath = projectPath;
        private readonly Dictionary<string, Dictionary<int, Dictionary<string, string>>> _savedXmlDicts = new();
        private readonly Dictionary<string, Dictionary<string, LocalizationEntry>> _cachedLocalizationData = new();
        private readonly Dictionary<string, Dictionary<string, string>> _cachedTranslations = new();
        private readonly StringPool _stringPool = new();
        private ArticyExportData _cachedFlowJson;
        private AjLinkerMeta _cachedMetaData;
        private Dictionary<string, string> _cachedLocalizationDict;
        private int _allWordsCount = 0;
        private static Dictionary<string, string> missingFiles = new();
        private List<string> _cachedSortedChapterIds;
        private ArticyExportData _cachedArticyExportData;
        private AjLinkerMeta _cachedMeta;
        private Dictionary<string, Model> _cachedBookEntities;
        private ArticyExportData _cachedEntitiesArticyExportData;
        private Dictionary<string, string> _cachedEntitiesNativeDict;
        private readonly Dictionary<LocalizationCacheKey, Dictionary<string, LocalizationEntry>> _localizationCache = new();

        private class LocalizationEntry
        {
            public string Text { get; set; }
            public string SpeakerDisplayName { get; set; }
            public string Emotion { get; set; }
            public bool IsInternal { get; set; }
        }

        private class StringPool
        {
            private readonly HashSet<string> _strings = new();

            public string Intern(string str)
            {
                if (string.IsNullOrEmpty(str)) return str;

                if (_strings.TryGetValue(str, out string existing)) return existing;

                _strings.Add(str);
                return str;
            }
        }

        private readonly struct LocalizationCacheKey
        {
            public string Chapter { get; }
            public string Language { get; }
            public bool IsInternal { get; }

            public LocalizationCacheKey(string chapter, string language, bool isInternal)
            {
                Chapter = chapter;
                Language = language;
                IsInternal = isInternal;
            }
        }

        #region Работа с Excel файлами и преобразование в словари

        /// <summary>
        /// Преобразует Excel таблицу в словарь ключ-значение
        /// </summary>
        private Dictionary<string, string> ConvertExcelToDictionary(string path, int column = 1)
        {
            if (_savedXmlDicts.TryGetValue(path, out Dictionary<int, Dictionary<string, string>> columnsDict) &&
                columnsDict.TryGetValue(column, out Dictionary<string, string> cachedDict))
                return cachedDict;

            var nativeDict = new Dictionary<string, string>();

            using (var xlPackage = new ExcelPackage(new FileInfo(path)))
            {
                if (xlPackage.Workbook.Worksheets.Count == 0)
                    throw new InvalidOperationException("The workbook contains no worksheets.");
                ExcelWorksheet myWorksheet = xlPackage.Workbook.Worksheets.First();
                int totalRows = myWorksheet.Dimension.End.Row;
                int totalColumns = myWorksheet.Dimension.End.Column;

                for (var rowNum = 1; rowNum <= totalRows; rowNum++)
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
                    if (string.IsNullOrWhiteSpace(firstRowStr) || string.IsNullOrWhiteSpace(secondRowStr)) continue;

                    if (!nativeDict.ContainsKey(firstRowStr))
                        nativeDict.Add(firstRowStr, secondRowStr);
                    else
                        Console.WriteLine($"Обнаружен дублирующийся ключ: {firstRowStr}");
                }
            }

            if (!_savedXmlDicts.ContainsKey(path))
                _savedXmlDicts[path] = new Dictionary<int, Dictionary<string, string>>();
            _savedXmlDicts[path][column] = nativeDict;

            return nativeDict;
        }

        /// <summary>
        /// Получает словарь локализации из Excel файла
        /// </summary>
        public Dictionary<string, string> GetLocalizationDictionary()
        {
            try
            {
                if (_cachedLocalizationDict != null)
                    return _cachedLocalizationDict;

                string path = GetLocalizationTablesPath(_projectPath);
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    Console.WriteLine("Файлы локализации отсутствуют. Создаем новые файлы...");
                    return new Dictionary<string, string>();
                }

                _cachedLocalizationDict = ConvertExcelToDictionary(path);
                return _cachedLocalizationDict ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке словаря локализации: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        #endregion

        #region Парсинг JSON файлов

        /// <summary>
        /// Парсит Flow.json файл
        /// </summary>
        public ArticyExportData ParseFlowJsonFile()
        {
            if (_cachedFlowJson != null) return _cachedFlowJson;

            using var r = new StreamReader(GetFlowJsonPath(_projectPath));
            string json = r.ReadToEnd();
            _cachedFlowJson = JsonConvert.DeserializeObject<ArticyExportData>(json);

            return _cachedFlowJson;
        }

        /// <summary>
        /// Парсит Meta.json файл и связанные Excel таблицы
        /// </summary>
        public AjLinkerMeta ParseMetaDataFromExcel()
        {
            if (_cachedMetaData != null) return _cachedMetaData;

            _cachedMetaData = new AjLinkerMeta { Version = new BookVersionInfo() };

            string metaXmlPath = _projectPath + @"\Raw\Meta.xlsx";

            var nativeDict = new Dictionary<string, string>();

            using var xlPackage = new ExcelPackage(new FileInfo(metaXmlPath));

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
                    case "UniqueID": _cachedMetaData.UniqueId = fieldValue; break;
                    case "SpritePrefix": _cachedMetaData.SpritePrefix = fieldValue; break;
                    case "VersionBin": _cachedMetaData.Version.BinVersion = fieldValue; break;
                    case "VersionPreview": _cachedMetaData.Version.PreviewVersion = fieldValue; break;
                    case "VersionBaseResources": _cachedMetaData.Version.BaseResourcesVersion = fieldValue; break;
                    case "StandartizedUI": _cachedMetaData.StandartizedUi = fieldValue == "1"; break;
                    case "UITextBlockFontSize": _cachedMetaData.UiTextBlockFontSize = int.Parse(fieldValue); break;
                    case "UIChoiceBlockFontSize": _cachedMetaData.UiChoiceBlockFontSize = int.Parse(fieldValue); break;
                    case "KarmaCurrency": _cachedMetaData.KarmaCurrency = fieldValue; break;
                    case "KarmaBadBorder": _cachedMetaData.KarmaBadBorder = int.Parse(fieldValue); break;
                    case "KarmaGoodBorder": _cachedMetaData.KarmaGoodBorder = int.Parse(fieldValue); break;
                    case "KarmaTopLimit": _cachedMetaData.KarmaTopLimit = int.Parse(fieldValue); break;
                    case "CurrenciesInOrderOfUI":
                        _cachedMetaData.CurrenciesInOrderOfUi = new List<string>(fieldValue.Split(','));
                        break;
                    case "RacesList":
                        _cachedMetaData.RacesList = fieldValue != "-"
                                                        ? new List<string>(fieldValue.Split(','))
                                                        : new List<string>();
                        break;
                    case "ClothesSpriteNames":
                        _cachedMetaData.ClothesSpriteNames = new List<string>(fieldValue.Split(','));
                        break;
                    case "UndefinedClothesFuncVariant":
                        _cachedMetaData.UndefinedClothesFuncVariant = int.Parse(fieldValue);
                        break;
                    case "ExceptionsWeaponLayer": _cachedMetaData.ExceptionsWeaponLayer = fieldValue == "1"; break;
                    case "UITextPlateLimits":
                        values = fieldValue.Split(',');

                        _cachedMetaData.UiTextPlateLimits = new List<int>();

                        foreach (string el in values) _cachedMetaData.UiTextPlateLimits.Add(int.Parse(el));

                        break;
                    case "UIPaintFirstLetterInRedException":
                        _cachedMetaData.UiPaintFirstLetterInRedException = fieldValue == "1";
                        break;
                    case "UITextPlateOffset": _cachedMetaData.UiTextPlateOffset = int.Parse(fieldValue); break;
                    case "UIOverridedTextColor": _cachedMetaData.UiOverridedTextColor = fieldValue == "1"; break;
                    case "UITextColor":
                        values = fieldValue.Split(',');

                        _cachedMetaData.UiTextColor = new List<int>();

                        foreach (string el in values) _cachedMetaData.UiTextColor.Add(int.Parse(el));

                        break;
                    case "UIBlockedTextColor":
                        values = fieldValue.Split(',');

                        _cachedMetaData.UiBlockedTextColor = new List<int>();

                        foreach (string el in values) _cachedMetaData.UiBlockedTextColor.Add(int.Parse(el));

                        break;
                    case "UIChNameTextColor":
                        values = fieldValue.Split(',');

                        _cachedMetaData.UiChNameTextColor = new List<int>();

                        foreach (string el in values) _cachedMetaData.UiChNameTextColor.Add(int.Parse(el));

                        break;
                    case "UIOutlineColor":
                        values = fieldValue.Split(',');

                        _cachedMetaData.UiOutlineColor = new List<int>();

                        foreach (string el in values) _cachedMetaData.UiOutlineColor.Add(int.Parse(el));

                        break;
                    case "UIResTextColor":
                        values = fieldValue.Split(',');

                        _cachedMetaData.UiResTextColor = new List<int>();

                        foreach (string el in values) _cachedMetaData.UiResTextColor.Add(int.Parse(el));

                        break;
                    case "WardrobeEnabled": _cachedMetaData.WardrobeEnabled = fieldValue == "1"; break;
                    case "MainHeroHasDifferentGenders":
                        _cachedMetaData.MainHeroHasDifferentGenders = fieldValue == "1";
                        break;
                    case "MainHeroHasSplittedHairSprite":
                        _cachedMetaData.MainHeroHasSplittedHairSprite = fieldValue == "1";
                        break;
                    case "CustomClothesCount": _cachedMetaData.CustomClothesCount = int.Parse(fieldValue); break;
                    case "CustomHairsCount": _cachedMetaData.CustomHairCount = int.Parse(fieldValue); break;
                }
            }

            myWorksheet = xlPackage.Workbook.Worksheets[2];
            totalRows = myWorksheet.Dimension.End.Row;

            Func<object[], int> checkRow = ValidateExcelRow();

            var characters = new List<AjMetaCharacterData>();

            for (var rowNum = 2; rowNum <= totalRows; rowNum++)
            {
                object[] cells =
                [
                    myWorksheet.Cells[rowNum, 1].Value,
                    myWorksheet.Cells[rowNum, 2].Value,
                    myWorksheet.Cells[rowNum, 3].Value,
                    myWorksheet.Cells[rowNum, 4].Value
                ];

                int chResult = checkRow(cells);

                switch (chResult)
                {
                    case -1: continue;
                    case 0: return null;
                }

                var ch = new AjMetaCharacterData();

                ch.DisplayName = cells[0].ToString();
                ch.ClothesVariableName = cells[1].ToString();
                ch.AtlasFileName = cells[2].ToString();
                ch.BaseNameInAtlas = cells[3].ToString();

                characters.Add(ch);
            }

            _cachedMetaData.Characters = characters;

            myWorksheet = xlPackage.Workbook.Worksheets[3];
            totalRows = myWorksheet.Dimension.End.Row;

            var locations = new List<AjMetaLocationData>();

            for (var rowNum = 2; rowNum <= totalRows; rowNum++)
            {
                object[] cells =
                [
                    myWorksheet.Cells[rowNum, 1].Value,
                    myWorksheet.Cells[rowNum, 2].Value,
                    myWorksheet.Cells[rowNum, 3].Value,
                    myWorksheet.Cells[rowNum, 4].Value,
                    myWorksheet.Cells[rowNum, 5].Value
                ];

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

                if (cells[4].ToString() == "1") _cachedMetaData.IntroLocation = rowNum - 1;

                locations.Add(loc);
            }

            _cachedMetaData.Locations = locations;

            return _cachedMetaData;
        }

        #endregion

        #region Вспомогательные методы для проверки данных

        /// <summary>
        /// Проверяет строку на пустоту и корректность данных
        /// </summary>
        private static Func<object[], int> ValidateExcelRow()
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

        #endregion

        #region Работа с сущностями книги

        /// <summary>
        /// Получает все сущности книги из Flow.json
        /// </summary>
        public Dictionary<string, Model> ExtractBookEntities(ArticyExportData ajfile, Dictionary<string, string> nativeDict)
        {
            // Проверяем кэш
            if (_cachedBookEntities != null &&
                _cachedEntitiesArticyExportData == ajfile &&
                _cachedEntitiesNativeDict == nativeDict)
            {
                return _cachedBookEntities;
            }

            var objectsList = new Dictionary<string, Model>();

            List<Model> models = ajfile.Packages[0].Models;

            var chaptersIdNames = new Dictionary<string, int>();

            foreach (Model ns in models)
            {

                switch (ns.Type)
                {
                    case "FlowFragment":
                        string displayName = ns.Properties.DisplayName;
                        if (string.IsNullOrEmpty(displayName))
                        {
                            Form1.ShowMessage($"Пустое название фрагмента с ID: {ns.Properties.Id}");
                            continue;
                        }

                        if (!nativeDict.TryGetValue(displayName, out string translatedName))
                        {
                            Form1.ShowMessage($"Отсутствует перевод для названия фрагмента: {displayName}");
                            continue;
                        }

                        string value = Regex.Match(translatedName, @"\d+").Value;
                        if (string.IsNullOrEmpty(value))
                        {
                            Form1.ShowMessage($"Некорректный формат названия фрагмента (нет номера): {translatedName}");
                            throw new FormatException($"Некорректный формат названия фрагмента (нет номера): {translatedName}");
                        }

                        int intValue = int.Parse(value);

                        chaptersIdNames.Add(ns.Properties.Id, intValue);
                        break;
                }



                objectsList.Add(ns.Properties.Id, ns);
            }

            // Сохраняем в кэш
            _cachedBookEntities = objectsList;
            _cachedEntitiesArticyExportData = ajfile;
            _cachedEntitiesNativeDict = nativeDict;

            return objectsList;
        }

        /// <summary>
        /// Получает отсортированный список глав
        /// </summary>
        private List<string> GetSortedChapterIds(Dictionary<string, Model> objList,
                                                 Dictionary<string, string> nativeDict)
        {
            if (_cachedSortedChapterIds != null) return _cachedSortedChapterIds;

            var chaptersIds = new List<string>();
            var chaptersIdNames = new Dictionary<string, int>();

            foreach (KeyValuePair<string, Model> kobj in objList)
            {
                if (kobj.Value.TypeEnum != TypeEnum.FlowFragment) continue;

                string displayName = kobj.Value.Properties.DisplayName;
                if (!nativeDict.TryGetValue(displayName, out string translatedName))
                {
                    Form1.ShowMessage($"Отсутствует перевод для названия главы: {displayName}");
                    throw new KeyNotFoundException($"Отсутствует перевод для названия главы: {displayName}");
                }

                string value = Regex.Match(translatedName, @"\d+").Value;
                if (string.IsNullOrEmpty(value))
                {
                    Form1.ShowMessage($"Некорректный формат названия главы (нет номера): {translatedName}");
                    throw new FormatException($"Некорректный формат названия главы (нет номера): {translatedName}");
                }

                int intValue = int.Parse(value);
                chaptersIdNames.Add(kobj.Value.Properties.Id, intValue);
            }

            IOrderedEnumerable<KeyValuePair<string, int>> sortedChapterNames =
                from entry in chaptersIdNames orderby entry.Value ascending select entry;

            foreach (KeyValuePair<string, int> pair in sortedChapterNames) chaptersIds.Add(pair.Key);

            _cachedSortedChapterIds = chaptersIds;
            return _cachedSortedChapterIds;
        }

        /// <summary>
        /// Получает ID глав и подглав
        /// </summary>
        private List<string>[] GetChapterAndSubchapterHierarchy(List<string> chaptersIds,
                                                                Dictionary<string, Model> objList)
        {
            var ids = new List<List<string>>();

            for (var i = 0; i < chaptersIds.Count; i++)
            {
                string chapterId = chaptersIds[i];

                ids.Add(new List<string>());
                ids[i].Add(chapterId);

                foreach (KeyValuePair<string, Model> kobj in objList)
                {
                    if (kobj.Value.TypeEnum != TypeEnum.Dialogue) continue; //subchapter 

                    string subchapterId = kobj.Value.Properties.Id;

                    string parent = kobj.Value.Properties.Parent;

                    while (true)
                        if (parent == chapterId)
                        {
                            ids[i].Add(subchapterId);
                            break;
                        }
                        else
                        {
                            if (objList.TryGetValue(parent, out Model value))
                                parent = value.Properties.Parent;
                            else
                                break;
                        }
                }
            }

            return ids.ToArray();
        }

        #endregion

        #region Генерация таблиц локализации

        /// <summary>
        /// Загружает базовые данные: словарь локализации, Flow.json и список объектов
        /// </summary>
        /// <returns>Кортеж из словаря локализации, объекта AjFile и словаря объектов</returns>
        public ArticyExportData LoadBaseData()
        {
            // Используем DataCacheManager для получения ArticyExportData
            if (DataCacheManager.TryGetArticyData(_projectPath, out var articyData, out _))
            {
                return articyData;
            }

            Console.WriteLine("Ошибка при загрузке базовых данных.");
            return null;
        }

        /// <summary>
        /// Генерирует таблицы локализации для всех глав
        /// </summary>
        public bool GenerateLocalizationTables()
        {
            try
            {
                // Загружаем базовые данные один раз
                ArticyExportData ajfile = LoadBaseData();
                if (ajfile == null)
                {
                    Form1.ShowMessage("❌ Ошибка при загрузке базовых данных.");
                    return false;
                }

                Dictionary<string, string> nativeDict = ajfile.NativeMap;
                if (nativeDict == null)
                {
                    Form1.ShowMessage("❌ Словарь локализации пуст или не загружен.");
                    nativeDict = new Dictionary<string, string>();
                }

                Dictionary<string, Model> objectsList = ajfile.GetModelDictionary();
                if (objectsList == null || objectsList.Count == 0)
                {
                    Form1.ShowMessage("❌ Список объектов пуст или не загружен.");
                    return false;
                }

                List<string> chaptersIds = GetSortedChapterIds(objectsList, nativeDict);
                if (chaptersIds == null || chaptersIds.Count == 0)
                {
                    Form1.ShowMessage("❌ Не удалось получить идентификаторы глав.");
                    return false;
                }

                if (chaptersIds.Count < Form1.AvailableChapters)
                {
                    Form1.ShowMessage("❌ Глав в книге меньше введённого количества");
                    return false;
                }

                chaptersIds.RemoveRange(Form1.AvailableChapters, chaptersIds.Count - Form1.AvailableChapters);
                List<string>[] csparentsIds = GetChapterAndSubchapterHierarchy(chaptersIds, objectsList);
                if (csparentsIds == null || csparentsIds.Length == 0)
                {
                    Form1.ShowMessage("❌ Не удалось создать иерархию глав и подглав.");
                    return false;
                }

                // Проверяем наличие файлов локализации
                string russianFolder = Path.Combine(_projectPath, "Localization", "Russian");
                bool needInitialization = !Directory.Exists(russianFolder);

                if (!needInitialization)
                {
                    // Проверяем наличие файлов для каждой главы
                    for (var i = 0; i < csparentsIds.Length; i++)
                    {
                        string forTranslatingPath = Path.Combine(russianFolder, $"Chapter_{i + 1}_for_translating.xlsx");
                        string internalPath = Path.Combine(russianFolder, $"Chapter_{i + 1}_internal.xlsx");

                        if (!File.Exists(forTranslatingPath) || !File.Exists(internalPath))
                        {
                            needInitialization = true;
                            break;
                        }
                    }
                }

                if (needInitialization)
                {
                    Console.WriteLine("Файлы локализации отсутствуют. Создаем новые файлы...");
                    if (!InitializeLocalization())
                    {
                        return false;
                    }
                }

                var charactersIds = new List<string>();
                Dictionary<string, LocalizationEntry> charactersLocalizData = new();
                var charactersNames = new Dictionary<string, string>();

                // Обработка глав с использованием кеша
                for (var i = 0; i < csparentsIds.Length; i++)
                {
                    try
                    {
                        int chapterN = i + 1;
                        if (csparentsIds[i] == null || csparentsIds[i].Count == 0)
                        {
                            Form1.ShowMessage($"❌ Список ID для главы {chapterN} пуст.");
                            continue;
                        }

                        ProcessChapterLocalization(chapterN, csparentsIds[i], objectsList, nativeDict,
                            charactersIds, charactersLocalizData, charactersNames);
                    }
                    catch (Exception ex)
                    {
                        Form1.ShowMessage($"❌ Ошибка при обработке главы {i + 1}: {ex.Message}");
                        return false;
                    }
                }

                // Сохраняем данные персонажей в кеш
                _cachedLocalizationData["characters"] = charactersLocalizData;

                if (Form1.FOR_LOCALIZATORS_MODE)
                {
                    Console.WriteLine($"Общее количество слов: {_allWordsCount}");
                    Form1.ShowMessage($"✅ Генерация таблиц локализации завершена. Общее количество слов: {_allWordsCount}");
                }
                else
                {
                    Form1.ShowMessage("✅ Генерация таблиц локализации успешно завершена");
                }

                return true;
            }
            catch (Exception ex)
            {
                Form1.ShowMessage($"❌ Критическая ошибка при генерации таблиц локализации: {ex.Message}");
                Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
                return false;
            }
        }

        private void ProcessChapterLocalization(int chapterN, List<string> parentsIds,
                                                Dictionary<string, Model> objectsList, Dictionary<string, string> nativeDict,
                                                List<string> charactersIds, Dictionary<string, LocalizationEntry> charactersLocalizData,
                                                Dictionary<string, string> charactersNames)
        {
            if (parentsIds == null || objectsList == null || nativeDict == null ||
                charactersIds == null || charactersLocalizData == null || charactersNames == null)
            {
                Form1.ShowMessage($"❌ Один из параметров ProcessChapterLocalization равен null для главы {chapterN}");
                return;
            }

            var chapterKey = $"chapter_{chapterN}_Russian";
            Dictionary<string, LocalizationEntry> chapterData = new();
            Dictionary<string, LocalizationEntry> internalData = new();

            Console.WriteLine($"Обработка главы {chapterN}...");

            foreach (KeyValuePair<string, Model> scobj in objectsList)
            {
                // Проверка на null перед обращением к свойствам
                if (scobj.Value?.Properties?.Parent == null) continue;

                if (!parentsIds.Contains(scobj.Value.Properties.Parent)) continue;

                Model dfobj = scobj.Value;
                if (dfobj == null) continue;

                if (dfobj.TypeEnum != TypeEnum.DialogueFragment) continue;

                // Проверка на null для свойств диалоговых фрагментов
                if (dfobj.Properties == null) continue;

                string chId = dfobj.Properties.Speaker;
                if (string.IsNullOrEmpty(chId))
                {
                    Form1.ShowMessage($"❌ Пустой ID спикера в главе {chapterN}");
                    continue;
                }

                if (!objectsList.TryGetValue(chId, out Model character))
                {
                    Form1.ShowMessage($"❌ Не найден персонаж с ID {chId} в главе {chapterN}");
                    continue;
                }

                ProcessCharacterData(chId, character, nativeDict, chapterN, charactersIds,
                    charactersLocalizData, charactersNames);

                // Проверяем, доступен ли charactersNames[chId] после обработки персонажа
                if (!charactersNames.ContainsKey(chId))
                {
                    Form1.ShowMessage($"❌ Не удалось получить имя персонажа с ID {chId} после обработки");
                    continue;
                }

                ProcessDialogueFragmentData(dfobj, nativeDict, chapterN, charactersNames[chId],
                    chapterData, internalData);
            }

            // Сохраняем данные в кэш
            _cachedLocalizationData[chapterKey] = chapterData;
            _cachedLocalizationData[chapterKey + "_internal"] = internalData;

            // Подсчитываем количество слов для статистики
            if (!Form1.FOR_LOCALIZATORS_MODE) return;

            foreach (LocalizationEntry entry in chapterData.Values.Where(e => !string.IsNullOrEmpty(e.Text)))
                _allWordsCount += CalculateWordCount(entry.Text);

            Console.WriteLine($"Глава {chapterN} обработана, количество слов: {_allWordsCount}");
        }

        private void ProcessCharacterData(string chId,
            Model character,
            Dictionary<string, string> nativeDict,
            int chapterN,
            List<string> charactersIds,
            Dictionary<string, LocalizationEntry> charactersLocalizData,
            Dictionary<string, string> charactersNames)
        {
            // Проверка на null параметров
            if (string.IsNullOrEmpty(chId) || character == null || nativeDict == null ||
                charactersIds == null || charactersLocalizData == null || charactersNames == null)
            {
                Form1.ShowMessage($"❌ Один из параметров ProcessCharacterData равен null для главы {chapterN}");
                return;
            }

            if (charactersIds.Contains(chId)) return;

            // Проверка на null свойств персонажа
            if (character.Properties == null)
            {
                Form1.ShowMessage($"❌ Properties персонажа с ID {chId} равен null в главе {chapterN}");
                return;
            }

            string displayName = character.Properties.DisplayName;
            if (string.IsNullOrEmpty(displayName))
            {
                Form1.ShowMessage($"❌ Пустое имя персонажа с ID {chId} в главе {chapterN}");
                return;
            }

            if (!nativeDict.TryGetValue(displayName, out string characterText))
            {
                Console.WriteLine($"Отсутствует перевод для персонажа: {displayName} в главе {chapterN}");
                characterText = string.Empty;
            }

            charactersIds.Add(chId);
            charactersLocalizData[displayName] = new LocalizationEntry
            {
                Text = _stringPool.Intern(characterText),
                SpeakerDisplayName = string.Empty,
                IsInternal = false
            };

            charactersNames[chId] = characterText;
        }

        private void ProcessDialogueFragmentData(Model dfobj,
                                                 Dictionary<string, string> nativeDict,
                                                 int chapterN,
                                                 string speakerName,
                                                 Dictionary<string, LocalizationEntry> chapterData,
                                                 Dictionary<string, LocalizationEntry> internalData)
        {
            // Проверка на null параметров
            if (dfobj == null || nativeDict == null || string.IsNullOrEmpty(speakerName) ||
                chapterData == null || internalData == null)
            {
                Form1.ShowMessage($"❌ Один из параметров ProcessDialogueFragmentData равен null для главы {chapterN}");
                return;
            }

            // Проверка наличия Properties
            if (dfobj.Properties == null)
            {
                Form1.ShowMessage($"❌ Properties у диалогового фрагмента равен null в главе {chapterN}");
                return;
            }

            // Обработка основного текста
            if (!string.IsNullOrEmpty(dfobj.Properties.Text))
            {
                if (!nativeDict.TryGetValue(dfobj.Properties.Text, out string translatedText))
                {
                    Console.WriteLine($"Отсутствует перевод для текста: {dfobj.Properties.Text} в главе {chapterN}");
                    translatedText = string.Empty;
                }

                if (translatedText != string.Empty)
                {
                    chapterData[dfobj.Properties.Id] = new LocalizationEntry
                    {
                        Text = _stringPool.Intern(translatedText),
                        SpeakerDisplayName = _stringPool.Intern(speakerName),
                        IsInternal = false
                    };
                }
            }

            // Обработка меню-текста
            if (!string.IsNullOrEmpty(dfobj.Properties.MenuText))
            {
                if (!nativeDict.TryGetValue(dfobj.Properties.MenuText, out string translatedMenuText))
                {
                    Console.WriteLine($"Отсутствует перевод для меню-текста: {dfobj.Properties.MenuText} в главе {chapterN}");
                    translatedMenuText = string.Empty;
                }

                if (translatedMenuText != string.Empty)
                {
                    string keyId = dfobj.Properties.Id + "_Choice";
                    chapterData[keyId] = new LocalizationEntry
                    {
                        Text = _stringPool.Intern(translatedMenuText),
                        SpeakerDisplayName = string.Empty,
                        IsInternal = false
                    };
                }
            }

            // Обработка внутреннего текста
            if (!string.IsNullOrEmpty(dfobj.Properties.StageDirections))
            {
                if (!nativeDict.TryGetValue(dfobj.Properties.StageDirections, out string translatedStageText))
                {
                    Console.WriteLine($"Отсутствует перевод для внутреннего текста: {dfobj.Properties.StageDirections} в главе {chapterN}");
                    translatedStageText = string.Empty;
                }

                if (translatedStageText != string.Empty)
                {
                    internalData[dfobj.Properties.Id] = new LocalizationEntry
                    {
                        Text = _stringPool.Intern(translatedStageText),
                        SpeakerDisplayName = string.Empty,
                        IsInternal = true
                    };
                }
            }
        }

        /// <summary>
        /// Подсчитывает количество слов в тексте
        /// </summary>
        public int CalculateWordCount(string text)
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

        #region Генерация выходной папки

        public bool GenerateOutputStructure()
        {
            try
            {
                if (!InitializeAndValidateData(out ArticyExportData ajfile, out AjLinkerMeta meta))
                {
                    Form1.ShowMessage("❌ Ошибка при инициализации и валидации данных");
                    return false;
                }

                string tempFolder = CreateAndInitializeTempFolders(meta);
                (Dictionary<string, string> nativeDict, Dictionary<string, Model> objectsList) =
                    LoadAndPrepareData(ajfile);
                List<string> chaptersIds = PrepareChaptersData(objectsList, nativeDict);

                if (!ValidateChaptersCount(chaptersIds))
                {
                    Form1.ShowMessage("❌ Ошибка при валидации количества глав");
                    return false;
                }

                (AjAssetGridLinker gridLinker, List<Model> sharedObjs) =
                    InitializeGridAndSharedObjects(objectsList, meta, nativeDict);
                Dictionary<string, int> langsCols = DetermineLanguageColumns();

                if (!ProcessAllChapters(tempFolder, meta, ajfile, objectsList, nativeDict, gridLinker, sharedObjs,
                                        langsCols))
                {
                    Form1.ShowMessage("❌ Ошибка при обработке глав");
                    return false;
                }

                Form1.ShowMessage("✅ Генерация структуры успешно завершена");
                return true;
            }
            catch (Exception ex)
            {
                Form1.ShowMessage($"❌ Критическая ошибка при генерации структуры: {ex.Message}");
                return false;
            }
        }

        private bool InitializeAndValidateData(out ArticyExportData ajfile, out AjLinkerMeta meta)
        {
            Form1.ShowMessage("Начинаем...");

            ajfile = LoadBaseData();
            meta = _cachedMeta ??= ParseMetaDataFromExcel();

            if (meta != null && ajfile != null)
            {
                return ValidateCharactersData(meta, ajfile) &&
                       ValidateLocationsData(meta);
            }

            Form1.ShowMessage("Ошибка загрузки данных: Meta или Flow JSON не могут быть загружены.");
            return false;
        }

        /// <summary>
        /// Проверяет персонажа на корректность данных
        /// </summary>
        private bool ValidateCharacter(AjMetaCharacterData character, ArticyExportData ajfile)
        {
            // Проверка вторичного персонажа
            if (character.AtlasFileName.Contains("Sec_") || character.BaseNameInAtlas.Contains("Sec_"))
            {
                if (character.AtlasFileName != character.BaseNameInAtlas)
                {
                    Form1.ShowMessage($"AtlasFileName и BaseNameInAtlas у второстепенных должны быть одинаковы: {character.DisplayName}");
                    return false;
                }
            }

            // Проверка одежды персонажа
            if (character.ClothesVariableName.Trim() == "-") return true;

            int clothesNsIndex = ajfile.GlobalVariables.FindIndex(ns => ns.Namespace == "Clothes");
            if (clothesNsIndex != -1 &&
                ajfile.GlobalVariables[clothesNsIndex]
                      .Variables.FindIndex(v => v.Variable == character.ClothesVariableName) != -1)
                return true;
            Form1.ShowMessage($"В артиси не определена переменная с именем Clothes.{character.ClothesVariableName}");
            return false;
        }

        private bool ValidateCharactersData(AjLinkerMeta meta, ArticyExportData ajfile)
        {
            for (var i = 0; i < meta.Characters.Count; i++)
            {
                AjMetaCharacterData character = meta.Characters[i];

                if (!ValidateCharacterDuplicates(meta, character, i)) return false;
                if (!ValidateCharacter(character, ajfile)) return false;
            }

            return true;
        }

        private bool ValidateCharacterDuplicates(AjLinkerMeta meta, AjMetaCharacterData character, int currentIndex)
        {
            for (var j = 0; j < meta.Characters.Count; j++)
            {
                if (currentIndex == j) continue;

                AjMetaCharacterData aObj = meta.Characters[j];
                if (character.DisplayName != aObj.DisplayName &&
                    (character.BaseNameInAtlas != aObj.BaseNameInAtlas ||
                     character.BaseNameInAtlas == "-" ||
                     meta.UniqueId == "Shism_1") &&
                    (character.ClothesVariableName != aObj.ClothesVariableName || character.ClothesVariableName.Trim() == "-"))
                    continue;

                Form1.ShowMessage($"Найдены дублирующиеся значения среди персонажей: {aObj.DisplayName}");
                return false;
            }

            return true;
        }

        private bool ValidateLocationsData(AjLinkerMeta meta)
        {
            if (meta.UniqueId == "Pirates_1") return true;

            for (var i = 0; i < meta.Locations.Count; i++)
            {
                AjMetaLocationData cObj = meta.Locations[i];
                for (var j = 0; j < meta.Locations.Count; j++)
                {
                    if (i == j) continue;

                    AjMetaLocationData aObj = meta.Locations[j];
                    if (cObj.DisplayName != aObj.DisplayName && cObj.SpriteName != aObj.SpriteName) continue;

                    Form1.ShowMessage($"Найдены дублирующиеся значения среди локаций: {aObj.DisplayName}");
                    return false;
                }
            }

            return true;
        }

        private string CreateAndInitializeTempFolders(AjLinkerMeta meta)
        {
            string tempFolder = Path.Combine(_projectPath, "Temp");

            try
            {
                if (Directory.Exists(tempFolder))
                {
                    // Попытка очистить папку вместо полного удаления
                    foreach (string file in Directory.GetFiles(tempFolder, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (IOException)
                        {
                            Form1.ShowMessage($"Не удалось удалить файл {file} - возможно он используется другим процессом");
                            throw;
                        }
                    }

                    foreach (string dir in Directory.GetDirectories(tempFolder))
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                        }
                        catch (IOException)
                        {
                            Form1.ShowMessage($"Не удалось удалить папку {dir} - возможно она используется другим процессом");
                            throw;
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory(tempFolder);
                }

                InitializeFolderStructure(tempFolder, meta);
                return tempFolder;
            }
            catch (Exception ex)
            {
                Form1.ShowMessage($"Ошибка при подготовке временной папки: {ex.Message}");
                throw;
            }
        }

        private void InitializeFolderStructure(string tempFolder, AjLinkerMeta meta)
        {
            Func<string, string, string> getVersionName = GenerateVersionFolderName();
            string binFolder = Path.Combine(tempFolder, getVersionName("bin", meta.Version.BinVersion));
            string brFolder =
                Path.Combine(tempFolder, getVersionName("baseResources", meta.Version.BaseResourcesVersion));
            string previewFolder = Path.Combine(tempFolder, getVersionName("preview", meta.Version.PreviewVersion));

            CreateDirectoryStructure(previewFolder, ["Covers", "Strings"]);
            CreateDirectoryStructure(binFolder, ["SharedStrings"]);
            CreateDirectoryStructure(brFolder, ["UI", "Music"]);
        }

        private void CreateDirectoryStructure(string baseFolder, string[] subFolders)
        {
            Directory.CreateDirectory(baseFolder);
            foreach (string folder in subFolders) Directory.CreateDirectory(Path.Combine(baseFolder, folder));
        }

        private (Dictionary<string, string> nativeDict, Dictionary<string, Model> objectsList) LoadAndPrepareData(ArticyExportData ajfile)
        {
            Dictionary<string, string> nativeDict = ajfile.NativeMap;
            Dictionary<string, Model> objectsList = ajfile.GetModelDictionary();
            return (nativeDict, objectsList);
        }

        private List<string> PrepareChaptersData(Dictionary<string, Model> objectsList, Dictionary<string, string> nativeDict)
        {
            List<string> chaptersIds = GetSortedChapterIds(objectsList, nativeDict);
            if (chaptersIds.Count > Form1.AvailableChapters)
                chaptersIds.RemoveRange(Form1.AvailableChapters, chaptersIds.Count - Form1.AvailableChapters);
            return chaptersIds;
        }

        private bool ValidateChaptersCount(List<string> chaptersIds)
        {
            if (chaptersIds.Count >= Form1.AvailableChapters) return true;

            Form1.ShowMessage("Глав в книге меньше введённого количества");
            return false;
        }

        private Dictionary<string, int> DetermineLanguageColumns()
        {
            string translatedDataFolder = Path.Combine(_projectPath, "TranslatedData");
            bool multiLangOutput = Directory.Exists(translatedDataFolder);
            var langsCols = new Dictionary<string, int> { { "Russian", multiLangOutput ? 3 : 1 } };

            if (multiLangOutput)
            {
                foreach (string folder in Directory.GetDirectories(translatedDataFolder))
                {
                    string language = Path.GetFileName(folder);
                    if (language != "Russian") langsCols.Add(language, 4);
                }
            }

            if (Form1.ONLY_ENGLISH_MODE && !langsCols.ContainsKey("English")) langsCols.Add("English", -1);

            return langsCols;
        }

        private (AjAssetGridLinker gridLinker, List<Model> sharedObjs) InitializeGridAndSharedObjects(
            Dictionary<string, Model> objectsList,
            AjLinkerMeta meta,
            Dictionary<string, string> nativeDict)
        {
            var gridLinker = new AjAssetGridLinker();
            List<Model> sharedObjs = AssignArticyIdsToMetaData(objectsList, meta, nativeDict);
            return (gridLinker, sharedObjs);
        }

        private bool ProcessAllChapters(string tempFolder,
                                        AjLinkerMeta meta,
                                        ArticyExportData ajfile,
                                        Dictionary<string, Model> objectsList,
                                        Dictionary<string, string> nativeDict,
                                        AjAssetGridLinker gridLinker,
                                        List<Model> sharedObjs,
                                        Dictionary<string, int> langsCols)
        {
            Func<string, string, string> getVersionName = GenerateVersionFolderName();
            string binFolder = Path.Combine(tempFolder, getVersionName("bin", meta.Version.BinVersion));
            string brFolder =
                Path.Combine(tempFolder, getVersionName("baseResources", meta.Version.BaseResourcesVersion));
            string previewFolder = Path.Combine(tempFolder, getVersionName("preview", meta.Version.PreviewVersion));

            var gridAssetFile = new AjGridAssetJson();
            var allDicts = new Dictionary<string, Dictionary<string, string>>();
            var origLangData = new Dictionary<string, AjLocalizInJsonFile>();

            List<string>[] csparentsIds =
                GetChapterAndSubchapterHierarchy(GetSortedChapterIds(objectsList, nativeDict), objectsList);
            meta.ChaptersEntryPoints = new List<string>();

            for (var i = 0; i < csparentsIds.Length; i++)
            {
                if (!ProcessSingleChapter(i, csparentsIds[i], tempFolder, binFolder, previewFolder, meta, objectsList,
                                          nativeDict,
                                          gridLinker, gridAssetFile, allDicts, origLangData, langsCols))
                    return false;
            }

            FinalizeOutput(ajfile, sharedObjs, meta, gridAssetFile, binFolder, brFolder, previewFolder);
            return true;
        }

        private void FinalizeOutput(ArticyExportData ajfile,
                                    List<Model> sharedObjs,
                                    AjLinkerMeta meta,
                                    AjGridAssetJson gridAssetFile,
                                    string binFolder,
                                    string brFolder,
                                    string previewFolder)
        {
            var baseJson = new AjLinkerOutputBase { GlobalVariables = ajfile.GlobalVariables, SharedObjs = sharedObjs };

            SaveJsonFiles(binFolder, baseJson, meta, gridAssetFile);
            CopyMusicFiles(brFolder);
            CopyPreviewFiles(previewFolder);
        }

        private void SaveJsonFiles(string binFolder,
                                   AjLinkerOutputBase baseJson,
                                   AjLinkerMeta meta,
                                   AjGridAssetJson gridAssetFile)
        {
            File.WriteAllText(Path.Combine(binFolder, "Base.json"), JsonConvert.SerializeObject(baseJson));
            File.WriteAllText(Path.Combine(binFolder, "Meta.json"), JsonConvert.SerializeObject(meta));
            File.WriteAllText(Path.Combine(binFolder, "AssetsByChapters.json"),
                              JsonConvert.SerializeObject(gridAssetFile));
        }

        private void CopyMusicFiles(string brFolder)
        {
            string musicSourcePath = Path.Combine(_projectPath, "Audio", "Music");
            string musicTempPath = Path.Combine(brFolder, "Music");

            if (!Directory.Exists(musicSourcePath)) return;

            Directory.CreateDirectory(musicTempPath);
            foreach (string srcPath in Directory.GetFiles(musicSourcePath))
                File.Copy(srcPath, srcPath.Replace(musicSourcePath, musicTempPath), true);
        }

        private void CopyPreviewFiles(string previewFolder)
        {
            CopyPreviewCovers(previewFolder);
            CopySliderBanners(previewFolder);
        }

        private void CopyPreviewCovers(string previewFolder)
        {
            string pcoversSourcePath = Path.Combine(_projectPath, "Art", "PreviewCovers");
            string pcoversTempPath = Path.Combine(previewFolder, "Covers");

            if (!ValidatePreviewCovers(pcoversSourcePath)) return;

            CopyDirectoryWithStructure(pcoversSourcePath, pcoversTempPath);
        }

        private bool ValidatePreviewCovers(string pcoversSourcePath)
        {
            if (File.Exists(Path.Combine(pcoversSourcePath, "Russian", "PreviewCover.png"))) return true;

            Form1.ShowMessage("Не все preview обложки присуствуют.");
            return false;

        }

        private void CopySliderBanners(string previewFolder)
        {
            string pbannersSourcePath = Path.Combine(_projectPath, "Art", "SliderBanners");
            if (!Directory.Exists(pbannersSourcePath)) return;

            string pbannersTempPath = Path.Combine(previewFolder, "Banners");
            CopyDirectoryWithStructure(pbannersSourcePath, pbannersTempPath);
        }

        private void CopyDirectoryWithStructure(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourceDir, targetDir));

            foreach (string newPath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourceDir, targetDir), true);
        }

        private bool ProcessSingleChapter(int index,
                                          List<string> parentsIds,
                                          string tempFolder,
                                          string binFolder,
                                          string previewFolder,
                                          AjLinkerMeta meta,
                                          Dictionary<string, Model> objectsList,
                                          Dictionary<string, string> nativeDict,
                                          AjAssetGridLinker gridLinker,
                                          AjGridAssetJson gridAssetFile,
                                          Dictionary<string, Dictionary<string, string>> allDicts,
                                          Dictionary<string, AjLocalizInJsonFile> origLangData,
                                          Dictionary<string, int> langsCols)
        {
            gridLinker.AddChapter();

            int chapterN = index + 1;
            meta.ChaptersEntryPoints.Add(parentsIds[0]);

            var chapterObjs = new List<Model>();

            foreach (KeyValuePair<string, Model> pair in objectsList)
            {
                if (!parentsIds.Contains(pair.Value.Properties.Parent) &&
                    !parentsIds.Contains(pair.Value.Properties.Id))
                    continue;

                Model dfobj = pair.Value;

                switch (dfobj.TypeEnum)
                {
                    case TypeEnum.DialogueFragment:
                        {
                            string chId = dfobj.Properties.Speaker;
                            ValidateAndAddCharacter(nativeDict, objectsList, meta, gridLinker)(chId);
                            break;
                        }
                    case TypeEnum.Dialogue:
                        {
                            List<string> attachments = dfobj.Properties.Attachments;

                            foreach (string el in attachments)
                            {
                                Model atObj = objectsList[el];

                                if (atObj.TypeEnum == TypeEnum.Location)
                                    ValidateAndAddLocation(nativeDict, objectsList, meta, gridLinker)(el);
                                else if (atObj.TypeEnum == TypeEnum.Entity)
                                    ValidateAndAddCharacter(nativeDict, objectsList, meta, gridLinker)(el);
                            }

                            break;
                        }
                    case TypeEnum.Instruction:
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
                                    ValidateAndAddLocationById(meta, gridLinker)(locId);
                                }
                            }

                            break;
                        }
                }

                chapterObjs.Add(dfobj);
            }

            var flowJson = new AjLinkerOutputChapterFlow { Objects = chapterObjs };

            string chapterFolder
                = Path.Combine(tempFolder, GenerateVersionFolderName()("chapter" + chapterN, meta.Version.BinVersion));

            Directory.CreateDirectory(chapterFolder);
            Directory.CreateDirectory(Path.Combine(chapterFolder, "Resources"));
            Directory.CreateDirectory(Path.Combine(chapterFolder, "Strings"));

            File.WriteAllText(Path.Combine(chapterFolder, "Flow.json"), JsonConvert.SerializeObject(flowJson));

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
                    if (ch.BaseNameInAtlas == "-" || atlasFileName == "-" || gridLinker.IsChExist(atlasFileName))
                        continue;

                    gridLinker.AddCharacter(atlasFileName, ch.Aid);

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

                if (!gridLinker.IsLocExist(el))
                {
                    gridLinker.AddLocation(el, loc.Aid);

                    File.Copy(string.Format(_projectPath + @"\Art\Locations\{0}.png", el),
                              string.Format(chapterFolder + @"\Resources\{0}.png", el));
                }

                if (loc.SoundIdleName == "-" || gridLinker.IsLocExist(loc.SoundIdleName)) continue;

                gridLinker.AddLocation(loc.SoundIdleName, loc.Aid);
                File.Copy(string.Format(_projectPath + @"\Audio\Idles\{0}.mp3", loc.SoundIdleName),
                          string.Format(chapterFolder + @"\Resources\{0}.mp3", loc.SoundIdleName));
            }

            var gridAssetChapter = new AjGridAssetChapterJson
            {
                CharactersIDs = gridLinker.GetCharactersIDsFromCurChapter(),
                LocationsIDs = gridLinker.GetLocationsIDsFromCurChapter()
            };

            gridAssetFile.Chapters.Add(gridAssetChapter);

            var origLangDataForChapter = new Dictionary<string, AjLocalizInJsonFile>();

            // Передаем список языков в GenerateLocalizationJson при создании Func
            Func<string, string, string[], string, int, List<string>, string> generateLocalizationJson =
                GenerateLocalizationJson(allDicts, origLangDataForChapter);

            string langOriginFolder = Path.Combine(_projectPath, "Localization", "Russian");

            Action<string, string> showLocalizError = DisplayLocalizationError();

            foreach (KeyValuePair<string, int> langPair in langsCols)
            {
                string lang = langPair.Key;
                int colNum = langPair.Value;

                bool nativeLang = lang == "Russian" || colNum == -1;

                string langFolder
                    = nativeLang ? langOriginFolder : Path.Combine(_projectPath, "TranslatedData", lang);
                string bookDescsPath = Path.Combine(_projectPath, "Raw", "BookDescriptions", lang + ".xlsx");

                // Проверяем альтернативный путь для файла локализации
                if (!File.Exists(bookDescsPath) && !nativeLang)
                    bookDescsPath = Path.Combine(_projectPath, "TranslatedData", lang, lang + ".xlsx");

                Console.WriteLine("GENERATE TABLES FOR LANGUAGE: " + lang);

                if (!Directory.Exists(langFolder)) continue;

                string[] langFiles =
                [
                    string.Format(langFolder + @"\Chapter_{0}_for_translating.xlsx", chapterN),
                    string.Format(langOriginFolder + @"\Chapter_{0}_internal.xlsx", chapterN)
                ];

                if (!File.Exists(langFiles[0])) break;

                // Вызываем Func, передавая список языков
                string correct = generateLocalizationJson(lang,
                                                          "chapter" + chapterN,
                                                          langFiles,
                                                          Path.Combine(chapterFolder, "Strings", lang + ".json"),
                                                          colNum != -1 ? colNum : 1,
                                                          langsCols.Keys.ToList());

                if (!string.IsNullOrEmpty(correct))
                    showLocalizError(correct, "chapter" + chapterN);

                if (chapterN != 1) continue;

                string[] sharedLangFiles =
                [
                    string.Format(langFolder + @"\CharacterNames.xlsx", chapterN), bookDescsPath
                ];

                Console.WriteLine("generate sharedstrings " + bookDescsPath);

                // Вызываем Func, передавая список языков
                correct = generateLocalizationJson(lang,
                                                   "sharedstrings",
                                                   sharedLangFiles,
                                                   Path.Combine(binFolder, "SharedStrings", lang + ".json"),
                                                   colNum != -1 ? colNum : 1,
                                                   langsCols.Keys.ToList());

                string[] stringToPreviewFile = [bookDescsPath];

                if (!string.IsNullOrEmpty(correct))
                {
                    showLocalizError(correct, "sharedstrings");
                    return false;
                }

                // Вызываем Func, передавая список языков
                correct = generateLocalizationJson(lang,
                                                   "previewstrings",
                                                   stringToPreviewFile,
                                                   Path.Combine(previewFolder, "Strings", lang + ".json"),
                                                   colNum != -1 ? colNum : 1,
                                                   langsCols.Keys.ToList());

                if (string.IsNullOrEmpty(correct)) continue;

                showLocalizError(correct, "previewstrings");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Генерирует имя версии для папки
        /// </summary>
        private static Func<string, string, string> GenerateVersionFolderName()
        {
            string VersionName(string folderName, string version) =>
                char.ToUpper(folderName[0]) + folderName.Substring(1);

            return VersionName;
        }

        #endregion

        #region Вспомогательные методы для проверки персонажей и локаций

        /// <summary>
        /// Проверяет и добавляет персонажа
        /// </summary>
        private static Action<string> ValidateAndAddCharacter(Dictionary<string, string> nativeDict,
                                                              Dictionary<string, Model> objectsList,
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

        /// <summary>
        /// Проверяет и добавляет локацию по ID
        /// </summary>
        private static Action<int> ValidateAndAddLocationById(AjLinkerMeta meta, AjAssetGridLinker gridLinker)
        {
            void AddLocInt(int intId)
            {
                AjMetaLocationData mdata = meta.Locations.Find(chf => chf.Id == intId);

                if (!gridLinker.IsLocExist(mdata.DisplayName)) gridLinker.AddLocation(mdata.DisplayName, mdata.Aid);
            }

            return AddLocInt;
        }

        /// <summary>
        /// Проверяет и добавляет локацию
        /// </summary>
        private static Action<string> ValidateAndAddLocation(Dictionary<string, string> nativeDict,
                                                             Dictionary<string, Model> objectsList,
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

        #endregion

        #region Работа с локализацией

        /// <summary>
        /// Перечисление эмоций персонажей в тексте
        /// </summary>
        public enum EChEmotion
        {
            Angry, //red
            Happy, //green
            Sad, //purple
            Surprised, //yellow
            IsntSetOrNeutral //blue
        }

        /// <summary>
        /// Показывает ошибки локализации
        /// </summary>
        private static Action<string, string> DisplayLocalizationError()
        {
            void LocalizError(string missingKey, string fileGroupId)
            {
                var errorMessage =
                    $"Ошибка мультиязыкового вывода: Ключ '{missingKey}' отсутствует или пуст в данных для группы файлов '{fileGroupId}'";

                // Проверяем, был ли файл не найден
                if (missingFiles.TryGetValue(fileGroupId, out string file))
                    errorMessage += $"\nПричина: Файл локализации не найден: {file}";

                Form1.ShowMessage(errorMessage);
            }

            return LocalizError;
        }

        /// <summary>
        /// Генерирует JSON файл локализации
        /// </summary>
        private Func<string, string, string[], string, int, List<string>, string> GenerateLocalizationJson(
            Dictionary<string, Dictionary<string, string>> allDicts,
            Dictionary<string, AjLocalizInJsonFile> origLangData)
        {
            return (language, id, inPaths, outputPath, colN, knownLanguages) =>
                   {
                       if (!allDicts.TryGetValue(language, out Dictionary<string, string> allStrings))
                       {
                           allStrings = new Dictionary<string, string>();
                           allDicts[language] = allStrings;
                       }

                       AjLocalizInJsonFile jsonData = LoadLocalizationFromXml(inPaths, colN, knownLanguages);
                       bool origLang = !origLangData.ContainsKey(id);

                       if (origLang) origLangData[id] = jsonData;

                       AjLocalizInJsonFile origJsonData = origLangData[id];
                       if (origLang) jsonData = LoadLocalizationFromXml(inPaths, colN, knownLanguages);

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

                               if (CheckTranslationCompleteness(translatedValue,
                                                                origValue,
                                                                origLang,
                                                                jsonData.Data[pair.Key]))
                                   Console.WriteLine($"String with ID {pair.Key} isn't translated");
                           }
                       }

                       string localizationIssue = ValidateLocalizationData(origJsonData, jsonData, origLang);
                       SaveLocalizationToJson(jsonData, outputPath);

                       return localizationIssue;
                   };
        }

        /// <summary>
        /// Проверяет полноту перевода
        /// </summary>
        private bool
            CheckTranslationCompleteness(string translatedValue,
                                         string origValue,
                                         bool origLang,
                                         string jsonDataValue) =>
            string.IsNullOrEmpty(translatedValue.Trim()) ||
            (origValue.Trim() == translatedValue.Trim() &&
             !origLang &&
             origValue.Length > 10 &&
             !origValue.Contains("*SystemLinkTo*") &&
             !origValue.Contains("NextChoiceIsTracked") &&
             !jsonDataValue.Contains("StageDirections") &&
             !string.IsNullOrEmpty(jsonDataValue.Replace(".", "").Trim()) &&
             !origValue.ToLower().Contains("%pname%"));

        /// <summary>
        /// Проверяет проблемы локализации
        /// </summary>
        private string ValidateLocalizationData(AjLocalizInJsonFile origJsonData,
                                                AjLocalizInJsonFile jsonData,
                                                bool origLang)
        {
            foreach (KeyValuePair<string, string> pair in origJsonData.Data)
                // Проверяем только наличие ключа, игнорируем пустые значения
                if (!jsonData.Data.ContainsKey(pair.Key) && origLang)
                    return pair.Key;

            return string.Empty;
        }

        /// <summary>
        /// Получает данные из XML файла
        /// </summary>
        private AjLocalizInJsonFile LoadLocalizationFromXml(string[] pathsToXmls,
                                                            int defaultColumn,
                                                            List<string> knownLanguages)
        {
            // Создаем ключ кэша
            var cacheKey = new LocalizationCacheKey(
                string.Join("_", pathsToXmls),
                defaultColumn.ToString(),
                pathsToXmls.Any(p => p.Contains("_internal"))
            );

            if (IsCacheValid(cacheKey) && _localizationCache.TryGetValue(cacheKey, out var cachedData))
            {
                Console.WriteLine($"Используем кэшированные данные для файлов: {string.Join(", ", pathsToXmls)}");
                var result = new AjLocalizInJsonFile
                {
                    Data = cachedData.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Text
                    )
                };
                return result;
            }

            var total = new Dictionary<string, string>();
            var knownLanguagesSet = new HashSet<string>(knownLanguages ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

            Console.WriteLine("=== Начало обработки файлов локализации ===");

            foreach (string path in pathsToXmls)
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"ВНИМАНИЕ: Файл не найден: {path}");
                    string fileGroupId = path.Contains("BookDescriptions") ? "sharedstrings" :
                                        path.Contains("CharacterNames") ? "sharedstrings" : "chapter1";
                    missingFiles[fileGroupId] = path;
                    continue;
                }

                Console.WriteLine($"\nОбработка файла: {path}");
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                Dictionary<string, string> fileDict = null;

                try
                {
                    bool isBookDescription = path.Contains("BookDescriptions");
                    bool isTranslatedData = path.Contains("TranslatedData");

                    if (isBookDescription && knownLanguagesSet.Contains(fileNameWithoutExtension))
                    {
                        Console.WriteLine($"Обработка файла описания книги: {path}");

                        // Пробуем получить данные из колонки D (индекс 3)
                        Dictionary<string, string> dictD = ConvertExcelToDictionary(path, 3)
                            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                            .ToDictionary(x => x.Key, x => x.Value.Trim());

                        // Если в колонке D нет данных, берем из колонки B (индекс 1)
                        if (dictD.Count == 0)
                        {
                            Console.WriteLine($"В колонке D нет данных, используем колонку B");
                            fileDict = ConvertExcelToDictionary(path, 1)
                                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                                .ToDictionary(x => x.Key, x => x.Value.Trim());
                        }
                        else
                        {
                            fileDict = dictD;
                        }
                    }
                    else if (isTranslatedData)
                    {
                        Console.WriteLine($"Применяем логику колонки E для переведенного файла: {path}");
                        fileDict = ConvertExcelToDictionary(path, 4);
                    }
                    else
                    {
                        Console.WriteLine($"Применяем стандартную логику для колонки {defaultColumn}: {path}");
                        fileDict = ConvertExcelToDictionary(path, defaultColumn);
                    }

                    if (fileDict != null)
                    {
                        int addedCount = 0;
                        int skippedCount = 0;
                        foreach (KeyValuePair<string, string> pair in fileDict.Where(p => p.Key != "ID"))
                        {
                            if (!total.ContainsKey(pair.Key))
                            {
                                total.Add(pair.Key, pair.Value);
                                addedCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                        Console.WriteLine($"Добавлено записей: {addedCount}, пропущено дубликатов: {skippedCount}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке файла {path}: {ex.Message}");
                    Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
                    throw;
                }
            }

            var jsonFile = new AjLocalizInJsonFile { Data = total };

            // Кэшируем результат
            _localizationCache[cacheKey] = total.ToDictionary(
                kvp => kvp.Key,
                kvp => new LocalizationEntry { Text = kvp.Value, IsInternal = false }
            );

            Console.WriteLine($"=== Обработка файлов локализации завершена. Всего записей: {total.Count} ===");
            return jsonFile;
        }

        /// <summary>
        /// Записывает JSON файл
        /// </summary>
        private AjLocalizInJsonFile SaveLocalizationToJson(AjLocalizInJsonFile jsonFile, string pathToJson)
        {
            File.WriteAllText(pathToJson, JsonConvert.SerializeObject(jsonFile));

            return jsonFile;
        }

        /// <summary>
        /// Конвертирует XML в JSON
        /// </summary>
        public AjLocalizInJsonFile ConvertLocalizationXmlToJson(string[] pathsToXmls, string pathToJson, int column)
        {
            var total = new Dictionary<string, string>();

            foreach (string el in pathsToXmls)
            {
                Dictionary<string, string> fileDict = ConvertExcelToDictionary(el, column);

                foreach (KeyValuePair<string, string> pair in fileDict.Where(pair => pair.Key != "ID"))
                    total.Add(pair.Key, pair.Value);
            }

            var jsonFile = new AjLocalizInJsonFile();
            jsonFile.Data = total;

            File.WriteAllText(pathToJson, JsonConvert.SerializeObject(jsonFile));

            return jsonFile;
        }

        /// <summary>
        /// Создает структуру папок для локализации
        /// </summary>
        private void CreateLocalizationStructure()
        {
            string localizationFolder = Path.Combine(_projectPath, "Localization");
            string russianFolder = Path.Combine(localizationFolder, "Russian");

            if (!Directory.Exists(localizationFolder))
                Directory.CreateDirectory(localizationFolder);

            if (!Directory.Exists(russianFolder))
                Directory.CreateDirectory(russianFolder);

            Console.WriteLine($"✅ Создана структура папок локализации в {russianFolder}");
        }

        /// <summary>
        /// Генерирует Excel файлы локализации из Flow.json
        /// </summary>
        private void GenerateLocalizationExcelFiles(Dictionary<string, Model> objectsList,
                                                   List<string>[] csparentsIds,
                                                   Dictionary<string, string> nativeDict)
        {
            Console.WriteLine("Начинаем генерацию Excel файлов локализации...");
            string russianFolder = Path.Combine(_projectPath, "Localization", "Russian");

            // Для каждой главы создаем отдельный пакет Excel
            for (var i = 0; i < csparentsIds.Length; i++)
            {
                Console.WriteLine($"Обрабатываем главу {i + 1}...");
                int chapterN = i + 1;

                using var package = new ExcelPackage();
                var forTranslatingWorksheet = package.Workbook.Worksheets.Add($"Chapter_{chapterN}");
                var internalWorksheet = package.Workbook.Worksheets.Add($"Chapter_{chapterN}_internal");

                // Заголовки для основного файла
                forTranslatingWorksheet.Cells[1, 1].Value = "ID";
                forTranslatingWorksheet.Cells[1, 2].Value = "Speaker";
                forTranslatingWorksheet.Cells[1, 3].Value = "Emotion";
                forTranslatingWorksheet.Cells[1, 4].Value = "Text";

                // Заголовки для internal файла
                internalWorksheet.Cells[1, 1].Value = "ID";
                internalWorksheet.Cells[1, 2].Value = "Text";

                int mainRow = 2;
                int internalRow = 2;

                foreach (string parentId in csparentsIds[i])
                {
                    Console.WriteLine($"Обрабатываем parentId: {parentId}");
                    foreach (KeyValuePair<string, Model> pair in objectsList.Where(p => p.Value.Properties.Parent == parentId))
                    {
                        Model dfobj = pair.Value;
                        if (dfobj.TypeEnum != TypeEnum.DialogueFragment) continue;

                        // Обработка основного текста
                        if (!string.IsNullOrEmpty(dfobj.Properties.Text))
                        {
                            try
                            {
                                forTranslatingWorksheet.Cells[mainRow, 1].Value = dfobj.Properties.Text;

                                if (!string.IsNullOrEmpty(dfobj.Properties.Speaker) &&
                                    objectsList.TryGetValue(dfobj.Properties.Speaker, out Model speaker))
                                {
                                    forTranslatingWorksheet.Cells[mainRow, 2].Value = nativeDict[speaker.Properties.DisplayName];
                                }

                                forTranslatingWorksheet.Cells[mainRow, 3].Value = RecognizeEmotion(dfobj.Properties.Color);
                                forTranslatingWorksheet.Cells[mainRow, 4].Value = nativeDict[dfobj.Properties.Text];
                                mainRow++;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка при обработке текста: {ex.Message}");
                                throw;
                            }
                        }

                        // Обработка сценических указаний
                        if (!string.IsNullOrEmpty(dfobj.Properties.StageDirections))
                        {
                            try
                            {
                                internalWorksheet.Cells[internalRow, 1].Value = dfobj.Properties.StageDirections;

                                // Проверяем наличие ключа в словаре
                                if (nativeDict.TryGetValue(dfobj.Properties.StageDirections, out string translatedDirections))
                                {
                                    internalWorksheet.Cells[internalRow, 2].Value = translatedDirections;
                                }
                                else
                                {
                                    // Если перевод отсутствует, используем оригинальный текст
                                    internalWorksheet.Cells[internalRow, 2].Value = dfobj.Properties.StageDirections;
                                    Console.WriteLine($"Предупреждение: Отсутствует перевод для сценических указаний: {dfobj.Properties.StageDirections}");
                                }

                                internalRow++;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Предупреждение при обработке сценических указаний: {ex.Message}");
                                // Продолжаем выполнение, не выбрасывая исключение
                            }
                        }
                    }
                }

                Console.WriteLine($"Сохраняем файлы для главы {chapterN}...");

                try
                {
                    // Сохраняем файлы
                    string forTranslatingPath = Path.Combine(russianFolder, $"Chapter_{chapterN}_for_translating.xlsx");
                    string internalPath = Path.Combine(russianFolder, $"Chapter_{chapterN}_internal.xlsx");

                    Console.WriteLine($"Путь для основного файла: {forTranslatingPath}");
                    Console.WriteLine($"Путь для internal файла: {internalPath}");

                    // Проверяем, есть ли данные для сохранения
                    if (mainRow > 2)
                    {
                        File.WriteAllBytes(forTranslatingPath, package.GetAsByteArray());
                        Console.WriteLine("Основной файл сохранен");
                    }
                    else
                    {
                        Console.WriteLine("Нет данных для основного файла");
                    }

                    // Создаем новый пакет для internal файла
                    if (internalRow > 2)
                    {
                        using var internalPackage = new ExcelPackage();
                        var ws = internalPackage.Workbook.Worksheets.Add("Internal");
                        ws.Cells["A1:B" + (internalRow - 1)].Value = internalWorksheet.Cells["A1:B" + (internalRow - 1)].Value;
                        File.WriteAllBytes(internalPath, internalPackage.GetAsByteArray());
                        Console.WriteLine("Internal файл сохранен");
                    }
                    else
                    {
                        Console.WriteLine("Нет данных для internal файла");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при сохранении файлов: {ex.Message}");
                    throw;
                }
            }

            Console.WriteLine("✅ Генерация Excel файлов локализации завершена");
        }

        private void GenerateCharacterLocalizationFiles(Dictionary<string, Model> objectsList,
                                                    Dictionary<string, string> nativeDict)
        {
            string russianFolder = Path.Combine(_projectPath, "Localization", "Russian");
            string characterNamesPath = Path.Combine(russianFolder, "CharacterNames.xlsx");

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Characters");

            // Заголовки
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Text";

            int row = 2;

            // Собираем всех персонажей
            foreach (KeyValuePair<string, Model> pair in objectsList)
            {
                if (pair.Value.TypeEnum != TypeEnum.Entity) continue;

                string displayName = pair.Value.Properties.DisplayName;
                if (string.IsNullOrEmpty(displayName)) continue;

                worksheet.Cells[row, 1].Value = displayName;

                if (nativeDict.TryGetValue(displayName, out string translatedName))
                {
                    worksheet.Cells[row, 2].Value = translatedName;
                }

                row++;
            }

            File.WriteAllBytes(characterNamesPath, package.GetAsByteArray());
            Console.WriteLine("✅ Создан файл локализации персонажей");
        }

        /// <summary>
        /// Создает или обновляет файлы локализации
        /// </summary>
        public bool InitializeLocalization()
        {
            try
            {
                CreateLocalizationStructure();

                // Загружаем базовые данные
                ArticyExportData ajfile = LoadBaseData();

                if (ajfile == null)
                {
                    Form1.ShowMessage("❌ Не удалось загрузить Flow.json");
                    return false;
                }

                Dictionary<string, string> nativeDict = ajfile.NativeMap;
                Dictionary<string, Model> objectsList = ajfile.GetModelDictionary();

                if (nativeDict == null || nativeDict.Count == 0)
                {
                    Console.WriteLine("Предупреждение: Словарь локализации пуст или не загружен");
                    nativeDict = new Dictionary<string, string>();
                }

                List<string> chaptersIds = GetSortedChapterIds(objectsList, nativeDict);

                if (chaptersIds.Count < Form1.AvailableChapters)
                {
                    Form1.ShowMessage("❌ Глав в книге меньше введённого количества");
                    return false;
                }

                chaptersIds.RemoveRange(Form1.AvailableChapters, chaptersIds.Count - Form1.AvailableChapters);
                List<string>[] csparentsIds = GetChapterAndSubchapterHierarchy(chaptersIds, objectsList);

                try
                {
                    GenerateLocalizationExcelFiles(objectsList, csparentsIds, nativeDict);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Предупреждение при генерации Excel файлов: {ex.Message}");
                    // Продолжаем выполнение
                }

                try
                {
                    GenerateCharacterLocalizationFiles(objectsList, nativeDict);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Предупреждение при генерации файлов персонажей: {ex.Message}");
                    // Продолжаем выполнение
                }

                Form1.ShowMessage("✅ Файлы локализации успешно созданы/обновлены");
                return true;
            }
            catch (Exception ex)
            {
                Form1.ShowMessage($"❌ Ошибка при инициализации локализации: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Работа с кешем локализации

        private Dictionary<string, LocalizationEntry> CacheExcelData(string path,
                                                                     string chapterKey,
                                                                     bool isInternal = false)
        {
            var cacheKey = new LocalizationCacheKey(chapterKey, Path.GetDirectoryName(path).Split('\\').Last(), isInternal);

            if (_localizationCache.TryGetValue(cacheKey, out Dictionary<string, LocalizationEntry> cachedData))
                return cachedData;

            var data = new Dictionary<string, LocalizationEntry>();

            using (var xlPackage = new ExcelPackage(new FileInfo(path)))
            {
                ExcelWorksheet worksheet = xlPackage.Workbook.Worksheets.First();
                int totalRows = worksheet.Dimension.End.Row;

                for (var rowNum = 2; rowNum <= totalRows; rowNum++)
                {
                    var id = worksheet.Cells[rowNum, 1].Value?.ToString();
                    if (string.IsNullOrEmpty(id)) continue;

                    var entry = new LocalizationEntry
                    {
                        Text = _stringPool.Intern(worksheet.Cells[rowNum, Form1.FOR_LOCALIZATORS_MODE ? 4 : 2]
                                                           .Value?.ToString() ??
                                                  string.Empty),
                        SpeakerDisplayName =
                                        Form1.FOR_LOCALIZATORS_MODE
                                            ? _stringPool.Intern(worksheet.Cells[rowNum, 2].Value?.ToString() ??
                                                                 string.Empty)
                                            : string.Empty,
                        Emotion = Form1.FOR_LOCALIZATORS_MODE
                                                  ? _stringPool.Intern(worksheet.Cells[rowNum, 3].Value?.ToString() ??
                                                                       string.Empty)
                                                  : string.Empty,
                        IsInternal = isInternal
                    };

                    data[_stringPool.Intern(id)] = entry;
                }
            }

            _localizationCache[cacheKey] = data;
            return data;
        }

        private void CacheChapterData(int chapterNumber, string language)
        {
            var forTranslatingPath =
                $"{_projectPath}\\Localization\\{language}\\Chapter_{chapterNumber}_for_translating.xlsx";
            var internalPath = $"{_projectPath}\\Localization\\Russian\\Chapter_{chapterNumber}_internal.xlsx";

            CacheExcelData(forTranslatingPath, $"chapter_{chapterNumber}");
            CacheExcelData(internalPath, $"chapter_{chapterNumber}", true);
        }

        public void ClearCache()
        {
            _localizationCache.Clear();
            _cachedTranslations.Clear();
        }

        public void ClearCacheForChapter(int chapterNumber)
        {
            var keysToRemove = _localizationCache.Keys
                .Where(k => k.Chapter.StartsWith($"chapter_{chapterNumber}"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _localizationCache.Remove(key);
            }
        }

        private bool IsCacheValid(LocalizationCacheKey key) =>
            _localizationCache.ContainsKey(key) && _localizationCache[key].Any();

        /// <summary>
        /// Очищает все кэши
        /// </summary>
        public void ClearAllCaches()
        {
            _cachedFlowJson = null;
            _cachedMetaData = null;
            _cachedLocalizationDict = null;
            _localizationCache.Clear();
            _cachedTranslations.Clear();
            _savedXmlDicts.Clear();
            _cachedBookEntities = null;
            _cachedEntitiesArticyExportData = null;
            _cachedEntitiesNativeDict = null;
        }

        #endregion

        #region Пути к файлам

        /// <summary>
        /// Получает путь к таблицам локализации
        /// </summary>
        public static string GetLocalizationTablesPath(string projPath)
        {
            string path = projPath + @"\Raw\loc_All objects_en.xlsx";

            if (!File.Exists(path)) path = projPath + @"\Raw\loc_All objects_ru.xlsx";

            return path;
        }

        /// <summary>
        /// Получает путь к Flow.json
        /// </summary>
        public static string GetFlowJsonPath(string projPath) => projPath + @"\Raw\Flow.json";

        /// <summary>
        /// Получает путь к Meta.json
        /// </summary>
        public static string GetMetaJsonPath(string projPath) => projPath + @"\Raw\Meta.json";

        #endregion

        private string RecognizeEmotion(ArticyExportColor color)
        {
            var emotion = EChEmotion.IsntSetOrNeutral;

            bool ColorsEquals(Color32 a, Color32 b) =>
                Math.Abs(a.R - b.R) < 20 && Math.Abs(a.G - b.G) < 20 && Math.Abs(a.B - b.B) < 20;

            var fragColor = color.ToColor32();
            Color32[] emotionsColor = [new(255, 0, 0, 0), new(0, 110, 20, 0), new(41, 6, 88, 0), new(255, 134, 0, 0)];

            for (var i = 0; i < emotionsColor.Length; i++)
            {
                if (!ColorsEquals(emotionsColor[i], fragColor)) continue;

                emotion = (EChEmotion)i;
                break;
            }

            return emotion.ToString();
        }

        private List<Model> AssignArticyIdsToMetaData(Dictionary<string, Model> objectsList,
                                                      AjLinkerMeta meta,
                                                      Dictionary<string, string> nativeDict)
        {
            var sharedObjs = new List<Model>();

            foreach (KeyValuePair<string, Model> pair in objectsList.Where(p => p.Value.TypeEnum == TypeEnum.Entity ||
                                                                               p.Value.TypeEnum == TypeEnum.Location))
            {
                string dname = nativeDict[pair.Value.Properties.DisplayName];

                if (pair.Value.TypeEnum == TypeEnum.Entity)
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

            return sharedObjs;
        }
    }
}