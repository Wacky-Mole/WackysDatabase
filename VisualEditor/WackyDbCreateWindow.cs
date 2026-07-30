using System;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using wackydatabase.Datas;

namespace wackydatabase.VisualEditor
{
    internal sealed class WackyDbCreateWindow : MonoBehaviour
    {
        private const int WindowId = 19850423;
        private const int MaximumVisibleResults = 250;

        private static WackyDbCreateWindow _instance;

        private readonly WackyDbEditorSession _session = new WackyDbEditorSession();
        private readonly WackyDbObjectSelector _selector = new WackyDbObjectSelector();
        private readonly WackyDbMaterialSlotInspector _inspector = new WackyDbMaterialSlotInspector();
        private readonly WackyDbMaterialLibrary _materialLibrary = new WackyDbMaterialLibrary();
        private readonly WackyDbYamlExporter _exporter = new WackyDbYamlExporter();
        private readonly WackyDbTextureBrowser _textureBrowser = new WackyDbTextureBrowser();
        private WackyDbPreviewRenderer _preview;

        private Rect _windowRect = new Rect(80f, 60f, 1250f, 700f);
        private Rect _normalWindowRect;
        private Vector2 _resultScroll;
        private Vector2 _detailScroll;
        private string _searchText = string.Empty;
        private string _status = string.Empty;
        private string _materialSearch = string.Empty;
        private string _materialLibrarySelection = string.Empty;
        private int _sharedReferenceCount;
        private bool _previewDragging;
        private bool _fullScreen;
        private string _textureSearch = string.Empty;
        private string _selectedTextureName = string.Empty;
        private string _selectedTextureProperty = string.Empty;
        private bool _showSharedMaterialLibrary;
        private bool _showColorEditor = true;
        private bool _showFloatEditor;
        private bool _showTextureEditor;
        private bool _showRendererSlots = true;
        private WackyDbObjectCandidate _pendingSelection;
        private bool _pendingClose;
        private bool _confirmOverwrite;
        private bool _confirmOverwriteReload;

        internal static void Open(string prefabName = null)
        {
            WackyDbCreateWindow window = EnsureInstance();
            if (!window)
            {
                return;
            }

            window.CenterWindow();
            window.enabled = true;
            window.RefreshCandidates();

            if (!string.IsNullOrWhiteSpace(prefabName))
            {
                window._searchText = prefabName.Trim();
                WackyDbObjectCandidate candidate = window._selector.Resolve(window._searchText);
                if (candidate != null)
                {
                    window.Select(candidate);
                }
                else
                {
                    window._status = "Prefab not found: " + window._searchText;
                }
            }
            else if (window._session.SelectedObject != null)
            {
                window.Select(window._session.SelectedObject);
            }
        }

        internal static void Toggle()
        {
            WackyDbCreateWindow window = EnsureInstance();
            if (!window)
            {
                return;
            }

            if (window.enabled)
            {
                window.Close();
            }
            else
            {
                Open();
            }
        }

        internal static void ToggleWithGameUi(string prefabName = null)
        {
            WackyDbCreateWindow window = EnsureInstance();
            if (!window)
            {
                return;
            }

            bool opening = !window.enabled || !string.IsNullOrWhiteSpace(prefabName);
            if (!string.IsNullOrWhiteSpace(prefabName))
            {
                Open(prefabName);
            }
            else
            {
                Toggle();
            }

            WMRecipeCust.context.StartCoroutine(PrepareGameUi(opening));
        }

        private static System.Collections.IEnumerator PrepareGameUi(bool opening)
        {
            yield return null;

            if (global::Console.instance && global::Console.IsVisible())
            {
                global::Console.instance.m_chatWindow.gameObject.SetActive(false);
            }

            yield return null;

            if (opening && InventoryGui.instance)
            {
                InventoryGui.instance.Show(null);
            }
        }

        private static WackyDbCreateWindow EnsureInstance()
        {
            if (_instance)
            {
                return _instance;
            }

            if (!WMRecipeCust.context)
            {
                WMRecipeCust.WLog.LogWarning("Unable to open WackyDB Creator because the plugin is not initialized.");
                return null;
            }

            _instance = WMRecipeCust.context.GetComponent<WackyDbCreateWindow>();
            if (!_instance)
            {
                _instance = WMRecipeCust.context.gameObject.AddComponent<WackyDbCreateWindow>();
                _instance.enabled = false;
            }

            return _instance;
        }

        private void OnGUI()
        {
            if (_fullScreen)
            {
                _windowRect = new Rect(10f, 10f, Mathf.Max(300f, Screen.width - 20f), Mathf.Max(300f, Screen.height - 20f));
            }
            else
            {
                _windowRect.width = Mathf.Min(_windowRect.width, Screen.width - 20f);
                _windowRect.height = Mathf.Min(_windowRect.height, Screen.height - 20f);
            }
            _windowRect = GUILayout.Window(WindowId, _windowRect, DrawWindow, "WackyDB Creator");
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, Mathf.Max(0f, Screen.width - _windowRect.width));
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, Mathf.Max(0f, Screen.height - _windowRect.height));
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();
            DrawToolbar();
            GUILayout.Label("1. Choose an object  >  2. Choose a material slot  >  3. Edit the material  >  4. Save overwrite or clone YAML");

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            DrawObjectResults();
            GUILayout.Space(8f);
            DrawPreview();
            GUILayout.Space(8f);
            DrawSelectionDetails();
            GUILayout.EndHorizontal();

            DrawSavePanel();
            DrawConfirmationPanel();

            if (!string.IsNullOrEmpty(_status))
            {
                GUILayout.Space(4f);
                GUILayout.Label(_status);
            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width - 45f, 24f));
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Find object", GUILayout.Width(70f));
            _searchText = GUILayout.TextField(_searchText, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Clear", GUILayout.Width(50f)))
            {
                _searchText = string.Empty;
                _resultScroll = Vector2.zero;
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(75f)))
            {
                RefreshCandidates();
            }

            if (GUILayout.Button(_fullScreen ? "Windowed" : "Full Screen", GUILayout.Width(85f)))
            {
                ToggleFullScreen();
            }

            if (GUILayout.Button("X", GUILayout.Width(28f)))
            {
                Close();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawObjectResults()
        {
            List<WackyDbObjectCandidate> results = _selector.Search(_searchText);

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(315f), GUILayout.ExpandHeight(true));
            GUILayout.Label("Step 1 — Choose Object (" + results.Count + ")");
            _resultScroll = GUILayout.BeginScrollView(_resultScroll, GUILayout.ExpandHeight(true));

            int visibleCount = Mathf.Min(results.Count, MaximumVisibleResults);
            for (int index = 0; index < visibleCount; index++)
            {
                WackyDbObjectCandidate candidate = results[index];
                bool selected = _session.SelectedObject == candidate;
                string label = (selected ? "? " : string.Empty) + candidate.Name + "  [" + candidate.Type + "]";
                if (GUILayout.Button(label, GUILayout.Height(24f)))
                {
                    Select(candidate);
                }

                if (!string.IsNullOrEmpty(candidate.DisplayName) && candidate.DisplayName != candidate.Name)
                {
                    GUILayout.Label("  " + candidate.DisplayName);
                }
            }

            if (results.Count > MaximumVisibleResults)
            {
                GUILayout.Label("Refine the search to view the remaining results.");
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawPreview()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(310f), GUILayout.ExpandHeight(true));
            GUILayout.Label("Preview — drag with the mouse to rotate");

            Rect previewRect = GUILayoutUtility.GetRect(290f, 290f, GUILayout.ExpandWidth(true));
            if (_preview != null && _preview.HasPreview)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    _preview.Render();
                }

                GUI.DrawTexture(previewRect, _preview.Texture, ScaleMode.ScaleToFit, false);
                HandlePreviewInput(previewRect);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<"))
                {
                    _preview.Rotate(-15f);
                }
                if (GUILayout.Button(">"))
                {
                    _preview.Rotate(15f);
                }
                if (GUILayout.Button("Up"))
                {
                    _preview.Rotate(0f, -10f);
                }
                if (GUILayout.Button("Down"))
                {
                    _preview.Rotate(0f, 10f);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Zoom +"))
                {
                    _preview.Zoom(-0.15f);
                }
                if (GUILayout.Button("Zoom -"))
                {
                    _preview.Zoom(0.15f);
                }
                if (GUILayout.Button("Reset View"))
                {
                    _preview.ResetView();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUI.Box(previewRect, "Select a previewable prefab");
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Preview clones are isolated from gameplay and source materials.");
            GUILayout.EndVertical();
        }

        private void HandlePreviewInput(Rect previewRect)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && previewRect.Contains(current.mousePosition))
            {
                _previewDragging = true;
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && _previewDragging)
            {
                _preview.Rotate(current.delta.x * 0.6f, -current.delta.y * 0.45f);
                current.Use();
            }
            else if (current.type == EventType.MouseUp && _previewDragging)
            {
                _previewDragging = false;
                current.Use();
            }
        }

        private void DrawSelectionDetails()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (_session.SelectedObject == null)
            {
                GUILayout.Label("Select an item, piece, or prefab to inspect its materials.");
                GUILayout.EndVertical();
                return;
            }

            WackyDbObjectCandidate selected = _session.SelectedObject;
            GUILayout.Label("Prefab: " + selected.Name);
            GUILayout.Label("Type: " + selected.Type);
            if (!string.IsNullOrEmpty(selected.DisplayName))
            {
                GUILayout.Label("Display name: " + selected.DisplayName);
            }

            if (selected.Type == WackyDbObjectType.Piece)
            {
                GUILayout.Label("Piece hammer: " + selected.PieceHammer);
            }

            _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUILayout.ExpandHeight(true));
            _showRendererSlots = GUILayout.Toggle(
                _showRendererSlots,
                (_showRendererSlots ? "? " : "? ") + "Step 2 — Renderer / Material Slots (" + _session.RendererInfos.Count + ")",
                GUI.skin.button);
            if (_showRendererSlots)
            {
                foreach (WackyDbRendererInfo rendererInfo in _session.RendererInfos)
                {
                    DrawRendererInfo(rendererInfo);
                }
            }

            GUILayout.Space(4f);
            DrawMaterialEditor();
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void DrawMaterialEditor()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Step 3 — Material Editor");
            if (GUILayout.Button("Reset preview materials to prefab defaults"))
            {
                ResetPrefabPreview();
                GUILayout.EndVertical();
                return;
            }

            if (!_session.SelectedRenderer || !_session.WorkingBaseMaterial)
            {
                GUILayout.Label("Select a material slot below to begin editing.");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label("Source material: " + _session.WorkingBaseMaterial.name);
            GUILayout.Label("The source material is copied as the base for preview and editing.");

            _showSharedMaterialLibrary = GUILayout.Toggle(
                _showSharedMaterialLibrary,
                (_showSharedMaterialLibrary ? "? " : "? ") + "Use an Existing Shared Material",
                GUI.skin.button);
            if (_showSharedMaterialLibrary)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Search", GUILayout.Width(50f));
                _materialSearch = GUILayout.TextField(_materialSearch);
                if (GUILayout.Button("Clear", GUILayout.Width(50f)))
                {
                    _materialSearch = string.Empty;
                    _materialLibrarySelection = string.Empty;
                }
                GUILayout.EndHorizontal();

                List<string> matches = _materialLibrary.Search(_materialSearch, 6);
                foreach (string materialName in matches)
                {
                    string prefix = materialName == _materialLibrarySelection ? "? " : string.Empty;
                    if (GUILayout.Button(prefix + materialName))
                    {
                        _materialLibrarySelection = materialName;
                        _materialSearch = materialName;
                    }
                }

                bool pickerEnabled = GUI.enabled;
                GUI.enabled = !string.IsNullOrEmpty(_materialLibrarySelection);
                if (GUILayout.Button("Apply Selected Shared Material"))
                {
                    UseSharedMaterial(_materialLibrarySelection);
                }
                GUI.enabled = pickerEnabled;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("New shared material name", GUILayout.Width(155f));
            _session.NewMaterialName = GUILayout.TextField(_session.NewMaterialName);
            GUILayout.EndHorizontal();
            GUILayout.Label("This names the reusable Material YAML.");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("New Shared Material"))
            {
                BeginNewSharedMaterial(false);
            }
            if (GUILayout.Button("Duplicate Current"))
            {
                BeginNewSharedMaterial(true);
            }
            GUILayout.EndHorizontal();

            if (_session.IsEditingExistingSharedMaterial)
            {
                GUILayout.Label("Editing shared material: " + _session.SelectedSharedMaterialName);
                if (_sharedReferenceCount > 1)
                {
                    GUILayout.Label("Warning: referenced by " + _sharedReferenceCount + " object YAML files. Duplicate is recommended.");
                }
            }
            else if (_session.IsCreatingNewMaterial)
            {
                GUILayout.Label("Creating: " + _session.NewMaterialName);
            }

            DrawMaterialRouteEditor();
            DrawPieceMaterialRouteEditor();

            _showColorEditor = GUILayout.Toggle(
                _showColorEditor,
                (_showColorEditor ? "? " : "? ") + "Colors",
                GUI.skin.button);
            if (_showColorEditor)
            {
                DrawWorkingColors();
            }

            _showTextureEditor = GUILayout.Toggle(
                _showTextureEditor,
                (_showTextureEditor ? "? " : "? ") + "Textures",
                GUI.skin.button);
            if (_showTextureEditor)
            {
                DrawTextureEditor();
            }

            _showFloatEditor = GUILayout.Toggle(
                _showFloatEditor,
                (_showFloatEditor ? "? " : "? ") + "Shader Floats / Ranges",
                GUI.skin.button);
            if (_showFloatEditor)
            {
                DrawWorkingFloats();
            }
            GUILayout.EndVertical();
        }

        private void DrawPieceMaterialRouteEditor()
        {
            if (_session.SelectedObject?.Type != WackyDbObjectType.Piece)
            {
                return;
            }

            GUILayout.Space(4f);
            GUILayout.Label("Piece Material State");
            GUILayout.BeginHorizontal();
            DrawPieceMaterialRouteButton("Full Health", WackyDbPieceMaterialRoute.FullHealth);
            DrawPieceMaterialRouteButton("Damaged", WackyDbPieceMaterialRoute.Damaged);
            GUILayout.EndHorizontal();

            GUILayout.Label(_session.PieceMaterialRoute == WackyDbPieceMaterialRoute.FullHealth
                ? "The current material will be saved to material."
                : "The current material will be saved to damagedMaterial.");

            if (GUILayout.Button("Assign Current Material to " +
                (_session.PieceMaterialRoute == WackyDbPieceMaterialRoute.FullHealth ? "Full Health" : "Damaged")))
            {
                AssignCurrentPieceMaterial();
            }

            GUILayout.Label("Assigned piece materials:");
            GUILayout.Label("  Full Health: " + EmptyAsNone(_session.PieceMaterialName));
            GUILayout.Label("  Damaged: " + EmptyAsNone(_session.DamagedPieceMaterialName));
            if (!string.IsNullOrEmpty(_session.PieceMaterialName)
                || !string.IsNullOrEmpty(_session.DamagedPieceMaterialName))
            {
                if (GUILayout.Button("Clear Piece Material Assignments"))
                {
                    _session.PieceMaterialName = string.Empty;
                    _session.DamagedPieceMaterialName = string.Empty;
                }
            }
        }

        private void DrawPieceMaterialRouteButton(string label, WackyDbPieceMaterialRoute route)
        {
            string prefix = _session.PieceMaterialRoute == route ? "? " : string.Empty;
            if (GUILayout.Button(prefix + label))
            {
                _session.PieceMaterialRoute = route;
            }
        }

        private void AssignCurrentPieceMaterial()
        {
            string materialName = GetActiveMaterialName();
            if (string.IsNullOrWhiteSpace(materialName))
            {
                _status = "Choose or name a shared material before assigning a piece state.";
                return;
            }

            if ((_session.IsCreatingNewMaterial || _session.MaterialChangesDirty) && !SaveMaterialYaml(false))
            {
                return;
            }
            materialName = GetActiveMaterialName();

            if (_session.PieceMaterialRoute == WackyDbPieceMaterialRoute.FullHealth)
            {
                _session.PieceMaterialName = materialName;
            }
            else
            {
                _session.DamagedPieceMaterialName = materialName;
            }
            _status = "Assigned " + materialName + " to the piece's " +
                (_session.PieceMaterialRoute == WackyDbPieceMaterialRoute.FullHealth ? "full-health" : "damaged") +
                " state.";
        }

        private void DrawWorkingFloats()
        {
            Material material = _session.WorkingBaseMaterial;
            if (!material || !material.shader || _session.WorkingChanges.floats.Count == 0)
            {
                GUILayout.Label("No float or range properties were found on this shader.");
                return;
            }

            Shader shader = material.shader;
            bool changed = false;
            for (int index = 0; index < shader.GetPropertyCount(); index++)
            {
                ShaderPropertyType type = shader.GetPropertyType(index);
                if (type != ShaderPropertyType.Float && type != ShaderPropertyType.Range)
                {
                    continue;
                }

                string propertyName = shader.GetPropertyName(index);
                if (!_session.WorkingChanges.floats.TryGetValue(propertyName, out float value))
                {
                    continue;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(propertyName, GUILayout.Width(150f));
                float updated;
                if (type == ShaderPropertyType.Range)
                {
                    Vector2 limits = shader.GetPropertyRangeLimits(index);
                    updated = GUILayout.HorizontalSlider(value, limits.x, limits.y);
                }
                else
                {
                    updated = value;
                    if (GUILayout.Button("-", GUILayout.Width(28f)))
                    {
                        updated -= GetFloatStep(value);
                    }
                    if (GUILayout.Button("+", GUILayout.Width(28f)))
                    {
                        updated += GetFloatStep(value);
                    }
                }
                GUILayout.Label(updated.ToString("0.###"), GUILayout.Width(55f));
                GUILayout.EndHorizontal();

                if (!Mathf.Approximately(updated, value))
                {
                    _session.WorkingChanges.floats[propertyName] = updated;
                    changed = true;
                }
            }

            if (changed)
            {
                EnsureNewMaterialForChange();
                ApplyWorkingMaterial();
            }
        }

        private static float GetFloatStep(float value)
        {
            return Mathf.Max(0.01f, Mathf.Abs(value) * 0.05f);
        }

        private void DrawMaterialRouteEditor()
        {
            if (_session.SelectedObject?.Type != WackyDbObjectType.Item)
            {
                return;
            }

            GUILayout.Space(4f);
            GUILayout.Label("Item Material Output Route");
            GUILayout.BeginHorizontal();
            DrawMaterialRouteButton("Standard", WackyDbMaterialRoute.Material);
            DrawMaterialRouteButton("Base", WackyDbMaterialRoute.Base);
            DrawMaterialRouteButton("Chest Armor", WackyDbMaterialRoute.Chest);
            DrawMaterialRouteButton("Leg Armor", WackyDbMaterialRoute.Legs);
            GUILayout.EndHorizontal();

            if (_session.MaterialRoute != WackyDbMaterialRoute.Material)
            {
                if (GUILayout.Button("Assign Current Material to " + _session.MaterialRoute))
                {
                    AssignCurrentMaterialRoute();
                }
            }

            if (!string.IsNullOrEmpty(_session.BaseMaterialName)
                || !string.IsNullOrEmpty(_session.ChestMaterialName)
                || !string.IsNullOrEmpty(_session.LegsMaterialName))
            {
                GUILayout.Label("Assigned armor materials:");
                GUILayout.Label("  Base: " + EmptyAsNone(_session.BaseMaterialName));
                GUILayout.Label("  Chest: " + EmptyAsNone(_session.ChestMaterialName));
                GUILayout.Label("  Legs: " + EmptyAsNone(_session.LegsMaterialName));
                if (GUILayout.Button("Clear Armor Assignments"))
                {
                    _session.BaseMaterialName = string.Empty;
                    _session.ChestMaterialName = string.Empty;
                    _session.LegsMaterialName = string.Empty;
                }
            }

            switch (_session.MaterialRoute)
            {
                case WackyDbMaterialRoute.Base:
                    GUILayout.Label("Saves as customVisual.base_mat for the item's rendered model.");
                    break;
                case WackyDbMaterialRoute.Chest:
                    GUILayout.Label("Saves as customVisual.chest and uses the material's _ChestTex texture.");
                    SelectPreferredArmorTexture("_ChestTex");
                    break;
                case WackyDbMaterialRoute.Legs:
                    GUILayout.Label("Saves as customVisual.legs and uses the material's leg texture property.");
                    SelectPreferredArmorTexture("_LegsTex", "_LegTex");
                    break;
                default:
                    GUILayout.Label("Saves to the standard material field. Recommended for most items.");
                    break;
            }
        }

        private void DrawMaterialRouteButton(string label, WackyDbMaterialRoute route)
        {
            string prefix = _session.MaterialRoute == route ? "? " : string.Empty;
            if (GUILayout.Button(prefix + label))
            {
                _session.MaterialRoute = route;
                _selectedTextureProperty = string.Empty;
            }
        }

        private void AssignCurrentMaterialRoute()
        {
            string materialName = GetActiveMaterialName();
            if (string.IsNullOrWhiteSpace(materialName))
            {
                _status = "Choose or name a shared material before assigning an armor route.";
                return;
            }

            if ((_session.IsCreatingNewMaterial || _session.MaterialChangesDirty) && !SaveMaterialYaml(false))
            {
                return;
            }
            materialName = GetActiveMaterialName();

            switch (_session.MaterialRoute)
            {
                case WackyDbMaterialRoute.Base:
                    _session.BaseMaterialName = materialName;
                    break;
                case WackyDbMaterialRoute.Chest:
                    _session.ChestMaterialName = materialName;
                    break;
                case WackyDbMaterialRoute.Legs:
                    _session.LegsMaterialName = materialName;
                    break;
            }
            _status = "Assigned " + materialName + " to " + _session.MaterialRoute + ".";
        }

        private static string EmptyAsNone(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value;
        }

        private void SelectPreferredArmorTexture(params string[] propertyNames)
        {
            if (!string.IsNullOrEmpty(_selectedTextureProperty) || !_session.WorkingBaseMaterial)
            {
                return;
            }

            List<string> available = GetTexturePropertyNames(_session.WorkingBaseMaterial);
            foreach (string propertyName in propertyNames)
            {
                if (available.Contains(propertyName))
                {
                    _selectedTextureProperty = propertyName;
                    _showTextureEditor = true;
                    return;
                }
            }
        }

        private void DrawTextureEditor()
        {
            List<string> properties = GetTexturePropertyNames(_session.WorkingBaseMaterial);
            if (properties.Count == 0)
            {
                GUILayout.Label("Texture properties: none");
                return;
            }

            GUILayout.Space(4f);
            GUILayout.Label("Texture Properties");
            foreach (string propertyName in properties)
            {
                bool hasChange = _session.WorkingChanges.textures.ContainsKey(propertyName);
                string prefix = propertyName == _selectedTextureProperty ? "> " : string.Empty;
                string suffix = hasChange ? "  [changed]" : string.Empty;
                if (GUILayout.Button(prefix + propertyName + suffix))
                {
                    _selectedTextureProperty = propertyName;
                }
            }

            if (string.IsNullOrEmpty(_selectedTextureProperty))
            {
                GUILayout.Label("Select a shader texture property.");
                return;
            }

            Texture currentTexture = _session.WorkingChanges.textures.TryGetValue(
                _selectedTextureProperty,
                out Texture2D changedTexture)
                ? changedTexture
                : _session.WorkingBaseMaterial.GetTexture(_selectedTextureProperty);
            GUILayout.Label("Property: " + _selectedTextureProperty);
            GUILayout.Label("Current: " + (currentTexture ? currentTexture.name : "<none>"));

            _textureSearch = GUILayout.TextField(_textureSearch);
            foreach (string textureName in _textureBrowser.Search(_textureSearch, 8))
            {
                string prefix = textureName == _selectedTextureName ? "> " : string.Empty;
                if (GUILayout.Button(prefix + textureName))
                {
                    _selectedTextureName = textureName;
                    _textureSearch = textureName;
                }
            }

            Texture2D selectedTexture = _textureBrowser.GetTexture(_selectedTextureName);
            if (selectedTexture)
            {
                Rect thumbnail = GUILayoutUtility.GetRect(96f, 96f, GUILayout.ExpandWidth(false));
                GUI.DrawTexture(thumbnail, selectedTexture, ScaleMode.ScaleToFit, true);
            }

            GUILayout.BeginHorizontal();
            bool previousEnabled = GUI.enabled;
            GUI.enabled = selectedTexture;
            if (GUILayout.Button("Assign Selected Texture"))
            {
                _session.WorkingChanges.textures[_selectedTextureProperty] = selectedTexture;
                EnsureNewMaterialForChange();
                ApplyWorkingMaterial();
            }
            GUI.enabled = previousEnabled && _session.WorkingChanges.textures.ContainsKey(_selectedTextureProperty);
            if (GUILayout.Button("Remove Texture Change"))
            {
                _session.WorkingChanges.textures.Remove(_selectedTextureProperty);
                EnsureNewMaterialForChange();
                ApplyWorkingMaterial();
            }
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
        }

        private void DrawWorkingColors()
        {
            if (_session.WorkingChanges?.colors == null || _session.WorkingChanges.colors.Count == 0)
            {
                GUILayout.Label("No color properties were found on this shader.");
                return;
            }

            bool changed = false;
            List<string> propertyNames = new List<string>(_session.WorkingChanges.colors.Keys);
            foreach (string propertyName in propertyNames)
            {
                Color color = _session.WorkingChanges.colors[propertyName];
                GUILayout.Label(propertyName);
                color.r = DrawColorChannel("R", color.r, ref changed);
                color.g = DrawColorChannel("G", color.g, ref changed);
                color.b = DrawColorChannel("B", color.b, ref changed);
                color.a = DrawColorChannel("A", color.a, ref changed);
                _session.WorkingChanges.colors[propertyName] = color;

                Color previousColor = GUI.color;
                GUI.color = color;
                GUILayout.Box(string.Empty, GUILayout.Height(12f), GUILayout.ExpandWidth(true));
                GUI.color = previousColor;
            }

            if (changed)
            {
                EnsureNewMaterialForChange();
                ApplyWorkingMaterial();
            }
        }

        private void DrawSavePanel()
        {
            if (_session.SelectedObject == null)
            {
                return;
            }

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Step 4 — Save YAML");
            string materialName = GetActiveMaterialName();
            GUILayout.Label(string.IsNullOrEmpty(materialName)
                ? "Material reference: choose an existing shared material or create a new one."
                : "Material reference: " + materialName);

            GUILayout.BeginHorizontal();
            bool previousEnabled = GUI.enabled;
            GUI.enabled = CanSaveMaterial();
            if (GUILayout.Button("Save Material YAML"))
            {
                SaveMaterialYaml(true);
            }

            GUI.enabled = previousEnabled && CanSaveObject(materialName);
            if (GUILayout.Button("Save Overwrite YAML"))
            {
                RequestOverwrite(false);
            }
            if (GUILayout.Button("Save Overwrite + Reload"))
            {
                RequestOverwrite(true);
            }
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
            GUILayout.Label("Overwrite changes the selected prefab. Material YAML is saved automatically when required.");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Clone prefab name", GUILayout.Width(120f));
            _session.CloneName = GUILayout.TextField(_session.CloneName);
            GUILayout.Label("Display name", GUILayout.Width(80f));
            _session.DisplayName = GUILayout.TextField(_session.DisplayName);
            GUILayout.EndHorizontal();
            GUILayout.Label("Clone prefab name creates a new item/piece. It is separate from the shared material name above.");

            GUI.enabled = previousEnabled
                && CanSaveObject(materialName)
                && !string.IsNullOrWhiteSpace(_session.CloneName)
                && !string.IsNullOrWhiteSpace(_session.DisplayName);
            if (GUILayout.Button("Clone New Object"))
            {
                SaveObject(true, false);
            }
            GUI.enabled = previousEnabled;

            if (!string.IsNullOrEmpty(_exporter.LastSavedPath) && GUILayout.Button("Open Saved YAML Folder"))
            {
                OpenSavedFolder();
            }
            GUILayout.EndVertical();
        }

        private void DrawConfirmationPanel()
        {
            if (_pendingSelection != null || _pendingClose)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Unsaved material changes will be discarded.");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Keep Editing"))
                {
                    _pendingSelection = null;
                    _pendingClose = false;
                }
                if (GUILayout.Button("Discard Changes"))
                {
                    WackyDbObjectCandidate selection = _pendingSelection;
                    bool close = _pendingClose;
                    _pendingSelection = null;
                    _pendingClose = false;
                    _session.MaterialChangesDirty = false;
                    if (close)
                    {
                        CloseImmediately();
                    }
                    else if (selection != null)
                    {
                        SelectImmediately(selection);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            if (_confirmOverwrite)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Overwrite the YAML for " + _session.SelectedObject.Name + "?");
                GUILayout.Label("This replaces an existing file with the same name.");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Cancel"))
                {
                    _confirmOverwrite = false;
                }
                if (GUILayout.Button("Confirm Overwrite"))
                {
                    bool reload = _confirmOverwriteReload;
                    _confirmOverwrite = false;
                    SaveObject(false, reload);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        private void RequestOverwrite(bool reload)
        {
            _confirmOverwrite = true;
            _confirmOverwriteReload = reload;
        }

        private void OpenSavedFolder()
        {
            try
            {
                string folder = System.IO.Path.GetDirectoryName(_exporter.LastSavedPath);
                if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
                {
                    System.Diagnostics.Process.Start("explorer.exe", folder);
                }
            }
            catch (Exception exception)
            {
                _status = "Unable to open saved YAML folder: " + exception.Message;
            }
        }

        private static float DrawColorChannel(string label, float value, ref bool changed)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(16f));
            float result = GUILayout.HorizontalSlider(value, 0f, 1f);
            GUILayout.Label(result.ToString("0.000"), GUILayout.Width(42f));
            GUILayout.EndHorizontal();
            if (!Mathf.Approximately(result, value))
            {
                changed = true;
            }
            return result;
        }

        private void DrawRendererInfo(WackyDbRendererInfo rendererInfo)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Renderer: " + rendererInfo.Path);

            if (rendererInfo.Materials.Count == 0)
            {
                GUILayout.Label("  No material slots");
            }

            foreach (WackyDbMaterialInfo materialInfo in rendererInfo.Materials)
            {
                bool selected = _session.SelectedRenderer == rendererInfo.Renderer
                    && _session.SelectedMaterialSlot == materialInfo.Slot;
                string prefix = selected ? "> " : string.Empty;

                if (GUILayout.Button(prefix + "Slot " + materialInfo.Slot + ": " + materialInfo.Name))
                {
                    SelectMaterialSlot(rendererInfo.Renderer, materialInfo);
                }

                GUILayout.Label("  Shader: " + materialInfo.ShaderName);
                if (materialInfo.ColorProperties.Count == 0)
                {
                    GUILayout.Label("  Color properties: none");
                }
                else
                {
                    GUILayout.Label("  Color properties:");
                    foreach (WackyDbShaderPropertyInfo property in materialInfo.ColorProperties)
                    {
                        Color value = property.ColorValue;
                        GUILayout.Label(string.Format(
                            "    {0}  R:{1:0.###} G:{2:0.###} B:{3:0.###} A:{4:0.###}",
                            property.Name,
                            value.r,
                            value.g,
                            value.b,
                            value.a));
                    }
                }
            }

            GUILayout.EndVertical();
        }

        private void RefreshCandidates()
        {
            try
            {
                _selector.Refresh();
                _materialLibrary.Refresh();
                _textureBrowser.Refresh();
                _status = _selector.GetCandidates().Count + " objects discovered.";
            }
            catch (Exception exception)
            {
                _status = "Object discovery failed: " + exception.Message;
                WMRecipeCust.WLog.LogError(_status);
            }
        }

        private void SelectMaterialSlot(Renderer renderer, WackyDbMaterialInfo materialInfo)
        {
            if (!materialInfo.Material)
            {
                _status = "The selected material slot is empty.";
                return;
            }

            _session.ClearMaterialSelection();
            _session.SelectedRenderer = renderer;
            _session.SelectedMaterialSlot = materialInfo.Slot;
            _session.OriginalMaterialName = materialInfo.Name;
            _session.NewMaterialName = materialInfo.Name + "_Wacky";
            _session.WorkingBaseMaterial = materialInfo.Material;
            _session.WorkingChanges = GetColorChanges(materialInfo.Material);
            _session.IsCreatingNewMaterial = true;
            _session.IsEditingExistingSharedMaterial = false;
            _session.MaterialChangesDirty = false;
            _showRendererSlots = false;
            _showColorEditor = true;
            _detailScroll = Vector2.zero;
            _materialLibrarySelection = string.Empty;
            _selectedTextureProperty = string.Empty;
            _selectedTextureName = string.Empty;
            _textureSearch = string.Empty;
            _sharedReferenceCount = 0;
            ApplyWorkingMaterial();
        }

        private void UseSharedMaterial(string materialName)
        {
            Material material = _materialLibrary.GetMaterial(materialName);
            if (!material)
            {
                _status = "Shared material is not currently loaded: " + materialName;
                return;
            }

            _session.SelectedSharedMaterialName = materialName;
            _session.NewMaterialName = materialName;
            _session.WorkingBaseMaterial = material;
            _session.WorkingChanges = GetColorChanges(material);
            _session.IsEditingExistingSharedMaterial = _materialLibrary.IsWackyMaterial(materialName);
            MaterialInstance existingMaterial = _materialLibrary.LoadMaterialYaml(materialName);
            if (existingMaterial?.changes != null)
            {
                if (existingMaterial.changes.floats != null)
                {
                    foreach (KeyValuePair<string, float> entry in existingMaterial.changes.floats)
                    {
                        _session.WorkingChanges.floats[entry.Key] = entry.Value;
                    }
                }
                if (existingMaterial.changes.textures != null)
                {
                    foreach (KeyValuePair<string, Texture2D> entry in existingMaterial.changes.textures)
                    {
                        if (entry.Value)
                        {
                            _session.WorkingChanges.textures[entry.Key] = entry.Value;
                        }
                    }
                }
            }
            _session.IsCreatingNewMaterial = false;
            _session.MaterialChangesDirty = false;
            _selectedTextureProperty = string.Empty;
            _selectedTextureName = string.Empty;
            _textureSearch = string.Empty;
            _sharedReferenceCount = _session.IsEditingExistingSharedMaterial
                ? _materialLibrary.CountYamlReferences(materialName)
                : 0;
            ApplyWorkingMaterial();
        }

        private void BeginNewSharedMaterial(bool duplicate)
        {
            if (!_session.WorkingBaseMaterial)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_session.NewMaterialName))
            {
                string baseName = string.IsNullOrEmpty(_session.SelectedSharedMaterialName)
                    ? _session.WorkingBaseMaterial.name
                    : _session.SelectedSharedMaterialName;
                _session.NewMaterialName = baseName + (duplicate ? "_Copy" : "_Wacky");
            }
            else if (duplicate && _session.NewMaterialName == _session.SelectedSharedMaterialName)
            {
                _session.NewMaterialName += "_Copy";
            }

            _session.IsCreatingNewMaterial = true;
            _session.IsEditingExistingSharedMaterial = false;
            _session.MaterialChangesDirty = true;
            _sharedReferenceCount = 0;
            ApplyWorkingMaterial();
        }

        private static MaterialData GetColorChanges(Material material)
        {
            MaterialData changes = new MaterialData();
            if (!material || !material.shader)
            {
                return changes;
            }

            Shader shader = material.shader;
            int propertyCount = shader.GetPropertyCount();
            for (int index = 0; index < propertyCount; index++)
            {
                ShaderPropertyType type = shader.GetPropertyType(index);
                if (type == ShaderPropertyType.Color)
                {
                    string propertyName = shader.GetPropertyName(index);
                    changes.colors[propertyName] = material.GetColor(propertyName);
                }
                else if (type == ShaderPropertyType.Float || type == ShaderPropertyType.Range)
                {
                    string propertyName = shader.GetPropertyName(index);
                    changes.floats[propertyName] = material.GetFloat(propertyName);
                }
            }
            return changes;
        }

        private static List<string> GetTexturePropertyNames(Material material)
        {
            List<string> properties = new List<string>();
            if (!material || !material.shader)
            {
                return properties;
            }

            Shader shader = material.shader;
            for (int index = 0; index < shader.GetPropertyCount(); index++)
            {
                if (shader.GetPropertyType(index) == ShaderPropertyType.Texture)
                {
                    properties.Add(shader.GetPropertyName(index));
                }
            }
            return properties;
        }

        private void EnsureNewMaterialForChange()
        {
            if (!_session.IsCreatingNewMaterial && !_session.IsEditingExistingSharedMaterial)
            {
                string baseName = string.IsNullOrWhiteSpace(_session.SelectedSharedMaterialName)
                    ? _session.WorkingBaseMaterial.name
                    : _session.SelectedSharedMaterialName;
                _session.NewMaterialName = baseName + "_Wacky";
                _session.IsCreatingNewMaterial = true;
            }
            _session.MaterialChangesDirty = true;
        }

        private string GetActiveMaterialName()
        {
            if (_session.IsCreatingNewMaterial)
            {
                return _session.NewMaterialName?.Trim() ?? string.Empty;
            }

            return _session.SelectedSharedMaterialName?.Trim() ?? string.Empty;
        }

        private bool CanSaveMaterial()
        {
            return _session.WorkingBaseMaterial
                && (_session.IsCreatingNewMaterial || _session.IsEditingExistingSharedMaterial)
                && !string.IsNullOrWhiteSpace(GetActiveMaterialName());
        }

        private bool CanSaveObject(string materialName)
        {
            return !string.IsNullOrWhiteSpace(materialName)
                && (_session.SelectedObject.Type == WackyDbObjectType.Item
                    || _session.SelectedObject.Type == WackyDbObjectType.Piece);
        }

        private MaterialInstance BuildMaterialInstance()
        {
            string materialName = GetActiveMaterialName();
            MaterialInstance existing = _session.IsEditingExistingSharedMaterial
                ? _materialLibrary.LoadMaterialYaml(materialName)
                : null;

            return new MaterialInstance
            {
                name = materialName,
                original = existing != null && !string.IsNullOrWhiteSpace(existing.original)
                    ? existing.original
                    : _session.WorkingBaseMaterial.name,
                overwrite = existing?.overwrite ?? false,
                changes = _session.WorkingChanges
            };
        }

        private bool SaveMaterialYaml(bool force)
        {
            if (!CanSaveMaterial())
            {
                _status = "Choose New Shared Material or an existing WackyDB shared material before saving material YAML.";
                return false;
            }

            if (!force && !_session.IsCreatingNewMaterial && !_session.MaterialChangesDirty)
            {
                return true;
            }

            MaterialInstance material = BuildMaterialInstance();
            if (!_exporter.SaveMaterial(material))
            {
                _status = "Material save failed: " + _exporter.LastError;
                return false;
            }

            _session.MaterialChangesDirty = false;
            _session.SelectedSharedMaterialName = material.name;
            _session.IsCreatingNewMaterial = false;
            _session.IsEditingExistingSharedMaterial = true;
            _materialLibrary.Refresh();
            _status = "Saved material YAML: " + _exporter.LastSavedPath;
            return true;
        }

        private void SaveObject(bool clone, bool reload)
        {
            string materialName = GetActiveMaterialName();
            if (!CanSaveObject(materialName))
            {
                _status = "Select an item or piece and choose a shared material before saving.";
                return;
            }

            if ((_session.IsCreatingNewMaterial || _session.MaterialChangesDirty) && !SaveMaterialYaml(false))
            {
                return;
            }

            WackyDbObjectCandidate selected = _session.SelectedObject;
            CustomVisual customVisual = BuildCustomVisual(materialName);
            bool saved;
            if (selected.Type == WackyDbObjectType.Item)
            {
                saved = clone
                    ? _exporter.SaveItemClone(selected.Prefab, selected.Name, _session.CloneName.Trim(), _session.DisplayName.Trim(), materialName, customVisual)
                    : _exporter.SaveItemOverwrite(selected.Prefab, selected.Name, materialName, customVisual);
            }
            else
            {
                GetPieceMaterialNames(materialName, out string fullHealthMaterial, out string damagedMaterial);
                saved = clone
                    ? _exporter.SavePieceClone(selected.Name, _session.CloneName.Trim(), _session.DisplayName.Trim(), selected.PieceHammer, fullHealthMaterial, damagedMaterial)
                    : _exporter.SavePieceOverwrite(selected.Name, selected.PieceHammer, fullHealthMaterial, damagedMaterial);
            }

            if (!saved)
            {
                _status = "Object save failed: " + _exporter.LastError;
                return;
            }

            _status = clone
                ? "Saved cloned object YAML: " + _exporter.LastSavedPath
                : "Saved overwrite YAML: " + _exporter.LastSavedPath;

            if (reload)
            {
                ReloadSavedYaml();
            }
        }

        private void GetPieceMaterialNames(
            string currentMaterialName,
            out string fullHealthMaterial,
            out string damagedMaterial)
        {
            fullHealthMaterial = NullIfEmpty(_session.PieceMaterialName);
            damagedMaterial = NullIfEmpty(_session.DamagedPieceMaterialName);
            if (_session.PieceMaterialRoute == WackyDbPieceMaterialRoute.FullHealth)
            {
                fullHealthMaterial = currentMaterialName;
            }
            else
            {
                damagedMaterial = currentMaterialName;
            }
        }

        private CustomVisual BuildCustomVisual(string currentMaterialName)
        {
            if (_session.MaterialRoute == WackyDbMaterialRoute.Material)
            {
                return null;
            }

            CustomVisual visual = new CustomVisual
            {
                base_mat = NullIfEmpty(_session.BaseMaterialName),
                chest = NullIfEmpty(_session.ChestMaterialName),
                legs = NullIfEmpty(_session.LegsMaterialName)
            };

            switch (_session.MaterialRoute)
            {
                case WackyDbMaterialRoute.Base:
                    visual.base_mat = currentMaterialName;
                    break;
                case WackyDbMaterialRoute.Chest:
                    visual.chest = currentMaterialName;
                    break;
                case WackyDbMaterialRoute.Legs:
                    visual.legs = currentMaterialName;
                    break;
            }
            return visual;
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private void ReloadSavedYaml()
        {
            if (!ObjectDB.instance || !WMRecipeCust.issettoSinglePlayer)
            {
                _status += " Reload is only available in a loaded single-player world.";
                return;
            }

            wackydatabase.Read.ReadFiles readNow = new wackydatabase.Read.ReadFiles();
            WMRecipeCust.context.StartCoroutine(readNow.GetDataFromFiles(true));
            WMRecipeCust.readFiles = readNow;

            wackydatabase.SetData.Reload reload = new wackydatabase.SetData.Reload();
            WMRecipeCust.CurrentReload = reload;
            if (WMRecipeCust.HasLobbied)
            {
                WMRecipeCust.context.StartCoroutine(reload.LoadAllRecipeData(true, true, true));
            }
            else
            {
                WMRecipeCust.context.StartCoroutine(reload.LoadAllRecipeData(true, true));
            }
            _status += " Reload started.";
        }

        private void ApplyWorkingMaterial()
        {
            if (_preview == null || !_preview.HasPreview || !_session.SelectedRenderer || !_session.WorkingBaseMaterial)
            {
                return;
            }

            try
            {
                _preview.ApplyMaterial(
                    _session.SelectedRenderer,
                    _session.SelectedMaterialSlot,
                    _session.WorkingBaseMaterial,
                    _session.WorkingChanges);
            }
            catch (Exception exception)
            {
                _status = "Live material preview failed: " + exception.Message;
                WMRecipeCust.WLog.LogWarning(_status);
            }
        }

        private void ResetPrefabPreview()
        {
            if (_session.SelectedObject == null || !_session.SelectedObject.Prefab)
            {
                return;
            }

            try
            {
                if (_preview == null)
                {
                    _preview = new WackyDbPreviewRenderer();
                }
                _preview.SetPrefab(_session.SelectedObject.Prefab);
                _session.ClearMaterialSelection();
                _materialLibrarySelection = string.Empty;
                _materialSearch = string.Empty;
                _sharedReferenceCount = 0;
                _status = "Preview materials restored from the original prefab.";
            }
            catch (Exception exception)
            {
                _status = "Unable to reset preview: " + exception.Message;
                WMRecipeCust.WLog.LogWarning(_status);
            }
        }

        private void CenterWindow()
        {
            if (_fullScreen)
            {
                return;
            }

            float width = Mathf.Min(1250f, Mathf.Max(300f, Screen.width - 20f));
            float height = Mathf.Min(700f, Mathf.Max(300f, Screen.height - 20f));
            _windowRect = new Rect(
                Mathf.Max(0f, (Screen.width - width) * 0.5f),
                Mathf.Max(0f, (Screen.height - height) * 0.5f),
                width,
                height);
            _normalWindowRect = _windowRect;
        }

        private void ToggleFullScreen()
        {
            if (_fullScreen)
            {
                _fullScreen = false;
                _windowRect = _normalWindowRect.width > 0f ? _normalWindowRect : _windowRect;
                CenterWindow();
            }
            else
            {
                _normalWindowRect = _windowRect;
                _fullScreen = true;
            }
        }

        private void Select(WackyDbObjectCandidate candidate)
        {
            if (_session.SelectedObject != null
                && _session.SelectedObject != candidate
                && _session.MaterialChangesDirty)
            {
                _pendingSelection = candidate;
                _pendingClose = false;
                return;
            }

            SelectImmediately(candidate);
        }

        private void SelectImmediately(WackyDbObjectCandidate candidate)
        {
            _session.ClearSelection();
            _session.SelectedObject = candidate;
            _detailScroll = Vector2.zero;
            _showRendererSlots = true;
            _session.CloneName = candidate.Name + "_Wacky";
            _session.DisplayName = string.IsNullOrEmpty(candidate.DisplayName)
                ? candidate.Name + " Wacky"
                : candidate.DisplayName + " Wacky";
            _session.MaterialRoute = GetDefaultMaterialRoute(candidate);

            try
            {
                _session.RendererInfos = _inspector.GetRendererInfos(candidate.Prefab);
                _status = _session.RendererInfos.Count == 0
                    ? "No renderers were found on this prefab."
                    : _session.RendererInfos.Count + " renderers found.";
            }
            catch (Exception exception)
            {
                _status = "Material inspection failed: " + exception.Message;
                WMRecipeCust.WLog.LogError(_status);
            }

            try
            {
                if (_preview == null)
                {
                    _preview = new WackyDbPreviewRenderer();
                }
                _preview.SetPrefab(candidate.Prefab);
            }
            catch (Exception exception)
            {
                _status += " Preview unavailable: " + exception.Message;
                WMRecipeCust.WLog.LogWarning("Preview unavailable for " + candidate.Name + ": " + exception.Message);
            }
        }

        private static WackyDbMaterialRoute GetDefaultMaterialRoute(WackyDbObjectCandidate candidate)
        {
            ItemDrop itemDrop = candidate?.Prefab ? candidate.Prefab.GetComponent<ItemDrop>() : null;
            string itemType = itemDrop?.m_itemData?.m_shared?.m_itemType.ToString() ?? string.Empty;
            if (itemType.IndexOf("Chest", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return WackyDbMaterialRoute.Chest;
            }
            if (itemType.IndexOf("Leg", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return WackyDbMaterialRoute.Legs;
            }
            return WackyDbMaterialRoute.Material;
        }

        private void Close()
        {
            if (_session.MaterialChangesDirty)
            {
                _pendingSelection = null;
                _pendingClose = true;
                return;
            }

            CloseImmediately();
        }

        private void CloseImmediately()
        {
            if (_preview != null)
            {
                _preview.Dispose();
                _preview = null;
            }
            enabled = false;
        }

        private void OnDestroy()
        {
            if (_preview != null)
            {
                _preview.Dispose();
                _preview = null;
            }
        }
    }

    internal sealed class WackyDbCreateHotkeyListener : MonoBehaviour
    {
        private void OnGUI()
        {
            Event current = Event.current;
            if (current.type == EventType.KeyDown
                && WMRecipeCust.modEnabled.Value
                && WMRecipeCust.creatorHotkey != null
                && current.keyCode == WMRecipeCust.creatorHotkey.Value)
            {
                WackyDbCreateWindow.ToggleWithGameUi();
                current.Use();
            }
        }
    }
}
