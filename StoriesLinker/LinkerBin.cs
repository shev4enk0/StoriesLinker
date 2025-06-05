namespace StoriesLinker
{
    public class LinkerBin
    {
        #region Поля и инициализация

        protected string _projectPath;
        protected string _baseLanguage; // Базовый язык локализации
        private static Dictionary<string, Dictionary<string, string>> _savedXMLDicts = new();
        private int _allWordsCount = 0;

        public LinkerBin(string projectPath)
        {
            _projectPath = projectPath;
            // Убираем инициализацию - теперь кэш статический
            
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

        /// <summary>
        /// Очищает кэш загруженных Excel файлов
        /// </summary>
        public static void ClearCache()
        {
            _savedXMLDicts.Clear();
            Console.WriteLine("🗑️ Кэш Excel файлов очищен");
        }

        /// <summary>
        /// Получает информацию о текущем состоянии кэша
        /// </summary>
        public static string GetCacheInfo()
        {
            if (_savedXMLDicts.Count == 0)
                return "📊 Кэш пуст";

            var uniqueFiles = _savedXMLDicts.Keys
                .Select(key => key.Split('|')[0]) // Извлекаем путь к файлу
                .Distinct() // Убираем дубликаты
                .Select(path => Path.GetFileName(path))
                .ToList();

            return $"📊 В кэше: {_savedXMLDicts.Count} записей из {uniqueFiles.Count} уникальных файлов ({string.Join(", ", uniqueFiles.Take(3))}{(uniqueFiles.Count > 3 ? "..." : "")})";
        }
     
        #endregion

        #region Работа с Excel таблицами

        // Преобразование Excel-таблицы в словарь
        private Dictionary<string, string> XMLTableToDict(string path, int column = 1)
        {
            // Создаем уникальный ключ кэша, учитывающий и путь, и колонку
            string cacheKey = $"{path}|column:{column}";
            
            if (_savedXMLDicts.TryGetValue(cacheKey, out Dictionary<string, string> dict))
            {
                Console.WriteLine($"💾 Используем кэш для: {Path.GetFileName(path)} (колонка {column})");
                return dict;
            }

            Console.WriteLine($"📖 Читаем файл: {Path.GetFileName(path)} (колонка {column})");

            // Устанавливаем контекст лицензии EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var nativeDict = new Dictionary<string, string>();

            // ПРОВЕРКА: Файл должен существовать
            if (!File.Exists(path))
            {
                Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Файл локализации не найден: {path}");
                _savedXMLDicts.Add(cacheKey, nativeDict);
                return nativeDict;
            }

            try
            {
                using var xlPackage = new ExcelPackage(new FileInfo(path));
                // ПРОВЕРКА: В файле должны быть листы
                if (xlPackage.Workbook.Worksheets.Count == 0)
                {
                    Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Excel файл не содержит листов: {path}");
                    _savedXMLDicts.Add(cacheKey, nativeDict);
                    return nativeDict;
                }

                ExcelWorksheet myWorksheet = xlPackage.Workbook.Worksheets.First();
                    
                // ПРОВЕРКА: Лист должен иметь данные
                if (myWorksheet.Dimension == null)
                {
                    Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Excel лист пустой: {path}");
                    _savedXMLDicts.Add(cacheKey, nativeDict);
                    return nativeDict;
                }

                int totalRows = myWorksheet.Dimension.End.Row;
                for (var rowNum = 1; rowNum <= totalRows; rowNum++)
                {
                    ExcelRange firstRow = myWorksheet.Cells[rowNum, 1];
                    ExcelRange secondRow = myWorksheet.Cells[rowNum, column + 1];

                    string firstRowStr = firstRow is { Value: not null }
                        ? firstRow.Value.ToString()
                        : "";
                    string secondRowStr = secondRow is { Value: not null }
                        ? secondRow.Value.ToString()
                        : " ";

                    if (string.IsNullOrEmpty(firstRowStr)) continue;

                    if (!nativeDict.TryAdd(firstRowStr, secondRowStr))
                        Console.WriteLine("double key critical error " + firstRowStr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ОШИБКА при чтении Excel файла {path}: {ex.Message}");
                // Возвращаем пустой словарь вместо падения
            }

            _savedXMLDicts.Add(cacheKey, nativeDict);

            return nativeDict;
        }

        // Специальная версия для обработки BookDescriptions для основного языка
        private Dictionary<string, string> XMLTableToDictBookDesc(string path)
        {
            // Создаем уникальный ключ кэша для BookDescriptions
            string cacheKey = $"{path}|bookdesc";
            
            if (_savedXMLDicts.TryGetValue(cacheKey, out Dictionary<string, string> dict))
            {
                Console.WriteLine($"💾 Используем кэш для BookDescriptions: {Path.GetFileName(path)}");
                return dict;
            }

            Console.WriteLine($"📖 Читаем BookDescriptions: {Path.GetFileName(path)}");

            // Устанавливаем контекст лицензии EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var nativeDict = new Dictionary<string, string>();

            // ПРОВЕРКА: Файл должен существовать
            if (!File.Exists(path))
            {
                Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Файл описания книги не найден: {path}");
                _savedXMLDicts.Add(cacheKey, nativeDict);
                return nativeDict;
            }

            try
            {
                using var xlPackage = new ExcelPackage(new FileInfo(path));
                // ПРОВЕРКА: В файле должны быть листы
                if (xlPackage.Workbook.Worksheets.Count == 0)
                {
                    Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Excel файл описания книги не содержит листов: {path}");
                    _savedXMLDicts.Add(cacheKey, nativeDict);
                    return nativeDict;
                }

                ExcelWorksheet myWorksheet = xlPackage.Workbook.Worksheets.First();
                    
                // ПРОВЕРКА: Лист должен иметь данные
                if (myWorksheet.Dimension == null)
                {
                    Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Excel лист описания книги пустой: {path}");
                    _savedXMLDicts.Add(cacheKey, nativeDict);
                    return nativeDict;
                }

                int totalRows = myWorksheet.Dimension.End.Row;
                for (var rowNum = 1; rowNum <= totalRows; rowNum++)
                {
                    ExcelRange firstRow = myWorksheet.Cells[rowNum, 1];  // Колонка A (ID)
                    ExcelRange columnD = myWorksheet.Cells[rowNum, 4];   // Колонка D
                    ExcelRange columnB = myWorksheet.Cells[rowNum, 2];   // Колонка B

                    string firstRowStr = firstRow is { Value: not null }
                        ? firstRow.Value.ToString()
                        : "";
                                                    
                    if (string.IsNullOrEmpty(firstRowStr)) continue;

                    // Проверяем сначала колонку D, если пусто, берем из B
                    string valueStr;
                    if (columnD is { Value: not null } && !string.IsNullOrWhiteSpace(columnD.Value.ToString()))
                    {
                        valueStr = columnD.Value.ToString();
                    }
                    else
                    {
                        valueStr = columnB is { Value: not null }
                            ? columnB.Value.ToString()
                            : " ";
                    }

                    if (!nativeDict.TryAdd(firstRowStr, valueStr))
                        Console.WriteLine("double key critical error " + firstRowStr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ОШИБКА при чтении Excel файла описания книги {path}: {ex.Message}");
                // Возвращаем пустой словарь вместо падения
            }

            _savedXMLDicts.Add(cacheKey, nativeDict);

            return nativeDict;
        }

        // Получение основного словаря локализации
        public Dictionary<string, string> GetNativeDict() => XMLTableToDict(GetLocalizTablesPath(_projectPath));

        #endregion

        #region Работа с Excel таблицами с эмоциями

        /// <summary>
        /// Преобразование Excel-таблицы в словарь с эмоциями (для for_translating файлов)
        /// Читает: колонка A - ID, колонка B - Speaker, колонка C - Emotion, колонка D - Text
        /// </summary>
        private Dictionary<string, LocalizEntityWithEmotion> XMLTableToDictWithEmotions(string path)
        {
            // Устанавливаем контекст лицензии EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            var resultDict = new Dictionary<string, LocalizEntityWithEmotion>();

            // ПРОВЕРКА: Файл должен существовать
            if (!File.Exists(path))
            {
                Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Файл с эмоциями не найден: {path}");
                return resultDict;
            }

            try
            {
                using var xlPackage = new ExcelPackage(new FileInfo(path));
                // ПРОВЕРКА: В файле должны быть листы
                if (xlPackage.Workbook.Worksheets.Count == 0)
                {
                    Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Excel файл с эмоциями не содержит листов: {path}");
                    return resultDict;
                }

                ExcelWorksheet myWorksheet = xlPackage.Workbook.Worksheets.First();
                    
                // ПРОВЕРКА: Лист должен иметь данные
                if (myWorksheet.Dimension == null)
                {
                    Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Excel лист с эмоциями пустой: {path}");
                    return resultDict;
                }

                int totalRows = myWorksheet.Dimension.End.Row;

                // Пропускаем заголовок (строка 1)
                for (var rowNum = 2; rowNum <= totalRows; rowNum++)
                {
                    var idCell = myWorksheet.Cells[rowNum, 1];      // Колонка A - ID
                    var speakerCell = myWorksheet.Cells[rowNum, 2]; // Колонка B - Speaker  
                    var emotionCell = myWorksheet.Cells[rowNum, 3]; // Колонка C - Emotion
                    var textCell = myWorksheet.Cells[rowNum, 4];    // Колонка D - Text

                    string localizId = idCell?.Value?.ToString() ?? "";
                    string speaker = speakerCell?.Value?.ToString() ?? "";
                    string emotion = emotionCell?.Value?.ToString() ?? "";
                    string text = textCell?.Value?.ToString() ?? "";

                    if (string.IsNullOrEmpty(localizId) || string.IsNullOrEmpty(text)) 
                        continue;

                    var entity = new LocalizEntityWithEmotion
                    {
                        LocalizID = localizId,
                        Text = text,
                        SpeakerDisplayName = speaker,
                        Emotion = emotion
                    };

                    if (!resultDict.TryAdd(localizId, entity))
                    {
                        Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Дублирующийся ключ в файле с эмоциями: {localizId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ОШИБКА при чтении Excel файла с эмоциями {path}: {ex.Message}");
            }

            Console.WriteLine($"📊 Загружено {resultDict.Count} записей с эмоциями из {path}");
            return resultDict;
        }

        /// <summary>
        /// Создает JSON файл локализации с эмоциями
        /// </summary>
        private AjLocalizWithEmotionsInJsonFile CreateLocalizationWithEmotions(string pathToForTranslatingFile)
        {
            var emotionsDict = XMLTableToDictWithEmotions(pathToForTranslatingFile);
            
            var jsonFile = new AjLocalizWithEmotionsInJsonFile();
            jsonFile.Data = emotionsDict;

            return jsonFile;
        }

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

            // Автоматически обновляем эмоции для всех объектов после загрузки
            EmotionUpdateUtility.UpdateEmotionsInAjFile(jsonObj);

            return jsonObj;
        }

        public AjLinkerMeta GetParsedMetaInputJsonFile()
        {
            // Устанавливаем контекст лицензии EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            var jsonObj = new AjLinkerMeta { Version = new BookVersionInfo() };

            string metaXMLPath = _projectPath + @"\Raw\Meta.xlsx";

            // ПРОВЕРКА: Файл Meta.xlsx должен существовать
            if (!File.Exists(metaXMLPath))
            {
                Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Файл Meta.xlsx не найден: {metaXMLPath}");
                return jsonObj;
            }

            try
            {
                using var xlPackage = new ExcelPackage(new FileInfo(metaXMLPath));
                // ПРОВЕРКА: Должны быть все три листа (Base, Characters, Locations)
                if (xlPackage.Workbook.Worksheets.Count < 3)
                {
                    Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Meta.xlsx должен содержать 3 листа (Base, Characters, Locations), найдено: {xlPackage.Workbook.Worksheets.Count}");
                    return jsonObj;
                }

                ExcelWorksheet myWorksheet = xlPackage.Workbook.Worksheets.First();
                    
                // ПРОВЕРКА: Лист должен иметь данные
                if (myWorksheet.Dimension == null)
                {
                    Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Первый лист Meta.xlsx пустой");
                    return jsonObj;
                }

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

                // ПРОВЕРКА: Должен быть третий лист (индекс 2)
                if (xlPackage.Workbook.Worksheets.Count < 3)
                {
                    Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Meta.xlsx не содержит третий лист для данных персонажей");
                    return jsonObj;
                }

                myWorksheet = xlPackage.Workbook.Worksheets[1]; // Characters лист (индекс 1)
                    
                // ПРОВЕРКА: Лист Characters должен иметь данные
                if (myWorksheet.Dimension == null)
                {
                    Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Лист Characters в Meta.xlsx пустой");
                    return jsonObj;
                }

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

                myWorksheet = xlPackage.Workbook.Worksheets[2]; // Locations лист (индекс 2)
                    
                // ПРОВЕРКА: Лист Locations должен иметь данные
                if (myWorksheet.Dimension == null)
                {
                    Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Лист Locations в Meta.xlsx пустой");
                    return jsonObj;
                }
                    
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ОШИБКА при чтении файла Meta.xlsx: {ex.Message}");
                return jsonObj;
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
                return rowHasEmptyField ? 0 : 1;
            }

            return Row;
        }

        private AjLocalizInJsonFile GetXMLFile(string[] pathsToXmls, int column)
        {
            var total = new Dictionary<string, string>();

            foreach (string el in pathsToXmls)
            {
                Console.WriteLine($"📖 Обрабатываем файл: {el}");
                
                Dictionary<string, string> fileDict;
                
                // Проверяем, является ли файл описанием книги для основного языка
                if (el.Contains("BookDescriptions") && el.Contains(_baseLanguage))
                {
                    // Для основного языка используем специальный метод чтения BookDescriptions
                    fileDict = XMLTableToDictBookDesc(el);
                    Console.WriteLine($"📖 Загружено {fileDict.Count} ключей из BookDescriptions");
                }
                else
                {
                    // Для других файлов используем стандартный метод
                    fileDict = XMLTableToDict(el, column);
                    Console.WriteLine($"📖 Загружено {fileDict.Count} ключей из {Path.GetFileName(el)}");
                }

                foreach (KeyValuePair<string, string> pair in fileDict.Where(pair => pair.Key != "ID"))
                {
                    if (total.ContainsKey(pair.Key))
                    {
                        Console.WriteLine($"⚠️ ПРЕДУПРЕЖДЕНИЕ: Дублирующийся ключ '{pair.Key}' в файле {Path.GetFileName(el)}");
                    }
                    else
                    {
                        total.Add(pair.Key, pair.Value);
                    }
                }
            }

            Console.WriteLine($"📖 Итого загружено {total.Count} уникальных ключей");
            var jsonFile = new AjLocalizInJsonFile { Data = total };

            return jsonFile;
        }

        private AjLocalizInJsonFile WriteJsonFile(AjLocalizInJsonFile jsonFile, string pathToJson)
        {
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

                string displayNameKey = kobj.Value.Properties.DisplayName;
                if (!nativeDict.TryGetValue(displayNameKey, out string displayValue))
                {
                    Console.WriteLine($"Предупреждение: ключ главы '{displayNameKey}' не найден в словаре локализации. Пропускаем главу.");
                    continue;
                }

                string value = Regex.Match(displayValue, @"\d+").Value;

                if (string.IsNullOrEmpty(value))
                {
                    Console.WriteLine($"Предупреждение: не найден номер главы в значении '{displayValue}' для ключа '{displayNameKey}'. Пропускаем главу.");
                    continue;
                }

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

        // Enum EChEmotion перенесен в EmotionColorMapper.cs для избежания дублирования

        #endregion

        #region Генерация таблиц локализации

        /// <summary>
        /// Генерирует таблицы локализации на основе данных книги
        /// </summary>
        protected bool GenerateLocalizTables()
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
                Form1.ShowMessage($"Глав в книге меньше введённого количества. Найдено: {chaptersIds.Count}, требуется: {Form1.AvailableChapters}");

                return false;
            }

            chaptersIds.RemoveRange(Form1.AvailableChapters, chaptersIds.Count - Form1.AvailableChapters);

            // Используем улучшенную функцию распознавания эмоций, которая поддерживает оба стандарта
            string RecognizeEmotion(AjColor color) => ImprovedEmotionRecognizer.RecognizeEmotion(color);

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

                            // Проверяем наличие ключа перед добавлением в charactersNames
                            string displayNameKey = objectsList[chID].Properties.DisplayName;
                            if (nativeDict.TryGetValue(displayNameKey, out string characterName))
                            {
                                charactersNames.Add(chID, characterName);
                            }
                            else
                            {
                                Console.WriteLine($"Предупреждение: имя персонажа с ключом '{displayNameKey}' не найдено в словаре локализации. Используем техническое имя.");
                                charactersNames.Add(chID, displayNameKey); // Используем сам ключ как fallback
                            }
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

            CreateLocalizTable("CharacterNames", charactersLocalizIds, nativeDict);

            // НОВОЕ: Предварительно кэшируем все созданные файлы для ускорения последующих операций
            Console.WriteLine("🔄 Запускаем предварительное кэширование созданных файлов...");
            PreCacheCreatedLocalizationFiles();

            return true;
        }

        /// <summary>
        /// Создает таблицу локализации в формате Excel
        /// </summary>
        private void CreateLocalizTable(string name, List<LocalizEntity> ids, Dictionary<string, string> nativeDict)
        {
            // Устанавливаем контекст лицензии EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            var wordCount = 0;

            using var eP = new ExcelPackage();
            bool forTranslating = name.Contains("for_translating");
            ExcelWorksheet sheet = eP.Workbook.Worksheets.Add("Data");

            var row = 1;
            var col = 1;

            sheet.Cells[row, col].Value = "ID";
            sheet.Cells[row, col + 1].Value = "Speaker";
            sheet.Cells[row, col + 2].Value = "Emotion";
            sheet.Cells[row, col + 3].Value = "Text";

            row++;

            var replacedIds = new List<string>();
            
            // Словарь для кэширования данных
            var cacheDict = new Dictionary<string, string>();

            foreach (LocalizEntity item in ids)
            {
                string id = item.LocalizID;

                // Проверяем наличие ключа в словаре
                if (!nativeDict.TryGetValue(id, out var value))
                {
                    // Если ключ не найден, проверяем - возможно это уже готовый текст
                    if (IsReadableText(id))
                    {
                        // Используем само значение LocalizID как готовый текст
                        value = id;
                        Console.WriteLine($"Используем готовый текст: '{id}'");
                    }
                    else
                    {
                        Console.WriteLine($"Предупреждение: ключ '{id}' не найден в словаре локализации и не является читаемым текстом. Пропускаем.");
                        continue;
                    }
                }

                if (forTranslating)
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
                sheet.Cells[row, col + 1].Value = item.SpeakerDisplayName;
                sheet.Cells[row, col + 2].Value = item.Emotion;
                sheet.Cells[row, col + 3].Value = value;

                // Добавляем в кэш-словарь для последующего использования
                cacheDict.TryAdd(item.LocalizID, value);

                if (!replacedIds.Contains(id)) wordCount += CountWords(value);

                row++;
            }

            byte[] bin = eP.GetAsByteArray();
            string filePath = _projectPath + @"\Localization\" + _baseLanguage + @"\" + name + ".xlsx";
            
            File.WriteAllBytes(filePath, bin);

            // НОВОЕ: Сразу кэшируем созданный файл в _savedXMLDicts
            // Для созданных файлов кэшируем только для колонки 1, так как текст всегда в колонке 4
            string cacheKeyCol1 = $"{filePath}|column:1";
            
            _savedXMLDicts.TryAdd(cacheKeyCol1, new Dictionary<string, string>(cacheDict));
            
            Console.WriteLine($"💾 Кэшированы данные для файла: {name}.xlsx");

            if (name.Contains("internal")) return;

            Console.WriteLine("Таблица " + name + " сгенерирована, количество слов: " + wordCount);

            _allWordsCount += wordCount;

            if (name.Contains("12")) Console.WriteLine("total count = " + _allWordsCount);
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
        protected bool GenerateOutputFolder()
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
                Form1.ShowMessage($"Глав в книге меньше введённого количества. Найдено: {chaptersIds.Count}, требуется: {Form1.AvailableChapters}");

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
                    if (langName == _baseLanguage || !langsCols.TryAdd(langName, 4)) continue;
                    
                    Console.WriteLine($"Добавлен язык для локализации: {langName}");
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

                    string duplicateCharError = $"Найдены дублирующиеся значения среди персонажей: {aObj.DisplayName} (ключ: '{cObj.DisplayName}', AID: '{cObj.Aid}')";
                    Form1.ShowMessage(duplicateCharError);

                    return false;
                }

                if (cObj.AtlasFileName.Contains("Sec_") || cObj.BaseNameInAtlas.Contains("Sec_"))
                {
                    if (cObj.AtlasFileName != cObj.BaseNameInAtlas)
                    {
                        string atlasError = $"AtlasFileName и BaseNameInAtlas у второстепенных должны быть одинаковы: {cObj.DisplayName}";
                        Form1.ShowMessage(atlasError);

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

                string clothesError = $"В артиси не определена переменная с именем Clothes: {cObj.ClothesVariableName}";
                Form1.ShowMessage(clothesError);

                return false;
            }
            return true;
        }

        // Проверка локаций
        private bool CheckLocations(AjLinkerMeta meta)
        {
            if (meta.UniqueId == "Pirates_1") return true;
            
            for (var i = 0; i < meta.Locations.Count; i++)
            {
                AjMetaLocationData cObj = meta.Locations[i];

                for (var j = 0; j < meta.Locations.Count; j++)
                {
                    if (i == j) continue;

                    AjMetaLocationData aObj = meta.Locations[j];

                    if (cObj.DisplayName != aObj.DisplayName && cObj.SpriteName != aObj.SpriteName)
                        continue;

                    string duplicateLocError = $"Найдены дублирующиеся значения среди локаций: {aObj.DisplayName}";
                    Form1.ShowMessage(duplicateLocError);
                    return false;
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

                string displayNameKey = pair.Value.Properties.DisplayName;
                if (!nativeDict.TryGetValue(displayNameKey, out string dname))
                {
                    Console.WriteLine($"Предупреждение: ключ '{displayNameKey}' не найден в словаре локализации для объекта {pair.Value.EType}. Используем техническое имя.");
                    dname = displayNameKey; // Используем сам ключ как fallback
                }

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

                // НОВОЕ: Создаем JSON файл с эмоциями для for_translating файлов
                /*if (File.Exists(langFiles[0])) // Если есть файл Chapter_X_for_translating.xlsx
                {
                    var emotionsData = CreateLocalizationWithEmotions(langFiles[0]);
                    string emotionsJsonPath = chapterFolder + @"\Strings\" + lang + "_emotions.json";
                    File.WriteAllText(emotionsJsonPath, JsonConvert.SerializeObject(emotionsData, Formatting.Indented));
                    Console.WriteLine($"✅ Создан файл с эмоциями: {emotionsJsonPath}");
                }*/

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
                    throw new Exception($"Ошибка при генерации sharedstrings: проблемный ключ '{correct}' в языке '{lang}'. Проверьте файлы: {string.Join(", ", sharedLangFiles)}");
                }

                correct = generateLjson(lang,
                                           "previewstrings",
                                           stringToPreviewFile,
                                           previewFolder + @"\Strings\" + lang + ".json",
                                           colNum != -1 ? colNum : 1);

                if (string.IsNullOrEmpty(correct)) continue;
                
                showLocalizError(correct, "previewstrings");
                throw new Exception($"Ошибка при генерации previewstrings: проблемный ключ '{correct}' в языке '{lang}'. Проверьте файл: {bookDescsPath}");
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
                string expectedPath = pcoversSourcePath + @"\Russian\PreviewCover.png";
                Form1.ShowMessage($"Не все preview обложки присутствуют. Отсутствует: {expectedPath}");
                throw new Exception($"Отсутствуют preview обложки. Ожидаемый файл: {expectedPath}. Проверьте папку: {pcoversSourcePath}");
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
                string displayNameKey = objectsList[aid].Properties.DisplayName;
                if (!nativeDict.TryGetValue(displayNameKey, out string dname))
                {
                    Console.WriteLine($"Предупреждение: ключ персонажа '{displayNameKey}' не найден в словаре локализации. Используем техническое имя.");
                    dname = displayNameKey; // Используем сам ключ как fallback
                }

                if (meta.Characters.Find(l => l.DisplayName == dname) == null)
                {
                    string errorMsg = $"В таблице Meta.xlsx нет персонажа с именем '{dname}' (ключ: '{displayNameKey}', AID: '{aid}')";
                    Form1.ShowMessage(errorMsg);
                    throw new Exception(errorMsg + ". Проверьте лист Characters в файле Meta.xlsx");
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
                string displayNameKey = objectsList[aid].Properties.DisplayName;
                if (!nativeDict.TryGetValue(displayNameKey, out string dname))
                {
                    Console.WriteLine($"Предупреждение: ключ локации '{displayNameKey}' не найден в словаре локализации. Используем техническое имя.");
                    dname = displayNameKey; // Используем сам ключ как fallback
                }

                if (meta.Locations.Find(l => l.DisplayName == dname) == null)
                {
                    string locationErrorMsg = $"В таблице Meta.xlsx нет локации с именем '{dname}' (ключ: '{displayNameKey}', AID: '{aid}')";
                    Form1.ShowMessage(locationErrorMsg);
                    throw new Exception(locationErrorMsg + ". Проверьте лист Locations в файле Meta.xlsx");
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
                Console.WriteLine($"🔄 Начинаем генерацию {id} для языка {language}");
                Console.WriteLine($"🔄 Входные файлы: {string.Join(", ", inPaths.Select(Path.GetFileName))}");
                
                if (!allDicts.TryGetValue(language, out Dictionary<string, string> allStrings))
                {
                    allStrings = new Dictionary<string, string>();
                    allDicts[language] = allStrings;
                }

                AjLocalizInJsonFile jsonData = GetXMLFile(inPaths, colN);
                
                // ИСПРАВЛЕНО: определяем оригинальный язык на основе _baseLanguage, а не порядка обработки
                bool origLang = language == _baseLanguage;

                // Если это оригинальный язык или данных еще нет - сохраняем как оригинал
                if (origLang || !origLangData.ContainsKey(id)) 
                {
                    origLangData[id] = jsonData;
                    Console.WriteLine($"🔄 Сохранили как оригинальные данные для {id} (origLang: {origLang})");
                }

                AjLocalizInJsonFile origJsonData = origLangData[id];

                Console.WriteLine($"🔄 Начинаем обработку {origJsonData.Data.Count} ключей для {id} (origLang: {origLang})");
                
                foreach (KeyValuePair<string, string> pair in origJsonData.Data)
                {
                    string origValue = pair.Value.Trim();
                    if (!jsonData.Data.TryGetValue(pair.Key, out string translatedValue))
                    {
                        Console.WriteLine($"⚠️ Ключ {pair.Key} не найден в данных перевода для {language}");
                        continue;
                    }

                    translatedValue = translatedValue.Replace("Pname", "pname");

                    allStrings.TryAdd(pair.Key, translatedValue);

                    if (origValue.Contains("*SystemLinkTo*"))
                    {
                        string linkId = origValue.Split('*')[2];
                        if (!jsonData.Data.TryGetValue(linkId, out string linkedValue) &&
                            !allStrings.TryGetValue(linkId, out linkedValue))
                        {
                            Console.WriteLine($"⚠️ Связанный ключ {linkId} не найден");
                            continue;
                        }

                        jsonData.Data[pair.Key] = linkedValue;
                        translatedValue = linkedValue;
                    }

                    // Для оригинального языка проверяем только критические проблемы (пустые строки)
                    // Для переводов выполняем полную проверку
                    if (origLang && !string.IsNullOrEmpty(translatedValue.Trim())) continue;
                    
                    if (IsTranslationIncomplete(translatedValue,
                            origValue,
                            origLang,
                            jsonData.Data[pair.Key]))
                        Console.WriteLine($"⚠️ Неполный перевод для ключа {pair.Key}");
                }

                Console.WriteLine($"🔄 Проверяем проблемы локализации для {id}...");
                string localizationIssue = "";

                // Для оригинального языка проверяем только пустые значения
                localizationIssue = origLang ? CheckForEmptyValues(jsonData) :
                    // Для переводов выполняем полную проверку
                    CheckLocalizationIssues(origJsonData, jsonData);

                Console.WriteLine(!string.IsNullOrEmpty(localizationIssue)
                    ? $"❌ Найдена проблема локализации: {localizationIssue}"
                    : $"✅ Проблем локализации не найдено для {id}");

                WriteJsonFile(jsonData, outputPath);
                Console.WriteLine($"✅ Записан файл: {outputPath}");

                return localizationIssue;
            };
        }

        /// <summary>
        /// Проверка неполного перевода
        /// Для оригинального языка: проверяет только пустые строки
        /// Для переводов: выполняет полную проверку качества перевода
        /// </summary>
        private bool IsTranslationIncomplete(string translatedValue, string origValue, bool origLang, string jsonDataValue) =>
            // Всегда проверяем пустые строки (критично для всех языков)
            string.IsNullOrEmpty(translatedValue.Trim()) ||
            // Дополнительные проверки только для переводов (!origLang)
            (/*origValue.Trim() == translatedValue.Trim() &&*/
             !origLang &&  // Только для переводов, НЕ для оригинального языка
             origValue.Length > 10 &&
             !origValue.Contains("*SystemLinkTo*") &&
             !origValue.Contains("NextChoiceIsTracked") &&
             !jsonDataValue.Contains("StageDirections") &&
             !string.IsNullOrEmpty(jsonDataValue.Replace(".", "").Trim()) &&
             !origValue.ToLower().Contains("%pname%"));

        // Проверка пустых значений (для оригинального языка)
        private string CheckForEmptyValues(AjLocalizInJsonFile jsonData)
        {
            foreach (KeyValuePair<string, string> pair in jsonData.Data)
            {
                if (!string.IsNullOrEmpty(pair.Value.Trim())) continue;
                
                Console.WriteLine($"⚠️ Пустое значение для ключа '{pair.Key}' в оригинальном языке");
                return pair.Key;
            }

            return string.Empty;
        }

        // Проверка проблем локализации (для переводов)
        private string CheckLocalizationIssues(AjLocalizInJsonFile origJsonData,
                                               AjLocalizInJsonFile jsonData)
        {
            foreach (KeyValuePair<string, string> pair in origJsonData.Data)
            {
                // Проверяем, что ключ присутствует в переводе
                if (!jsonData.Data.TryGetValue(pair.Key, out var value))
                {
                    Console.WriteLine($"⚠️ Ключ '{pair.Key}' отсутствует в переводе");
                    return pair.Key;
                }
                
                // Проверяем, что значение не пустое
                if (!string.IsNullOrEmpty(value.Trim())) continue;
                
                Console.WriteLine($"⚠️ Пустое значение для ключа '{pair.Key}'");
                return pair.Key;
            }

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

        /// <summary>
        /// Определяет, является ли строка читаемым текстом (а не ключом)
        /// </summary>
        private static bool IsReadableText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Если содержит пробелы - скорее всего это текст
            if (text.Contains(" "))
                return true;

            // Если содержит знаки препинания - скорее всего это текст
            if (text.Contains("?") || text.Contains("!") || text.Contains(".") || text.Contains(",") || 
                text.Contains(";") || text.Contains(":") || text.Contains("'") || text.Contains("\""))
                return true;

            // Если начинается с "DFr_" или содержит подобные паттерны - это определенно ключ
            if (text.StartsWith("DFr_") || text.Contains(".Text") || text.Contains("_0x") || 
                text.Contains("0x") || text.Contains("Dfr_"))
                return false;

            // Проверяем является ли это техническим идентификатором
            // Если содержит много "_" или цифр в начале/конце - вероятно ключ
            int underscoreCount = text.Count(c => c == '_');
            if (underscoreCount >= 2)
                return false;

            // Если начинается или заканчивается цифрами и содержит "_" - вероятно ключ
            if ((char.IsDigit(text[0]) || char.IsDigit(text[^1])) && text.Contains("_"))
                return false;

            // Если содержит буквы (любого алфавита) и достаточно длинный - скорее всего текст
            bool hasLetters = text.Any(char.IsLetter);
            if (hasLetters && text.Length > 4)
            {
                // Дополнительная проверка: если это не выглядит как GUID или хеш
                bool looksLikeTechnicalId = text.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-') && 
                                           !text.Any(char.IsWhiteSpace) && 
                                           text.Length > 20;
                
                return !looksLikeTechnicalId;
            }

            return false;
        }

        #endregion

        /// <summary>
        /// Предварительно кэширует все созданные Excel файлы локализации
        /// Вызывается после GenerateLocalizTables для ускорения последующих операций
        /// </summary>
        public void PreCacheCreatedLocalizationFiles()
        {
            // Сначала очищаем возможные дублирующиеся записи
            CleanupDuplicateCache();
            
            string localizationPath = _projectPath + @"\Localization\" + _baseLanguage;
            
            if (!Directory.Exists(localizationPath))
            {
                Console.WriteLine("⚠️ Папка локализации не найдена для предварительного кэширования");
                return;
            }

            var filesToCache = new List<string>();
            
            // Ищем все созданные Excel файлы
            filesToCache.AddRange(Directory.GetFiles(localizationPath, "Chapter_*_for_translating.xlsx"));
            filesToCache.AddRange(Directory.GetFiles(localizationPath, "Chapter_*_internal.xlsx"));
            filesToCache.AddRange(Directory.GetFiles(localizationPath, "CharacterNames.xlsx"));

            Console.WriteLine($"🔄 Предварительное кэширование {filesToCache.Count} файлов локализации...");

            foreach (string filePath in filesToCache)
            {
                // Кэшируем только для колонки 1 (основная), так как созданные файлы имеют текст в колонке 4
                string cacheKey1 = $"{filePath}|column:1";
                if (!_savedXMLDicts.ContainsKey(cacheKey1))
                {
                    var dict = XMLTableToDict(filePath, 1);
                    Console.WriteLine($"💾 Предварительно кэширован: {Path.GetFileName(filePath)}");
                }
            }

            Console.WriteLine($"✅ Предварительное кэширование завершено. Кэшировано файлов: {filesToCache.Count}");
        }

        /// <summary>
        /// Очищает дублирующиеся записи в кэше для созданных файлов локализации
        /// </summary>
        public static void CleanupDuplicateCache()
        {
            var keysToRemove = new List<string>();
            
            foreach (string key in _savedXMLDicts.Keys)
            {
                // Ищем дублирующиеся записи для созданных файлов (column:2)
                if (key.Contains("Chapter_") && key.Contains("column:2") && 
                    (key.Contains("for_translating.xlsx") || key.Contains("internal.xlsx")))
                {
                    keysToRemove.Add(key);
                }
                else if (key.Contains("CharacterNames.xlsx") && key.Contains("column:2"))
                {
                    keysToRemove.Add(key);
                }
            }
            
            foreach (string key in keysToRemove)
            {
                _savedXMLDicts.Remove(key);
                Console.WriteLine($"🗑️ Удалена дублирующаяся запись кэша: {Path.GetFileName(key.Split('|')[0])} (column:2)");
            }
            
            if (keysToRemove.Count > 0)
            {
                Console.WriteLine($"✅ Очищено {keysToRemove.Count} дублирующихся записей кэша");
            }
        }
    }
}