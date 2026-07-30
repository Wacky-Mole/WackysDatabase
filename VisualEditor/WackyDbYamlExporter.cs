using System;
using System.IO;
using System.Linq;
using UnityEngine;
using wackydatabase.Datas;
using wackydatabase.GetData;
using YamlDotNet.Serialization;

namespace wackydatabase.VisualEditor
{
    internal sealed class WackyDbYamlExporter
    {
        private readonly YamlLoader _loader = new YamlLoader();
        private readonly ISerializer _objectSerializer = new SerializerBuilder()
            .WithNewLine("\n")
            .Build();

        internal string LastError { get; private set; } = string.Empty;
        internal string LastSavedPath { get; private set; } = string.Empty;

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

        internal bool SaveItemOverwrite(
            GameObject prefab,
            string prefabName,
            string materialName,
            string[] materials,
            CustomVisual customVisual)
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
                material = customVisual == null && materials == null ? materialName : null,
                materials = customVisual == null ? materials : null,
                customVisual = customVisual
            };
            return WriteObject(WMRecipeCust.assetPathItems, "Item_" + SanitizeFileName(prefabName) + ".yml", data);
        }

        internal bool SavePieceOverwrite(
            string prefabName,
            string pieceHammer,
            string materialName,
            string damagedMaterialName)
        {
            PieceData data = new PieceData
            {
                name = prefabName,
                piecehammer = string.IsNullOrWhiteSpace(pieceHammer) ? "Hammer" : pieceHammer,
                material = string.IsNullOrWhiteSpace(materialName) ? null : materialName,
                damagedMaterial = string.IsNullOrWhiteSpace(damagedMaterialName) ? null : damagedMaterialName
            };
            return WriteObject(WMRecipeCust.assetPathPieces, "Piece_" + SanitizeFileName(prefabName) + ".yml", data);
        }

        internal bool SaveItemClone(
            GameObject originalPrefab,
            string originalPrefabName,
            string cloneName,
            string displayName,
            string materialName,
            string[] materials,
            CustomVisual customVisual)
        {
            if (!originalPrefab || !ObjectDB.instance)
            {
                return Fail("The selected item prefab or ObjectDB is unavailable.");
            }

            WItemData data = new GetDataYML().GetItemDataByName(originalPrefabName, ObjectDB.instance);
            if (data == null)
            {
                return Fail("Unable to extract all fields from item " + originalPrefabName + ".");
            }

            data.name = cloneName;
            data.clonePrefabName = originalPrefabName;
            data.m_name = displayName;
            data.material = customVisual == null && materials == null ? materialName : null;
            data.materials = customVisual == null ? materials : null;
            data.customVisual = customVisual;
            return WriteObject(WMRecipeCust.assetPathItems, "Item_" + SanitizeFileName(cloneName) + ".yml", data);
        }

        internal bool SavePieceClone(
            string originalPrefabName,
            string cloneName,
            string displayName,
            string pieceHammer,
            string materialName,
            string damagedMaterialName)
        {
            if (!ObjectDB.instance)
            {
                return Fail("ObjectDB is unavailable.");
            }

            PieceData data = new GetDataYML().GetPieceRecipeByName(originalPrefabName, ObjectDB.instance);
            if (data == null)
            {
                return Fail("Unable to extract all fields from piece " + originalPrefabName + ".");
            }

            data.name = cloneName;
            data.clonePrefabName = originalPrefabName;
            data.m_name = displayName;
            data.piecehammer = string.IsNullOrWhiteSpace(pieceHammer) ? data.piecehammer : pieceHammer;
            data.material = string.IsNullOrWhiteSpace(materialName) ? null : materialName;
            data.damagedMaterial = string.IsNullOrWhiteSpace(damagedMaterialName) ? null : damagedMaterialName;
            return WriteObject(WMRecipeCust.assetPathPieces, "Piece_" + SanitizeFileName(cloneName) + ".yml", data);
        }

        private bool WriteObject<T>(string directory, string fileName, T data)
        {
            try
            {
                WMRecipeCust.CheckModFolder();
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, fileName);
                File.WriteAllText(path, _objectSerializer.Serialize(data));
                return CompleteWrite(path);
            }
            catch (Exception exception)
            {
                return Fail(exception.Message);
            }
        }

        private bool Write<T>(string directory, string fileName, T data)
        {
            try
            {
                WMRecipeCust.CheckModFolder();
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, fileName);
                if (!_loader.Write(path, data))
                {
                    return Fail("Failed to write " + path);
                }

                return CompleteWrite(path);
            }
            catch (Exception exception)
            {
                return Fail(exception.Message);
            }
        }

        private bool CompleteWrite(string path)
        {
            if (!File.Exists(path))
            {
                return Fail("The YAML writer completed but no file was found at " + path);
            }

            LastError = string.Empty;
            LastSavedPath = path;
            WMRecipeCust.WLog.LogInfo("WackyDB Creator saved YAML: " + path);
            return true;
        }

        private bool Fail(string error)
        {
            LastError = error;
            LastSavedPath = string.Empty;
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
