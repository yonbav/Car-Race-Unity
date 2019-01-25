using UnityEngine;
using System.Collections;


namespace DungeonArchitect.Terrains
{
    /// <summary>
    /// The type of the texture defined in the landscape paint settings.  
    /// This determines how the specified texture would be painted in the modified terrain
    /// </summary>
    public enum LandscapeTextureType
    {
        Fill,
        Room,
        Corridor,
        Cliff
    }

    /// <summary>
    /// Data-structure to hold the texture settings.  This contains enough information to paint the texture 
    /// on to the terrain
    /// </summary>
    [System.Serializable]
    public class LandscapeTexture
    {
        public LandscapeTextureType textureType;
        public Texture2D diffuse;
        public Texture2D normal;
        public float metallic = 0;
        public Vector2 size = new Vector2(15, 15);
        public Vector2 offset = Vector2.zero;
    }
}
