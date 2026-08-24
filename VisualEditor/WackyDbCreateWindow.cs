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
        private const string TutorialPreferenceKey = "WackyDB.Creator.ShowTutorial";

        private static WackyDbCreateWindow _instance;
        private static int _lastHotkeyToggleFrame = -1;

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
        private Vector2 _materialScroll;
        private string _searchText = string.Empty;
        private string _status = string.Empty;
        private string _materialSearch = string.Empty;
        private string _materialLibrarySelection = string.Empty;
        private string _hoveredMaterialName = string.Empty;
        private int _sharedReferenceCount;
        private bool _previewDragging;
        private Vector2 _previewPointerDown;
        private bool _fullScreen = true;
        private bool _showTutorial = true;
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
        private bool _usePlayerModelPreview;

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

        internal static void ToggleFromHotkey()
        {
            if (_lastHotkeyToggleFrame == Time.frameCount)
            {
                return;
            }

            _lastHotkeyToggleFrame = Time.frameCount;
            ToggleWithGameUi();
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
            if (Event.current.type == EventType.KeyDown
                && WMRecipeCust.creatorHotkey != null
                && Event.current.keyCode == WMRecipeCust.creatorHotkey.Value)
            {
                ToggleFromHotkey();
                Event.current.Use();
                return;
            }

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
            if (GUI.Button(
                    new Rect(_windowRect.width - 38f, 2f, 32f, 20f),
                    new GUIContent("X", "Close the creator. You will be warned about unsaved material changes.")))
            {
                Close();
            }

            GUILayout.BeginVertical();
            DrawToolbar();
            GUILayout.Label("1. Choose an object  >  2. Choose a material slot  >  3. Customize the material  >  4. Save the result");
            DrawTutorial();

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
                GUILayout.Label("Status: " + _status);
            }

            GUILayout.Space(2f);
            GUILayout.Label(
                string.IsNullOrEmpty(GUI.tooltip)
                    ? "Tip: Hover over a control to learn what it does."
                    : "Tip: " + GUI.tooltip,
                GUI.skin.box);

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width - 45f, 24f));
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Find object", "Search by the internal prefab name or the in-game display name."), GUILayout.Width(70f));
            _searchText = GUILayout.TextField(_searchText, GUILayout.ExpandWidth(true));

            if (GUILayout.Button(new GUIContent("Clear", "Clear the search and show every available object."), GUILayout.Width(50f)))
            {
                _searchText = string.Empty;
                _resultScroll = Vector2.zero;
            }

            if (GUILayout.Button(new GUIContent("Refresh", "Scan the currently loaded game and mods for objects, materials, and textures."), GUILayout.Width(75f)))
            {
                RefreshCandidates();
            }

            if (GUILayout.Button(new GUIContent(_fullScreen ? "Windowed" : "Full Screen", "Switch between the full-screen workspace and a movable window."), GUILayout.Width(85f)))
            {
                ToggleFullScreen();
            }

            if (GUILayout.Button(new GUIContent(_showTutorial ? "Hide Guide" : "Show Guide", "Toggle the step-by-step tutorial. This preference is remembered."), GUILayout.Width(82f)))
            {
                _showTutorial = !_showTutorial;
                PlayerPrefs.SetInt(TutorialPreferenceKey, _showTutorial ? 1 : 0);
                PlayerPrefs.Save();
            }

            if (GUILayout.Button(new GUIContent("Reset Edits", "Discard unsaved material edits and restore the original preview."), GUILayout.Width(78f)))
            {
                ResetUnsavedMaterialEdits();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawTutorial()
        {
            if (!_showTutorial)
            {
                return;
            }

            int step;
            string title;
            string instruction;
            if (_session.SelectedObject == null)
            {
                step = 1;
                title = "Choose what to customize";
                instruction = "Search on the left, then click an item or piece. The center preview helps confirm that you chose the right object.";
            }
            else if (!_session.SelectedRenderer || !_session.WorkingBaseMaterial)
            {
                step = 2;
                title = "Choose the visible part to edit";
                instruction = "On the right, open Renderer / Material Slots and click a slot. Objects can have separate materials for different visible parts.";
            }
            else if (string.IsNullOrWhiteSpace(_session.SelectedSharedMaterialName)
                && !_session.MaterialChangesDirty)
            {
                step = 3;
                title = "Name or choose a reusable material";
                instruction = "Enter a unique shared material name, or open Existing Shared Material to reuse one. Then adjust colors, textures, or shader values.";
            }
            else
            {
                step = 4;
                title = "Preview and save";
                instruction = "Check the center preview. Save Overwrite changes this object; Clone New Object creates a separate object. Saving the object also saves required material changes.";
            }

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("GUIDED MODE — Step " + step + " of 4: " + title);
            GUILayout.Label(instruction);
            GUILayout.EndVertical();
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
                string label = (selected ? "[Selected] " : string.Empty) + candidate.Name + "  [" + candidate.Type + "]";
                if (GUILayout.Button(new GUIContent(label, "Select this " + candidate.Type.ToString().ToLowerInvariant() + " for preview and editing."), GUILayout.Height(24f)))
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
            GUILayout.Label(new GUIContent("Preview — click a part or drag to rotate", "Click a visible part to open its renderer's material slots. Drag, use the arrow buttons, or zoom to inspect material changes before saving."));

            if (CanUsePlayerModelPreview())
            {
                string previewModeLabel = _usePlayerModelPreview ? "Item model preview" : "Player model preview";
                bool usePlayerModel = GUILayout.Toggle(_usePlayerModelPreview, previewModeLabel, GUI.skin.button);
                if (usePlayerModel != _usePlayerModelPreview)
                {
                    SaveCurrentMaterialEdit();
                    _usePlayerModelPreview = usePlayerModel;
                    WackyDbRendererInfo selectedRendererInfo = _session.RendererInfos.Find(
                        info => info.Renderer == _session.SelectedRenderer);
                    if (_session.SelectedRenderer && !IsRendererVisibleForPreview(selectedRendererInfo))
                    {
                        _session.ClearMaterialSelection();
                    }
                    ResetPrefabPreview();
                }
            }

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
                if (GUILayout.Button(new GUIContent("Snapshot Icon", "Save the current preview as a PNG and reference it as the object's custom icon.")))
                {
                    SavePreviewSnapshot();
                }
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
                GUI.Box(previewRect, "Complete Step 1 to show a preview");
            }

            GUILayout.FlexibleSpace();
            if (_preview != null && _preview.CanPickRenderer)
            {
                GUILayout.Label("Click a visible part to locate its renderer.");
            }
            else if (_usePlayerModelPreview)
            {
                GUILayout.Label("Switch to item model preview to select a renderer by clicking.");
            }
            GUILayout.Label("Preview clones are isolated from gameplay and source materials.");
            GUILayout.EndVertical();
        }

        private void HandlePreviewInput(Rect previewRect)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && previewRect.Contains(current.mousePosition))
            {
                _previewDragging = true;
                _previewPointerDown = current.mousePosition;
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
                if ((current.mousePosition - _previewPointerDown).sqrMagnitude <= 16f)
                {
                    SelectPreviewRenderer(previewRect, current.mousePosition);
                }
                current.Use();
            }
        }

        private void SelectPreviewRenderer(Rect previewRect, Vector2 mousePosition)
        {
            if (_preview == null || !_preview.CanPickRenderer)
            {
                _status = _usePlayerModelPreview
                    ? "Renderer picking is available in item model preview, not player model preview."
                    : "The clicked preview part could not be matched to a renderer.";
                return;
            }

            Vector2 viewportPosition = new Vector2(
                Mathf.InverseLerp(previewRect.xMin, previewRect.xMax, mousePosition.x),
                Mathf.InverseLerp(previewRect.yMax, previewRect.yMin, mousePosition.y));
            if (!_preview.TryPickRenderer(viewportPosition, out Renderer renderer))
            {
                _status = "No editable renderer was found at the clicked preview position.";
                return;
            }

            WackyDbRendererInfo rendererInfo = _session.RendererInfos.Find(info => info.Renderer == renderer);
            if (rendererInfo == null)
            {
                _status = "The clicked renderer is not available for material editing.";
                return;
            }

            _showRendererSlots = true;
            _detailScroll = Vector2.zero;
            if (rendererInfo.Materials.Count == 1)
            {
                SelectMaterialSlot(renderer, rendererInfo.Materials[0]);
                _status = "Selected renderer: " + rendererInfo.Path;
            }
            else
            {
                _status = "Selected renderer: " + rendererInfo.Path + ". Choose one of its " + rendererInfo.Materials.Count + " material slots.";
            }
        }

        private bool CanUsePlayerModelPreview()
        {
            ItemDrop itemDrop = _session.SelectedObject?.Prefab
                ? _session.SelectedObject.Prefab.GetComponent<ItemDrop>()
                : null;
            if (!itemDrop)
            {
                return false;
            }

            string itemType = itemDrop.m_itemData.m_shared.m_itemType.ToString();
            return itemType.IndexOf("Chest", StringComparison.OrdinalIgnoreCase) >= 0
                || itemType.IndexOf("Leg", StringComparison.OrdinalIgnoreCase) >= 0
                || itemType.IndexOf("Helmet", StringComparison.OrdinalIgnoreCase) >= 0
                || itemType.IndexOf("Shoulder", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SavePreviewSnapshot()
        {
            if (_preview == null || !_preview.HasPreview || _session.SelectedObject == null)
            {
                _status = "Select a previewable item or piece before creating an icon snapshot.";
                return;
            }

            Texture2D image = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                _preview.Render();
                RenderTexture.active = _preview.Texture;
                image = new Texture2D(_preview.Texture.width, _preview.Texture.height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0f, 0f, image.width, image.height), 0, 0);
                image.Apply();

                string iconName = SanitizeIconFileName(_session.SelectedObject.Name) + "_WackyDB.png";
                System.IO.Directory.CreateDirectory(WMRecipeCust.assetPathIcons);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(WMRecipeCust.assetPathIcons, iconName), image.EncodeToPNG());
                _session.SnapshotIconName = iconName;
                _status = "Saved preview snapshot: " + iconName + ". It will be referenced by the next YAML save.";
            }
            catch (Exception exception)
            {
                _status = "Unable to save preview snapshot: " + exception.Message;
                WMRecipeCust.WLog.LogWarning(_status);
            }
            finally
            {
                RenderTexture.active = previous;
                if (image)
                {
                    Destroy(image);
                }
            }
        }

        private static string SanitizeIconFileName(string value)
        {
            foreach (char invalidCharacter in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidCharacter, '_');
            }
            return value.Trim();
        }

        private void DrawSelectionDetails()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (_session.SelectedObject == null)
            {
                GUILayout.Label("Nothing selected yet.");
                GUILayout.Label("Start with Step 1 on the left: search for an item or piece, then click it to inspect its materials.");
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
                (_showRendererSlots ? "Hide " : "Show ") + "Step 2 — Renderer / Material Slots (" + GetVisibleRendererCount() + ")",
                GUI.skin.button);
            if (_showRendererSlots)
            {
                foreach (WackyDbRendererInfo rendererInfo in _session.RendererInfos)
                {
                    if (IsRendererVisibleForPreview(rendererInfo))
                    {
                        DrawRendererInfo(rendererInfo);
                    }
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
            if (GUILayout.Button(new GUIContent("Reset preview materials to prefab defaults", "Restore this object's preview and discard its current in-memory material selection.")))
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
                (_showSharedMaterialLibrary ? "Hide" : "Search") + " Existing Materials",
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

                GUILayout.BeginHorizontal();
                float materialListWidth = Mathf.Max(180f, (_windowRect.width - 660f) * 0.75f);
                GUILayout.BeginVertical(GUILayout.Width(materialListWidth));
                List<string> matches = _materialLibrary.Search(_materialSearch, int.MaxValue);
                if (Event.current.type == EventType.Repaint)
                {
                    _hoveredMaterialName = string.Empty;
                }

                _materialScroll = GUILayout.BeginScrollView(_materialScroll, GUILayout.Height(140f));
                foreach (string materialName in matches)
                {
                    string prefix = materialName == _materialLibrarySelection ? "[Selected] " : string.Empty;
                    if (GUILayout.Button(new GUIContent(prefix + materialName, "Hover to preview this material.")))
                    {
                        _materialLibrarySelection = materialName;
                        _materialSearch = materialName;
                    }

                    if (Event.current.type == EventType.Repaint
                        && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    {
                        _hoveredMaterialName = materialName;
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                DrawMaterialVisualizer();
                GUILayout.EndHorizontal();

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
            GUILayout.Label("Enter a name to create a reusable Material YAML.");

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

            bool supportsColorChanges = _session.WorkingChanges?.colors != null
                && _session.WorkingChanges.colors.Count > 0;
            if (supportsColorChanges)
            {
                _showColorEditor = GUILayout.Toggle(
                    _showColorEditor,
                    (_showColorEditor ? "Hide " : "Show ") + "Colors",
                    GUI.skin.button);
                if (_showColorEditor)
                {
                    DrawWorkingColors();
                }
            }
            else
            {
                GUILayout.Label(
                    new GUIContent(
                        "Colors unavailable for this material",
                        "This material's shader has no RGB color properties, so color editing is hidden."),
                    GUI.skin.box);
            }

            _showTextureEditor = GUILayout.Toggle(
                _showTextureEditor,
                (_showTextureEditor ? "Hide " : "Show ") + "Textures",
                GUI.skin.button);
            if (_showTextureEditor)
            {
                DrawTextureEditor();
            }

            _showFloatEditor = GUILayout.Toggle(
                _showFloatEditor,
                (_showFloatEditor ? "Hide " : "Show ") + "Advanced Shader Values",
                GUI.skin.button);
            if (_showFloatEditor)
            {
                DrawWorkingFloats();
            }
            GUILayout.EndVertical();
        }

        private void DrawMaterialVisualizer()
        {
            string materialName = string.IsNullOrEmpty(_hoveredMaterialName)
                ? _materialLibrarySelection
                : _hoveredMaterialName;
            Rect visualizerRect = GUILayoutUtility.GetRect(
                140f,
                140f,
                GUILayout.Width(140f),
                GUILayout.Height(140f));
            GUI.Box(visualizerRect, GUIContent.none);
            GUI.Label(
                new Rect(visualizerRect.x + 6f, visualizerRect.y + 4f, 128f, 18f),
                string.IsNullOrEmpty(_hoveredMaterialName) ? "Selected material" : "Hover preview");

            Material material = string.IsNullOrEmpty(materialName)
                ? null
                : _materialLibrary.GetMaterial(materialName);
            Rect thumbnail = new Rect(visualizerRect.x + 6f, visualizerRect.y + 24f, 96f, 76f);
            if (material && material.mainTexture)
            {
                GUI.DrawTexture(thumbnail, material.mainTexture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                GUI.Box(thumbnail, "No\ntexture");
            }

            string displayName = string.IsNullOrEmpty(materialName) ? "Hover over a material." : materialName;
            string shaderName = material ? (material.shader ? material.shader.name : "<no shader>") : "Not loaded";
            GUIStyle clippedLabel = new GUIStyle(GUI.skin.label)
            {
                clipping = TextClipping.Clip,
                wordWrap = false
            };
            GUI.Label(
                new Rect(visualizerRect.x + 6f, visualizerRect.y + 102f, 128f, 17f),
                new GUIContent(displayName, displayName),
                clippedLabel);
            GUI.Label(
                new Rect(visualizerRect.x + 6f, visualizerRect.y + 119f, 128f, 17f),
                new GUIContent(shaderName, shaderName),
                clippedLabel);
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
            string prefix = _session.PieceMaterialRoute == route ? "[Selected] " : string.Empty;
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

            if (GUILayout.Button("Assign Current Material to " + _session.MaterialRoute))
            {
                AssignCurrentMaterialRoute();
            }

            if (!string.IsNullOrEmpty(_session.StandardMaterialName)
                || !string.IsNullOrEmpty(_session.BaseMaterialName)
                || !string.IsNullOrEmpty(_session.ChestMaterialName)
                || !string.IsNullOrEmpty(_session.LegsMaterialName))
            {
                GUILayout.Label("Assigned item materials:");
                GUILayout.Label("  Standard / item model: " + EmptyAsNone(_session.StandardMaterialName));
                GUILayout.Label("  Base: " + EmptyAsNone(_session.BaseMaterialName));
                GUILayout.Label("  Chest: " + EmptyAsNone(_session.ChestMaterialName));
                GUILayout.Label("  Legs: " + EmptyAsNone(_session.LegsMaterialName));
                if (GUILayout.Button("Clear Item Material Assignments"))
                {
                    _session.StandardMaterialName = string.Empty;
                    _session.BaseMaterialName = string.Empty;
                    _session.ChestMaterialName = string.Empty;
                    _session.LegsMaterialName = string.Empty;
                }
            }

            switch (_session.MaterialRoute)
            {
                case WackyDbMaterialRoute.Material:
                    GUILayout.Label("Saves to the standard material field used by the item model.");
                    break;
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
            string prefix = _session.MaterialRoute == route ? "[Selected] " : string.Empty;
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
                case WackyDbMaterialRoute.Material:
                    _session.StandardMaterialName = materialName;
                    break;
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
            bool textureButtonsEnabled = GUI.enabled;
            GUI.enabled = currentTexture is Texture2D;
            if (GUILayout.Button(new GUIContent(
                    "Save + Open Current Texture",
                    "Export the material's current texture to the WackyDB Textures folder and open it in the system image editor/viewer.")))
            {
                SaveAndOpenTexture(currentTexture);
            }
            GUI.enabled = textureButtonsEnabled && selectedTexture;
            if (GUILayout.Button(new GUIContent(
                    "Save + Open Selected Texture",
                    "Export the selected browser texture to the WackyDB Textures folder and open it in the system image editor/viewer.")))
            {
                SaveAndOpenTexture(selectedTexture);
            }
            GUI.enabled = textureButtonsEnabled;
            GUILayout.EndHorizontal();

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

        private void SaveAndOpenTexture(Texture texture)
        {
            if (!(texture is Texture2D))
            {
                _status = "The selected texture cannot be exported as a PNG.";
                return;
            }

            try
            {
                System.IO.Directory.CreateDirectory(WMRecipeCust.assetPathTextures);
                TextureDataManager.SaveTexture(texture.name, texture);
                string path = System.IO.Path.Combine(WMRecipeCust.assetPathTextures, texture.name + ".png");
                _textureBrowser.Refresh();
                System.Diagnostics.Process.Start(path);
                _status = "Saved and opened texture: " + path;
            }
            catch (Exception exception)
            {
                _status = "Unable to save or open texture: " + exception.Message;
                WMRecipeCust.WLog.LogWarning(_status);
            }
        }

        private int GetVisibleRendererCount()
        {
            int count = 0;
            foreach (WackyDbRendererInfo rendererInfo in _session.RendererInfos)
            {
                if (IsRendererVisibleForPreview(rendererInfo))
                {
                    count++;
                }
            }
            return count;
        }

        private bool IsRendererVisibleForPreview(WackyDbRendererInfo rendererInfo)
        {
            if (rendererInfo == null)
            {
                return false;
            }

            bool hasItemRenderer = _session.RendererInfos.Exists(info => IsLogRenderer(info.Renderer));
            bool isItemRenderer = IsLogRenderer(rendererInfo.Renderer);
            return _usePlayerModelPreview ? !isItemRenderer : !hasItemRenderer || isItemRenderer;
        }

        private static bool IsLogRenderer(Renderer renderer)
        {
            Transform current = renderer ? renderer.transform : null;
            while (current)
            {
                if (current.name.StartsWith("log", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                current = current.parent;
            }
            return false;
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
            if (CanUseItemMaterialArray())
            {
                GUILayout.Label("Multi-slot item: only selected slot " + _session.SelectedMaterialSlot + " will change in the materials array.");
            }

            GUILayout.BeginHorizontal();
            bool previousEnabled = GUI.enabled;
            GUI.enabled = CanSaveMaterial();
            if (GUILayout.Button(new GUIContent("Save Material Only", "Save the reusable material YAML without saving or cloning the selected object.")))
            {
                SaveMaterialYaml(true);
            }

            GUI.enabled = previousEnabled && CanSaveObject(materialName);
            if (GUILayout.Button(new GUIContent("Save Object Overwrite", "Create YAML that applies this material to the selected object. Confirmation is required.")))
            {
                RequestOverwrite(false);
            }
            if (GUILayout.Button(new GUIContent("Save Overwrite + Reload", "Save the object overwrite and immediately reload WackyDB data in a single-player world.")))
            {
                RequestOverwrite(true);
            }
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
            GUILayout.Label("Overwrite changes the selected prefab. Material YAML is saved automatically when required.");
            if (!string.IsNullOrEmpty(_session.SnapshotIconName))
            {
                GUILayout.Label("Custom icon snapshot: " + _session.SnapshotIconName);
            }

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
            if (GUILayout.Button(new GUIContent("Clone as New Object", "Create a separate item or piece using the clone prefab name and display name above.")))
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

                if (GUILayout.Button(new GUIContent(prefix + "Slot " + materialInfo.Slot + ": " + materialInfo.Name, "Edit this material slot. The source material is copied, so the original game material is not modified.")))
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

            SaveCurrentMaterialEdit();
            _session.SelectedRenderer = renderer;
            _session.SelectedMaterialSlot = materialInfo.Slot;
            if (_session.SelectedObject?.Type == WackyDbObjectType.Item)
            {
                _session.MaterialRoute = IsLogRenderer(renderer)
                    ? WackyDbMaterialRoute.Material
                    : GetDefaultMaterialRoute(_session.SelectedObject);
            }
            string editKey = WackyDbEditorSession.GetMaterialEditKey(renderer, materialInfo.Slot);
            if (_session.MaterialEdits.TryGetValue(editKey, out WackyDbMaterialEditState existingEdit))
            {
                _session.RestoreMaterialEdit(existingEdit);
                _showRendererSlots = false;
                _showColorEditor = true;
                _detailScroll = Vector2.zero;
                ApplyWorkingMaterial();
                return;
            }

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

        private void SaveCurrentMaterialEdit(bool savedToYaml = false)
        {
            if (!_session.SelectedRenderer || !_session.WorkingBaseMaterial)
            {
                return;
            }

            string key = WackyDbEditorSession.GetMaterialEditKey(
                _session.SelectedRenderer,
                _session.SelectedMaterialSlot);
            bool wasSaved = savedToYaml || (_session.MaterialEdits.TryGetValue(key, out WackyDbMaterialEditState existing) && existing.SavedToYaml);
            _session.MaterialEdits[key] = _session.CaptureMaterialEdit(wasSaved);
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
            SaveCurrentMaterialEdit(true);
            return true;
        }

        private void ResetUnsavedMaterialEdits()
        {
            SaveCurrentMaterialEdit();
            List<string> unsavedEdits = new List<string>();
            foreach (KeyValuePair<string, WackyDbMaterialEditState> edit in _session.MaterialEdits)
            {
                if (!edit.Value.SavedToYaml)
                {
                    unsavedEdits.Add(edit.Key);
                }
            }

            foreach (string key in unsavedEdits)
            {
                _session.MaterialEdits.Remove(key);
            }

            ResetPrefabPreview();
            foreach (WackyDbMaterialEditState edit in _session.MaterialEdits.Values)
            {
                if (edit.Renderer && edit.WorkingBaseMaterial)
                {
                    _preview.ApplyMaterial(edit.Renderer, edit.Slot, edit.WorkingBaseMaterial, edit.WorkingChanges);
                }
            }
            _status = unsavedEdits.Count == 0
                ? "There were no unsaved material edits to reset."
                : "Reset " + unsavedEdits.Count + " unsaved material edit(s). Saved YAML edits remain in the preview.";
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
            string itemMaterialName = _session.MaterialRoute == WackyDbMaterialRoute.Material
                ? materialName
                : NullIfEmpty(_session.StandardMaterialName);
            CustomVisual customVisual = BuildCustomVisual(materialName);
            string[] itemMaterials = BuildItemMaterialArray(itemMaterialName, customVisual);
            bool saved;
            if (selected.Type == WackyDbObjectType.Item)
            {
                saved = clone
                    ? _exporter.SaveItemClone(selected.Prefab, selected.Name, _session.CloneName.Trim(), _session.DisplayName.Trim(), itemMaterialName, itemMaterials, customVisual, _session.SnapshotIconName)
                    : _exporter.SaveItemOverwrite(selected.Prefab, selected.Name, itemMaterialName, itemMaterials, customVisual, _session.SnapshotIconName);
            }
            else
            {
                GetPieceMaterialNames(materialName, out string fullHealthMaterial, out string damagedMaterial);
                saved = clone
                    ? _exporter.SavePieceClone(selected.Name, _session.CloneName.Trim(), _session.DisplayName.Trim(), selected.PieceHammer, fullHealthMaterial, damagedMaterial, _session.SnapshotIconName)
                    : _exporter.SavePieceOverwrite(selected.Name, selected.PieceHammer, fullHealthMaterial, damagedMaterial, _session.SnapshotIconName);
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

        private bool CanUseItemMaterialArray()
        {
            return _session.SelectedObject?.Type == WackyDbObjectType.Item
                && _session.MaterialRoute == WackyDbMaterialRoute.Material
                && _session.SelectedRenderer
                && _session.SelectedRenderer.sharedMaterials.Length > 1
                && _session.SelectedMaterialSlot >= 0
                && _session.SelectedMaterialSlot < _session.SelectedRenderer.sharedMaterials.Length;
        }

        private string[] BuildItemMaterialArray(string currentMaterialName, CustomVisual customVisual)
        {
            if (customVisual != null || !CanUseItemMaterialArray())
            {
                return null;
            }

            Material[] sourceMaterials = _session.SelectedRenderer.sharedMaterials;
            string[] materials = new string[sourceMaterials.Length];
            for (int index = 0; index < sourceMaterials.Length; index++)
            {
                Material source = sourceMaterials[index];
                materials[index] = index == _session.SelectedMaterialSlot
                    ? currentMaterialName
                    : source ? source.name : currentMaterialName;
            }
            return materials;
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
            bool hasAssignedVisual = !string.IsNullOrWhiteSpace(_session.BaseMaterialName)
                || !string.IsNullOrWhiteSpace(_session.ChestMaterialName)
                || !string.IsNullOrWhiteSpace(_session.LegsMaterialName);
            if (_session.MaterialRoute == WackyDbMaterialRoute.Material && !hasAssignedVisual)
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
                _preview.SetPrefab(_session.SelectedObject.Prefab, _usePlayerModelPreview);
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
                _preview.SetPrefab(candidate.Prefab, _usePlayerModelPreview && CanUsePlayerModelPreview());
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
                ResetUnsavedMaterialEdits();
                _session.MaterialChangesDirty = false;
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

    [DefaultExecutionOrder(-10000)]
    internal sealed class WackyDbCreateHotkeyListener : MonoBehaviour
    {
        private void Update()
        {
            if (WMRecipeCust.modEnabled != null
                && WMRecipeCust.modEnabled.Value
                && WMRecipeCust.creatorHotkey != null
                && ZInput.GetKeyDown(WMRecipeCust.creatorHotkey.Value))
            {
                WackyDbCreateWindow.ToggleFromHotkey();
            }
        }
    }
}
