using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace wackydatabase
{
    public static class TextureDataManager
    {
        public static Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

        //public static event EventHandler<TextureEventArgs> OnTextureAdd;
        //public static event EventHandler<TextureEventArgs> OnTextureChange;

        public static void SaveTexture(string name, Material material, string property)
        {
            Texture2D texture = (Texture2D)material.GetTexture(property);

            byte[] data = texture.EncodeToPNG();

            File.WriteAllBytes(Path.Combine(WMRecipeCust.assetPathTextures, name + ".png"), data);
        }

        public static Texture2D LoadTexture(string name)
        {
            string path = Path.Combine(WMRecipeCust.assetPathTextures, name + ".png");

            if (!File.Exists(path))
            {
                return null;
            }

            byte[] data = File.ReadAllBytes(path);

            Texture2D texture = new Texture2D(16, 16);
            texture.LoadImage(data);

            return texture;

        }

        public static Texture2D GetTexture(string name)
        {
            if (!textureCache.ContainsKey(name))
            {
                textureCache[name] = LoadTexture(name);
            }

            return textureCache[name];
        }

        public static Dictionary<string, Texture2D> GetTextures(Dictionary<string, string> textures)
        {
            Dictionary<string, Texture2D> data = new Dictionary<string, Texture2D>();

            foreach (KeyValuePair<string, string> entry in textures)
            {
                data[entry.Key] = GetTexture(entry.Value);
            }

            return data;
        }
    }

    public class TextureEventArgs : EventArgs
    {
        public Texture2D Texture { get; private set; }

        public TextureEventArgs(Texture2D t)
        {
            Texture = t;
        }
    }
}
