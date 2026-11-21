using System;
using System.IO;
using System.Linq;
using UnityEngine;
using wackydatabase.Datas;
using wackydatabase.GetData;
using YamlDotNet.Serialization;

namespace wackydatabase.VisualEditor
{
    internal sealed class WackyDbCloneOptions
    {
        internal string Name;
        internal string? DisplayName;
        internal string Description;
        internal float? Weight;
        internal int? StackSize;
        internal float? PieceHealth;
        internal int? Comfort;
    }

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
            CustomVisual customVisual,
            string customIcon)
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
                material = materials == null ? materialName : null,
                materials = materials,
                customVisual = customVisual,
                customIcon = string.IsNullOrWhiteSpace(customIcon) ? null : customIcon
            };
            return WriteObject(WMRecipeCust.assetPathItems, "Item_" + SanitizeFileName(prefabName) + ".yml", data);
        }

        internal bool SavePieceOverwrite(
            string prefabName,
            string pieceHammer,
            string materialName,
            string damagedMaterialName,
            string customIcon)
        {
            PieceData data = new PieceData
            {
                name = prefabName,
                piecehammer = string.IsNullOrWhiteSpace(pieceHammer) ? "Hammer" : pieceHammer,
                material = string.IsNullOrWhiteSpace(materialName) ? null : materialName,
                damagedMaterial = string.IsNullOrWhiteSpace(damagedMaterialName) ? null : damagedMaterialName,
                customIcon = string.IsNullOrWhiteSpace(customIcon) ? null : customIcon
            };
            return WriteObject(WMRecipeCust.assetPathPieces, "Piece_" + SanitizeFileName(prefabName) + ".yml", data);
        }

        internal bool SaveItemClone(
            GameObject originalPrefab,
            string originalPrefabName,
            WackyDbCloneOptions options,
            string materialName,
            string[] materials,
            CustomVisual customVisual,
            string customIcon)
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

            data.name = options.Name;
            data.clonePrefabName = originalPrefabName;
            data.m_name = options.DisplayName ?? data.m_name;
            data.m_description = options.Description ?? data.m_description;
            data.m_weight = options.Weight ?? data.m_weight;
            data.m_maxStackSize = options.StackSize ?? data.m_maxStackSize;
            if (materialName != null || materials != null || customVisual != null)
            {
                data.material = materials == null ? materialName : null;
                data.materials = materials;
                data.customVisual = customVisual;
            }
            if (!string.IsNullOrWhiteSpace(customIcon))
            {
                data.customIcon = customIcon;
            }
            return WriteObject(WMRecipeCust.assetPathItems, GetCloneFileName(WackyDbObjectType.Item, options.Name), data, createNew: true);
        }

        internal bool SavePieceClone(
            string originalPrefabName,
            WackyDbCloneOptions options,
            string pieceHammer,
            string materialName,
            string damagedMaterialName,
            string customIcon)
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

            if (options.PieceHealth.HasValue && data.wearNTearData == null)
            {
                return Fail("The source piece does not have editable durability.");
            }

            data.name = options.Name;
            data.clonePrefabName = originalPrefabName;
            data.m_name = options.DisplayName ?? data.m_name;
            data.m_description = options.Description ?? data.m_description;
            data.piecehammer = string.IsNullOrWhiteSpace(pieceHammer) ? data.piecehammer : pieceHammer;
            if (options.PieceHealth.HasValue)
            {
                data.wearNTearData.health = options.PieceHealth;
            }
            if (options.Comfort.HasValue)
            {
                data.comfort ??= new ComfortData();
                data.comfort.comfort = options.Comfort;
            }
            data.material = materialName ?? data.material;
            data.damagedMaterial = damagedMaterialName ?? data.damagedMaterial;
            if (!string.IsNullOrWhiteSpace(customIcon))
            {
                data.customIcon = customIcon;
            }
            return WriteObject(WMRecipeCust.assetPathPieces, GetCloneFileName(WackyDbObjectType.Piece, options.Name), data, createNew: true);
        }

        internal bool CloneFileExists(WackyDbObjectType type, string cloneName)
        {
            string directory = type == WackyDbObjectType.Item ? WMRecipeCust.assetPathItems : WMRecipeCust.assetPathPieces;
            return File.Exists(Path.Combine(directory, GetCloneFileName(type, cloneName)));
        }

        private static string GetCloneFileName(WackyDbObjectType type, string cloneName)
        {
            return $"{type}_{SanitizeFileName(cloneName)}.yml";
        }

        private bool WriteObject<T>(string directory, string fileName, T data, bool createNew = false)
        {
            try
            {
                WMRecipeCust.CheckModFolder();
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, fileName);
                string yaml = _objectSerializer.Serialize(data);
                if (createNew)
                {
                    using FileStream stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                    using StreamWriter writer = new StreamWriter(stream);
                    writer.Write(yaml);
                }
                else
                {
                    File.WriteAllText(path, yaml);
                }
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
