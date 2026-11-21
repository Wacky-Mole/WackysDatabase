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
        private const float SidePreviewMinWidth = 920f;
        private static readonly Color Accent = new Color(0.52f, 0.91f, 0.83f);

        private enum Page { Browse, Preview, Edit, Export }
        private enum SaveMode { Clone, Overwrite, Material }

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
        private Vector2 _exportScroll;
        private Vector2 _materialScroll;
        private Page _page;
        private SaveMode _saveMode;
        private WackyDbObjectType _objectFilter = WackyDbObjectType.Item;
        private List<WackyDbObjectCandidate> _results;
        private WackyDbObjectType _lastObjectFilter;
        private string _lastSearchText;
        private GUIStyle _heading;
        private GUIStyle _body;
        private GUIStyle _resultButton;
        private GUIStyle _skinLabel;
        private string _searchText = string.Empty;
        private string _status = string.Empty;
        private string _materialSearch = string.Empty;
        private string _materialLibrarySelection = string.Empty;
        private string _hoveredMaterialName = string.Empty;
        private int _sharedReferenceCount;
        private bool _previewDragging;
        private Vector2 _previewPointerDown;
        private bool _fullScreen = true;
        private bool _toggleFullScreen;
        private bool _showTutorial;
        private string _textureSearch = string.Empty;
        private string _selectedTextureName = string.Empty;
        private string _selectedTextureProperty = string.Empty;
        private bool _showSharedMaterialLibrary;
        private bool _showColorEditor = true;
        private bool _showFloatEditor;
        private bool _showTextureEditor;
        private bool _showRendererSlots = true;
        private bool _showMaterialSettings;
        private Renderer _pickedRenderer;
        private WackyDbObjectCandidate _pendingSelection;
        private bool _pendingClose;
        private bool _pendingReset;
        private bool _confirmOverwrite;
        private bool _confirmOverwriteReload;
        private bool _usePlayerModelPreview;

        private void Awake()
        {
            _showTutorial = PlayerPrefs.GetInt(TutorialPreferenceKey, 1) != 0;
        }

        internal static void Open(string prefabName = null)
        {
            WackyDbCreateWindow window = EnsureInstance();
            if (!window)
            {
                return;
            }

            if (!window.enabled)
            {
                window.CenterWindow();
                window.enabled = true;
                window.RefreshCandidates();
                if (window._session.SelectedObject != null)
                {
                    WackyDbObjectCandidate previous = window._selector.Resolve(window._session.SelectedObject.Name);
                    if (previous != null)
                    {
                        window.SelectImmediately(previous);
                    }
                    else
                    {
                        window._session.ClearSelection();
                        window._page = Page.Browse;
                        window._status = "The previous object isn't loaded. Choose another in Browse.";
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(prefabName))
            {
                window._searchText = prefabName.Trim();
                WackyDbObjectCandidate candidate = window._selector.Resolve(window._searchText);
                if (candidate != null)
                {
                    window._objectFilter = candidate.Type;
                    window.Select(candidate);
                }
                else
                {
                    window._objectFilter = WackyDbObjectType.Unknown;
                    window._page = Page.Browse;
                    window._status = "No exact prefab named " + window._searchText + ". Try the matches in Browse.";
                }
            }
        }

        private static void Toggle()
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

        internal static void OpenWithGameUi(string prefabName = null)
        {
            Open(prefabName);
            if (_instance && _instance.enabled)
            {
                WMRecipeCust.context.StartCoroutine(PrepareGameUi());
            }
        }

        internal static void ToggleFromHotkey()
        {
            if (_lastHotkeyToggleFrame == Time.frameCount)
            {
                return;
            }

            _lastHotkeyToggleFrame = Time.frameCount;
            Toggle();
            if (_instance && _instance.enabled && !_instance._pendingClose)
            {
                WMRecipeCust.context.StartCoroutine(PrepareGameUi());
            }
        }

        private static System.Collections.IEnumerator PrepareGameUi()
        {
            yield return null;

            if (global::Console.instance && global::Console.IsVisible())
            {
                global::Console.instance.m_chatWindow.gameObject.SetActive(false);
            }

            yield return null;

            if (_instance && _instance.enabled && InventoryGui.instance && !InventoryGui.IsVisible())
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

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                if (HasConfirmation)
                {
                    CancelConfirmation();
                }
                else
                {
                    Close();
                }
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
            _windowRect = GUILayout.Window(WindowId, _windowRect, DrawWorkspace, "WackyDB Creator");
            if (_toggleFullScreen)
            {
                _toggleFullScreen = false;
                ToggleFullScreen();
            }
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, Mathf.Max(0f, Screen.width - _windowRect.width));
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, Mathf.Max(0f, Screen.height - _windowRect.height));
        }

        private void DrawWorkspace(int id)
        {
            EnsureStyles();
            if (GUI.Button(
                    new Rect(_windowRect.width - 38f, 2f, 32f, 20f),
                    new GUIContent("X", "Close the creator (Esc).")))
            {
                Close();
            }

            bool hasSidePreview = _windowRect.width >= SidePreviewMinWidth;
            Page page = hasSidePreview && _page == Page.Preview ? Page.Edit : _page;
            GUILayout.BeginVertical();
            DrawWorkspaceHeader();
            if (HasConfirmation)
            {
                DrawConfirmationPanel();
                GUILayout.FlexibleSpace();
            }
            else
            {
                DrawNavigation(page, hasSidePreview);
                if (_showTutorial)
                {
                    DrawGuide(page);
                }

                GUILayout.Space(6f);
                GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
                if (hasSidePreview)
                {
                    DrawPreview(Mathf.Min(380f, _windowRect.width * 0.36f));
                    GUILayout.Space(8f);
                }

                switch (page)
                {
                    case Page.Browse: DrawBrowser(); break;
                    case Page.Preview: DrawPreview(Mathf.Min(440f, _windowRect.width - 24f)); break;
                    case Page.Edit: DrawSelectionDetails(); break;
                    case Page.Export: DrawSavePanel(); break;
                }
                GUILayout.EndHorizontal();
            }

            if (!string.IsNullOrEmpty(_status))
            {
                GUILayout.Space(4f);
                GUILayout.Label(_status, _body);
            }

            GUILayout.Space(2f);
            string tip = string.IsNullOrEmpty(GUI.tooltip) ? "Hover over a control for details." : GUI.tooltip;
            GUILayout.Label(tip, _body, GUILayout.Height(32f));

            GUILayout.EndVertical();
            if (!_fullScreen)
            {
                GUI.DragWindow(new Rect(0f, 0f, _windowRect.width - 45f, 24f));
            }
        }

        private void EnsureStyles()
        {
            if (_skinLabel == GUI.skin.label)
            {
                return;
            }
            _skinLabel = GUI.skin.label;
            _body = new GUIStyle(_skinLabel) { wordWrap = true, richText = false };
            _heading = new GUIStyle(_body) { fontSize = 17, fontStyle = FontStyle.Bold };
            _heading.normal.textColor = Accent;
            _resultButton = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                wordWrap = false,
                padding = new RectOffset(12, 12, 4, 4)
            };
        }

        private static bool AccentButton(string label, bool highlighted, params GUILayoutOption[] options)
        {
            Color previous = GUI.backgroundColor;
            if (highlighted)
            {
                GUI.backgroundColor = previous * Accent;
            }
            bool pressed = GUILayout.Button(label, options);
            GUI.backgroundColor = previous;
            return pressed;
        }

        private void DrawWorkspaceHeader()
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label(HasUnsavedMaterialEdits ? "Material Studio | Unsaved edits" : "Material Studio", _heading);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_fullScreen ? "Windowed" : "Full Screen"))
            {
                _toggleFullScreen = true;
            }
            if (GUILayout.Button(_showTutorial ? "Hide Guide" : "Show Guide"))
            {
                _showTutorial = !_showTutorial;
                PlayerPrefs.SetInt(TutorialPreferenceKey, _showTutorial ? 1 : 0);
                PlayerPrefs.Save();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawNavigation(Page activePage, bool hasSidePreview)
        {
            GUILayout.BeginHorizontal();
            DrawPageButton(Page.Browse, activePage, "1. Browse");
            if (!hasSidePreview)
            {
                DrawPageButton(Page.Preview, activePage, "Preview");
            }
            DrawPageButton(Page.Edit, activePage, "2. Edit");
            DrawPageButton(Page.Export, activePage, "3. Export");
            GUILayout.EndHorizontal();
        }

        private void DrawPageButton(Page page, Page activePage, string label)
        {
            if (AccentButton(label, page == activePage, GUILayout.Height(28f), GUILayout.ExpandWidth(true)))
            {
                GUI.FocusControl(null);
                _previewDragging = false;
                _page = page;
            }
        }

        private void DrawGuide(Page page)
        {
            string instruction = page switch
            {
                Page.Browse => "Search by in-game name or prefab ID. Select a result to start editing.",
                Page.Preview => "Drag to rotate. Click a visible part to find the material it uses.",
                Page.Edit when !_session.WorkingBaseMaterial => "Choose a material slot, or click a part of the model to find it.",
                Page.Edit => "Try a color or texture change. Check the preview, then open Export when you're ready.",
                _ => "Start with a new copy to keep the original prefab unchanged. Export writes WackyDB YAML files."
            };
            GUILayout.Label(instruction, _body);
        }

        private void DrawBrowser()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Label("Find an object", _heading);
            GUILayout.BeginHorizontal();
            _searchText = GUILayout.TextField(_searchText);
            if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
            {
                _searchText = string.Empty;
            }
            if (GUILayout.Button("Refresh", GUILayout.ExpandWidth(false)))
            {
                RefreshCandidates();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawObjectFilter("Items", WackyDbObjectType.Item);
            DrawObjectFilter("Pieces", WackyDbObjectType.Piece);
            DrawObjectFilter("Other", WackyDbObjectType.Prefab);
            DrawObjectFilter("All", WackyDbObjectType.Unknown);
            GUILayout.EndHorizontal();

            if (_results == null || _lastSearchText != _searchText || _lastObjectFilter != _objectFilter)
            {
                _lastSearchText = _searchText;
                _lastObjectFilter = _objectFilter;
                _results = _selector.Search(_searchText, _objectFilter);
                _resultScroll = Vector2.zero;
            }
            GUILayout.Label(_results.Count == 0
                ? "No matches. Try another search or switch to All."
                : _results.Count > MaximumVisibleResults
                    ? $"Showing {MaximumVisibleResults} of {_results.Count}. Search to narrow the list."
                    : $"{_results.Count} matches", _body);
            _resultScroll = GUILayout.BeginScrollView(_resultScroll, GUILayout.ExpandHeight(true));
            int visibleCount = Mathf.Min(_results.Count, MaximumVisibleResults);
            for (int index = 0; index < visibleCount; index++)
            {
                WackyDbObjectCandidate candidate = _results[index];
                bool selected = IsCurrentObject(candidate);
                string label = candidate.Title == candidate.Name ? candidate.Name : $"{candidate.Title}  /  {candidate.Name}";
                if (GUILayout.Button(new GUIContent((selected ? "> " : string.Empty) + label, $"{candidate.Type}: {candidate.Name}"),
                    _resultButton, GUILayout.Height(28f), GUILayout.MinWidth(0f)))
                {
                    Select(candidate);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawObjectFilter(string label, WackyDbObjectType filter)
        {
            if (AccentButton(label, _objectFilter == filter, GUILayout.ExpandWidth(true)))
            {
                _objectFilter = filter;
            }
        }

        private void DrawPreview(float width)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(width), GUILayout.ExpandHeight(true));
            GUILayout.Label("Live preview", _heading);
            GUILayout.Label(_session.SelectedObject?.Title ?? "No object selected", _body);
            if (CanUsePlayerModelPreview() && GUILayout.Button(_usePlayerModelPreview ? "Show item model" : "Show on player"))
            {
                SaveCurrentMaterialEdit();
                _usePlayerModelPreview = !_usePlayerModelPreview;
                WackyDbRendererInfo selectedRendererInfo = _session.RendererInfos.Find(info => info.Renderer == _session.SelectedRenderer);
                if (_session.SelectedRenderer && !IsRendererVisibleForPreview(selectedRendererInfo))
                {
                    _session.ClearMaterialSelection();
                    _showRendererSlots = true;
                }
                _pickedRenderer = null;
                RebuildPreview();
            }

            float size = Mathf.Min(width - 24f, Mathf.Max(96f, _windowRect.height - 315f));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect previewRect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.Box(previewRect, GUIContent.none);

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
                GUI.Box(previewRect, _session.SelectedObject == null ? "Choose an object in Browse" : "Preview unavailable");
            }

            GUILayout.FlexibleSpace();
            if (_preview != null && _preview.CanPickRenderer)
            {
                GUILayout.Label("Drag to rotate. Click a part to edit it.", _body);
            }
            else if (_usePlayerModelPreview)
            {
                GUILayout.Label("Use the item model to select parts by clicking.", _body);
            }
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
                if (previewRect.Contains(current.mousePosition) && (current.mousePosition - _previewPointerDown).sqrMagnitude <= 16f)
                {
                    SelectPreviewRenderer(previewRect, current.mousePosition);
                }
                current.Use();
            }
            else if (current.type == EventType.ScrollWheel && previewRect.Contains(current.mousePosition))
            {
                _preview.Zoom(current.delta.y * 0.06f);
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
            _pickedRenderer = renderer;
            _page = Page.Edit;
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
            GUILayout.Label("Edit materials", _heading);
            if (_session.SelectedObject == null)
            {
                GUILayout.Label("Choose an object in Browse to see its materials here.", _body);
                GUILayout.EndVertical();
                return;
            }

            WackyDbObjectCandidate selected = _session.SelectedObject;
            GUILayout.Label($"{selected.Title} / {selected.Type}: {selected.Name}", _body);
            _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUILayout.ExpandHeight(true));
            int count = GetVisibleRendererCount();
            _showRendererSlots = GUILayout.Toggle(_showRendererSlots,
                (_showRendererSlots ? "Hide" : "Choose") + $" material slot ({count} renderers)", GUI.skin.button);
            if (_showRendererSlots)
            {
                if (_pickedRenderer && GUILayout.Button("Show all renderers"))
                {
                    _pickedRenderer = null;
                }
                foreach (WackyDbRendererInfo rendererInfo in _session.RendererInfos)
                {
                    if ((!_pickedRenderer || rendererInfo.Renderer == _pickedRenderer) && IsRendererVisibleForPreview(rendererInfo))
                    {
                        DrawRendererInfo(rendererInfo);
                    }
                }
                if (count == 0)
                {
                    GUILayout.Label("No editable renderers in this preview mode.", _body);
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
            if (!_session.SelectedRenderer || !_session.WorkingBaseMaterial)
            {
                GUILayout.Label("Choose a slot above to reveal its color, texture, and shader controls.", _body);
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label($"Slot {_session.SelectedMaterialSlot}: {_session.WorkingBaseMaterial.name}", _heading);
            GUILayout.Label(_session.IsEditingExistingSharedMaterial
                ? "Editing a shared material. Saving may affect other objects that use it."
                : "Changes are previewed here before you export.", _body);

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
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                List<string> matches = _materialLibrary.Search(_materialSearch, MaximumVisibleResults);
                if (Event.current.type == EventType.Repaint)
                {
                    _hoveredMaterialName = string.Empty;
                }

                _materialScroll = GUILayout.BeginScrollView(_materialScroll, GUILayout.Height(140f));
                foreach (string materialName in matches)
                {
                    string prefix = materialName == _materialLibrarySelection ? "[Selected] " : string.Empty;
                    if (GUILayout.Button(new GUIContent(prefix + materialName, materialName), _resultButton, GUILayout.MinWidth(0f)))
                    {
                        _materialLibrarySelection = materialName;
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
                GUI.enabled = pickerEnabled && !string.IsNullOrEmpty(_materialLibrarySelection);
                if (GUILayout.Button("Apply Selected Shared Material"))
                {
                    UseSharedMaterial(_materialLibrarySelection);
                }
                GUI.enabled = pickerEnabled;
            }

            if (_session.IsEditingExistingSharedMaterial && _sharedReferenceCount > 1)
            {
                GUILayout.Label($"Used by {_sharedReferenceCount} object YAML files. Make a material copy to keep those objects unchanged.", _body);
            }
            if (!_session.IsCreatingNewMaterial && GUILayout.Button("Make a material copy"))
            {
                DuplicateWorkingMaterial();
            }

            if (_session.MaterialRoute == WackyDbMaterialRoute.Chest)
            {
                SelectPreferredArmorTexture("_ChestTex");
            }
            else if (_session.MaterialRoute == WackyDbMaterialRoute.Legs)
            {
                SelectPreferredArmorTexture("_LegsTex", "_LegTex");
            }

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
            _showMaterialSettings = GUILayout.Toggle(_showMaterialSettings, "Advanced: material assignments", GUI.skin.button);
            if (_showMaterialSettings)
            {
                DrawNewMaterialName();
                DrawMaterialRouteEditor();
                DrawPieceMaterialRouteEditor();
            }
            if (GUILayout.Button("Reset preview to prefab defaults"))
            {
                if (HasUnsavedMaterialEdits)
                {
                    _pendingReset = true;
                }
                else
                {
                    ResetPreviewEdits();
                }
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
                    break;
                case WackyDbMaterialRoute.Legs:
                    GUILayout.Label("Saves as customVisual.legs and uses the material's leg texture property.");
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
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Label("Export YAML", _heading);
            if (_session.SelectedObject == null)
            {
                GUILayout.Label("Choose an object in Browse before exporting.", _body);
                GUILayout.EndVertical();
                return;
            }

            _exportScroll = GUILayout.BeginScrollView(_exportScroll, GUILayout.ExpandHeight(true));
            GUILayout.Label(_session.SelectedObject.Title, _body);
            if (!_session.WorkingBaseMaterial)
            {
                GUILayout.Label("Choose a material slot in Edit before exporting.", _body);
            }
            else
            {
                DrawExportOptions();
            }
            if (!string.IsNullOrEmpty(_exporter.LastSavedPath) && GUILayout.Button("Open saved YAML folder"))
            {
                OpenSavedFolder();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawExportOptions()
        {
            bool objectSupported = _session.SelectedObject.Type is WackyDbObjectType.Item or WackyDbObjectType.Piece;
            SaveMode mode = objectSupported ? _saveMode : SaveMode.Material;
            GUILayout.BeginHorizontal();
            if (objectSupported)
            {
                DrawSaveModeButton(SaveMode.Clone, mode, "New copy");
                DrawSaveModeButton(SaveMode.Overwrite, mode, "Change original");
            }
            DrawSaveModeButton(SaveMode.Material, mode, "Material only");
            GUILayout.EndHorizontal();

            if (!objectSupported)
            {
                GUILayout.Label("Only items and building pieces can be exported as objects. You can still export this material.", _body);
            }
            if (_session.IsCreatingNewMaterial)
            {
                DrawNewMaterialName();
            }
            else
            {
                GUILayout.Label("Material: " + GetActiveMaterialName(), _body);
            }

            bool previousEnabled = GUI.enabled;
            string materialName = GetActiveMaterialName();
            if (mode == SaveMode.Material)
            {
                GUILayout.Label(CanSaveMaterial()
                    ? "Writes a reusable material YAML without exporting the object."
                    : "Enter a material ID, or make a copy in Edit if you're using a built-in material.", _body);
                GUI.enabled = previousEnabled && CanSaveMaterial();
                if (AccentButton("Export material YAML", true, GUILayout.Height(28f)))
                {
                    SaveMaterialYaml(true);
                }
            }
            else
            {
                bool clone = mode == SaveMode.Clone;
                if (clone)
                {
                    GUILayout.Label("Creates a separate item or building piece. Use a unique prefab ID.", _body);
                    GUILayout.Label("New prefab ID", _body);
                    _session.CloneName = GUILayout.TextField(_session.CloneName);
                    GUILayout.Label("In-game name", _body);
                    _session.DisplayName = GUILayout.TextField(_session.DisplayName);
                }
                else
                {
                    GUILayout.Label($"Applies the material to {_session.SelectedObject.Name} when its YAML is loaded.", _body);
                }
                if (!string.IsNullOrEmpty(_session.SnapshotIconName))
                {
                    GUILayout.Label("Custom icon: " + _session.SnapshotIconName, _body);
                }
                if (CanUseItemMaterialArray())
                {
                    GUILayout.Label($"Only slot {_session.SelectedMaterialSlot} in the material array changes.", _body);
                }

                string cloneId = _session.CloneName.Trim();
                bool cloneNameTaken = clone && (string.Equals(cloneId, _session.SelectedObject.Name, StringComparison.OrdinalIgnoreCase)
                    || _selector.Resolve(cloneId) != null);
                bool cloneReady = !string.IsNullOrEmpty(cloneId) && !string.IsNullOrWhiteSpace(_session.DisplayName) && !cloneNameTaken;
                if (!CanSaveObject(materialName) || (clone && !cloneReady))
                {
                    GUILayout.Label(string.IsNullOrEmpty(materialName)
                        ? "Enter a material ID to continue."
                        : cloneNameTaken ? "That prefab ID is already in use. Choose a new one."
                        : "Enter a new prefab ID and an in-game name.", _body);
                }
                GUI.enabled = previousEnabled && CanSaveObject(materialName) && (!clone || cloneReady);
                if (AccentButton(clone ? "Export new copy" : "Export changes to original", true, GUILayout.Height(28f)))
                {
                    if (clone)
                    {
                        SaveObject(true, false);
                    }
                    else
                    {
                        RequestOverwrite(false);
                    }
                }
                if (!clone)
                {
                    bool canReload = ObjectDB.instance && WMRecipeCust.issettoSinglePlayer;
                    GUI.enabled = GUI.enabled && canReload;
                    if (GUILayout.Button("Export and reload now"))
                    {
                        RequestOverwrite(true);
                    }
                    GUI.enabled = previousEnabled;
                    if (!canReload)
                    {
                        GUILayout.Label("Reload is only available in a loaded single-player world.", _body);
                    }
                }
            }
            GUI.enabled = previousEnabled;
        }

        private void DrawNewMaterialName()
        {
            if (!_session.IsCreatingNewMaterial)
            {
                return;
            }

            GUILayout.Label("Material ID (used to reference the reusable material)", _body);
            string name = GUILayout.TextField(_session.NewMaterialName);
            if (_session.NewMaterialName != name)
            {
                _session.NewMaterialName = name;
                _session.MaterialChangesDirty = true;
            }
        }

        private void DrawSaveModeButton(SaveMode mode, SaveMode activeMode, string label)
        {
            if (AccentButton(label, mode == activeMode, GUILayout.ExpandWidth(true)))
            {
                _saveMode = mode;
            }
        }

        private bool HasConfirmation => _pendingSelection != null || _pendingClose || _pendingReset || _confirmOverwrite;

        private void CancelConfirmation()
        {
            _pendingSelection = null;
            _pendingClose = false;
            _pendingReset = false;
            _confirmOverwrite = false;
        }

        private void DrawConfirmationPanel()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            if (_confirmOverwrite)
            {
                GUILayout.Label("Change " + _session.SelectedObject.Name + "?", _heading);
                GUILayout.Label(_confirmOverwriteReload
                    ? "This can replace existing YAML for this prefab and reloads WackyDB data now."
                    : "This can replace existing YAML for this prefab. Changes apply when WackyDB loads the file.", _body);
                GUILayout.BeginHorizontal();
                if (AccentButton("Cancel", true))
                {
                    CancelConfirmation();
                }
                if (GUILayout.Button("Confirm overwrite"))
                {
                    bool reload = _confirmOverwriteReload;
                    CancelConfirmation();
                    SaveObject(false, reload);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Discard unsaved material edits?", _heading);
                GUILayout.Label("This only affects the preview. YAML files you've already exported are not changed.", _body);
                GUILayout.BeginHorizontal();
                if (AccentButton("Keep editing", true))
                {
                    CancelConfirmation();
                }
                if (GUILayout.Button("Discard edits"))
                {
                    WackyDbObjectCandidate selection = _pendingSelection;
                    bool close = _pendingClose;
                    bool reset = _pendingReset;
                    CancelConfirmation();
                    if (close)
                    {
                        CloseImmediately();
                    }
                    else if (reset)
                    {
                        ResetPreviewEdits();
                    }
                    else if (selection != null)
                    {
                        SelectImmediately(selection);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
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
            GUILayout.Label(rendererInfo.Path, _body);

            if (rendererInfo.Materials.Count == 0)
            {
                GUILayout.Label("  No material slots");
            }

            foreach (WackyDbMaterialInfo materialInfo in rendererInfo.Materials)
            {
                bool selected = _session.SelectedRenderer == rendererInfo.Renderer
                    && _session.SelectedMaterialSlot == materialInfo.Slot;
                string label = (selected ? "> " : string.Empty) + $"Slot {materialInfo.Slot}: {materialInfo.Name}";
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && materialInfo.Material;
                if (GUILayout.Button(new GUIContent(label, $"{label}\nShader: {materialInfo.ShaderName}"), _resultButton, GUILayout.MinWidth(0f)))
                {
                    SelectMaterialSlot(rendererInfo.Renderer, materialInfo);
                }
                GUI.enabled = previousEnabled;
            }

            GUILayout.EndVertical();
        }

        private void RefreshCandidates()
        {
            _results = null;
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

            if (_session.SelectedRenderer == renderer && _session.SelectedMaterialSlot == materialInfo.Slot)
            {
                _showRendererSlots = false;
                _detailScroll = Vector2.zero;
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
            _materialLibrarySelection = string.Empty;
            _selectedTextureProperty = string.Empty;
            _selectedTextureName = string.Empty;
            _textureSearch = string.Empty;
            string editKey = WackyDbEditorSession.GetMaterialEditKey(renderer, materialInfo.Slot);
            if (_session.MaterialEdits.TryGetValue(editKey, out WackyDbMaterialEditState existingEdit))
            {
                _session.RestoreMaterialEdit(existingEdit);
                _sharedReferenceCount = _session.IsEditingExistingSharedMaterial
                    ? _materialLibrary.CountYamlReferences(_session.SelectedSharedMaterialName) : 0;
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
            _sharedReferenceCount = 0;
            ApplyWorkingMaterial();
        }

        private void SaveCurrentMaterialEdit()
        {
            if (!_session.SelectedRenderer || !_session.WorkingBaseMaterial)
            {
                return;
            }

            string key = WackyDbEditorSession.GetMaterialEditKey(
                _session.SelectedRenderer,
                _session.SelectedMaterialSlot);
            _session.MaterialEdits[key] = _session.CaptureMaterialEdit();
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

        private void DuplicateWorkingMaterial()
        {
            if (!_session.WorkingBaseMaterial)
            {
                return;
            }

            string baseName = string.IsNullOrEmpty(_session.SelectedSharedMaterialName)
                ? _session.WorkingBaseMaterial.name : _session.SelectedSharedMaterialName;
            _session.NewMaterialName = baseName + "_Copy";

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
            SaveCurrentMaterialEdit();
            return true;
        }

        private bool HasUnsavedMaterialEdits
        {
            get
            {
                if (_session.MaterialChangesDirty)
                {
                    return true;
                }
                string activeKey = WackyDbEditorSession.GetMaterialEditKey(_session.SelectedRenderer, _session.SelectedMaterialSlot);
                foreach (KeyValuePair<string, WackyDbMaterialEditState> edit in _session.MaterialEdits)
                {
                    if (edit.Key != activeKey && edit.Value.MaterialChangesDirty)
                    {
                        return true;
                    }
                }
                return false;
            }
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

        private void ResetPreviewEdits()
        {
            _session.ClearMaterialSelection();
            _session.MaterialEdits.Clear();
            _session.BaseMaterialName = string.Empty;
            _session.ChestMaterialName = string.Empty;
            _session.LegsMaterialName = string.Empty;
            _session.StandardMaterialName = string.Empty;
            _session.PieceMaterialName = string.Empty;
            _session.DamagedPieceMaterialName = string.Empty;
            _showRendererSlots = true;
            _pickedRenderer = null;
            _detailScroll = Vector2.zero;
            if (RebuildPreview())
            {
                _status = "Preview restored to prefab defaults. Exported files were not changed.";
            }
        }

        private bool RebuildPreview()
        {
            if (_session.SelectedObject == null || !_session.SelectedObject.Prefab)
            {
                return false;
            }

            try
            {
                if (_preview == null)
                {
                    _preview = new WackyDbPreviewRenderer();
                }
                _preview.SetPrefab(_session.SelectedObject.Prefab, _usePlayerModelPreview && CanUsePlayerModelPreview());
                foreach (WackyDbMaterialEditState edit in _session.MaterialEdits.Values)
                {
                    if (edit.Renderer && edit.WorkingBaseMaterial)
                    {
                        _preview.ApplyMaterial(edit.Renderer, edit.Slot, edit.WorkingBaseMaterial, edit.WorkingChanges);
                    }
                }
                return true;
            }
            catch (Exception exception)
            {
                _status = "Preview unavailable: " + exception.Message;
                WMRecipeCust.WLog.LogWarning(_status);
                return false;
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
                if (_normalWindowRect.width > 0f)
                {
                    _windowRect = _normalWindowRect;
                }
                else
                {
                    CenterWindow();
                }
            }
            else
            {
                _normalWindowRect = _windowRect;
                _fullScreen = true;
            }
        }

        private void Select(WackyDbObjectCandidate candidate)
        {
            if (IsCurrentObject(candidate))
            {
                _page = Page.Edit;
                return;
            }

            CancelConfirmation();
            if (HasUnsavedMaterialEdits)
            {
                _pendingSelection = candidate;
                return;
            }
            SelectImmediately(candidate);
        }

        private bool IsCurrentObject(WackyDbObjectCandidate candidate)
        {
            WackyDbObjectCandidate current = _session.SelectedObject;
            return current != null && current.Prefab == candidate.Prefab && current.Type == candidate.Type
                && current.Name.Equals(candidate.Name, StringComparison.OrdinalIgnoreCase);
        }

        private void SelectImmediately(WackyDbObjectCandidate candidate)
        {
            _session.ClearSelection();
            _session.SelectedObject = candidate;
            _page = Page.Edit;
            _saveMode = SaveMode.Clone;
            _detailScroll = Vector2.zero;
            _exportScroll = Vector2.zero;
            _showRendererSlots = true;
            _pickedRenderer = null;
            _usePlayerModelPreview = false;
            _showSharedMaterialLibrary = false;
            _showMaterialSettings = false;
            _session.CloneName = candidate.Name + "_Wacky";
            _session.DisplayName = candidate.Title + " Wacky";
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

            RebuildPreview();
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
            if (HasUnsavedMaterialEdits)
            {
                CancelConfirmation();
                _pendingClose = true;
                return;
            }

            CloseImmediately();
        }

        private void CloseImmediately()
        {
            CancelConfirmation();
            _previewDragging = false;
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
