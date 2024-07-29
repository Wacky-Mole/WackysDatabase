using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace wackydatabase.Datas
{
    public class TextureConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(Texture2D);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            string texture_name = null;
            FilterMode texture_filterMode = FilterMode.Point;

            if (parser.TryConsume<MappingStart>(out var mappingStart))
            {

                while (!parser.TryConsume<MappingEnd>(out var _))
                {
                    var key = parser.Consume<Scalar>().Value;
                    var value = parser.Consume<Scalar>().Value;

                    switch (key)
                    {
                        case "name": texture_name = value; break;
                        case "filterMode": Enum.TryParse(value, out texture_filterMode); break;
                    }

                }
            }
            else
            {
                // single value with texture name
                texture_name = parser.Consume<Scalar>().Value;
            }

            Texture2D t = TextureDataManager.LoadTexture(texture_name);
            if (t != null)
            {
                t.name = texture_name;
                t.filterMode = texture_filterMode;
            }
            else
            { 
                WMRecipeCust.WLog.LogInfo("Texture '" + texture_name + "' not found"); 
            }
            

            return t;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            Texture2D t = (Texture2D)value;
            if (t.filterMode == FilterMode.Point)
            { 
                // single value style to keep it short (Point is the default)
                emitter.Emit(new Scalar(t.name));
            }
            else
            {
                // multivalue style
                emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));
                emitter.Emit(new Scalar("name"));
                emitter.Emit(new Scalar(t.name));
                emitter.Emit(new Scalar("filterMode"));
                emitter.Emit(new Scalar(t.filterMode.ToString()));
                emitter.Emit(new MappingEnd());
            }
        }
    }
}
