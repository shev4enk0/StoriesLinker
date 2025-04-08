using System;
using System.Collections.Generic;
using System.IO;
using StoriesLinker;

# region Модели данных
public class CharacterAtlasInfo
{
    public AjMetaCharacterData MetaData { get; set; }
    public string SpritePrefix { get; set; }
    public bool HasGenderVariants { get; set; }
    public Dictionary<string, string> RequiredSprites { get; set; }

    public void AddClothesVariant(int id, List<string> clothesNames)
    {
        if (RequiredSprites.ContainsKey(id.ToString()))
        {
            Console.WriteLine("Вариант одежды для проверки уже существует");
            return;
        }

        string clothName = id < clothesNames.Count ? clothesNames[id] : "";

        if (HasGenderVariants)
        {
            if (RequiredSprites.ContainsKey($"Male_{clothName}"))
            {
                Console.WriteLine("Вариант одежды для проверки уже существует");
                return;
            }

            RequiredSprites.Add($"Male_{clothName}", $"Male_{clothName}");
            RequiredSprites.Add($"Female_{clothName}", $"Female_{clothName}");
        }
        else
        {
            RequiredSprites.Add(id.ToString(), clothName);
        }
    }
}

public enum InstructionAction
{
    Minus,
    Plus,
    Divide,
    Equal
}

public enum InstructionVarType
{
    Integer,
    Boolean
}

public class ScriptInstruction
{
    public int Value { get; set; }
    public string Variable { get; set; }
    public InstructionVarType VarType { get; set; }
    public InstructionAction ActionType { get; set; }
    public bool HasParseError { get; set; }

    public ScriptInstruction(string rawScript)
    {
        ParseInstruction(rawScript.Trim());
    }

    private void ParseInstruction(string script)
    {
        script = script.TrimEnd(';');

        string[] signs = ["-=", "+=", "/=", "="];
        int signIndex = -1;

        for (var i = 0; i < signs.Length; i++)
        {
            if (script.IndexOf(signs[i], StringComparison.Ordinal) != -1)
            {
                signIndex = i;
                break;
            }
        }

        if (signIndex == -1)
        {
            LogParseError("Не найден оператор присваивания", script);
            return;
        }

        string[] parts = script.Replace(signs[signIndex], "|").Split('|');
        ActionType = (InstructionAction)signIndex;
        Variable = parts[0].Trim();
        string valueStr = parts[1].Trim();

        if (int.TryParse(valueStr, out int result))
        {
            Value = result;
            VarType = InstructionVarType.Integer;
        }
        else if (valueStr is "true" or "false")
        {
            VarType = InstructionVarType.Boolean;
            Value = valueStr == "true" ? 1 : 0;
        }
        else
        {
            LogParseError("Некорректное значение", script);
            return;
        }

        if (VarType == InstructionVarType.Boolean && ActionType != InstructionAction.Equal)
        {
            LogParseError("Некорректная операция для boolean", script);
        }
    }

    private void LogParseError(string error, string script)
    {
        HasParseError = true;
        Console.WriteLine($"ОШИБКА РАЗБОРА ИНСТРУКЦИИ: {error}. Скрипт: {script}");
    }
}
#endregion

#region Проверка атласов
public class LinkerAtlasChecker
{
    private readonly AjLinkerMeta _metaData;
    private readonly List<CharacterAtlasInfo> _charactersToCheck;

    public LinkerAtlasChecker(AjLinkerMeta meta, List<AjMetaCharacterData> characters)
    {
        _metaData = meta;
        _charactersToCheck = InitializeCharactersList(characters);
    }

    private List<CharacterAtlasInfo> InitializeCharactersList(List<AjMetaCharacterData> characters)
    {
        var result = new List<CharacterAtlasInfo>();

        foreach (AjMetaCharacterData character in characters)
        {
            if (character.AtlasFileName == "-" || character.AtlasFileName.Contains("Sec_"))
                continue;

            var characterInfo = new CharacterAtlasInfo
            {
                MetaData = character,
                SpritePrefix = _metaData.SpritePrefix + character.BaseNameInAtlas + "_",
                RequiredSprites = new Dictionary<string, string>()
            };

            bool isMainHero = character.BaseNameInAtlas == "Main";
            characterInfo.HasGenderVariants = isMainHero && _metaData.MainHeroHasDifferentGenders;

            InitializeRequiredSprites(characterInfo, isMainHero);
            result.Add(characterInfo);
        }

        return result;
    }

    private void InitializeRequiredSprites(CharacterAtlasInfo characterInfo, bool isMainHero)
    {
        var genderPrefixes = new List<string>();

        if (characterInfo.HasGenderVariants)
        {
            genderPrefixes.Add("Male_");
            genderPrefixes.Add("Female_");
            characterInfo.SpritePrefix = _metaData.SpritePrefix + "Main";
        }
        else
        {
            genderPrefixes.Add("");
        }

        foreach (string genderPrefix in genderPrefixes)
        {
            AddBaseEmotionSprites(characterInfo, genderPrefix);

            if (isMainHero && _metaData.RacesList?.Count > 0)
            {
                AddRaceSpecificSprites(characterInfo, genderPrefix);
            }
        }

        if (isMainHero && _metaData.CustomHairCount > 0)
        {
            AddCustomHairSprites(characterInfo);
        }
    }

    private static void AddBaseEmotionSprites(CharacterAtlasInfo characterInfo, string genderPrefix)
    {
        var emotions = new[] { "Base", "Emotions_Angry", "Emotions_Happy", "Emotions_Standart", 
                             "Emotions_Surprised", "Emotions_Sad" };
        
        foreach (string emotion in emotions)
        {
            characterInfo.RequiredSprites.Add(genderPrefix + emotion, "");
        }
    }

    private void AddRaceSpecificSprites(CharacterAtlasInfo characterInfo, string genderPrefix)
    {
        foreach (string race in _metaData.RacesList)
        {
            string racePrefix = race + "_";
            AddBaseEmotionSprites(characterInfo, genderPrefix + racePrefix);
        }
    }

    private static void AddCustomHairSprites(CharacterAtlasInfo characterInfo)
    {
        for (int i = 1; i <= 3; i++)
        {
            characterInfo.RequiredSprites.Add($"Hair{i}", "");
        }
    }

    public void ProcessClothesInstruction(string rawScript)
    {
        string[] scripts = rawScript.EscapeString()
                                  .Replace("\\n", "")
                                  .Replace("\\r", "")
                                  .Split(';');

        foreach (string script in scripts)
        {
            if (string.IsNullOrEmpty(script) || !script.Contains("Clothes."))
                continue;

            Console.WriteLine($"Обработка скрипта: {script}");

            var instruction = new ScriptInstruction(script);
            if (instruction.HasParseError)
                continue;

            string variableName = instruction.Variable;
            CharacterAtlasInfo character = _charactersToCheck.Find(c => 
                "Clothes." + c.MetaData.ClothesVariableName == variableName);

            character?.AddClothesVariant(instruction.Value, _metaData.ClothesSpriteNames);
        }
    }

    public string ValidateAtlases(string projectPath)
    {
        foreach (CharacterAtlasInfo character in _charactersToCheck)
        {
            var verifiedSprites = new List<string>();
            string[] atlases = character.MetaData.AtlasFileName.Split(',');

            for (var i = 0; i < atlases.Length; i++)
            {
                if (string.IsNullOrEmpty(atlases[i]))
                    continue;

                string atlasPath = $"{projectPath}\\Art\\Characters\\{atlases[i]}.tpsheet";
                string atlasContent = File.ReadAllText(atlasPath);

                foreach (KeyValuePair<string, string> sprite in character.RequiredSprites)
                {
                    if (verifiedSprites.Contains(sprite.Key))
                        continue;

                    string primarySpriteName = character.SpritePrefix + sprite.Key;
                    Console.WriteLine($"Проверка спрайта {primarySpriteName} в {atlasPath}");

                    if (atlasContent.Contains(primarySpriteName))
                    {
                        verifiedSprites.Add(sprite.Key);
                        continue;
                    }

                    string alternativeSpriteName = character.SpritePrefix + sprite.Value;
                    Console.WriteLine($"Проверка альтернативного спрайта {alternativeSpriteName} в {atlasPath}");

                    if (atlasContent.Contains(alternativeSpriteName) && !string.IsNullOrEmpty(sprite.Value))
                    {
                        verifiedSprites.Add(sprite.Key);
                        continue;
                    }

                    // Пропускаем проверку для главного героя с кастомной одеждой
                    if (character.MetaData.BaseNameInAtlas == "Main" && _metaData.CustomClothesCount > 0)
                        continue;

                    if (i + 1 >= atlases.Length)
                        return $"Спрайт {primarySpriteName}/{alternativeSpriteName} не найден";
                }
            }
        }

        return string.Empty;
    }
}
#endregion