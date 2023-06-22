using System;
using System.Collections.Generic;
using UnityEngine;

namespace wackydatabase.Datas
{
    [Serializable]
    public enum TextureEffect
    {
        Screen,
        Multiply,
        Overlay,
        Edge
    }

    [Serializable]
    public class TextureData
    {
        public TextureData()
        {
            Name = "";
            Effect = TextureEffect.Screen;
            Colors = new List<Color>();
        }

        public string Name;
        public TextureEffect Effect;
        public List<Color> Colors;
    }
}
