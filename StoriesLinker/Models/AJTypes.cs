﻿using System;
using System.Collections.Generic;

namespace StoriesLinker
{
    [Serializable]
    public class AjNamespace
    {
        public string Namespace;
        public string Description;
        public List<AjVariable> Variables;
    }

    [Serializable]
    public class AjVariable
    {
        public string Variable;
        public string Type;
        public string Value;
        public string Description;
    }

    [Serializable]
    public class AjPackage
    {
        public string Name;
        public string Description;
        public bool IsDefaultPackage;
        public List<AjObj> Models;
    }

    [Serializable]
    public class AjFile
    {
        public List<AjNamespace> GlobalVariables;
        public List<AjPackage> Packages;
    }

    public enum AjType
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
    public class AjObj
    {
        public string Type;
        public AjType EType;
        public AjObjProps Properties;

        public override string ToString()
        {
            return Properties.Id + " " + Properties.DisplayName + " " + Type;
        }
    }

    [Serializable]
    public class AjConnection
    {
        public string Label;
        public string TargetPin;
        public string Target;
    }

    [Serializable]
    public class AjPin
    {
        public string Text;
        public string Id;
        public string Owner;

        public List<AjConnection> Connections;
    }

    [Serializable]
    public class AjObjProps //FlowFragment, Dialogue, Entity, Location
    {
        public string TechnicalName;
        public string Id;
        public string DisplayName;
        public string Parent;
        public List<string> Attachments;

        public AjColor Color;

        public string Text;
        public string ExternalId;
        public string ShortId;

        public List<AjPin> InputPins;
        public List<AjPin> OutputPins;

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
    public class AjColor
    {
        public float R;
        public float G;
        public float B;
        public float A;

        public Color32 ToColor32()
        {
            return new Color32(R * 255f, G * 255f, B * 255f, A * 255f);
        }
    }

    public class Color32
    {
        public float R;
        public float G;
        public float B;
        public float A;

        public Color32(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }

    [Serializable]
    public class AjLinkerOutputBase
    {
        public List<AjNamespace> GlobalVariables;
        public List<AjObj> SharedObjs;
    }

    [Serializable]
    public class AjLinkerOutputChapterFlow
    {
        public List<AjObj> Objects;
    }

    [Serializable]
    public class AjMetaCharacterData
    {
        public string Aid;

        public string DisplayName;
        public string ClothesVariableName;
        public string AtlasFileName;
        public string BaseNameInAtlas;
    }

    [Serializable]
    public class AjMetaLocationData
    {
        public string Aid;

        public int Id;
        public string DisplayName;
        public string SpriteName;
        public string SoundIdleName;
    }

    [Serializable]
    public class AjLinkerMeta
    {
        public string UniqueId;
        public string SpritePrefix;

        public BookVersionInfo Version;


        public List<string> ClothesSpriteNames;
        public int UndefinedClothesFuncVariant;
        public bool ExceptionsWeaponLayer;

        public bool StandartizedUi;

        public int UiTextBlockFontSize;
        public int UiChoiceBlockFontSize;

        public string KarmaCurrency;

        public int KarmaBadBorder;
        public int KarmaGoodBorder;
        public int KarmaTopLimit;

        public List<int> UiTextPlateLimits;
        public bool UiPaintFirstLetterInRedException;
        public int UiTextPlateOffset;

        public bool UiOverridedTextColor;

        public List<int> UiTextColor;
        public List<int> UiBlockedTextColor;
        public List<int> UiChNameTextColor;

        public List<int> UiOutlineColor;
        public List<int> UiResTextColor;
        
        public bool WardrobeEnabled;
        public bool MainHeroHasDifferentGenders;
        public bool MainHeroHasSplittedHairSprite;

        public int IntroLocation;

        public int CustomClothesCount;
        public int CustomHairCount;

        public List<string> CurrenciesInOrderOfUi;
        public List<string> RacesList;
        public List<string> ChaptersEntryPoints;

        public List<AjMetaCharacterData> Characters;
        public List<AjMetaLocationData> Locations;
    }

    [Serializable]
    public class BookVersionInfo
    {
        public string BinVersion;
        public string PreviewVersion;
        public string BaseResourcesVersion;
    }

    public class AjChapterAsset
    {
        public int ChapterN;

        public List<string> CharacterIDs;
        public List<string> CharacterNames;
        public List<string> LocationIDs;
        public List<string> LocationNames;

        public AjChapterAsset(int chapterN)
        {
            ChapterN = chapterN;

            CharacterIDs = new List<string>();
            LocationIDs = new List<string>();
            CharacterNames = new List<string>();
            LocationNames = new List<string>();
        }

        public void AddCh(string name, string aid)
        {
            CharacterNames.Add(name);
            CharacterIDs.Add(aid);
        }

        public void AddLoc(string name, string aid)
        {
            LocationNames.Add(name);
            LocationIDs.Add(aid);
        }
    }

    public class AjAssetGridLinker
    {
        private List<string> _addedChs;
        private List<string> _addedLocs;

        public List<AjChapterAsset> AssetsByChapters;

        private int _currentChapter;

        public AjAssetGridLinker()
        {
            AssetsByChapters = new List<AjChapterAsset>();

            _addedChs = new List<string>();
            _addedLocs = new List<string>();
        }

        public bool IsLocExist(string name)
        {
            return _addedLocs.Contains(name);
        }

        public bool IsChExist(string name)
        {
            return _addedChs.Contains(name);
        }

        public void AddChapter()
        {
            _currentChapter = AssetsByChapters.Count + 1;

            AssetsByChapters.Add(new AjChapterAsset(_currentChapter));
        }

        public void AddCharacter(string name, string aid)
        {
            _addedChs.Add(name);
            AssetsByChapters[_currentChapter - 1].AddCh(name, aid);
        }

        public void AddLocation(string name, string aid)
        {
            _addedLocs.Add(name);
            AssetsByChapters[_currentChapter - 1].AddLoc(name, aid);
        }

        public string[] GetCharactersNamesFromCurChapter()
        {
            return AssetsByChapters[_currentChapter - 1].CharacterNames.ToArray();
        }

        public string[] GetLocationsNamesFromCurChapter()
        {
            return AssetsByChapters[_currentChapter - 1].LocationNames.ToArray();
        }

        public List<string> GetCharactersIDsFromCurChapter()
        {
            return AssetsByChapters[_currentChapter - 1].CharacterIDs;
        }

        public List<string> GetLocationsIDsFromCurChapter()
        {
            return AssetsByChapters[_currentChapter - 1].LocationIDs;
        }
    }

    [Serializable]
    public class AjGridAssetChapterJson
    {
        public List<string> CharactersIDs;
        public List<string> LocationsIDs;

        public AjGridAssetChapterJson()
        {
            CharactersIDs = new List<string>();
            LocationsIDs = new List<string>();
        }
    }

    [Serializable]
    public class AjGridAssetJson
    {
        public List<AjGridAssetChapterJson> Chapters;

        public AjGridAssetJson()
        {
            Chapters = new List<AjGridAssetChapterJson>();
        }
    }

    [Serializable]
    public class AjLocalizInJsonFile
    {
        public Dictionary<string, string> Data;
    }
}