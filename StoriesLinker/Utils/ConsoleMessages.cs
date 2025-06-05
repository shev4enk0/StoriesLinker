namespace StoriesLinker;

/// <summary>
/// Centralized console messages in English to avoid encoding issues
/// </summary>
public static class ConsoleMessages
{
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

    public static string ArticyDataProcessingComplete() =>
        "Articy X data processing complete!";

    public static string ConversionModeDetected(string mode) =>
        $"Conversion mode detected: {mode}";
    
    public static string CreatingLocalizationExcel() =>
        "Creating localization Excel file from Articy X data...";

    public static string LocalizationFileCreated(string path) =>
        $"Localization file created: {path}";

    public static string LocalizationEntriesWritten(int total, int newEntries) =>
        $"Written {total} localization entries ({newEntries} new)";

    public static string ObjectConversionError(string error) =>
        $"Object conversion error: {error}";
} 