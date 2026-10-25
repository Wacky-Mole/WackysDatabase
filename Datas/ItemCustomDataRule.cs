using System;
using System.Collections.Generic;
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
        public List<string> remove = new();

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(name) || name != name.Trim())
                throw new ArgumentException("A custom-data rule must have an exact prefab name without surrounding whitespace.");
            if (!Enum.IsDefined(typeof(ItemCustomDataScope), applyTo))
                throw new ArgumentException($"Unknown custom-data applyTo mode for {name}: {applyTo}.");
            if (set == null || remove == null)
                throw new ArgumentException($"Custom-data set and remove must not be null for {name}.");
            if (set.Any(pair => string.IsNullOrEmpty(pair.Key) || pair.Value == null)
                || remove.Any(string.IsNullOrEmpty))
                throw new ArgumentException($"Custom-data keys must be nonempty and values must be strings, not null, for {name}.");
            if (remove.Any(set.ContainsKey))
                throw new ArgumentException($"A custom-data key cannot appear in both set and remove for {name}.");
        }

        public bool Apply(Dictionary<string, string> data)
        {
            bool changed = false;
            foreach (string key in remove)
                changed |= data.Remove(key);
            foreach (var pair in set)
            {
                if (!data.TryGetValue(pair.Key, out string current) || current != pair.Value)
                {
                    data[pair.Key] = pair.Value;
                    changed = true;
                }
            }
            return changed;
        }
    }
}