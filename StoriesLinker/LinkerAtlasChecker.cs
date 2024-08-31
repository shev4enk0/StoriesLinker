using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

using Articy.Unity;
using Microsoft.Win32;

using StoriesLinker;

public class AtlasCheckerChInfo {
    public AJMetaCharacterData MetaData;

    public string SpritePrefix;

    public bool MainHeroWithTwoGenders;

    public Dictionary<string, string> RequiredSprites;

    public void AddClothesForCheck(int _id, List<string> _clothes_names) {
        //Console.WriteLine("AddClothesForCheck " + _id + ", prefix: " + SpritePrefix);

        if (RequiredSprites.ContainsKey(_id.ToString())) {
            Console.WriteLine("clothes for check exist");

            return;
        }

        string _cloth_name = (_id < _clothes_names.Count ? _clothes_names[_id] : "");

        //Console.WriteLine("RequiredSprites add " + _cloth_name);

        if (MainHeroWithTwoGenders)
        {
            if (RequiredSprites.ContainsKey("Male_" + _cloth_name))
            {
                Console.WriteLine("clothes for check exist");

                return;
            }

            RequiredSprites.Add("Male_" + _cloth_name, "Male_" + _cloth_name);
            RequiredSprites.Add("Female_" + _cloth_name, "Female_" + _cloth_name);
        }
        else {
            RequiredSprites.Add(_id.ToString(), _cloth_name);
        }
    }
}

public class LinkerAtlasChecker
{
    public AJLinkerMeta MetaData;
    public List<AtlasCheckerChInfo> CheckCharactersList;

    public LinkerAtlasChecker(AJLinkerMeta _meta, List<AJMetaCharacterData> _chs)
    {
        MetaData = _meta;
        CheckCharactersList = new List<AtlasCheckerChInfo>();

        foreach (AJMetaCharacterData _mch in _chs) {
            if (_mch.AtlasFileName == "-" || _mch.AtlasFileName.Contains("Sec_")) continue;

            AtlasCheckerChInfo _ch = new AtlasCheckerChInfo();

            _ch.SpritePrefix = _meta.SpritePrefix + _mch.BaseNameInAtlas + "_";
            _ch.MetaData = _mch;

            _ch.RequiredSprites = new Dictionary<string, string>();

            bool _main_hero = _mch.BaseNameInAtlas == "Main";

            List<string> _gender_prefix = new List<string>();

            _ch.MainHeroWithTwoGenders = _main_hero && _meta.MainHeroHasDifferentGenders;

            if (_main_hero && _meta.MainHeroHasDifferentGenders)
            {
                _gender_prefix.Add("Male_");
                _gender_prefix.Add("Female_");

                _ch.SpritePrefix = _meta.SpritePrefix + "Main";
            }
            else {
                _gender_prefix.Add("");
            }

            for (int g = 0; g < _gender_prefix.Count; g++)
            {
                string _g_prefix = _gender_prefix[g];

                _ch.RequiredSprites.Add(_g_prefix + "Base", "");
                _ch.RequiredSprites.Add(_g_prefix + "Emotions_Angry", "");
                _ch.RequiredSprites.Add(_g_prefix + "Emotions_Happy", "");
                _ch.RequiredSprites.Add(_g_prefix + "Emotions_Standart", "");
                _ch.RequiredSprites.Add(_g_prefix + "Emotions_Surprised", "");
                _ch.RequiredSprites.Add(_g_prefix + "Emotions_Sad", "");

                if (_main_hero && _meta.RacesList != null && _meta.RacesList.Count > 0)
                {
                    for (int r = 0; r < _meta.RacesList.Count; r++)
                    {
                        string _r_prefix = _meta.RacesList[r] + "_";

                        _ch.RequiredSprites.Add(_g_prefix + _r_prefix + "Base", "");
                        _ch.RequiredSprites.Add(_g_prefix + _r_prefix + "Emotions_Angry", "");
                        _ch.RequiredSprites.Add(_g_prefix + _r_prefix + "Emotions_Happy", "");
                        _ch.RequiredSprites.Add(_g_prefix + _r_prefix + "Emotions_Standart", "");
                        _ch.RequiredSprites.Add(_g_prefix + _r_prefix + "Emotions_Surprised", "");
                        _ch.RequiredSprites.Add(_g_prefix + _r_prefix + "Emotions_Sad", "");
                    }
                }
            }

            if (_main_hero && _meta.CustomHairCount > 0) {
                _ch.RequiredSprites.Add("Hair1", "");
                _ch.RequiredSprites.Add("Hair2", "");
                _ch.RequiredSprites.Add("Hair3", "");
            }

            CheckCharactersList.Add(_ch);
        }

        /*foreach (AtlasCheckerChInfo _ch in CheckCharactersList)
        {
            foreach (KeyValuePair<string, string> _sp_pair in _ch.RequiredSprites)
            {
                Console.WriteLine(_ch.SpritePrefix + ": " + _sp_pair.Key + " | " + _sp_pair.Value);
            }
        }*/
    }

    public void PassClothesInstruction(string _raw_script)
    {
        string[] _scripts = _raw_script.EscapeString().Replace("\\n", "").Replace("\\r", "").Split(';');

        //Console.WriteLine("pass instr " + _raw_script);

        for (int i = 0; i < _scripts.Length; i++)
        {
            string _script = _scripts[i];

            if (string.IsNullOrEmpty(_script) || !_script.Contains("Clothes.")) continue;

            Console.WriteLine("_script " + _script);

            AInstruction _instr = new AInstruction(_script);

            if (!_instr.BadParse) {
                string _var = _instr.Variable;

                AtlasCheckerChInfo _ch = CheckCharactersList.Find(_c => "Clothes." + _c.MetaData.ClothesVariableName == _var);

                //!!!!
                if (_ch == null) {
                    continue;
                }

                _ch.AddClothesForCheck(_instr.Value, MetaData.ClothesSpriteNames);
            }
        }
    }

    public string BeginFinalCheck(string _path) {
        foreach (AtlasCheckerChInfo _ch in CheckCharactersList)
        {
            List<string> _checked_sprites = new List<string>();

            string[] _atlasses = _ch.MetaData.AtlasFileName.Split(',');

            for (int i = 0; i < _atlasses.Length; i++)
            {
                if (string.IsNullOrEmpty(_atlasses[i])) continue;

                string _atlas_path = string.Format(_path + @"\Art\Characters\{0}.tpsheet", _atlasses[i]);

                StreamReader _reader = File.OpenText(_atlas_path);

                string _text = _reader.ReadToEnd();

                foreach (KeyValuePair<string, string> _sp_pair in _ch.RequiredSprites)
                {
                    if (_checked_sprites.Contains(_sp_pair.Key)) continue;

                    string _sprite_name1 = _ch.SpritePrefix + _sp_pair.Key;

                    Console.WriteLine(_sprite_name1 + " in " + _atlas_path);

                    if (!_text.Contains(_sprite_name1))
                    {
                        string _sprite_name2 = _ch.SpritePrefix+ _sp_pair.Value;

                        Console.WriteLine(_sprite_name2 + " in " + _atlas_path);

                        if (_text.Contains(_sprite_name2) && !string.IsNullOrEmpty(_sp_pair.Value))
                        {
                            _checked_sprites.Add(_sp_pair.Key);

                            //Console.WriteLine("ok");

                            //всё ок
                        }
                        else {
                            //Console.WriteLine("error");

                            if (_ch.MetaData.BaseNameInAtlas == "Main" && MetaData.CustomClothesCount > 0) // если история с выбором одежды в начале игры, делаем исключения для главного героя
                            {

                            }
                            else if(i + 1 >= _atlasses.Length) {
                                return string.Format("Спрайт {0}/{1} не найден", _sprite_name1, _sprite_name2);
                            }
                        }
                    }
                    else {
                        _checked_sprites.Add(_sp_pair.Key);

                        Console.WriteLine("ok");
                    }
                }
            }
        }

        return "";
    }
}


public enum DInstuctionAction
{
    Minus,
    Plus,
    Divide,
    Equal
}

public enum DInstuctionVarType
{
    Integer,
    Boolean
}

public class AInstruction
{
    public int Value;
    public string Variable;

    public DInstuctionVarType VarType;

    public DInstuctionAction ActionType;

    public bool BadParse;

    public AInstruction(string _raw_script)
    {
        _raw_script = _raw_script.Trim(' ');
        _raw_script = _raw_script.TrimEnd(';');
        _raw_script = _raw_script.Replace(";", "");

        string[] _signs = new string[] { "-=", "+=", "/=", "=" };

        int _sign_index = -1;

        for (int i = 0; i < _signs.Length; i++)
        {
            if (_raw_script.IndexOf(_signs[i]) != -1)
            {
                _sign_index = i;

                break;
            }
        }

        //Debug.Log("_sign_index " + _sign_index);

        if (_sign_index != -1)
        {
            string[] _parts = _raw_script.Replace(_signs[_sign_index], "|").Split('|');

            ActionType = (DInstuctionAction)_sign_index;

            Variable = _parts[0].Trim(' ');
            string _value_str = _parts[1].Trim(' ');

            int _result;


            //Debug.Log(Variable + ", _value_str " + _value_str + " - " + int.TryParse(_value_str, out _result));


                if (int.TryParse(_value_str, out _result))
                {
                    if (int.TryParse(_value_str, out _result))
                    {
                        Value = int.Parse(_value_str);

                        VarType = DInstuctionVarType.Integer;
                    }
                    else {
                        Console.WriteLine("bad value " + _value_str);

                        BadParse = true;
                    }
                }
                else if (_value_str == "true" || _value_str == "false")
                {
                    VarType = DInstuctionVarType.Boolean;

                    Value = (_value_str == "true" ? 1 : 0);
                }
                else {
                        BadParse = true;
                }

        }
        else {
            Console.WriteLine("bad sign index" + _raw_script);

            BadParse = true;
        }

        if (VarType == DInstuctionVarType.Boolean && ActionType != DInstuctionAction.Equal)
            BadParse = true;

        if (BadParse)
        {
            Console.WriteLine("INSTRUCTION PARSE ERROR!" + _raw_script);
        }
    }
}

