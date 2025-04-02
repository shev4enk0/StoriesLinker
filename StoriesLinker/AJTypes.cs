using System;
using System.Collections.Generic;

namespace StoriesLinker
{
    [Serializable]
    public class AJNamespace
    {
        public string Namespace;
        public string Description;
        public List<AJVariable> Variables;
    }

    [Serializable]
    public class AJVariable
    {
        public string Variable;
        public string Type;
        public string Value;
        public string Description;
    }

    [Serializable]
    public class AJPackage
    {
        public string Name;
        public string Description;
        public bool IsDefaultPackage;
        public List<AJObj> Models;
    }

    [Serializable]
    public class AJFile
    {
        public List<AJNamespace> GlobalVariables;
        public List<AJPackage> Packages;
    }

    public enum AJType
    {
        FlowFragment,
        Dialogue,
        Entity,
        Location,
        DialogueFragment,
        Instruction,
        Condition,
        Jump,
        Other
    }

    [Serializable]
    public class AJObj
    {
        public string Type;
        public AJType EType;
        public AJObjProps Properties;

        public override string ToString()
        {
            return Properties.Id + " " + Properties.DisplayName + " " + Type;
        }
    }

    [Serializable]
    public class AJConnection
    {
        public string Label;
        public string TargetPin;
        public string Target;
    }

    [Serializable]
    public class AJPin
    {
        public string Text;
        public string Id;
        public string Owner;

        public List<AJConnection> Connections;
    }

    [Serializable]
    public class AJObjProps //FlowFragment, Dialogue, Entity, Location
    {
        public string TechnicalName;
        public string Id;
        public string DisplayName;
        public string Parent;
        public List<string> Attachments;

        public AJColor Color;

        public string Text;
        public string ExternalId;
        public string ShortId;

        public List<AJPin> InputPins;
        public List<AJPin> OutputPins;

        //DialogueFragment
        public string MenuText;
        public string StageDirections;
        public string Speaker;

        //Instruction, Condition
        public string Expression;

        //Jump
        public string Target;
        public string TargetPin;
    }

    [Serializable]
    public class AJColor
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color32 ToColor32()
        {
            return new Color32(r * 255f, g * 255f, b * 255f, a * 255f);
        }
    }

    public class Color32
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color32(float _r, float _g, float _b, float _a)
        {
            r = _r;
            g = _g;
            b = _b;
            a = _a;
        }
    }

    [Serializable]
    public class AJLinkerOutputBase
    {
        public List<AJNamespace> GlobalVariables;
        public List<AJObj> SharedObjs;
    }

    [Serializable]
    public class AJLinkerOutputChapterFlow
    {
        public List<AJObj> Objects;
    }

    [Serializable]
    public class AJMetaCharacterData
    {
        public string AID;

        public string DisplayName;
        public string ClothesVariableName;
        public string AtlasFileName;
        public string BaseNameInAtlas;
    }

    [Serializable]
    public class AJMetaLocationData
    {
        public string AID;

        public int ID;
        public string DisplayName;
        public string SpriteName;
        public string SoundIdleName;
    }

    [Serializable]
    public class AJLinkerMeta
    {
        public string UniqueID;
        public string SpritePrefix;

        public BookVersionInfo Version;

        public List<string> ClothesSpriteNames;
        public int UndefinedClothesFuncVariant;
        public bool ExceptionsWeaponLayer;

        public bool StandartizedUI;

        public int UITextBlockFontSize;
        public int UIChoiceBlockFontSize;

        public string KarmaCurrency;

        public int KarmaBadBorder;
        public int KarmaGoodBorder;
        public int KarmaTopLimit;

        public List<int> UITextPlateLimits;
        public bool UIPaintFirstLetterInRedException;
        public int UITextPlateOffset;

        public bool UIOverridedTextColor;

        public List<int> UITextColor;
        public List<int> UIBlockedTextColor;
        public List<int> UIChNameTextColor;

        public List<int> UIOutlineColor;
        public List<int> UIResTextColor;
        
        public bool WardrobeEnabled;
        public bool MainHeroHasDifferentGenders;
        public bool MainHeroHasSplittedHairSprite;

        public int IntroLocation;

        public int CustomClothesCount;
        public int CustomHairCount;

        public List<string> CurrenciesInOrderOfUI;
        public List<string> RacesList;
        public List<string> ChaptersEntryPoints;

        public List<AJMetaCharacterData> Characters;
        public List<AJMetaLocationData> Locations;
    }

    [Serializable]
    public class BookVersionInfo
    {
        public string BinVersion;
        public string PreviewVersion;
        public string BaseResourcesVersion;
    }

    public class AJChapterAsset
    {
        public int ChapterN;

        public List<string> CharacterIDs;
        public List<string> CharacterNames;
        public List<string> LocationIDs;
        public List<string> LocationNames;

        public AJChapterAsset(int _chapter_n)
        {
            ChapterN = _chapter_n;

            CharacterIDs = new List<string>();
            LocationIDs = new List<string>();
            CharacterNames = new List<string>();
            LocationNames = new List<string>();
        }

        public void AddCh(string _name, string _aid)
        {
            CharacterNames.Add(_name);
            CharacterIDs.Add(_aid);
        }

        public void AddLoc(string _name, string _aid)
        {
            LocationNames.Add(_name);
            LocationIDs.Add(_aid);
        }
    }

    public class AJAssetGridLinker
    {
        private List<string> AddedChs;
        private List<string> AddedLocs;

        public List<AJChapterAsset> AssetsByChapters;

        private int CurrentChapter;

        public AJAssetGridLinker()
        {
            AssetsByChapters = new List<AJChapterAsset>();

            AddedChs = new List<string>();
            AddedLocs = new List<string>();
        }

        public bool isLocExist(string _name)
        {
            return AddedLocs.Contains(_name);
        }

        public bool isChExist(string _name)
        {
            return AddedChs.Contains(_name);
        }

        public void AddChapter()
        {
            CurrentChapter = AssetsByChapters.Count + 1;

            AssetsByChapters.Add(new AJChapterAsset(CurrentChapter));
        }

        public void AddCharacter(string _name, string _aid)
        {
            AddedChs.Add(_name);
            AssetsByChapters[CurrentChapter - 1].AddCh(_name, _aid);
        }

        public void AddLocation(string _name, string _aid)
        {
            AddedLocs.Add(_name);
            AssetsByChapters[CurrentChapter - 1].AddLoc(_name, _aid);
        }

        public string[] GetCharactersNamesFromCurChapter()
        {
            return AssetsByChapters[CurrentChapter - 1].CharacterNames.ToArray();
        }

        public string[] GetLocationsNamesFromCurChapter()
        {
            return AssetsByChapters[CurrentChapter - 1].LocationNames.ToArray();
        }

        public List<string> GetCharactersIDsFromCurChapter()
        {
            return AssetsByChapters[CurrentChapter - 1].CharacterIDs;
        }

        public List<string> GetLocationsIDsFromCurChapter()
        {
            return AssetsByChapters[CurrentChapter - 1].LocationIDs;
        }
    }

    [Serializable]
    public class AJGridAssetChapterJSON
    {
        public List<string> CharactersIDs;
        public List<string> LocationsIDs;

        public AJGridAssetChapterJSON()
        {
            CharactersIDs = new List<string>();
            LocationsIDs = new List<string>();
        }
    }

    [Serializable]
    public class AJGridAssetJSON
    {
        public List<AJGridAssetChapterJSON> Chapters;

        public AJGridAssetJSON()
        {
            Chapters = new List<AJGridAssetChapterJSON>();
        }
    }

    [Serializable]
    public class AJLocalizInJSONFile
    {
        public Dictionary<string, string> Data;
    }
}