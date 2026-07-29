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

        internal void ClearSelection()
        {
            SelectedObject = null;
            SelectedRenderer = null;
            SelectedMaterialSlot = 0;
            RendererInfos.Clear();
            ClearMaterialSelection();
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
    }
}
