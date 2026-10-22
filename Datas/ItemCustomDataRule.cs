using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace wackydatabase.Datas
{
    public enum ItemCustomDataScope
    {
        ExportOnly,
        NewItems,
        AllItems
    }

    public class ItemCustomDataRule
    {
        public string name = "";
        public ItemCustomDataScope applyTo = ItemCustomDataScope.ExportOnly;
        public Dictionary<string, string> set = new(StringComparer.Ordinal);
        public Dictionary<string, string> types = new(StringComparer.Ordinal);
        public List<string> remove = new();

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(name) || name != name.Trim())
                throw new ArgumentException("A custom-data rule must have an exact prefab name without surrounding whitespace.");
            if (!Enum.IsDefined(typeof(ItemCustomDataScope), applyTo))
                throw new ArgumentException($"Unknown custom-data applyTo mode for {name}: {applyTo}.");
            if (set == null || types == null || remove == null)
                throw new ArgumentException($"Custom-data set, types, and remove must not be null for {name}.");
            if (set.Any(pair => string.IsNullOrEmpty(pair.Key) || pair.Value == null)
                || remove.Any(string.IsNullOrEmpty))
                throw new ArgumentException($"Custom-data keys must be nonempty and values must be strings, not null, for {name}.");
            if (remove.Any(set.ContainsKey))
                throw new ArgumentException($"A custom-data key cannot appear in both set and remove for {name}.");
            foreach (var hint in types)
            {
                if (string.IsNullOrEmpty(hint.Key) || !set.ContainsKey(hint.Key))
                    throw new ArgumentException($"Custom-data types key '{hint.Key}' must also appear in set for {name}.");
                FormatValue(hint.Key, set[hint.Key], hint.Value);
            }
        }

        private string FormatValue(string key, string value, string type)
        {
            switch (type)
            {
                case "string":
                    return value;
                case "int":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integer))
                        return integer.ToString(CultureInfo.InvariantCulture);
                    break;
                case "long":
                    if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long largeInteger))
                        return largeInteger.ToString(CultureInfo.InvariantCulture);
                    break;
                case "float":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number)
                        && !float.IsNaN(number) && !float.IsInfinity(number))
                        return number.ToString("R", CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new ArgumentException($"Unknown custom-data type '{type}' for '{key}' in {name}. Use string, int, long, or float.");
            }
            throw new ArgumentException($"Custom-data value '{value}' for '{key}' in {name} is not a valid {type} or is outside its supported range.");
        }

        public bool Apply(Dictionary<string, string> data)
        {
            bool changed = false;
            foreach (string key in remove)
                changed |= data.Remove(key);
            foreach (var pair in set)
            {
                string value = types.TryGetValue(pair.Key, out string type) ? FormatValue(pair.Key, pair.Value, type) : pair.Value;
                if (!data.TryGetValue(pair.Key, out string current) || current != value)
                {
                    data[pair.Key] = value;
                    changed = true;
                }
            }
            return changed;
        }
    }
}