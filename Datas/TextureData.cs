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
        Edge
    }

    [Serializable]
    public class TextureData
    {
        public string Name;
        public TextureEffect Effect;
        public List<Color> Colors;
    }
}
