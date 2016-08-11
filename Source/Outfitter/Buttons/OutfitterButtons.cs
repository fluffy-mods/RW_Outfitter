using UnityEngine;

namespace Verse
{
    [StaticConstructorOnStartup]
    internal class OutfitterTextures
    {
        public static Texture2D resetButton = ContentFinder<Texture2D>.Get("reset");

        public static Texture2D deleteButton = ContentFinder<Texture2D>.Get("delete");

        public static Texture2D addButton = ContentFinder<Texture2D>.Get("add");
    }
}
