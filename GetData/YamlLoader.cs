using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace wackydatabase.Datas {
    public class YamlLoader
    {
        private IDeserializer _deserializer;
        private ISerializer _serializer;
        private StringBuilder _stringBuilder;

        public YamlLoader()
        {
            _deserializer = new DeserializerBuilder()
                .WithTypeConverter(new ColorConverter())
                .WithTypeConverter(new ValheimTimeConverter())
                .IgnoreUnmatchedProperties() // future proofing
                .Build();

            _serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeConverter(new ColorConverter())
                .WithTypeConverter(new ValheimTimeConverter())
                .Build();

            _stringBuilder = new StringBuilder();
        }

        public bool Load<T>(string file, List<T> items, string ymlread = null) // need to rewrite this a bit
        {
            try
            {
                string yml;
                if (ymlread == null)  
                 yml = File.ReadAllText(file);
                else
                    yml = ymlread;

                _stringBuilder.Append(yml);
                _stringBuilder.Append(WMRecipeCust.StringSeparator);
                
                //WMRecipeCust.ymlstring = WMRecipeCust.ymlstring + yml + WMRecipeCust.StringSeparator;

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


        public bool Write<T>(string file, T data)
        {
            try
            {
                var fileContents = _serializer.Serialize(data);

                File.WriteAllText(file, fileContents);

                return true;
            } catch (Exception ex)
            {
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
