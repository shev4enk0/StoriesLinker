namespace StoriesLinker;

/// <summary>
/// Centralized console messages in English to avoid encoding issues
/// </summary>
public static class ConsoleMessages
{
    // Language detection messages
    public static string BaseLanguageDetected(string language, string filePath) =>
        $"Base language detected: {language} based on file {filePath}";

    // File operation messages
    public static string FileNotFound(string path) =>
        $"WARNING: File not found: {path}";

    public static string ExcelFileNoWorksheets(string path) =>
        $"WARNING: Excel file contains no worksheets: {path}";

    public static string ExcelWorksheetEmpty(string path) =>
        $"WARNING: Excel worksheet is empty: {path}";

    public static string ExcelFileReadError(string path, string error) =>
        $"ERROR reading Excel file {path}: {error}";

    public static string DuplicateKeyError(string key) =>
        $"Double key critical error: {key}";

    // Articy processing messages
    public static string ProcessingArticyFile() =>
        "Reading Flow.json file from Articy X...";

    public static string ProcessingLocalizationKey(string key, string value) =>
        $"Processing localization key {key} for value: {value}...";

    public static string ArticyObjectsProcessed(int count) =>
        $"Processed {count} objects from Articy X";

    public static string LocalizationKeysGenerated(int count) =>
        $"Generated {count} localization keys";

    public static string FlowJsonSaved(string path) =>
        $"Flow.json saved: {path}";

    // Localization table generation messages
    public static string GeneratingLocalizationTables() =>
        "Generating localization tables from Articy X data...";

    public static string LocalizationFileRead(string path) =>
        $"Reading localization file: {path}";

    public static string LocalizationEntriesLoaded(int count, int pages) =>
        $"Loaded {count} localization entries ({pages} pages)";

    public static string ArticyDataProcessingComplete() =>
        "Articy X data processing complete!";

    public static string ConversionModeDetected(string mode) =>
        $"Conversion mode detected: {mode}";

    public static string TableGenerated(string name, int wordCount) =>
        $"Table {name} generated, word count: {wordCount}";

    public static string CharacterNamesTableGenerated(string name, int wordCount) =>
        $"Table {name} generated, word count: {wordCount}";

    // Error messages
    public static string MetaFileNotFound(string path) =>
        $"WARNING: Meta.xlsx file not found: {path}";

    public static string MetaFileNoWorksheets() =>
        "WARNING: Meta.xlsx contains no worksheets";

    public static string MetaWorksheetEmpty() =>
        "WARNING: Meta.xlsx first worksheet is empty";

    public static string MetaFileReadError(string error) =>
        $"ERROR reading Meta.xlsx file: {error}";

    public static string CharacterNotFoundInTable(string name) =>
        $"Character not found in table: {name}";

    public static string LocationNotFoundInTable(string name) =>
        $"Location not found in table: {name}";

    // Processing status messages
    public static string ProcessingChapter(int chapterNumber) =>
        $"Processing chapter {chapterNumber}...";

    public static string GeneratingLanguageTables(string language) =>
        $"GENERATING TABLES FOR LANGUAGE: {language}";

    public static string GeneratingSharedStrings(string path) =>
        $"Generating shared strings: {path}";

    public static string EmotionsFileCreated(string path) =>
        $"Emotions file created: {path}";

    public static string EmotionsDataLoaded(int count, string path) =>
        $"Loaded {count} entries with emotions from {path}";

    // Validation messages
    public static string UsingReadyText(string text) =>
        $"Using ready text: '{text}'";

    public static string LocalizationKeyNotFound(string key) =>
        $"WARNING: Localization key '{key}' not found in dictionary and is not readable text. Skipping.";

    public static string CharacterKeyNotFound(string key) =>
        $"WARNING: Character key '{key}' not found in localization dictionary. Using technical name.";

    public static string LocationKeyNotFound(string key) =>
        $"WARNING: Location key '{key}' not found in localization dictionary. Using technical name.";

    public static string ChapterKeyNotFound(string key) =>
        $"WARNING: Chapter key '{key}' not found in localization dictionary. Skipping chapter.";

    public static string ChapterNumberNotFound(string value, string key) =>
        $"WARNING: Chapter number not found in value '{value}' for key '{key}'. Skipping chapter.";

    // Success messages
    public static string TestPassed() =>
        "TEST PASSED: Found localization keys in Articy 3 style!";

    public static string TestFailed() =>
        "TEST FAILED: No localization keys found!";

    public static string ProcessingComplete() =>
        "Processing completed successfully!";

    // Additional file operation messages
    public static string BookDescriptionFileNotFound(string path) =>
        $"WARNING: Book description file not found: {path}";

    public static string BookDescriptionFileNoWorksheets(string path) =>
        $"WARNING: Book description Excel file contains no worksheets: {path}";

    public static string BookDescriptionWorksheetEmpty(string path) =>
        $"WARNING: Book description Excel worksheet is empty: {path}";

    public static string BookDescriptionFileReadError(string path, string error) =>
        $"ERROR reading book description Excel file {path}: {error}";

    // Emotions file messages
    public static string EmotionsFileNotFound(string path) =>
        $"WARNING: Emotions file not found: {path}";

    public static string EmotionsFileNoWorksheets(string path) =>
        $"WARNING: Emotions Excel file contains no worksheets: {path}";

    public static string EmotionsWorksheetEmpty(string path) =>
        $"WARNING: Emotions Excel worksheet is empty: {path}";

    public static string EmotionsFileReadError(string path, string error) =>
        $"ERROR reading emotions Excel file {path}: {error}";

    public static string EmotionsDuplicateKey(string key) =>
        $"WARNING: Duplicate key in emotions file: {key}";

    // Meta file specific messages
    public static string MetaFileThirdWorksheetMissing() =>
        "WARNING: Meta.xlsx does not contain third worksheet for character data";

    public static string MetaFileThirdWorksheetEmpty() =>
        "WARNING: Meta.xlsx third worksheet is empty";

    public static string MetaFileFourthWorksheetMissing() =>
        "WARNING: Meta.xlsx does not contain fourth worksheet for location data";

    public static string MetaFileFourthWorksheetEmpty() =>
        "WARNING: Meta.xlsx fourth worksheet is empty";

    // Chapter processing messages
    public static string ChapterKeyNotFoundInDict(string key) =>
        $"WARNING: Chapter key '{key}' not found in localization dictionary. Skipping chapter.";

    public static string ChapterNumberNotFoundInValue(string value, string key) =>
        $"WARNING: Chapter number not found in value '{value}' for key '{key}'. Skipping chapter.";

    // Character and location processing
    public static string CharacterNameKeyNotFound(string key) =>
        $"WARNING: Character name key '{key}' not found in localization dictionary. Using technical name.";

    public static string LocationNameKeyNotFound(string key) =>
        $"WARNING: Location key '{key}' not found in localization dictionary. Using technical name.";

    public static string ObjectKeyNotFound(string key, string objectType) =>
        $"WARNING: Key '{key}' not found in localization dictionary for object {objectType}. Using technical name.";

    // Language processing
    public static string LanguageAdded(string language) =>
        $"Added language for localization: {language}";

    // Articy X specific messages
    public static string CreatingLocalizationExcel() =>
        "Creating localization Excel file from Articy X data...";

    public static string LocalizationFileCreated(string path) =>
        $"Localization file created: {path}";

    public static string LocalizationEntriesWritten(int total, int newEntries) =>
        $"Written {total} localization entries ({newEntries} new)";

    public static string ObjectConversionError(string error) =>
        $"Object conversion error: {error}";
} 