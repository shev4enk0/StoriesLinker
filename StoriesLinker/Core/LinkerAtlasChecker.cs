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
using Microsoft.Win32;

using StoriesLinker;

public class AtlasCheckerChInfo {
    public AjMetaCharacterData MetaData;

    public string SpritePrefix;

    public bool MainHeroWithTwoGenders;

    public Dictionary<string, string> RequiredSprites;

    public void AddClothesForCheck(int id, List<string> clothesNames) {
        //Console.WriteLine("AddClothesForCheck " + _id + ", prefix: " + SpritePrefix);

        if (RequiredSprites.ContainsKey(id.ToString())) {
            Console.WriteLine("clothes for check exist");

            return;
        }

        string clothName = (id < clothesNames.Count ? clothesNames[id] : "");

        //Console.WriteLine("RequiredSprites add " + _cloth_name);

        if (MainHeroWithTwoGenders)
        {
            if (RequiredSprites.ContainsKey("Male_" + clothName))
            {
                Console.WriteLine("clothes for check exist");

                return;
            }

            RequiredSprites.Add("Male_" + clothName, "Male_" + clothName);
            RequiredSprites.Add("Female_" + clothName, "Female_" + clothName);
        }
        else {
            RequiredSprites.Add(id.ToString(), clothName);
        }
    }
}

public class LinkerAtlasChecker
{
    public AjLinkerMeta MetaData;
    public List<AtlasCheckerChInfo> CheckCharactersList;

    public LinkerAtlasChecker(AjLinkerMeta meta, List<AjMetaCharacterData> chs)
    {
        MetaData = meta;
        CheckCharactersList = new List<AtlasCheckerChInfo>();

        foreach (AjMetaCharacterData mch in chs) {
            if (mch.AtlasFileName == "-" || mch.AtlasFileName.Contains("Sec_")) continue;

            AtlasCheckerChInfo ch = new AtlasCheckerChInfo();

            ch.SpritePrefix = meta.SpritePrefix + mch.BaseNameInAtlas + "_";
            ch.MetaData = mch;

            ch.RequiredSprites = new Dictionary<string, string>();

            bool mainHero = mch.BaseNameInAtlas == "Main";

            List<string> genderPrefix = new List<string>();

            ch.MainHeroWithTwoGenders = mainHero && meta.MainHeroHasDifferentGenders;

            if (mainHero && meta.MainHeroHasDifferentGenders)
            {
                genderPrefix.Add("Male_");
                genderPrefix.Add("Female_");

                ch.SpritePrefix = meta.SpritePrefix + "Main";
            }
            else {
                genderPrefix.Add("");
            }

            for (int g = 0; g < genderPrefix.Count; g++)
            {
                string gPrefix = genderPrefix[g];

                ch.RequiredSprites.Add(gPrefix + "Base", "");
                ch.RequiredSprites.Add(gPrefix + "Emotions_Angry", "");
                ch.RequiredSprites.Add(gPrefix + "Emotions_Happy", "");
                ch.RequiredSprites.Add(gPrefix + "Emotions_Standart", "");
                ch.RequiredSprites.Add(gPrefix + "Emotions_Surprised", "");
                ch.RequiredSprites.Add(gPrefix + "Emotions_Sad", "");

                if (mainHero && meta.RacesList != null && meta.RacesList.Count > 0)
                {
                    for (int r = 0; r < meta.RacesList.Count; r++)
                    {
                        string rPrefix = meta.RacesList[r] + "_";

                        ch.RequiredSprites.Add(gPrefix + rPrefix + "Base", "");
                        ch.RequiredSprites.Add(gPrefix + rPrefix + "Emotions_Angry", "");
                        ch.RequiredSprites.Add(gPrefix + rPrefix + "Emotions_Happy", "");
                        ch.RequiredSprites.Add(gPrefix + rPrefix + "Emotions_Standart", "");
                        ch.RequiredSprites.Add(gPrefix + rPrefix + "Emotions_Surprised", "");
                        ch.RequiredSprites.Add(gPrefix + rPrefix + "Emotions_Sad", "");
                    }
                }
            }

            if (mainHero && meta.CustomHairCount > 0) {
                ch.RequiredSprites.Add("Hair1", "");
                ch.RequiredSprites.Add("Hair2", "");
                ch.RequiredSprites.Add("Hair3", "");
            }

            CheckCharactersList.Add(ch);
        }

        /*foreach (AtlasCheckerChInfo _ch in CheckCharactersList)
        {
            foreach (KeyValuePair<string, string> _sp_pair in _ch.RequiredSprites)
            {
                Console.WriteLine(_ch.SpritePrefix + ": " + _sp_pair.Key + " | " + _sp_pair.Value);
            }
        }*/
    }

    public void PassClothesInstruction(string rawScript)
    {
        string[] scripts = rawScript.EscapeString().Replace("\\n", "").Replace("\\r", "").Split(';');

        //Console.WriteLine("pass instr " + _raw_script);

        for (int i = 0; i < scripts.Length; i++)
        {
            string script = scripts[i];

            if (string.IsNullOrEmpty(script) || !script.Contains("Clothes.")) continue;

            Console.WriteLine("_script " + script);

            AInstruction instr = new AInstruction(script);

            if (!instr.BadParse) {
                string var = instr.Variable;

                AtlasCheckerChInfo ch = CheckCharactersList.Find(c => "Clothes." + c.MetaData.ClothesVariableName == var);

                //!!!!
                if (ch == null) {
                    continue;
                }

                ch.AddClothesForCheck(instr.Value, MetaData.ClothesSpriteNames);
            }
        }
    }

    public string BeginFinalCheck(string path) {
        foreach (AtlasCheckerChInfo ch in CheckCharactersList)
        {
            List<string> checkedSprites = new List<string>();

            string[] atlasses = ch.MetaData.AtlasFileName.Split(',');

            for (int i = 0; i < atlasses.Length; i++)
            {
                if (string.IsNullOrEmpty(atlasses[i])) continue;

                string atlasPath = string.Format(path + @"\Art\Characters\{0}.tpsheet", atlasses[i]);

                StreamReader reader = File.OpenText(atlasPath);

                string text = reader.ReadToEnd();

                foreach (KeyValuePair<string, string> spPair in ch.RequiredSprites)
                {
                    if (checkedSprites.Contains(spPair.Key)) continue;

                    string spriteName1 = ch.SpritePrefix + spPair.Key;

                    Console.WriteLine(spriteName1 + " in " + atlasPath);

                    if (!text.Contains(spriteName1))
                    {
                        string spriteName2 = ch.SpritePrefix+ spPair.Value;

                        Console.WriteLine(spriteName2 + " in " + atlasPath);

                        if (text.Contains(spriteName2) && !string.IsNullOrEmpty(spPair.Value))
                        {
                            checkedSprites.Add(spPair.Key);

                            //Console.WriteLine("ok");

                            //всё ок
                        }
                        else {
                            //Console.WriteLine("error");

                            if (ch.MetaData.BaseNameInAtlas == "Main" && MetaData.CustomClothesCount > 0) // если история с выбором одежды в начале игры, делаем исключения для главного героя
                            {

                            }
                            else if(i + 1 >= atlasses.Length) {
                                return string.Format("Спрайт {0}/{1} не найден", spriteName1, spriteName2);
                            }
                        }
                    }
                    else {
                        checkedSprites.Add(spPair.Key);

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

    public AInstruction(string rawScript)
    {
        rawScript = rawScript.Trim(' ');
        rawScript = rawScript.TrimEnd(';');
        rawScript = rawScript.Replace(";", "");

        string[] signs = new string[] { "-=", "+=", "/=", "=" };

        int signIndex = -1;

        for (int i = 0; i < signs.Length; i++)
        {
            if (rawScript.IndexOf(signs[i]) != -1)
            {
                signIndex = i;

                break;
            }
        }

        //Debug.Log("_sign_index " + _sign_index);

        if (signIndex != -1)
        {
            string[] parts = rawScript.Replace(signs[signIndex], "|").Split('|');

            ActionType = (DInstuctionAction)signIndex;

            Variable = parts[0].Trim(' ');
            string valueStr = parts[1].Trim(' ');

            int result;


            //Debug.Log(Variable + ", _value_str " + _value_str + " - " + int.TryParse(_value_str, out _result));


                if (int.TryParse(valueStr, out result))
                {
                    if (int.TryParse(valueStr, out result))
                    {
                        Value = int.Parse(valueStr);

                        VarType = DInstuctionVarType.Integer;
                    }
                    else {
                        Console.WriteLine("bad value " + valueStr);

                        BadParse = true;
                    }
                }
                else if (valueStr == "true" || valueStr == "false")
                {
                    VarType = DInstuctionVarType.Boolean;

                    Value = (valueStr == "true" ? 1 : 0);
                }
                else {
                        BadParse = true;
                }

        }
        else {
            Console.WriteLine("bad sign index" + rawScript);

            BadParse = true;
        }

        if (VarType == DInstuctionVarType.Boolean && ActionType != DInstuctionAction.Equal)
            BadParse = true;

        if (BadParse)
        {
            Console.WriteLine("INSTRUCTION PARSE ERROR!" + rawScript);
        }
    }
}

