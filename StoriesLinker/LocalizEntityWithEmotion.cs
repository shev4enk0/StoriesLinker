using System.Collections.Generic;

namespace StoriesLinker
{
    /// <summary>
    /// Класс для хранения локализационных данных с эмоциями
    /// </summary>
    public class LocalizEntityWithEmotion
    {
        public string LocalizID { get; set; }
        public string Text { get; set; }
        public string SpeakerDisplayName { get; set; }
        public string Emotion { get; set; }
    }

    /// <summary>
    /// Файл локализации с эмоциями
    /// </summary>
    public class AjLocalizWithEmotionsInJsonFile
    {
        public Dictionary<string, LocalizEntityWithEmotion> Data { get; set; } = new Dictionary<string, LocalizEntityWithEmotion>();
    }
} 