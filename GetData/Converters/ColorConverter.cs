using System;
using System.Collections.Generic;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace wackydatabase.Datas
{
    public class ColorConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(Color);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            SequenceStart s = parser.Consume<SequenceStart>();

            List<float> segments = new List<float>();

            while (!parser.TryConsume<SequenceEnd>(out var sequenceEnd))
            {
                var v = parser.Consume<Scalar>().Value;

                segments.Add(float.Parse(v));
            }

            Color color = new Color(
                segments.Count > 0 ? segments[0] : 0.0f,
                segments.Count > 1 ? segments[1] : 0.0f,
                segments.Count > 2 ? segments[2] : 0.0f,
                segments.Count > 3 ? segments[3] : 0.0f
            );

            return color;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Flow));
           
            Color i = (Color)value;

            emitter.Emit(new Scalar(i.r.ToString()));
            emitter.Emit(new Scalar(i.g.ToString()));
            emitter.Emit(new Scalar(i.b.ToString()));
            emitter.Emit(new Scalar(i.a.ToString()));

            emitter.Emit(new SequenceEnd());
        }
    }
}
