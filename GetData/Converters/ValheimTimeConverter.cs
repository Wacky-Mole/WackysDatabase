using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace wackydatabase.Datas
{
    public class ValheimTimeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(ValheimTime);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            SequenceStart s = parser.Consume<SequenceStart>();

            List<int> segments = new List<int>();

            while (!parser.TryConsume<SequenceEnd>(out var sequenceEnd))
            {
                var v = parser.Consume<Scalar>().Value;

                segments.Add(int.Parse(v));
            }

            ValheimTime vt = new ValheimTime(
                segments.Count > 0 ? segments[0] : 0,
                segments.Count > 1 ? segments[1] : 0,
                segments.Count > 2 ? segments[2] : 0,
                segments.Count > 3 ? segments[3] : 0
            );

            return vt;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Flow));

            ValheimTime vt = (ValheimTime) value;

            emitter.Emit(new Scalar(vt.Hour.ToString()));
            emitter.Emit(new Scalar(vt.Minute.ToString()));
            emitter.Emit(new Scalar(vt.Second.ToString()));
            emitter.Emit(new Scalar(vt.Day.ToString()));

            emitter.Emit(new SequenceEnd());
        }
    }
}
