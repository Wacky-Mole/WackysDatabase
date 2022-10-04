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
                .Build();
            _stringBuilder = new StringBuilder();
        }

        public bool Load<T>(string file, List<T> items, bool concat = false)
        {
            try
            {
                var yml = File.ReadAllText(file);

                if (concat)
                {
                    _stringBuilder.Append(yml);
                    _stringBuilder.Append(WMRecipeCust.StringSeparator);
                }

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
