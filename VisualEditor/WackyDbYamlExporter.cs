using System;
using System.IO;
using System.Linq;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase.VisualEditor
{
    internal sealed class WackyDbYamlExporter
    {
        private readonly YamlLoader _loader = new YamlLoader();

        internal string LastError { get; private set; } = string.Empty;

        internal bool SaveMaterial(MaterialInstance material)
        {
            if (material == null || string.IsNullOrWhiteSpace(material.name) || string.IsNullOrWhiteSpace(material.original))
            {
                return Fail("Material name and source material are required.");
            }

            return Write(
                WMRecipeCust.assetPathMaterials,
                SanitizeFileName(material.name) + ".yml",
                material);
        }

        internal bool SaveItemOverwrite(GameObject prefab, string prefabName, string materialName)
        {
            ItemDrop itemDrop = prefab ? prefab.GetComponent<ItemDrop>() : null;
            if (!itemDrop)
            {
                return Fail("The selected prefab is not an item.");
            }

            WItemData data = new WItemData
            {
                name = prefabName,
                m_weight = itemDrop.m_itemData.m_shared.m_weight,
                material = materialName
            };
            return Write(WMRecipeCust.assetPathItems, "Item_" + SanitizeFileName(prefabName) + ".yml", data);
        }

        internal bool SavePieceOverwrite(string prefabName, string pieceHammer, string materialName)
        {
            PieceData data = new PieceData
            {
                name = prefabName,
                piecehammer = string.IsNullOrWhiteSpace(pieceHammer) ? "Hammer" : pieceHammer,
                material = materialName
            };
            return Write(WMRecipeCust.assetPathPieces, "Piece_" + SanitizeFileName(prefabName) + ".yml", data);
        }

        internal bool SaveItemClone(
            GameObject originalPrefab,
            string originalPrefabName,
            string cloneName,
            string displayName,
            string materialName)
        {
            ItemDrop itemDrop = originalPrefab ? originalPrefab.GetComponent<ItemDrop>() : null;
            if (!itemDrop)
            {
                return Fail("The selected prefab is not an item.");
            }

            WItemData data = new WItemData
            {
                name = cloneName,
                clonePrefabName = originalPrefabName,
                m_name = displayName,
                m_weight = itemDrop.m_itemData.m_shared.m_weight,
                material = materialName
            };
            return Write(WMRecipeCust.assetPathItems, "Item_" + SanitizeFileName(cloneName) + ".yml", data);
        }

        internal bool SavePieceClone(
            string originalPrefabName,
            string cloneName,
            string displayName,
            string pieceHammer,
            string materialName)
        {
            PieceData data = new PieceData
            {
                name = cloneName,
                clonePrefabName = originalPrefabName,
                m_name = displayName,
                piecehammer = string.IsNullOrWhiteSpace(pieceHammer) ? "Hammer" : pieceHammer,
                material = materialName
            };
            return Write(WMRecipeCust.assetPathPieces, "Piece_" + SanitizeFileName(cloneName) + ".yml", data);
        }

        private bool Write<T>(string directory, string fileName, T data)
        {
            try
            {
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, fileName);
                if (!_loader.Write(path, data))
                {
                    return Fail("Failed to write " + path);
                }

                LastError = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                return Fail(exception.Message);
            }
        }

        private bool Fail(string error)
        {
            LastError = error;
            WMRecipeCust.WLog.LogWarning("WackyDB Creator save failed: " + error);
            return false;
        }

        private static string SanitizeFileName(string value)
        {
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            return new string(value.Trim().Select(character =>
                invalidCharacters.Contains(character) ? '_' : character).ToArray());
        }
    }
}
