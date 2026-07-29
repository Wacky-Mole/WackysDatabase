using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace wackydatabase.VisualEditor
{
    internal sealed class WackyDbTextureBrowser
    {
        private readonly List<string> _textureNames = new List<string>();

        internal void Refresh()
        {
            _textureNames.Clear();
            if (!Directory.Exists(WMRecipeCust.assetPathTextures))
            {
                return;
            }

            _textureNames.AddRange(Directory
                .GetFiles(WMRecipeCust.assetPathTextures, "*.png", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
        }

        internal List<string> Search(string text, int maximumResults)
        {
            IEnumerable<string> results = _textureNames;
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

        internal Texture2D GetTexture(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            Texture2D texture = TextureDataManager.GetTexture(name);
            if (texture)
            {
                texture.name = name;
            }
            return texture;
        }
    }
}
