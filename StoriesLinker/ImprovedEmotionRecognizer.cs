namespace StoriesLinker
{
    /// <summary>
    /// Улучшенный алгоритм распознавания эмоций по цвету
    /// Поддерживает как Articy 3, так и Articy X цвета
    /// </summary>
    public static class ImprovedEmotionRecognizer
    {
        /// <summary>
        /// Распознает эмоцию по цвету для обоих стандартов (Articy 3 и Articy X)
        /// </summary>
        public static string RecognizeEmotion(AjColor color)
        {
            // Если цвет черный (0,0,0) - это означает, что цвет не установлен в Articy
            if (color.R == 0 && color.G == 0 && color.B == 0)
            {
                return "IsntSetOrNeutral";
            }

            // Сначала проверяем точные совпадения в Articy 3
            var emotion3 = EmotionColorMapper.FindExactEmotion(color, EmotionColorMapper.Articy3EmotionColors);
            if (emotion3.HasValue)
            {
                return emotion3.Value.ToString();
            }

            // Затем проверяем точные совпадения в Articy X
            var emotionX = EmotionColorMapper.FindExactEmotion(color, EmotionColorMapper.ArticyXEmotionColors);
            if (emotionX.HasValue)
            {
                return emotionX.Value.ToString();
            }

            // Если точного совпадения нет, ищем ближайший цвет в Articy 3
            var closest3 = EmotionColorMapper.FindClosestEmotion(color, EmotionColorMapper.Articy3EmotionColors);
            
            // Ищем ближайший цвет в Articy X
            var closestX = EmotionColorMapper.FindClosestEmotion(color, EmotionColorMapper.ArticyXEmotionColors);
            
            // Вычисляем расстояния до ближайших цветов
            float distance3 = GetColorDistance(color, EmotionColorMapper.Articy3EmotionColors[closest3]);
            float distanceX = GetColorDistance(color, EmotionColorMapper.ArticyXEmotionColors[closestX]);
            
            // Возвращаем эмоцию с минимальным расстоянием
            return distance3 <= distanceX ? closest3.ToString() : closestX.ToString();
        }

        /// <summary>
        /// Детальное распознавание эмоций с информацией об источнике
        /// </summary>
        public static EmotionRecognitionResult RecognizeEmotionDetailed(AjColor color)
        {
            var result = new EmotionRecognitionResult
            {
                InputColor = color,
                RecognizedEmotion = EChEmotion.IsntSetOrNeutral,
                Source = "Unknown",
                IsExactMatch = false,
                Confidence = 0.0f
            };

            // Проверяем точные совпадения в Articy 3
            var emotion3 = EmotionColorMapper.FindExactEmotion(color, EmotionColorMapper.Articy3EmotionColors);
            if (emotion3.HasValue)
            {
                result.RecognizedEmotion = emotion3.Value;
                result.Source = "Articy 3";
                result.IsExactMatch = true;
                result.Confidence = 1.0f;
                return result;
            }

            // Проверяем точные совпадения в Articy X
            var emotionX = EmotionColorMapper.FindExactEmotion(color, EmotionColorMapper.ArticyXEmotionColors);
            if (emotionX.HasValue)
            {
                result.RecognizedEmotion = emotionX.Value;
                result.Source = "Articy X";
                result.IsExactMatch = true;
                result.Confidence = 1.0f;
                return result;
            }

            // Ищем ближайшие совпадения
            var closest3 = EmotionColorMapper.FindClosestEmotion(color, EmotionColorMapper.Articy3EmotionColors);
            var closestX = EmotionColorMapper.FindClosestEmotion(color, EmotionColorMapper.ArticyXEmotionColors);

            float distance3 = GetColorDistance(color, EmotionColorMapper.Articy3EmotionColors[closest3]);
            float distanceX = GetColorDistance(color, EmotionColorMapper.ArticyXEmotionColors[closestX]);

            if (distance3 <= distanceX)
            {
                result.RecognizedEmotion = closest3;
                result.Source = "Articy 3 (approximate)";
                result.Confidence = Math.Max(0, 1.0f - distance3);
            }
            else
            {
                result.RecognizedEmotion = closestX;
                result.Source = "Articy X (approximate)";
                result.Confidence = Math.Max(0, 1.0f - distanceX);
            }

            return result;
        }

        /// <summary>
        /// Вычисляет расстояние между двумя цветами
        /// </summary>
        private static float GetColorDistance(AjColor color1, AjColor color2)
        {
            float dr = color1.R - color2.R;
            float dg = color1.G - color2.G;
            float db = color1.B - color2.B;
            return (float)Math.Sqrt(dr * dr + dg * dg + db * db);
        }
    }

    /// <summary>
    /// Результат распознавания эмоции с детальной информацией
    /// </summary>
    public class EmotionRecognitionResult
    {
        public AjColor InputColor { get; set; }
        public EChEmotion RecognizedEmotion { get; set; }
        public string Source { get; set; }
        public bool IsExactMatch { get; set; }
        public float Confidence { get; set; }

        public override string ToString()
        {
            return $"Emotion: {RecognizedEmotion}, Source: {Source}, " +
                   $"Exact: {IsExactMatch}, Confidence: {Confidence:F2}";
        }
    }
} 