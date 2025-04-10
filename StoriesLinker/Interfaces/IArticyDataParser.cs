using System.Collections.Generic; // Добавлено для Dictionary
// Удалено using StoriesLinker.Models; т.к. AjFile находится в корневом пространстве имен

namespace StoriesLinker
{
    public interface IArticyDataParser
    {
        /// <summary>
        /// Parses Articy data (either version 3 or X) and returns the main data structure
        /// along with the localization dictionary.
        /// </summary>
        /// <returns>A tuple containing the parsed AjFile and the localization dictionary.</returns>
        (AjFile ParsedData, Dictionary<string, string> Localization) ParseData();
    }
}
