using UnityEngine;

namespace Verse
{
    [StaticConstructorOnStartup]
    internal class OutfitterTextures
    {
        public static readonly Texture2D resetButton = ContentFinder<Texture2D>.Get("reset");

        public static readonly Texture2D deleteButton = ContentFinder<Texture2D>.Get("delete");

        public static readonly Texture2D addButton = ContentFinder<Texture2D>.Get("add");

        public static readonly Texture2D BGColor = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.2f, 1));

        public static readonly Texture2D White = SolidColorMaterials.NewSolidColorTexture(Color.white);
    }
}
