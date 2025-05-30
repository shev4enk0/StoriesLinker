namespace StoriesLinker;

/// <summary>
/// Класс для хранения локализационных данных с эмоциями
/// </summary>
public class LocalizEntityWithEmotion
{
    public string LocalizID { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string SpeakerDisplayName { get; set; } = string.Empty;
    public string Emotion { get; set; } = string.Empty;
}

/// <summary>
/// Файл локализации с эмоциями
/// </summary>
public class AjLocalizWithEmotionsInJsonFile
{
    public Dictionary<string, LocalizEntityWithEmotion> Data { get; set; } = new();
} 