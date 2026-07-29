using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase.VisualEditor
{
    internal sealed class WackyDbMaterialLibrary
    {
        private readonly Dictionary<string, MaterialInstance> _yamlMaterials =
            new Dictionary<string, MaterialInstance>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _knownNames = new List<string>();

        internal void Refresh()
        {
            _yamlMaterials.Clear();
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (WMRecipeCust.originalMaterials != null)
            {
                names.UnionWith(WMRecipeCust.originalMaterials.Keys);
            }

            if (MaterialDataManager.Instance?.materials != null)
            {
                names.UnionWith(MaterialDataManager.Instance.materials.Keys);
            }

            if (Directory.Exists(WMRecipeCust.assetPathMaterials))
            {
                foreach (string file in Directory.GetFiles(WMRecipeCust.assetPathMaterials, "*.yml", SearchOption.AllDirectories))
                {
                    try
                    {
                        string yaml = File.ReadAllText(file);
                        MaterialInstance material = DataManager<MaterialInstance>.Deserializer.Deserialize<MaterialInstance>(yaml);
                        if (material == null || string.IsNullOrWhiteSpace(material.name))
                        {
                            continue;
                        }

                        _yamlMaterials[material.name] = material;
                        names.Add(material.name);
                    }
                    catch (Exception exception)
                    {
                        WMRecipeCust.WLog.LogWarning("Unable to inspect material YAML " + file + ": " + exception.Message);
                    }
                }
            }

            _knownNames.Clear();
            _knownNames.AddRange(names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
        }

        internal List<string> Search(string text, int maximumResults)
        {
            IEnumerable<string> results = _knownNames;
            if (!string.IsNullOrWhiteSpace(text))
            {
                string query = text.Trim();
                results = results
                    .Where(name => name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderByDescending(name => name.Equals(query, StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(name => name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    .ThenBy(name => name, StringComparer.OrdinalIgnoreCase);
            }

            return results.Take(maximumResults).ToList();
        }

        internal Material GetMaterial(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (MaterialDataManager.Instance?.materials != null
                && MaterialDataManager.Instance.materials.TryGetValue(name, out Material managedMaterial))
            {
                return managedMaterial;
            }

            if (WMRecipeCust.originalMaterials != null
                && WMRecipeCust.originalMaterials.TryGetValue(name, out Material originalMaterial))
            {
                return originalMaterial;
            }

            return null;
        }

        internal bool HasMaterialYaml(string name)
        {
            return !string.IsNullOrEmpty(name) && _yamlMaterials.ContainsKey(name);
        }

        internal MaterialInstance LoadMaterialYaml(string name)
        {
            _yamlMaterials.TryGetValue(name ?? string.Empty, out MaterialInstance material);
            return material;
        }

        internal bool IsWackyMaterial(string name)
        {
            return HasMaterialYaml(name)
                || MaterialDataManager.Instance?.materials != null
                && MaterialDataManager.Instance.materials.ContainsKey(name);
        }

        internal int CountYamlReferences(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
            {
                return 0;
            }

            string valuePattern = Regex.Escape(materialName.Trim());
            Regex reference = new Regex(
                "^\\s*(?:material|damagedMaterial):\\s*[\\\"']?" + valuePattern + "[\\\"']?\\s*(?:#.*)?$",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);
            int count = 0;

            count += CountReferencesInFolder(WMRecipeCust.assetPathItems, reference);
            count += CountReferencesInFolder(WMRecipeCust.assetPathPieces, reference);
            return count;
        }

        private static int CountReferencesInFolder(string folder, Regex reference)
        {
            if (!Directory.Exists(folder))
            {
                return 0;
            }

            int count = 0;
            foreach (string file in Directory.GetFiles(folder, "*.yml", SearchOption.AllDirectories))
            {
                try
                {
                    if (reference.IsMatch(File.ReadAllText(file)))
                    {
                        count++;
                    }
                }
                catch (IOException)
                {
                }
            }

            return count;
        }
    }
}
