using System.Collections.Generic;
using System.Collections.Generic;
using UnityEngine;
using wackydatabase.Datas;

namespace wackydatabase.VisualEditor
{
    internal enum WackyDbObjectType
    {
        Unknown,
        Item,
        Piece,
        Prefab
    }

    internal sealed class WackyDbMaterialEditState
    {
        internal Renderer Renderer;
        internal int Slot;
        internal string OriginalMaterialName = string.Empty;
        internal string SelectedSharedMaterialName = string.Empty;
        internal string NewMaterialName = string.Empty;
        internal Material WorkingBaseMaterial;
        internal MaterialData WorkingChanges = new MaterialData();
        internal bool IsEditingExistingSharedMaterial;
        internal bool IsCreatingNewMaterial;
        internal bool MaterialChangesDirty;
        internal bool SavedToYaml;
    }

    internal enum WackyDbMaterialRoute
    {
        Material,
        Base,
        Chest,
        Legs
    }

    internal enum WackyDbPieceMaterialRoute
    {
        FullHealth,
        Damaged
    }

    internal sealed class WackyDbObjectCandidate
    {
        internal string Name = string.Empty;
        internal string DisplayName = string.Empty;
        internal GameObject Prefab;
        internal WackyDbObjectType Type;
        internal string PieceHammer = string.Empty;
    }

    internal sealed class WackyDbEditorSession
    {
        internal WackyDbObjectCandidate SelectedObject;
        internal Renderer SelectedRenderer;
        internal int SelectedMaterialSlot;
        internal List<WackyDbRendererInfo> RendererInfos = new List<WackyDbRendererInfo>();
        internal string OriginalMaterialName = string.Empty;
        internal string SelectedSharedMaterialName = string.Empty;
        internal string NewMaterialName = string.Empty;
        internal Material WorkingBaseMaterial;
        internal MaterialData WorkingChanges = new MaterialData();
        internal bool IsEditingExistingSharedMaterial;
        internal bool IsCreatingNewMaterial;
        internal bool MaterialChangesDirty;
        internal string CloneName = string.Empty;
        internal string DisplayName = string.Empty;
        internal WackyDbMaterialRoute MaterialRoute = WackyDbMaterialRoute.Material;
        internal string BaseMaterialName = string.Empty;
        internal string ChestMaterialName = string.Empty;
        internal string LegsMaterialName = string.Empty;
        internal string StandardMaterialName = string.Empty;
        internal WackyDbPieceMaterialRoute PieceMaterialRoute = WackyDbPieceMaterialRoute.FullHealth;
        internal string PieceMaterialName = string.Empty;
        internal string DamagedPieceMaterialName = string.Empty;
        internal string SnapshotIconName = string.Empty;
        internal Dictionary<string, WackyDbMaterialEditState> MaterialEdits = new Dictionary<string, WackyDbMaterialEditState>();

        internal void ClearSelection()
        {
            SelectedObject = null;
            SelectedRenderer = null;
            SelectedMaterialSlot = 0;
            RendererInfos.Clear();
            ClearMaterialSelection();
            MaterialRoute = WackyDbMaterialRoute.Material;
            BaseMaterialName = string.Empty;
            ChestMaterialName = string.Empty;
            LegsMaterialName = string.Empty;
            StandardMaterialName = string.Empty;
            PieceMaterialRoute = WackyDbPieceMaterialRoute.FullHealth;
            PieceMaterialName = string.Empty;
            DamagedPieceMaterialName = string.Empty;
            SnapshotIconName = string.Empty;
            MaterialEdits.Clear();
        }

        internal void ClearMaterialSelection()
        {
            SelectedRenderer = null;
            SelectedMaterialSlot = 0;
            OriginalMaterialName = string.Empty;
            SelectedSharedMaterialName = string.Empty;
            NewMaterialName = string.Empty;
            WorkingBaseMaterial = null;
            WorkingChanges = new MaterialData();
            IsEditingExistingSharedMaterial = false;
            IsCreatingNewMaterial = false;
            MaterialChangesDirty = false;
        }

        internal static string GetMaterialEditKey(Renderer renderer, int slot)
        {
            return renderer ? renderer.GetInstanceID() + ":" + slot : string.Empty;
        }

        internal WackyDbMaterialEditState CaptureMaterialEdit(bool savedToYaml = false)
        {
            return new WackyDbMaterialEditState
            {
                Renderer = SelectedRenderer,
                Slot = SelectedMaterialSlot,
                OriginalMaterialName = OriginalMaterialName,
                SelectedSharedMaterialName = SelectedSharedMaterialName,
                NewMaterialName = NewMaterialName,
                WorkingBaseMaterial = WorkingBaseMaterial,
                WorkingChanges = WorkingChanges,
                IsEditingExistingSharedMaterial = IsEditingExistingSharedMaterial,
                IsCreatingNewMaterial = IsCreatingNewMaterial,
                MaterialChangesDirty = MaterialChangesDirty,
                SavedToYaml = savedToYaml
            };
        }

        internal void RestoreMaterialEdit(WackyDbMaterialEditState state)
        {
            OriginalMaterialName = state.OriginalMaterialName;
            SelectedSharedMaterialName = state.SelectedSharedMaterialName;
            NewMaterialName = state.NewMaterialName;
            WorkingBaseMaterial = state.WorkingBaseMaterial;
            WorkingChanges = state.WorkingChanges;
            IsEditingExistingSharedMaterial = state.IsEditingExistingSharedMaterial;
            IsCreatingNewMaterial = state.IsCreatingNewMaterial;
            MaterialChangesDirty = state.MaterialChangesDirty;
        }
    }
}
