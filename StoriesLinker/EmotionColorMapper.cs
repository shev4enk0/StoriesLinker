namespace StoriesLinker
{
    /// <summary>
    /// Перечисление эмоций персонажей
    /// </summary>
    public enum EChEmotion
    {
        Angry,
        Happy,
        Sad,
        Surprised,
        IsntSetOrNeutral
    }

    /// <summary>
    /// Класс для управления словарем цветов эмоций из Articy 3 и Articy X
    /// </summary>
    public static class EmotionColorMapper
    {
        /// <summary>
        /// Словарь цветов эмоций для Articy 3 (точные значения из таблицы)
        /// </summary>
        public static readonly Dictionary<EChEmotion, AjColor> Articy3EmotionColors = new Dictionary<EChEmotion, AjColor>
        {
            { EChEmotion.Angry, new AjColor { R = 1.0f, G = 0.0f, B = 0.0f, A = 1.0f } },
            { EChEmotion.Happy, new AjColor { R = 0.0f, G = 0.434153676f, B = 0.08021983f, A = 1.0f } },
            { EChEmotion.IsntSetOrNeutral, new AjColor { R = 0.577580452f, G = 0.7605245f, B = 0.7991027f, A = 1.0f } },
            { EChEmotion.Sad, new AjColor { R = 0.162029386f, G = 0.0295568351f, B = 0.351532638f, A = 1.0f } },
            { EChEmotion.Surprised, new AjColor { R = 1.0f, G = 0.527115166f, B = 0.0f, A = 1.0f } }
        };

        /// <summary>
        /// Словарь цветов эмоций для Articy X (точные значения из таблицы)
        /// </summary>
        public static readonly Dictionary<EChEmotion, AjColor> ArticyXEmotionColors = new Dictionary<EChEmotion, AjColor>
        {
            { EChEmotion.Angry, new AjColor { R = 1.0f, G = 0.0f, B = 0.0f, A = 1.0f } },
            { EChEmotion.Happy, new AjColor { R = 0.0f, G = 0.6901961f, B = 0.31373255f, A = 1.0f } },
            { EChEmotion.IsntSetOrNeutral, new AjColor { R = 0.78431374f, G = 0.8862745f, B = 0.90588236f, A = 1.0f } },
            { EChEmotion.Sad, new AjColor { R = 0.4392157f, G = 0.1882353f, B = 0.627451f, A = 1.0f } },
            { EChEmotion.Surprised, new AjColor { R = 1.0f, G = 0.7529412f, B = 0.0f, A = 1.0f } }
        };

        /// <summary>
        /// Проверяет, совпадают ли два цвета с заданной точностью
        /// </summary>
        private static bool ColorsMatch(AjColor color1, AjColor color2, float tolerance = 0.01f)
        {
            return Math.Abs(color1.R - color2.R) <= tolerance &&
                   Math.Abs(color1.G - color2.G) <= tolerance &&
                   Math.Abs(color1.B - color2.B) <= tolerance;
        }

        /// <summary>
        /// Ищет эмоцию по точному совпадению цвета в указанном словаре
        /// </summary>
        public static EChEmotion? FindExactEmotion(AjColor color, Dictionary<EChEmotion, AjColor> emotionColors)
        {
            foreach (var kvp in emotionColors)
            {
                if (ColorsMatch(color, kvp.Value))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// Ищет ближайшую эмоцию по цвету в указанном словаре
        /// </summary>
        public static EChEmotion FindClosestEmotion(AjColor color, Dictionary<EChEmotion, AjColor> emotionColors)
        {
            EChEmotion closestEmotion = EChEmotion.IsntSetOrNeutral;
            float minDistance = float.MaxValue;

            foreach (var kvp in emotionColors)
            {
                float distance = GetColorDistance(color, kvp.Value);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEmotion = kvp.Key;
                }
            }

            return closestEmotion;
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
} 