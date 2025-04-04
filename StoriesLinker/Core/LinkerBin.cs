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
        private string ProjectPath;

        private Dictionary<string, Dictionary<int, Dictionary<string, string>>> _savedXMLDicts;

        public LinkerBin(string _project_path)
        {
            ProjectPath = _project_path;

            _savedXMLDicts = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();
        }

        private Dictionary<string, string> XMLTableToDict(string _path, int _column = 1)
        {
            if (_savedXMLDicts.TryGetValue(_path, out var columnsDict) && columnsDict.TryGetValue(_column, out var cachedDict))
            {
                return cachedDict;
            }

            Dictionary<string, string> _nativeDict = new Dictionary<string, string>();

            using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(_path)))
            {
                if (xlPackage.Workbook.Worksheets.Count == 0)
                {
                    throw new InvalidOperationException("The workbook contains no worksheets.");
                }
                var myWorksheet = xlPackage.Workbook.Worksheets.First();
                var totalRows = myWorksheet.Dimension.End.Row;
                var totalColumns = myWorksheet.Dimension.End.Column;

                for (int rowNum = 1; rowNum <= totalRows; rowNum++)
                {
                    ExcelRange _first_row = myWorksheet.Cells[rowNum, 1];
                    ExcelRange _second_row = myWorksheet.Cells[rowNum, _column + 1];

                    string _first_row_str = _first_row != null && _first_row.Value != null
                                                ? _first_row.Value.ToString()
                                                : "";
                    string _second_row_str = _second_row != null && _second_row.Value != null
                                                 ? _second_row.Value.ToString()
                                                 : " ";

                    if (string.IsNullOrEmpty(_first_row_str)) continue;

                    if (!_nativeDict.ContainsKey(_first_row_str))
                    {
                        _nativeDict.Add(_first_row_str, _second_row_str);
                    }
                    else
                    {
                        Console.WriteLine("double key critical error " + _first_row_str);
                    }
                }
            }

            if (!_savedXMLDicts.ContainsKey(_path))
            {
                _savedXMLDicts[_path] = new Dictionary<int, Dictionary<string, string>>();
            }
            _savedXMLDicts[_path][_column] = _nativeDict;

            return _nativeDict;
        }

        public Dictionary<string, string> GetNativeDict() { return XMLTableToDict(GetLocalizTablesPath(ProjectPath)); }

        public AJFile GetParsedFlowJSONFile()
        {
            AJFile _json_obj;

            using (StreamReader r = new StreamReader(GetFlowJSONPath(ProjectPath)))
            {
                string json = r.ReadToEnd();
                _json_obj = JsonConvert.DeserializeObject<AJFile>(json);
            }

            return _json_obj;
        }

        public AJLinkerMeta GetParsedMetaInputJSONFile()
        {
            AJLinkerMeta _json_obj = new AJLinkerMeta();

            _json_obj.Version = new BookVersionInfo();

            string _meta_xml_path = ProjectPath + @"\Raw\Meta.xlsx";

            Dictionary<string, string> _nativeDict = new Dictionary<string, string>();

            using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(_meta_xml_path)))
            {
                var myWorksheet = xlPackage.Workbook.Worksheets.First();
                var totalRows = myWorksheet.Dimension.End.Row;

                for (int rowNum = 2; rowNum <= totalRows; rowNum++)
                {
                    ExcelRange _first_row = myWorksheet.Cells[rowNum, 1];
                    ExcelRange _second_row = myWorksheet.Cells[rowNum, 2];

                    string _field_name = _first_row.Value.ToString();
                    string _field_value = _second_row.Value.ToString();

                    string[] _values;

                    switch (_field_name)
                    {
                        case "UniqueID":
                            _json_obj.UniqueID = _field_value;
                            break;
                        case "SpritePrefix":
                            _json_obj.SpritePrefix = _field_value;
                            break;
                        case "VersionBin":
                            _json_obj.Version.BinVersion = _field_value;
                            break;
                        case "VersionPreview":
                            _json_obj.Version.PreviewVersion = _field_value;
                            break;
                        case "VersionBaseResources":
                            _json_obj.Version.BaseResourcesVersion = _field_value;
                            break;
                        case "StandartizedUI":
                            _json_obj.StandartizedUI = _field_value == "1";
                            break;
                        case "UITextBlockFontSize":
                            _json_obj.UITextBlockFontSize = int.Parse(_field_value);
                            break;
                        case "UIChoiceBlockFontSize":
                            _json_obj.UIChoiceBlockFontSize = int.Parse(_field_value);
                            break;
                        case "KarmaCurrency":
                            _json_obj.KarmaCurrency = _field_value;
                            break;
                        case "KarmaBadBorder":
                            _json_obj.KarmaBadBorder = int.Parse(_field_value);
                            break;
                        case "KarmaGoodBorder":
                            _json_obj.KarmaGoodBorder = int.Parse(_field_value);
                            break;
                        case "KarmaTopLimit":
                            _json_obj.KarmaTopLimit = int.Parse(_field_value);
                            break;
                        case "CurrenciesInOrderOfUI":
                            _json_obj.CurrenciesInOrderOfUI = new List<string>(_field_value.Split(','));
                            break;
                        case "RacesList":
                            _json_obj.RacesList = _field_value != "-" 
                                                      ? new List<string>(_field_value.Split(',')) 
                                                      : new List<string>();

                            break;
                        case "ClothesSpriteNames":
                            _json_obj.ClothesSpriteNames = new List<string>(_field_value.Split(','));
                            break;
                        case "UndefinedClothesFuncVariant":
                            _json_obj.UndefinedClothesFuncVariant = int.Parse(_field_value);
                            break;
                        case "ExceptionsWeaponLayer":
                            _json_obj.ExceptionsWeaponLayer = _field_value == "1";
                            break;
                        case "UITextPlateLimits":
                            _values = _field_value.Split(',');

                            _json_obj.UITextPlateLimits = new List<int>();

                            foreach (var el in _values)
                            {
                                _json_obj.UITextPlateLimits.Add(int.Parse(el));
                            }

                            break;
                        case "UIPaintFirstLetterInRedException":
                            _json_obj.UIPaintFirstLetterInRedException = _field_value == "1";
                            break;
                        case "UITextPlateOffset":
                            _json_obj.UITextPlateOffset = int.Parse(_field_value);
                            break;
                        case "UIOverridedTextColor":
                            _json_obj.UIOverridedTextColor = _field_value == "1";
                            break;
                        case "UITextColor":
                            _values = _field_value.Split(',');

                            _json_obj.UITextColor = new List<int>();

                            foreach (var el in _values) _json_obj.UITextColor.Add(int.Parse(el));

                            break;
                        case "UIBlockedTextColor":
                            _values = _field_value.Split(',');

                            _json_obj.UIBlockedTextColor = new List<int>();

                            foreach (var el in _values) _json_obj.UIBlockedTextColor.Add(int.Parse(el));

                            break;
                        case "UIChNameTextColor":
                            _values = _field_value.Split(',');

                            _json_obj.UIChNameTextColor = new List<int>();

                            foreach (var el in _values) _json_obj.UIChNameTextColor.Add(int.Parse(el));

                            break;
                        case "UIOutlineColor":
                            _values = _field_value.Split(',');

                            _json_obj.UIOutlineColor = new List<int>();

                            foreach (var el in _values) _json_obj.UIOutlineColor.Add(int.Parse(el));

                            break;
                        case "UIResTextColor":
                            _values = _field_value.Split(',');

                            _json_obj.UIResTextColor = new List<int>();

                            foreach (var el in _values) _json_obj.UIResTextColor.Add(int.Parse(el));

                            break;
                        case "WardrobeEnabled":
                            _json_obj.WardrobeEnabled = _field_value == "1";
                            break;
                        case "MainHeroHasDifferentGenders":
                            _json_obj.MainHeroHasDifferentGenders = _field_value == "1";
                            break;
                        case "MainHeroHasSplittedHairSprite":
                            _json_obj.MainHeroHasSplittedHairSprite = _field_value == "1";
                            break;
                        case "CustomClothesCount":
                            _json_obj.CustomClothesCount = int.Parse(_field_value);
                            break;
                        case "CustomHairsCount":
                            _json_obj.CustomHairCount = int.Parse(_field_value);
                            break;
                    }
                }

                myWorksheet = xlPackage.Workbook.Worksheets[2];
                totalRows = myWorksheet.Dimension.End.Row;

                var _check_row = CheckRow();

                List<AJMetaCharacterData> _characters = new List<AJMetaCharacterData>();

                for (int rowNum = 2; rowNum <= totalRows; rowNum++)
                {
                    object[] _cells = new object[]
                                      {
                                          myWorksheet.Cells[rowNum, 1].Value,
                                          myWorksheet.Cells[rowNum, 2].Value,
                                          myWorksheet.Cells[rowNum, 3].Value,
                                          myWorksheet.Cells[rowNum, 4].Value
                                      };

                    int _ch_result = _check_row(_cells);

                    switch (_ch_result)
                    {
                        case -1: continue;
                        case 0: return null;
                    }

                    AJMetaCharacterData _ch = new AJMetaCharacterData();

                    _ch.DisplayName = _cells[0].ToString();
                    _ch.ClothesVariableName = _cells[1].ToString();
                    _ch.AtlasFileName = _cells[2].ToString();
                    _ch.BaseNameInAtlas = _cells[3].ToString();

                    _characters.Add(_ch);
                }

                _json_obj.Characters = _characters;

                myWorksheet = xlPackage.Workbook.Worksheets[3];
                totalRows = myWorksheet.Dimension.End.Row;

                List<AJMetaLocationData> _locations = new List<AJMetaLocationData>();

                for (int rowNum = 2; rowNum <= totalRows; rowNum++)
                {
                    object[] _cells = new object[]
                                      {
                                          myWorksheet.Cells[rowNum, 1].Value,
                                          myWorksheet.Cells[rowNum, 2].Value,
                                          myWorksheet.Cells[rowNum, 3].Value,
                                          myWorksheet.Cells[rowNum, 4].Value,
                                          myWorksheet.Cells[rowNum, 5].Value
                                      };

                    int _ch_result = _check_row(_cells);

                    switch (_ch_result)
                    {
                        case -1: continue;
                        case 0: return null;
                    }

                    AJMetaLocationData _loc = new AJMetaLocationData
                                              {
                                                  ID = int.Parse(_cells[0].ToString()), DisplayName = _cells[1].ToString(),
                                                  SpriteName = _cells[2].ToString(),
                                                  SoundIdleName = _cells[3].ToString()
                                              };

                    if (_cells[4].ToString() == "1")
                    {
                        _json_obj.IntroLocation = rowNum - 1;
                    }

                    _locations.Add(_loc);
                }

                _json_obj.Locations = _locations;
            }

            return _json_obj;
        }

        private static Func<object[], int> CheckRow()
        {
            Func<object[], int> _check_row = (_cells) =>
                                             {
                                                 bool _row_is_completely_empty = true;
                                                 bool _row_has_empty_field = false;

                                                 foreach (object _cell in _cells)
                                                 {
                                                     if (_cell == null
                                                         || string.IsNullOrEmpty(_cell.ToString().Trim()))
                                                     {
                                                         _row_has_empty_field = true;
                                                     }
                                                     else
                                                     {
                                                         _row_is_completely_empty = false;
                                                     }
                                                 }

                                                 if (_row_is_completely_empty)
                                                     return -1;
                                                 else if (_row_has_empty_field) return 0;

                                                 return 1;
                                             };
            return _check_row;
        }

        public Dictionary<string, AJObj> GetAricyBookEntities(AJFile _ajfile, Dictionary<string, string> _nativeDict)
        {
            Dictionary<string, AJObj> ObjectsList = new Dictionary<string, AJObj>();

            List<AJObj> Models = _ajfile.Packages[0].Models;

            Dictionary<string, int> _chapters_id_names = new Dictionary<string, int>();

            foreach (AJObj _ns in Models)
            {
                AJType _type;

                switch (_ns.Type)
                {
                    case "FlowFragment":
                        _type = AJType.FlowFragment;
                        string _value = Regex.Match(_nativeDict[_ns.Properties.DisplayName], @"\d+").Value;

                        int _int_value = int.Parse(_value);

                        _chapters_id_names.Add(_ns.Properties.Id, _int_value);
                        break;
                    case "Dialogue":
                        _type = AJType.Dialogue;
                        break;
                    case "Entity":
                        _type = AJType.Entity;
                        break;
                    case "DefaultSupportingCharacterTemplate":
                        _type = AJType.Entity;
                        break;
                    case "DefaultMainCharacterTemplate":
                        _type = AJType.Entity;
                        break;
                    case "Location":
                        _type = AJType.Location;
                        break;
                    case "DialogueFragment":
                        _type = AJType.DialogueFragment;
                        break;
                    case "Instruction":
                        _type = AJType.Instruction;
                        break;
                    case "Condition":
                        _type = AJType.Condition;
                        break;
                    case "Jump":
                        _type = AJType.Jump;
                        break;
                    default:
                        _type = AJType.Other;
                        break;
                }

                _ns.EType = _type;

                ObjectsList.Add(_ns.Properties.Id, _ns);
            }

            return ObjectsList;
        }

        private List<string> GetSortedChaptersList(Dictionary<string, AJObj> _objList,
                                                   Dictionary<string, string> _nativeDict)
        {
            List<string> _chapters_ids = new List<string>();

            Dictionary<string, int> _chapters_id_names = new Dictionary<string, int>();

            foreach (KeyValuePair<string, AJObj> _kobj in _objList)
            {
                if (_kobj.Value.EType != AJType.FlowFragment) continue;
                
                string _value = Regex.Match(_nativeDict[_kobj.Value.Properties.DisplayName], @"\d+").Value;

                int _int_value = int.Parse(_value);

                _chapters_id_names.Add(_kobj.Value.Properties.Id, _int_value);
            }

            var sorted_chapter_names = from entry in _chapters_id_names orderby entry.Value ascending select entry;

            foreach (KeyValuePair<string, int> _pair in sorted_chapter_names)
            {
                _chapters_ids.Add(_pair.Key);
            }

            return _chapters_ids;
        }

        private List<string>[] GetChaptersAndSubchaptersParentsIDs(List<string> _chapters_ids,
                                                                   Dictionary<string, AJObj> _objList)
        {
            List<List<string>> _ids = new List<List<string>>();

            for (int i = 0; i < _chapters_ids.Count; i++)
            {
                string _chapter_id = _chapters_ids[i];

                _ids.Add(new List<string>());
                _ids[i].Add(_chapter_id);

                foreach (KeyValuePair<string, AJObj> _kobj in _objList)
                {
                    if (_kobj.Value.EType != AJType.Dialogue) continue; //subchapter 
                    
                    string _subchapter_id = _kobj.Value.Properties.Id;

                    string _parent = _kobj.Value.Properties.Parent;

                    while (true)
                    {
                        if (_parent == _chapter_id)
                        {
                            _ids[i].Add(_subchapter_id);
                            break;
                        }
                        else
                        {
                            if (_objList.ContainsKey(_parent))
                            {
                                _parent = _objList[_parent].Properties.Parent;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return _ids.ToArray();
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
            if (Directory.Exists(ProjectPath + @"\Localization"))
                Directory.Delete(ProjectPath + @"\Localization", true);

            Directory.CreateDirectory(ProjectPath + @"\Localization");
            Directory.CreateDirectory(ProjectPath + @"\Localization\Russian");

            AJFile _ajfile = GetParsedFlowJSONFile();

            Dictionary<string, string> _native_dict = GetNativeDict();
            Dictionary<string, AJObj> _objects_list = GetAricyBookEntities(_ajfile, _native_dict);

            List<string> _chapters_ids = GetSortedChaptersList(_objects_list, _native_dict);

            if (_chapters_ids.Count < Form1.AvailableChapters)
            {
                Form1.ShowMessage("Глав в книге меньше введённого количества");

                return false;
            }

            _chapters_ids.RemoveRange(Form1.AvailableChapters, _chapters_ids.Count - Form1.AvailableChapters);

            Func<AJColor, string> _recognize_emotion = (_color) =>
                                                       {
                                                           EChEmotion _emotion = EChEmotion.IsntSetOrNeutral;

                                                           Func<Color32, Color32, bool> _colors_equals = (_a, _b) =>
                                                                                                         {
                                                                                                             return
                                                                                                                 (Math
                                                                                                                      .Abs(_a
                                                                                                                               .r
                                                                                                                           - _b
                                                                                                                               .r)
                                                                                                                  < 20
                                                                                                                  && Math
                                                                                                                      .Abs(_a
                                                                                                                               .g
                                                                                                                           - _b
                                                                                                                               .g)
                                                                                                                  < 20
                                                                                                                  && Math
                                                                                                                      .Abs(_a
                                                                                                                               .b
                                                                                                                           - _b
                                                                                                                               .b)
                                                                                                                  < 20);
                                                                                                         };

                                                           Color32 _frag_color = _color.ToColor32();
                                                           Color32[] _emotions_color
                                                               = new Color32[]
                                                                 {
                                                                     new Color32(255, 0, 0, 0),
                                                                     new Color32(0, 110, 20, 0),
                                                                     new Color32(41, 6, 88, 0),
                                                                     new Color32(255, 134, 0, 0)
                                                                 };

                                                           for (int i = 0; i < _emotions_color.Length; i++)
                                                           {
                                                               if (_colors_equals(_emotions_color[i], _frag_color))
                                                               {
                                                                   _emotion = (EChEmotion)i;
                                                                   break;
                                                               }
                                                           }

                                                           return _emotion.ToString();
                                                       };

            List<string>[] _csparents_ids = GetChaptersAndSubchaptersParentsIDs(_chapters_ids, _objects_list);

            List<string> _characters_ids = new List<string>();
            List<LocalizEntity> _characters_localiz_ids = new List<LocalizEntity>();

            Dictionary<string, string> _characters_names = new Dictionary<string, string>();

            for (int i = 0; i < _csparents_ids.Length; i++)
            {
                int _chapter_n = i + 1;

                List<LocalizEntity> _for_translating = new List<LocalizEntity>();
                List<LocalizEntity> _non_translating = new List<LocalizEntity>();
                List<string> _parents_ids = _csparents_ids[i];

                foreach (KeyValuePair<string, AJObj> _scobj in _objects_list)
                {
                    if (_parents_ids.Contains(_scobj.Value.Properties.Parent))
                    {
                        AJObj _dfobj = _scobj.Value;

                        if (_dfobj.EType != AJType.DialogueFragment) continue;

                        string _ch_id = _dfobj.Properties.Speaker;

                        if (!_characters_ids.Contains(_ch_id))
                        {
                            LocalizEntity _entity = new LocalizEntity();

                            _entity.LocalizID = _objects_list[_ch_id].Properties.DisplayName;

                            _characters_ids.Add(_ch_id);
                            _characters_localiz_ids.Add(_entity);

                            _characters_names.Add(_ch_id, _native_dict[_objects_list[_ch_id].Properties.DisplayName]);
                        }


                        if (!string.IsNullOrEmpty(_dfobj.Properties.Text))
                        {
                            LocalizEntity _entity = new LocalizEntity();

                            _entity.LocalizID = _dfobj.Properties.Text;
                            _entity.SpeakerDisplayName = _characters_names[_ch_id];
                            _entity.Emotion = _recognize_emotion(_dfobj.Properties.Color);

                            _for_translating.Add(_entity);
                        }

                        if (!string.IsNullOrEmpty(_dfobj.Properties.MenuText))
                        {
                            LocalizEntity _entity = new LocalizEntity();

                            _entity.LocalizID = _dfobj.Properties.MenuText;
                            _entity.SpeakerDisplayName = _characters_names[_ch_id];
                            _entity.Emotion = _recognize_emotion(_dfobj.Properties.Color);

                            _for_translating.Add(_entity);
                        }

                        if (!string.IsNullOrEmpty(_dfobj.Properties.StageDirections))
                        {
                            LocalizEntity _entity = new LocalizEntity();

                            _entity.LocalizID = _dfobj.Properties.StageDirections;
                            _entity.SpeakerDisplayName = "";

                            _non_translating.Add(_entity);
                        }
                    }
                }

                CreateLocalizTable(string.Format("Chapter_{0}_for_translating", _chapter_n),
                                   _for_translating,
                                   _native_dict);
                CreateLocalizTable(string.Format("Chapter_{0}_internal", _chapter_n), _non_translating, _native_dict);
            }

            CreateLocalizTable(string.Format("CharacterNames"), _characters_localiz_ids, _native_dict);

            return true;
        }

        private int AllWordsCount = 0;

        private void CreateLocalizTable(string _name, List<LocalizEntity> _ids, Dictionary<string, string> _nativeDict)
        {
            int _wordCount = 0;

            using (var eP = new ExcelPackage())
            {
                bool _for_translating = _name.Contains("for_translating");
                var sheet = eP.Workbook.Worksheets.Add("Data");

                bool _for_localizators_mode = Form1.ForLocalizatorsMode;

                var row = 1;
                var col = 1;

                sheet.Cells[row, col].Value = "ID";

                if (_for_localizators_mode)
                {
                    sheet.Cells[row, col + 1].Value = "Speaker";
                    sheet.Cells[row, col + 2].Value = "Emotion";
                }

                sheet.Cells[row, col + (_for_localizators_mode ? 3 : 1)].Value = "Text";

                row++;

                List<string> _replaced_ids = new List<string>();

                foreach (LocalizEntity item in _ids)
                {
                    string _id = item.LocalizID;

                    string _value = _nativeDict[_id];

                    if (_for_translating && _for_localizators_mode)
                    {
                        _value = _value.Replace("pname", "%pname%");
                        _value = _value.Replace("Pname", "%pname%");

                        if (!_replaced_ids.Contains(_id))
                        {
                            List<string> _repeated_values = new List<string>();

                            foreach (KeyValuePair<string, string> _pair in _nativeDict)
                            {
                                if (_pair.Value == _value && _pair.Key != _id)
                                {
                                    _repeated_values.Add(_pair.Key);
                                }
                            }

                            if (_repeated_values.Count == 1 || (_repeated_values.Count > 1 && _value.Contains("?")))
                            {
                                foreach (var el in _repeated_values)
                                {
                                    _nativeDict[el] = "*SystemLinkTo*" + _id + "*";
                                    _replaced_ids.Add(el);
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(_value.Trim())) continue;

                    sheet.Cells[row, col].Value = item.LocalizID;

                    if (_for_localizators_mode)
                    {
                        sheet.Cells[row, col + 1].Value = item.SpeakerDisplayName;
                        sheet.Cells[row, col + 2].Value = item.Emotion;
                    }

                    sheet.Cells[row, col + (_for_localizators_mode ? 3 : 1)].Value = _value;

                    if (_for_localizators_mode && !_replaced_ids.Contains(_id))
                    {
                        _wordCount += CountWords(_value);
                    }

                    row++;
                }

                var bin = eP.GetAsByteArray();

                File.WriteAllBytes(ProjectPath + @"\Localization\Russian\" + _name + ".xlsx", bin);

                if (_name.Contains("internal") || !_for_localizators_mode) return;
                
                Console.WriteLine("Таблица " + _name + " сгенерирована, количество слов: " + _wordCount);

                AllWordsCount += _wordCount;

                if (_name.Contains("12")) Console.WriteLine("total count = " + AllWordsCount);
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

            string _temp_folder = ProjectPath + @"\Temp\";

            AJFile _ajfile = GetParsedFlowJSONFile();
            AJLinkerMeta _meta = GetParsedMetaInputJSONFile();

            if (_meta == null)
            {
                Form1.ShowMessage("Таблица содержит пустые поля в листе Characters или Locations.");

                return false;
            }


            // проверка на дубль имён персонажей и их имён в атласах

            for (int i = 0; i < _meta.Characters.Count; i++)
            {
                AJMetaCharacterData _c_obj = _meta.Characters[i];

                for (int j = 0; j < _meta.Characters.Count; j++)
                {
                    if (i == j) continue;

                    AJMetaCharacterData _a_obj = _meta.Characters[j];

                    if (_c_obj.DisplayName != _a_obj.DisplayName
                        && (_c_obj.BaseNameInAtlas != _a_obj.BaseNameInAtlas
                            || _c_obj.BaseNameInAtlas == "-"
                            || _meta.UniqueID == "Shism_1")
                        && (_c_obj.ClothesVariableName != _a_obj.ClothesVariableName
                            || _c_obj.ClothesVariableName.Trim() == "-"))
                        continue;
                    
                    Form1.ShowMessage("Найдены дублирующиеся значения среди персонажей: " + _a_obj.DisplayName);

                    return false;
                }

                if (_c_obj.AtlasFileName.Contains("Sec_") || _c_obj.BaseNameInAtlas.Contains("Sec_"))
                {
                    if (_c_obj.AtlasFileName != _c_obj.BaseNameInAtlas)
                    {
                        Form1.ShowMessage("AtlasFileName и BaseNameInAtlas у второстепенных должны быть одинаковы: "
                                          + _c_obj.DisplayName);

                        return false;
                    }
                }

                int _clothes_ns_index = _ajfile.GlobalVariables.FindIndex(_ns => _ns.Namespace == "Clothes");

                var state1 = _clothes_ns_index != -1;
                var state2 = _ajfile.GlobalVariables[_clothes_ns_index].Variables
                                    .FindIndex(_v => _v.Variable == _c_obj.ClothesVariableName) != -1;
                if (_c_obj.ClothesVariableName.Trim() == "-" || (state1 && state2))
                    continue;
                
                Form1.ShowMessage("В артиси не определена переменная с именем Clothes."
                                  + _c_obj.ClothesVariableName);

                return false;
            }


            // проверка на дубль имён и спрайтов локаций

            if (_meta.UniqueID != "Pirates_1")
            {
                for (int i = 0; i < _meta.Locations.Count; i++)
                {
                    AJMetaLocationData _c_obj = _meta.Locations[i];

                    for (int j = 0; j < _meta.Locations.Count; j++)
                    {
                        if (i == j) continue;

                        AJMetaLocationData _a_obj = _meta.Locations[j];

                        if (_c_obj.DisplayName != _a_obj.DisplayName && _c_obj.SpriteName != _a_obj.SpriteName)
                            continue;
                        
                        Form1.ShowMessage("Найдены дублирующиеся значения среди локаций: " + _a_obj.DisplayName);
                        return false;
                    }
                }
            }

            var _get_version_name = GetVersionName();

            if (Directory.Exists(_temp_folder)) Directory.Delete(_temp_folder, true);

            Directory.CreateDirectory(_temp_folder);

            string _bin_folder = _temp_folder + _get_version_name("bin", _meta.Version.BinVersion);
            string _br_folder = _temp_folder + _get_version_name("baseResources", _meta.Version.BaseResourcesVersion);
            string _preview_folder = _temp_folder + _get_version_name("preview", _meta.Version.PreviewVersion);

            Directory.CreateDirectory(_preview_folder);
            Directory.CreateDirectory(_preview_folder + @"\Covers");
            Directory.CreateDirectory(_preview_folder + @"\Strings");

            Directory.CreateDirectory(_bin_folder);
            Directory.CreateDirectory(_bin_folder + @"\SharedStrings");
            Directory.CreateDirectory(_br_folder);
            Directory.CreateDirectory(_br_folder + @"\UI");
            Directory.CreateDirectory(_br_folder + @"\Music");

            Dictionary<string, string> _native_dict = GetNativeDict();
            Dictionary<string, AJObj> _objects_list = GetAricyBookEntities(_ajfile, _native_dict);

            List<string> _chapters_ids = GetSortedChaptersList(_objects_list, _native_dict);

            if (_chapters_ids.Count < Form1.AvailableChapters)
            {
                Form1.ShowMessage("Глав в книге меньше введённого количества");

                return false;
            }

            _chapters_ids.RemoveRange(Form1.AvailableChapters, _chapters_ids.Count - Form1.AvailableChapters);

            List<string>[] _csparents_ids = GetChaptersAndSubchaptersParentsIDs(_chapters_ids, _objects_list);

            AJAssetGridLinker _grid_linker = new AJAssetGridLinker();

            var _check_add_ch = CheckAddCh(_native_dict, _objects_list, _meta, _grid_linker);

            var _check_add_loc_int = CheckAddLocINT(_meta, _grid_linker);

            var _check_add_loc = CheckAddLoc(_native_dict, _objects_list, _meta, _grid_linker);

            List<string> _copiedChAtlasses = new List<string>();
            List<string> _copiedLocSprites = new List<string>();
            List<string> _copiedLocIdles = new List<string>();

            List<AJObj> SharedObjs = new List<AJObj>();

            foreach (KeyValuePair<string, AJObj> _pair in _objects_list)
            {
                if (_pair.Value.EType != AJType.Entity && _pair.Value.EType != AJType.Location) continue;
                
                string _dname = _native_dict[_pair.Value.Properties.DisplayName];

                if (_pair.Value.EType == AJType.Entity)
                {
                    int _index = _meta.Characters.FindIndex(_ch => _ch.DisplayName == _dname);

                    if (_index != -1) _meta.Characters[_index].AID = _pair.Key;
                }
                else
                {
                    int _index = _meta.Locations.FindIndex(_loc => _loc.DisplayName == _dname);

                    if (_index != -1) _meta.Locations[_index].AID = _pair.Key;
                }

                SharedObjs.Add(_pair.Value);
            }

            foreach (var el in _meta.Locations.Where(el => string.IsNullOrEmpty(el.AID)))
            {
                el.AID = "fake_location_aid" + el.ID;
            }

            string _translated_data_folder = ProjectPath + @"\TranslatedData\";

            bool _multi_lang_output = Directory.Exists(_translated_data_folder);

            Dictionary<string, int> _langs_cols = new Dictionary<string, int> { { "Russian", _multi_lang_output ? 3 : 1 } };

            if (_multi_lang_output)
            {
                // Получаем все папки в директории TranslatedData
                var languageFolders = Directory.GetDirectories(_translated_data_folder);
                foreach (var folder in languageFolders)
                {
                    // Получаем имя папки (язык)
                    string language = Path.GetFileName(folder);
                    // Пропускаем папку Russian, так как она уже добавлена
                    if (language != "Russian")
                    {
                        _langs_cols.Add(language, 4);
                    }
                }
            }

            _meta.ChaptersEntryPoints = new List<string>();

            AJGridAssetJSON _grid_asset_file = new AJGridAssetJSON();

            Dictionary<string, Dictionary<string, string>> _allDicts
                = new Dictionary<string, Dictionary<string, string>>();


            for (int i = 0; i < _csparents_ids.Length; i++)
            {
                _grid_linker.AddChapter();

                int _chapter_n = i + 1;
                List<string> _parents_ids = _csparents_ids[i];

                _meta.ChaptersEntryPoints.Add(_parents_ids[0]);

                List<AJObj> _chapter_objs = new List<AJObj>();

                foreach (KeyValuePair<string, AJObj> _pair in _objects_list)
                {
                    if (!_parents_ids.Contains(_pair.Value.Properties.Parent)
                        && !_parents_ids.Contains(_pair.Value.Properties.Id))
                        continue;
                    
                    AJObj _dfobj = _pair.Value;

                    switch (_dfobj.EType)
                    {
                        case AJType.DialogueFragment:
                        {
                            string _ch_id = _dfobj.Properties.Speaker;

                            _check_add_ch(_ch_id);
                            break;
                        }
                        case AJType.Dialogue:
                        {
                            List<string> _attachments = _dfobj.Properties.Attachments;

                            foreach (var el in _attachments)
                            {
                                AJObj _at_obj = _objects_list[el];

                                if (_at_obj.EType == AJType.Location)
                                    _check_add_loc(el);
                                else if (_at_obj.EType == AJType.Entity) _check_add_ch(el);
                            }

                            break;
                        }
                        case AJType.Instruction:
                        {
                            string _raw_script = _dfobj.Properties.Expression;

                            if (_raw_script.Contains("Location.loc"))
                            {
                                string[] _scripts = _raw_script.EscapeString()
                                                               .Replace("\\n", "")
                                                               .Replace("\\r", "")
                                                               .Split(';');

                                foreach (var _uscript in _scripts)
                                {
                                    if (!_uscript.Contains("Location.loc")) continue;
                                    
                                    string[] _parts = _uscript.Split('=');
                                    int _loc_id = int.Parse(_parts[1].Trim());
                                    _check_add_loc_int(_loc_id);
                                }
                            }

                            break;
                        }
                    }

                    _chapter_objs.Add(_dfobj);
                }

                AJLinkerOutputChapterFlow _flow_json = new AJLinkerOutputChapterFlow { Objects = _chapter_objs };

                string _chapter_folder
                    = _temp_folder + _get_version_name("chapter" + _chapter_n, _meta.Version.BinVersion);

                Directory.CreateDirectory(_chapter_folder);
                Directory.CreateDirectory(_chapter_folder + @"\Resources");
                Directory.CreateDirectory(_chapter_folder + @"\Strings");

                File.WriteAllText(_chapter_folder + @"\Flow.json", JsonConvert.SerializeObject(_flow_json));

                string[] _chapter_chs = _grid_linker.GetCharactersNamesFromCurChapter();
                string[] _locations_chs = _grid_linker.GetLocationsNamesFromCurChapter();

                foreach (var el in _chapter_chs)
                {
                    AJMetaCharacterData _ch = _meta.Characters.Find(_lch => _lch.DisplayName.Trim() == el.Trim());

                    string _atlasNameFiled = _ch.AtlasFileName;

                    List<string> _atlases = new List<string>();

                    if (!_atlasNameFiled.Contains(","))
                    {
                        _atlases.Add(_atlasNameFiled);
                    }
                    else
                    {
                        string[] _atlas_strs = _atlasNameFiled.Split(',');

                        _atlases.AddRange(_atlas_strs.Where(t => !string.IsNullOrEmpty(t)));
                    }

                    foreach (var _atlasFileName in _atlases)
                    {
                        if (_ch.BaseNameInAtlas == "-" 
                            || _atlasFileName == "-" 
                            || _copiedChAtlasses.Contains(_atlasFileName))
                            continue;
                        
                        _copiedChAtlasses.Add(_atlasFileName);

                        if (!_atlasFileName.Contains("Sec_"))
                        {
                            File.Copy(string.Format(ProjectPath + @"\Art\Characters\{0}.png", _atlasFileName),
                                      string.Format(_chapter_folder + @"\Resources\{0}.png", _atlasFileName));
                            File.Copy(string.Format(ProjectPath + @"\Art\Characters\{0}.tpsheet", _atlasFileName),
                                      string.Format(_chapter_folder + @"\Resources\{0}.tpsheet", _atlasFileName));
                        }
                        else
                        {
                            string _file_name = _atlasFileName;
                            _file_name = _file_name.Replace("Sec_", _meta.SpritePrefix);

                            File.Copy(string.Format(ProjectPath + @"\Art\Characters\Secondary\{0}.png", _file_name),
                                      string.Format(_chapter_folder + @"\Resources\{0}.png", _atlasFileName));
                        }
                    }
                }

                foreach (var el in _locations_chs)
                {
                    AJMetaLocationData _loc = _meta.Locations.Find(_lloc => _lloc.DisplayName == el);

                    if (!_copiedLocSprites.Contains(_loc.SpriteName))
                    {
                        _copiedLocSprites.Add(_loc.SpriteName);

                        File.Copy(string.Format(ProjectPath + @"\Art\Locations\{0}.png", _loc.SpriteName),
                                  string.Format(_chapter_folder + @"\Resources\{0}.png", _loc.SpriteName));
                    }


                    if (_loc.SoundIdleName == "-" || _copiedLocIdles.Contains(_loc.SoundIdleName)) continue;
                    
                    _copiedLocIdles.Add(_loc.SoundIdleName);
                    File.Copy(string.Format(ProjectPath + @"\Audio\Idles\{0}.mp3", _loc.SoundIdleName),
                              string.Format(_chapter_folder + @"\Resources\{0}.mp3", _loc.SoundIdleName));
                }

                AJGridAssetChapterJSON _grid_asset_chapter = new AJGridAssetChapterJSON
                                                             {
                                                                 CharactersIDs
                                                                     = _grid_linker.GetCharactersIDsFromCurChapter(),
                                                                 LocationsIDs = _grid_linker.GetLocationsIDsFromCurChapter()
                                                             };

                _grid_asset_file.Chapters.Add(_grid_asset_chapter);

                Dictionary<string, AJLocalizInJSONFile> _orig_lang_data = new Dictionary<string, AJLocalizInJSONFile>();

                // Получаем список всех известных языков из ключей _langs_cols
                List<string> knownLanguagesList = _langs_cols.Keys.ToList();

                // Передаем список языков в GenerateLjson при создании Func
                var _generate_ljson = GenerateLjson(_allDicts, _orig_lang_data);

                string _lang_origin_folder = ProjectPath + @"\Localization\Russian";

                Action<string, string> _show_localiz_error = ShowLocalizError();

                if (Form1.OnlyEnglishMode)
                {
                    if (!_langs_cols.ContainsKey("English")) _langs_cols.Add("English", -1);
                     // Обновляем список известных языков, если добавили English
                    knownLanguagesList = _langs_cols.Keys.ToList();
                }

                foreach (KeyValuePair<string, int> _lang_pair in _langs_cols)
                {
                    string _lang = _lang_pair.Key;
                    int _col_num = _lang_pair.Value;

                    bool _native_lang = _lang == "Russian" || _col_num == -1;

                    string _lang_folder
                        = (_native_lang ? _lang_origin_folder : ProjectPath + @"\TranslatedData\" + _lang);
                    string _book_descs_path = ProjectPath + @"\Raw\BookDescriptions\" + _lang + ".xlsx";

                    // Проверяем альтернативный путь для файла локализации
                    if (!File.Exists(_book_descs_path) && !_native_lang)
                    {
                        _book_descs_path = ProjectPath + @"\TranslatedData\" + _lang + @"\" + _lang + ".xlsx";
                    }

                    Console.WriteLine("GENERATE TABLES FOR LANGUAGE: " + _lang);

                    if (!Directory.Exists(_lang_folder)) continue;
                    
                    string[] _lang_files = new string[]
                                           {
                                               string.Format(_lang_folder + @"\Chapter_{0}_for_translating.xlsx",
                                                             _chapter_n),
                                               string.Format(_lang_origin_folder + @"\Chapter_{0}_internal.xlsx",
                                                             _chapter_n)
                                           };

                    if (!File.Exists(_lang_files[0])) break;

                    // Вызываем Func, передавая список языков
                    string _correct = _generate_ljson(_lang,
                                                      "chapter" + _chapter_n,
                                                      _lang_files,
                                                      _chapter_folder + @"\Strings\" + _lang + ".json",
                                                      _col_num != -1 ? _col_num : 1,
                                                      knownLanguagesList);

                    if (!string.IsNullOrEmpty(_correct))
                    {
                        _show_localiz_error(_correct, "chapter" + _chapter_n);

                        //return false;
                    }

                    if (_chapter_n != 1) continue;
                        
                    string[] _shared_lang_files = new string[]
                                                  {
                                                      string.Format(_lang_folder + @"\CharacterNames.xlsx",
                                                                    _chapter_n),
                                                      _book_descs_path
                                                  };

                    Console.WriteLine("generate sharedstrings " + _book_descs_path);
                    
                    // Вызываем Func, передавая список языков
                    _correct = _generate_ljson(_lang,
                                               "sharedstrings",
                                               _shared_lang_files,
                                               _bin_folder + @"\SharedStrings\" + _lang + ".json",
                                               _col_num != -1 ? _col_num : 1,
                                               knownLanguagesList);

                    string[] _string_to_preview_file = new string[] { _book_descs_path };

                    if (!string.IsNullOrEmpty(_correct))
                    {
                        _show_localiz_error(_correct, "sharedstrings");
                        return false;
                    }
                    
                    // Вызываем Func, передавая список языков
                    _correct = _generate_ljson(_lang,
                                               "previewstrings",
                                               _string_to_preview_file,
                                               _preview_folder + @"\Strings\" + _lang + ".json",
                                               _col_num != -1 ? _col_num : 1,
                                               knownLanguagesList);

                    if (string.IsNullOrEmpty(_correct)) continue;
                            
                    _show_localiz_error(_correct, "previewstrings");
                    return false;
                }
            }

            AJLinkerOutputBase _base_json = new AJLinkerOutputBase
                                            {
                                                GlobalVariables = _ajfile.GlobalVariables, SharedObjs = SharedObjs
                                            };

            File.WriteAllText(_bin_folder + @"\Base.json", JsonConvert.SerializeObject(_base_json));
            File.WriteAllText(_bin_folder + @"\Meta.json", JsonConvert.SerializeObject(_meta));
            File.WriteAllText(_bin_folder + @"\AssetsByChapters.json", JsonConvert.SerializeObject(_grid_asset_file));

            string musicSourcePath = ProjectPath + @"\Audio\Music";
            string musicTempPath = _br_folder + @"\Music";
            if (!Directory.Exists(musicTempPath))
            {
                Directory.CreateDirectory(musicTempPath);
            }

            foreach (var srcPath in Directory.GetFiles(musicSourcePath))
            {
                //Copy the file from sourcepath and place into mentioned target path, 
                //Overwrite the file if same file is exist in target path
                File.Copy(srcPath, srcPath.Replace(musicSourcePath, musicTempPath), true);
            }

            string pcoversSourcePath = ProjectPath + @"\Art\PreviewCovers";
            string pcoversTempPath = _preview_folder + @"\Covers";
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

            string pbannersSourcePath = ProjectPath + @"\Art\SliderBanners";

            if (!Directory.Exists(pbannersSourcePath)) return true;
            {
                string pbannersTempPath = _preview_folder + @"\Banners";
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
            Func<string, string, string> _get_version_name = (_folder_name, _version) => char.ToUpper(_folder_name[0])
                                                                                   + _folder_name.Substring(1);
            return _get_version_name;
        }

        private static Action<string> CheckAddCh(Dictionary<string, string> _native_dict,
                                                 Dictionary<string, AJObj> _objects_list,
                                                 AJLinkerMeta _meta,
                                                 AJAssetGridLinker _grid_linker)
        {
            Action<string> _check_add_ch = _aid =>
                                           {
                                               string _dname = _native_dict[_objects_list[_aid].Properties.DisplayName];

                                               if (_meta.Characters.Find(_l => _l.DisplayName == _dname) == null)
                                               {
                                                   Form1.ShowMessage("В таблице нет персонажа с именем " + _dname);

                                                   throw new Exception("В таблице нет персонажа с именем " + _dname);
                                               }

                                               if (!_grid_linker.isChExist(_dname))
                                                   _grid_linker.AddCharacter(_dname, _aid);
                                           };
            return _check_add_ch;
        }

        private static Action<int> CheckAddLocINT(AJLinkerMeta _meta, AJAssetGridLinker _grid_linker)
        {
            Action<int> _check_add_loc_int = _int_id =>
                                             {
                                                 AJMetaLocationData _mdata
                                                     = _meta.Locations.Find(_chf => _chf.ID == _int_id);

                                                 if (!_grid_linker.isLocExist(_mdata.DisplayName))
                                                     _grid_linker.AddLocation(_mdata.DisplayName, _mdata.AID);
                                             };
            return _check_add_loc_int;
        }

        private static Action<string> CheckAddLoc(Dictionary<string, string> _native_dict,
                                                  Dictionary<string, AJObj> _objects_list,
                                                  AJLinkerMeta _meta,
                                                  AJAssetGridLinker _grid_linker)
        {
            Action<string> _check_add_loc = _aid =>
                                            {
                                                string _dname
                                                    = _native_dict[_objects_list[_aid].Properties.DisplayName];

                                                if (_meta.Locations.Find(_l => _l.DisplayName == _dname) == null)
                                                {
                                                    Form1.ShowMessage("В таблице нет локации с именем " + _dname);

                                                    throw new Exception("В таблице нет локации с именем " + _dname);
                                                }

                                                if (!_grid_linker.isLocExist(_dname))
                                                    _grid_linker.AddLocation(_dname, _objects_list[_aid].Properties.Id);
                                            };
            return _check_add_loc;
        }

        private static Action<string, string> ShowLocalizError()
        {
            Action<string, string> _show_localiz_error = (_missing_key, _file_group_id) =>
                                                         {
                                                             // Добавляем уточнение, что ключ отсутствует в данных для этой группы файлов
                                                             Form1.ShowMessage($"Ошибка мультиязыкового вывода: Ключ '{_missing_key}' отсутствует или пуст в данных для группы файлов '{_file_group_id}'");
                                                         };
            return _show_localiz_error;
        }

        private Func<string, string, string[], string, int, List<string>, string> GenerateLjson(Dictionary<string, Dictionary<string, string>> allDicts,
                                                                                  Dictionary<string, AJLocalizInJSONFile> origLangData)
        {
            return (language, id, inPaths, outputPath, colN, knownLanguages) =>
                   {
                       if (!allDicts.TryGetValue(language, out var allStrings))
                       {
                           allStrings = new Dictionary<string, string>();
                           allDicts[language] = allStrings;
                       }

                       var jsonData = GetXMLFile(inPaths, colN, knownLanguages);
                       var origLang = !origLangData.ContainsKey(id);

                       if (origLang) origLangData[id] = jsonData;

                       var origJsonData = origLangData[id];
                       if (origLang) jsonData = GetXMLFile(inPaths, colN, knownLanguages);

                       if (Form1.ForLocalizatorsMode)
                       {
                           Console.WriteLine($"start {id} {allStrings.Count}");
                           foreach (var pair in origJsonData.Data)
                           {
                               var origValue = pair.Value.Trim();
                               if (!jsonData.Data.TryGetValue(pair.Key, out var translatedValue))
                               {
                                   Console.WriteLine($"String with ID {pair.Key} not found");
                                   continue;
                               }

                               translatedValue = translatedValue.Replace("Pname", "pname");

                               if (!allStrings.ContainsKey(pair.Key)) allStrings[pair.Key] = translatedValue;

                               if (origValue.Contains("*SystemLinkTo*"))
                               {
                                   var linkId = origValue.Split('*')[2];
                                   if (!jsonData.Data.TryGetValue(linkId, out var linkedValue)
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

                       var localizationIssue = CheckLocalizationIssues(origJsonData, jsonData, origLang);
                       WriteJSONFile(jsonData, outputPath);

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

        private string CheckLocalizationIssues(AJLocalizInJSONFile origJsonData,
                                               AJLocalizInJSONFile jsonData,
                                               bool origLang)
        {
            foreach (var pair in origJsonData.Data)
                if (!jsonData.Data.ContainsKey(pair.Key)
                    || string.IsNullOrEmpty(jsonData.Data[pair.Key].Trim()))
                    return pair.Key;

            return string.Empty;
        }


        private AJLocalizInJSONFile GetXMLFile(string[] _paths_to_xmls, int _default_column, List<string> _known_languages)
        {
            Dictionary<string, string> _total = new Dictionary<string, string>();
            var knownLanguagesSet = new HashSet<string>(_known_languages ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

            Console.WriteLine("\n=== Начало обработки файлов локализации ===");
            foreach (var path in _paths_to_xmls)
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"ВНИМАНИЕ: Файл не найден: {path}");
                    continue;
                }

                Console.WriteLine($"\nОбработка файла: {path}");
                Dictionary<string, string> _file_dict = null;
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
                        Dictionary<string, string> dictD = XMLTableToDict(path, 3); // Колонка D
                        Dictionary<string, string> dictB = XMLTableToDict(path, 1); // Колонка B

                        Console.WriteLine($"Количество ключей в колонке D: {dictD.Count}");
                        Console.WriteLine($"Количество ключей в колонке B: {dictB.Count}");

                        _file_dict = new Dictionary<string, string>();
                        var allKeys = dictD.Keys.Union(dictB.Keys).Distinct();

                        foreach (var key in allKeys)
                        {
                            string valueD = dictD.TryGetValue(key, out var valD) ? valD?.Trim() : null;
                            string valueB = dictB.TryGetValue(key, out var valB) ? valB?.Trim() : null;

                            if (!string.IsNullOrEmpty(valueD))
                            {
                                _file_dict[key] = valueD;
                            }
                            else
                            {
                                _file_dict[key] = valueB ?? string.Empty;
                            }
                        }
                    }
                    else if (isTranslatedData)
                    {
                        // Для файлов из TranslatedData используем колонку E
                        Console.WriteLine($"Применяем логику колонки E для переведенного файла: {path}");
                        _file_dict = XMLTableToDict(path, 4); // Колонка E
                    }
                    else
                    {
                        // Для остальных файлов используем стандартную логику
                        Console.WriteLine($"Применяем стандартную логику для колонки {_default_column}: {path}");
                        _file_dict = XMLTableToDict(path, _default_column);
                    }

                    if (_file_dict != null)
                    {
                        foreach (var pair in _file_dict.Where(p => p.Key != "ID"))
                        {
                            if (!_total.ContainsKey(pair.Key))
                            {
                                _total.Add(pair.Key, pair.Value);
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

            AJLocalizInJSONFile _json_file = new AJLocalizInJSONFile();
            _json_file.Data = _total;

            return _json_file;
        }

        private AJLocalizInJSONFile WriteJSONFile(AJLocalizInJSONFile _json_file, string _path_to_json)
        {
            File.WriteAllText(_path_to_json, JsonConvert.SerializeObject(_json_file));

            return _json_file;
        }

        public AJLocalizInJSONFile ConvertXMLToJSON(string[] _paths_to_xmls, string _path_to_json, int _column)
        {
            Dictionary<string, string> _total = new Dictionary<string, string>();

            foreach (var el in _paths_to_xmls)
            {
                Dictionary<string, string> _file_dict = XMLTableToDict(el, _column);

                foreach (var _pair in _file_dict.Where(_pair => _pair.Key != "ID"))
                {
                    _total.Add(_pair.Key, _pair.Value);
                }
            }

            AJLocalizInJSONFile _json_file = new AJLocalizInJSONFile();
            _json_file.Data = _total;

            File.WriteAllText(_path_to_json, JsonConvert.SerializeObject(_json_file));

            return _json_file;
        }

        public static string GetLocalizTablesPath(string _proj_path)
        {
            string _path = _proj_path + @"\Raw\loc_All objects_en.xlsx";

            if (!File.Exists(_path)) _path = _proj_path + @"\Raw\loc_All objects_ru.xlsx";

            return _path;
        }

        public static string GetFlowJSONPath(string _proj_path) { return _proj_path + @"\Raw\Flow.json"; }

        public static string GetMetaJSONPath(string _proj_path) { return _proj_path + @"\Raw\Meta.json"; }
    }
}