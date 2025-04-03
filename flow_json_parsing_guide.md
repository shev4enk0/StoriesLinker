# Инструкция по парсингу flow.json в проекте StoriesLinker

## 1. Общая структура flow.json

Файл flow.json представляет собой структурированный JSON-файл, который содержит всю информацию о диалогах, персонажах, локациях и других игровых элементах. Он состоит из двух основных частей:

```json
{
    "GlobalVariables": [...],  // Глобальные переменные
    "Packages": [...]         // Пакеты с игровыми объектами
}
```

## 2. Основные классы для работы с flow.json

### 2.1 AJFile
Основной класс для хранения данных из flow.json:
```csharp
public class AJFile
{
    public List<AJNamespace> GlobalVariables;  // Глобальные переменные
    public List<AJPackage> Packages;           // Пакеты с объектами
}
```

**Методы для работы с AJFile:**
- `GetParsedFlowJSONFile()` в классе `LinkerBin` - читает и десериализует flow.json в объект AJFile
- `GetAricyBookEntities(AJFile _ajfile, Dictionary<string, string> _nativeDict)` в классе `LinkerBin` - обрабатывает объекты из flow.json и создает словарь с игровыми объектами

**Структура в flow.json:**
```json
{
    "GlobalVariables": [...],  // Корневой узел для глобальных переменных
    "Packages": [...]         // Корневой узел для пакетов с объектами
}
```

### 2.2 AJNamespace
Содержит информацию о пространстве имен и его переменных:
```csharp
public class AJNamespace
{
    public string Namespace;           // Имя пространства имен
    public string Description;         // Описание пространства имен
    public List<AJVariable> Variables; // Список переменных в пространстве имен
}
```

**Методы для работы с AJNamespace:**
- Данные берутся напрямую из flow.json при десериализации
- Используются в методе `GetParsedFlowJSONFile()` для заполнения поля GlobalVariables в AJFile

**Структура в flow.json:**
```json
{
    "GlobalVariables": [
        {
            "Namespace": "имя_пространства_имен",
            "Description": "описание_пространства_имен",
            "Variables": [...]
        },
        ...
    ]
}
```

### 2.3 AJVariable
Представляет переменную в пространстве имен:
```csharp
public class AJVariable
{
    public string Variable;    // Имя переменной
    public string Type;        // Тип переменной
    public string Value;       // Значение переменной
    public string Description; // Описание переменной
}
```

**Методы для работы с AJVariable:**
- Данные берутся напрямую из flow.json при десериализации
- Используются в методе `GetParsedFlowJSONFile()` для заполнения списка Variables в AJNamespace

**Структура в flow.json:**
```json
{
    "GlobalVariables": [
        {
            "Namespace": "имя_пространства_имен",
            "Description": "описание_пространства_имен",
            "Variables": [
                {
                    "Variable": "имя_переменной",
                    "Type": "тип_переменной",
                    "Value": "значение_переменной",
                    "Description": "описание_переменной"
                },
                ...
            ]
        },
        ...
    ]
}
```

### 2.4 AJPackage
Содержит информацию о пакете объектов:
```csharp
public class AJPackage
{
    public string Name;                // Имя пакета
    public string Description;         // Описание
    public bool IsDefaultPackage;      // Является ли пакетом по умолчанию
    public List<AJObj> Models;         // Список объектов в пакете
}
```

**Методы для работы с AJPackage:**
- Данные берутся напрямую из flow.json при десериализации
- Используются в методе `GetParsedFlowJSONFile()` для заполнения поля Packages в AJFile
- В методе `GetAricyBookEntities()` берется первый пакет (`_ajfile.Packages[0].Models`) для обработки объектов

**Структура в flow.json:**
```json
{
    "Packages": [
        {
            "Name": "имя_пакета",
            "Description": "описание_пакета",
            "IsDefaultPackage": true,
            "Models": [...]
        },
        ...
    ]
}
```

### 2.5 AJObj
Представляет собой игровой объект (диалог, персонаж, локацию и т.д.):
```csharp
public class AJObj
{
    public string Type;           // Тип объекта
    public AJType EType;          // Перечисление типа
    public AJObjProps Properties; // Свойства объекта
}
```

**Методы для работы с AJObj:**
- Данные берутся из flow.json при десериализации
- Обрабатываются в методе `GetAricyBookEntities()`, где определяется тип объекта (EType) на основе поля Type
- Добавляются в словарь ObjectsList с ключом Properties.Id

**Структура в flow.json:**
```json
{
    "Packages": [
        {
            "Name": "имя_пакета",
            "Description": "описание_пакета",
            "IsDefaultPackage": true,
            "Models": [
                {
                    "Type": "тип_объекта",
                    "Properties": {...}
                },
                ...
            ]
        },
        ...
    ]
}
```

### 2.6 AJType
Перечисление, определяющее типы игровых объектов:
```csharp
public enum AJType
{
    FlowFragment,      // Фрагмент потока диалога
    Dialogue,          // Диалог
    Entity,            // Сущность (персонаж)
    Location,          // Локация
    DialogueFragment,  // Фрагмент диалога
    Instruction,       // Инструкция
    Condition,         // Условие
    Jump,              // Переход
    Other              // Прочее
}
```

**Методы для работы с AJType:**
- Используется в методе `GetAricyBookEntities()` для определения типа объекта на основе строкового значения Type
- Устанавливается в поле EType объекта AJObj

**Структура в flow.json:**
Значение поля `Type` в объекте AJObj определяет тип объекта:
```json
{
    "Packages": [
        {
            "Models": [
                {
                    "Type": "FlowFragment",  // Определяет тип объекта
                    "Properties": {...}
                },
                {
                    "Type": "Dialogue",
                    "Properties": {...}
                },
                ...
            ]
        }
    ]
}
```

### 2.7 AJConnection
Представляет соединение между объектами:
```csharp
public class AJConnection
{
    public string Label;     // Метка соединения
    public string TargetPin; // Целевой пин
    public string Target;    // Целевой объект
}
```

**Методы для работы с AJConnection:**
- Данные берутся из flow.json при десериализации
- Используются в свойствах InputPins и OutputPins объекта AJObjProps

**Структура в flow.json:**
```json
{
    "Packages": [
        {
            "Models": [
                {
                    "Properties": {
                        "InputPins": [
                            {
                                "Connections": [
                                    {
                                        "Label": "метка_соединения",
                                        "TargetPin": "целевой_пин",
                                        "Target": "целевой_объект"
                                    },
                                    ...
                                ]
                            },
                            ...
                        ]
                    }
                }
            ]
        }
    ]
}
```

### 2.8 AJPin
Представляет пин (точку соединения) объекта:
```csharp
public class AJPin
{
    public string Text;                    // Текст пина
    public string Id;                      // Идентификатор пина
    public string Owner;                   // Владелец пина
    public List<AJConnection> Connections; // Список соединений
}
```

**Методы для работы с AJPin:**
- Данные берутся из flow.json при десериализации
- Используются в свойствах InputPins и OutputPins объекта AJObjProps

**Структура в flow.json:**
```json
{
    "Packages": [
        {
            "Models": [
                {
                    "Properties": {
                        "InputPins": [
                            {
                                "Text": "текст_пина",
                                "Id": "идентификатор_пина",
                                "Owner": "владелец_пина",
                                "Connections": [...]
                            },
                            ...
                        ],
                        "OutputPins": [
                            {
                                "Text": "текст_пина",
                                "Id": "идентификатор_пина",
                                "Owner": "владелец_пина",
                                "Connections": [...]
                            },
                            ...
                        ]
                    }
                }
            ]
        }
    ]
}
```

### 2.9 AJObjProps
Содержит свойства игрового объекта:
```csharp
public class AJObjProps //FlowFragment, Dialogue, Entity, Location
{
    public string TechnicalName;           // Техническое имя
    public string Id;                      // Идентификатор
    public string DisplayName;             // Отображаемое имя
    public string Parent;                  // Родительский объект
    public List<string> Attachments;       // Вложения
    public AJColor Color;                  // Цвет
    public string Text;                    // Текст
    public string ExternalId;              // Внешний идентификатор
    public string ShortId;                 // Короткий идентификатор
    public List<AJPin> InputPins;         // Входные пины
    public List<AJPin> OutputPins;        // Выходные пины
    public string MenuText;                // Текст меню (для DialogueFragment)
    public string StageDirections;         // Указания по сцене (для DialogueFragment)
    public string Speaker;                 // Говорящий (для DialogueFragment)
    public string Expression;              // Выражение (для Instruction, Condition)
    public string Target;                  // Цель (для Jump)
    public string TargetPin;               // Целевой пин (для Jump)
}
```

**Методы для работы с AJObjProps:**
- Данные берутся из flow.json при десериализации
- Используются в методе `GetAricyBookEntities()` для определения типа объекта и его свойств
- В методе `GetSortedChaptersList()` используется для сортировки глав по номеру

**Структура в flow.json:**
```json
{
    "Packages": [
        {
            "Models": [
                {
                    "Type": "тип_объекта",
                    "Properties": {
                        "TechnicalName": "техническое_имя",
                        "Id": "идентификатор",
                        "DisplayName": "отображаемое_имя",
                        "Parent": "родительский_объект",
                        "Attachments": ["вложение1", "вложение2", ...],
                        "Color": {...},
                        "Text": "текст",
                        "ExternalId": "внешний_идентификатор",
                        "ShortId": "короткий_идентификатор",
                        "InputPins": [...],
                        "OutputPins": [...],
                        "MenuText": "текст_меню",
                        "StageDirections": "указания_по_сцене",
                        "Speaker": "говорящий",
                        "Expression": "выражение",
                        "Target": "цель",
                        "TargetPin": "целевой_пин"
                    }
                }
            ]
        }
    ]
}
```

### 2.10 AJColor
Представляет цвет в формате RGBA:
```csharp
public class AJColor
{
    public float r; // Красный компонент
    public float g; // Зеленый компонент
    public float b; // Синий компонент
    public float a; // Альфа-компонент (прозрачность)

    public Color32 ToColor32()
    {
        return new Color32(r * 255f, g * 255f, b * 255f, a * 255f);
    }
}
```

**Методы для работы с AJColor:**
- Данные берутся из flow.json при десериализации
- Используется в свойстве Color объекта AJObjProps
- Метод ToColor32() преобразует значения в диапазон 0-255 для использования в Unity

**Структура в flow.json:**
```json
{
    "Packages": [
        {
            "Models": [
                {
                    "Properties": {
                        "Color": {
                            "r": 0.5,
                            "g": 0.3,
                            "b": 0.8,
                            "a": 1.0
                        }
                    }
                }
            ]
        }
    ]
}
```

### 2.11 AJLinkerOutputBase
Базовый класс для выходных данных линкера:
```csharp
public class AJLinkerOutputBase
{
    public List<AJNamespace> GlobalVariables; // Глобальные переменные
    public List<AJObj> SharedObjs;            // Общие объекты
}
```

**Методы для работы с AJLinkerOutputBase:**
- Используется для создания выходных данных линкера
- Заполняется в методах, которые обрабатывают данные из flow.json

**Структура в выходном JSON:**
```json
{
    "GlobalVariables": [...],
    "SharedObjs": [...]
}
```

### 2.12 AJLinkerOutputChapterFlow
Содержит объекты для главы:
```csharp
public class AJLinkerOutputChapterFlow
{
    public List<AJObj> Objects; // Список объектов главы
}
```

**Методы для работы с AJLinkerOutputChapterFlow:**
- Используется для создания выходных данных для конкретной главы
- Заполняется в методах, которые обрабатывают данные из flow.json

**Структура в выходном JSON:**
```json
{
    "Objects": [...]
}
```

### 2.13 AJMetaCharacterData
Содержит метаданные о персонаже:
```csharp
public class AJMetaCharacterData
{
    public string AID;                // Идентификатор персонажа
    public string DisplayName;        // Отображаемое имя
    public string ClothesVariableName; // Имя переменной для одежды
    public string AtlasFileName;      // Имя файла атласа
    public string BaseNameInAtlas;    // Базовое имя в атласе
}
```

**Методы для работы с AJMetaCharacterData:**
- Данные берутся из Meta.xlsx через метод `GetParsedMetaInputJSONFile()`
- Используются в классе `LinkerAtlasChecker` для проверки спрайтов персонажей

**Структура в Meta.xlsx:**
Данные берутся из листа "Characters" в файле Meta.xlsx, который содержит следующие колонки:
- AID
- DisplayName
- ClothesVariableName
- AtlasFileName
- BaseNameInAtlas

### 2.14 AJMetaLocationData
Содержит метаданные о локации:
```csharp
public class AJMetaLocationData
{
    public string AID;           // Идентификатор локации
    public int ID;               // Числовой идентификатор
    public string DisplayName;   // Отображаемое имя
    public string SpriteName;    // Имя спрайта
    public string SoundIdleName; // Имя звука простоя
}
```

**Методы для работы с AJMetaLocationData:**
- Данные берутся из Meta.xlsx через метод `GetParsedMetaInputJSONFile()`
- Используются для создания локаций в игре

**Структура в Meta.xlsx:**
Данные берутся из листа "Locations" в файле Meta.xlsx, который содержит следующие колонки:
- AID
- ID
- DisplayName
- SpriteName
- SoundIdleName

### 2.15 AJLinkerMeta
Содержит метаданные о проекте:
```csharp
public class AJLinkerMeta
{
    public string UniqueID;                  // Уникальный идентификатор
    public string SpritePrefix;              // Префикс спрайтов
    public BookVersionInfo Version;          // Информация о версии
    public List<string> ClothesSpriteNames;  // Имена спрайтов одежды
    public int UndefinedClothesFuncVariant;  // Вариант функции для неопределенной одежды
    public bool ExceptionsWeaponLayer;       // Исключения для слоя оружия
    public bool StandartizedUI;              // Стандартизированный интерфейс
    public int UITextBlockFontSize;          // Размер шрифта текстового блока
    public int UIChoiceBlockFontSize;        // Размер шрифта блока выбора
    public string KarmaCurrency;             // Валюта кармы
    public int KarmaBadBorder;               // Граница плохой кармы
    public int KarmaGoodBorder;              // Граница хорошей кармы
    public int KarmaTopLimit;                // Верхний предел кармы
    public List<int> UITextPlateLimits;      // Ограничения текстовой тарелки
    public bool UIPaintFirstLetterInRedException; // Исключение для первой буквы красным
    public int UITextPlateOffset;            // Смещение текстовой тарелки
    public bool UIOverridedTextColor;        // Переопределенный цвет текста
    public List<int> UITextColor;            // Цвет текста
    public List<int> UIBlockedTextColor;     // Цвет заблокированного текста
    public List<int> UIChNameTextColor;      // Цвет текста имени персонажа
    public List<int> UIOutlineColor;         // Цвет контура
    public List<int> UIResTextColor;         // Цвет текста ресурса
    public bool WardrobeEnabled;             // Включен гардероб
    public bool MainHeroHasDifferentGenders; // Главный герой имеет разные полы
    public bool MainHeroHasSplittedHairSprite; // Главный герой имеет разделенный спрайт волос
    public int IntroLocation;                // Вводная локация
    public int CustomClothesCount;           // Количество пользовательской одежды
    public int CustomHairCount;              // Количество пользовательских причесок
    public List<string> CurrenciesInOrderOfUI; // Валюты в порядке интерфейса
    public List<string> RacesList;           // Список рас
    public List<string> ChaptersEntryPoints; // Точки входа глав
    public List<AJMetaCharacterData> Characters; // Персонажи
    public List<AJMetaLocationData> Locations;   // Локации
}
```

**Методы для работы с AJLinkerMeta:**
- Данные берутся из Meta.xlsx через метод `GetParsedMetaInputJSONFile()`
- Используются для настройки проекта и его параметров
- Передаются в класс `LinkerAtlasChecker` для проверки спрайтов

**Структура в Meta.xlsx:**
Данные берутся из различных листов в файле Meta.xlsx:
- Лист "Settings" - основные настройки проекта
- Лист "Characters" - информация о персонажах
- Лист "Locations" - информация о локациях

### 2.16 BookVersionInfo
Содержит информацию о версии книги:
```csharp
public class BookVersionInfo
{
    public string BinVersion;           // Версия бинарного файла
    public string PreviewVersion;       // Версия предпросмотра
    public string BaseResourcesVersion; // Версия базовых ресурсов
}
```

**Методы для работы с BookVersionInfo:**
- Данные берутся из Meta.xlsx через метод `GetParsedMetaInputJSONFile()`
- Используются в свойстве Version объекта AJLinkerMeta

**Структура в Meta.xlsx:**
Данные берутся из листа "Settings" в файле Meta.xlsx, который содержит следующие колонки:
- BinVersion
- PreviewVersion
- BaseResourcesVersion

### 2.17 AJChapterAsset
Содержит информацию об активах главы:
```csharp
public class AJChapterAsset
{
    public int ChapterN;                // Номер главы
    public List<string> CharacterIDs;   // Идентификаторы персонажей
    public List<string> CharacterNames; // Имена персонажей
    public List<string> LocationIDs;    // Идентификаторы локаций
    public List<string> LocationNames;  // Имена локаций

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
```

**Методы для работы с AJChapterAsset:**
- Создается в классе `AJAssetGridLinker` для каждой главы
- Заполняется методами `AddCharacter()` и `AddLocation()` в классе `AJAssetGridLinker`
- Используется для хранения информации о персонажах и локациях в каждой главе

**Структура в выходном JSON:**
```json
{
    "ChapterN": 1,
    "CharacterIDs": ["id1", "id2", ...],
    "CharacterNames": ["name1", "name2", ...],
    "LocationIDs": ["id1", "id2", ...],
    "LocationNames": ["name1", "name2", ...]
}
```

### 2.18 AJAssetGridLinker
Управляет активами по главам:
```csharp
public class AJAssetGridLinker
{
    private List<string> AddedChs;           // Добавленные персонажи
    private List<string> AddedLocs;          // Добавленные локации
    public List<AJChapterAsset> AssetsByChapters; // Активы по главам
    private int CurrentChapter;              // Текущая глава

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
```

**Методы для работы с AJAssetGridLinker:**
- Создается для управления активами по главам
- Используется для добавления персонажей и локаций в главы
- Предоставляет методы для получения информации о персонажах и локациях в текущей главе

**Структура в выходном JSON:**
```json
{
    "AssetsByChapters": [
        {
            "ChapterN": 1,
            "CharacterIDs": ["id1", "id2", ...],
            "CharacterNames": ["name1", "name2", ...],
            "LocationIDs": ["id1", "id2", ...],
            "LocationNames": ["name1", "name2", ...]
        },
        {
            "ChapterN": 2,
            ...
        },
        ...
    ]
}
```

### 2.19 AJGridAssetChapterJSON
Содержит информацию об активах главы в формате JSON:
```csharp
public class AJGridAssetChapterJSON
{
    public List<string> CharactersIDs; // Идентификаторы персонажей
    public List<string> LocationsIDs;  // Идентификаторы локаций

    public AJGridAssetChapterJSON()
    {
        CharactersIDs = new List<string>();
        LocationsIDs = new List<string>();
    }
}
```

**Методы для работы с AJGridAssetChapterJSON:**
- Используется для сериализации данных о персонажах и локациях в главе в JSON
- Заполняется данными из AJChapterAsset

**Структура в выходном JSON:**
```json
{
    "CharactersIDs": ["id1", "id2", ...],
    "LocationsIDs": ["id1", "id2", ...]
}
```

### 2.20 AJGridAssetJSON
Содержит информацию об активах всех глав в формате JSON:
```csharp
public class AJGridAssetJSON
{
    public List<AJGridAssetChapterJSON> Chapters; // Список глав

    public AJGridAssetJSON()
    {
        Chapters = new List<AJGridAssetChapterJSON>();
    }
}
```

**Методы для работы с AJGridAssetJSON:**
- Используется для сериализации данных о всех главах в JSON
- Заполняется данными из AJAssetGridLinker

**Структура в выходном JSON:**
```json
{
    "Chapters": [
        {
            "CharactersIDs": ["id1", "id2", ...],
            "LocationsIDs": ["id1", "id2", ...]
        },
        {
            "CharactersIDs": ["id1", "id2", ...],
            "LocationsIDs": ["id1", "id2", ...]
        },
        ...
    ]
}
```

### 2.21 AJLocalizInJSONFile
Содержит данные локализации в формате JSON:
```csharp
public class AJLocalizInJSONFile
{
    public Dictionary<string, string> Data; // Данные локализации
}
```

**Методы для работы с AJLocalizInJSONFile:**
- Используется для сериализации и десериализации данных локализации
- Заполняется данными из Excel-файлов локализации

**Структура в выходном JSON:**
```json
{
    "Data": {
        "ключ1": "значение1",
        "ключ2": "значение2",
        ...
    }
}
```

## 3. Процесс парсинга

### 3.1 Чтение файла
Файл flow.json читается в методе `GetParsedFlowJSONFile()` класса `LinkerBin`:

```csharp
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
```

### 3.2 Типы объектов
В flow.json могут быть следующие типы объектов (AJType):
- FlowFragment - фрагмент потока диалога
- Dialogue - диалог
- Entity - сущность (персонаж)
- Location - локация
- DialogueFragment - фрагмент диалога
- Instruction - инструкция
- Condition - условие
- Jump - переход
- Other - прочее

### 3.3 Обработка объектов
Обработка объектов происходит в методе `GetAricyBookEntities()`:

```csharp
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
```

### 3.4 Сортировка глав
Сортировка глав происходит в методе `GetSortedChaptersList()`:

```csharp
private List<string> GetSortedChaptersList(Dictionary<string, AJObj> _objList, Dictionary<string, string> _nativeDict)
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
```

## 4. Примеры использования

### 4.1 Получение списка всех объектов
```csharp
LinkerBin linker = new LinkerBin(projectPath);
AJFile flowJson = linker.GetParsedFlowJSONFile();
Dictionary<string, AJObj> objects = linker.GetAricyBookEntities(flowJson, linker.GetNativeDict());
```

### 4.2 Работа с диалогами
```csharp
foreach (var obj in objects)
{
    if (obj.Value.EType == AJType.Dialogue)
    {
        // Обработка диалога
        string dialogueText = obj.Value.Properties.Text;
        string speaker = obj.Value.Properties.Speaker;
    }
}
```

### 4.3 Работа с инструкциями
```csharp
foreach (var obj in objects)
{
    if (obj.Value.EType == AJType.Instruction)
    {
        // Обработка инструкции
        string expression = obj.Value.Properties.Expression;
    }
}
```

### 4.4 Работа с персонажами
```csharp
foreach (var obj in objects)
{
    if (obj.Value.EType == AJType.Entity)
    {
        // Обработка персонажа
        string characterName = obj.Value.Properties.DisplayName;
        string characterId = obj.Value.Properties.Id;
    }
}
```

### 4.5 Работа с локациями
```csharp
foreach (var obj in objects)
{
    if (obj.Value.EType == AJType.Location)
    {
        // Обработка локации
        string locationName = obj.Value.Properties.DisplayName;
        string locationId = obj.Value.Properties.Id;
    }
}
```

## 5. Важные замечания

1. Все пути к файлам относительные и начинаются от корня проекта
2. При работе с flow.json необходимо учитывать кодировку файла (UTF-8)
3. Важно проверять наличие всех необходимых полей перед их использованием
4. При добавлении новых типов объектов необходимо обновлять enum AJType
5. При работе с персонажами и локациями необходимо учитывать метаданные из Meta.xlsx

## 6. Обработка ошибок

При работе с flow.json следует обрабатывать следующие возможные ошибки:
1. Отсутствие файла flow.json
2. Некорректный формат JSON
3. Отсутствие обязательных полей
4. Несоответствие типов данных
5. Отсутствие файла Meta.xlsx
6. Некорректные данные в Meta.xlsx

## 7. Оптимизация

1. Кэширование результатов парсинга
2. Использование асинхронного чтения для больших файлов
3. Валидация данных перед использованием
4. Очистка неиспользуемых объектов
5. Использование пулов объектов для часто создаваемых объектов 