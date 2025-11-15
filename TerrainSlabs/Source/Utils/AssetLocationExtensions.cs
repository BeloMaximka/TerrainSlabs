using System;
using Vintagestory.API.Common;

namespace TerrainSlabs.Source.Utils;

public static class AssetLocationExtensions
{
    /// <summary>
    /// We store and retrieve domain as varitant so we can easily get the original block from other mods
    /// <br/>"terrainslabs:muddygravel-game" -> "game:muddygravel"
    /// <br/>"terrainslabs:sandwavy-game-conglomerate" -> "game:sandwavy-conglomerate"
    /// </summary>
    public static AssetLocation UseFirstPartAsDomain(this AssetLocation location)
    {
        int firstPartIndex = location.Path.IndexOf('-') + 1; // -[g]ame-
        int secondHyphenIndex = location.Path.IndexOf('-', firstPartIndex); // -game[-]
        if (secondHyphenIndex == -1)
        {
            secondHyphenIndex = location.Path.Length;
        }

        return new(
            location.Path[firstPartIndex..secondHyphenIndex],
            string.Concat(location.Path.AsSpan(0, firstPartIndex - 1), location.Path.AsSpan(secondHyphenIndex))
        );
    }
}
