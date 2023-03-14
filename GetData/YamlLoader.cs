using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;

namespace wackydatabase.Datas {
    public class YamlLoader
    {
        private IDeserializer _deserializer;
        private StringBuilder _stringBuilder;

        public YamlLoader()
        {
            _deserializer = new DeserializerBuilder()
                .WithTypeConverter(new ColorConverter())
                .WithTypeConverter(new ValheimTimeConverter())
                .IgnoreUnmatchedProperties() // future proofing
                .Build();
            _stringBuilder = new StringBuilder();
        }

        public bool Load<T>(string file, List<T> items, bool concat = false) // need to rewrite this a bit
        {
            try
            {
                var yml = File.ReadAllText(file);

                if (concat)
                {
                    _stringBuilder.Append(yml);
                    _stringBuilder.Append(WMRecipeCust.StringSeparator);
                }

                /*
                if (true) // exclusiveZero replacer
                {
                    string[] lines = temp.Replace("\r", "").Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (line.Contains("\": 0")) continue;
                        output.AppendLine(line);
                    }
                    string _tmp = output.ToString().Replace("\r", "").Replace("\n", "").Replace(": ", ":").Replace("\t", "");
                    output = new StringBuilder();
                    for (int i = 0; i < _tmp.Length; i++)
                    {
                        char c_current = _tmp[i];
                        char c_next = ' ';

                        if (i + 1 < _tmp.Length)
                        {
                            c_next = _tmp[i + 1];
                        }

                        if (c_next == '}' || c_next == ']')
                        {
                            if (c_current == ',') continue;
                        }

                        output.Append(c_current);
                    } */

                    var result = _deserializer.Deserialize<T>(yml);

                if (result != null)
                {
                    items.Add(result);
                    return true;
                }
            } catch(Exception ex)
            {
                WMRecipeCust.WLog.LogWarning("Something went wrong in file " + file);
                WMRecipeCust.WLog.LogError(ex.Message);
                
            }

            return false;
        }

        public override string ToString()
        {
            return _stringBuilder.ToString();
        }




    }
}
