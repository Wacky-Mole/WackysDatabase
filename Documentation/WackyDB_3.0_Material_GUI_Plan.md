# WackyDB 3.0 Material GUI Plan

## Goal

Build an in-game `OnGUI` editor opened with `wackydb_create` that makes WackyDB's material system usable without Unity.

Primary use cases:

- Recolor items, armor, pieces, and other prefabs.
- Preview generic prefabs in-game.
- Select renderer/material slots.
- Reuse/shared material YAML across multiple prefabs.
- Save either an overwrite YAML for the selected prefab or a clone YAML with one click.

## Confirmed Product Decisions

1. **GUI:** Use Unity `OnGUI` / IMGUI for the first implementation.
2. **Preview:** Use a generic prefab preview first, not a player/armor mannequin.
3. **Default save mode:** Overwrite existing object by default.
4. **Clone flow:** Provide an easy one-click `Clone New Object` option.
5. **Material sharing:** Prefer shared named materials. Multiple prefabs should be able to reference the same material YAML.

---

## Discovery Summary

### Existing Material Data Model

File: `Datas/MaterialData.cs`

Existing YAML-backed structures:

- `MaterialInstance`
  - `name`
  - `original`
  - `overwrite`
  - `changes`
- `MaterialData`
  - `Dictionary<string, Color> colors`
  - `Dictionary<string, float> floats`
  - `Dictionary<string, Texture2D> textures`

This is a good base for the editor. The GUI should generate/modify `MaterialInstance` objects rather than hand-writing YAML strings.

### Material Runtime Cache / Application

File: `Visuals/DataManagers/MaterialDataManager.cs`

Important existing behavior:

- `MaterialDataManager.Instance` is the central material manager.
- `materials` stores WackyDB-created material instances.
- `Cache(MaterialInstance mi)`:
  - If `overwrite = true`, applies changes directly to `WMRecipeCust.originalMaterials[mi.original]`.
  - Otherwise clones `WMRecipeCust.originalMaterials[mi.original]`, names it `mi.name`, stores it in:
    - `WMRecipeCust.originalMaterials`
    - `MaterialDataManager.Instance.materials`
  - Applies changes using `MaterialManipulator`.

Implication for GUI:

- Shared materials should be saved as `MaterialInstance` with `overwrite = false`.
- Applying a shared material to an object should only write item/piece YAML pointing to `material: SharedMaterialName`.
- Editing an existing shared material should warn because all referencing objects may change.

### Material Property Application

File: `Visuals/Materials/MaterialManipulator.cs`

Existing behavior:

- Converts `MaterialData.colors` into `MaterialColorEffect`.
- Converts `MaterialData.floats` into `MaterialFloatEffect`.
- Converts `MaterialData.textures` into `MaterialTextureEffect`.
- Can apply effects to a `Renderer` or a `Material`.

Implication for GUI:

- Live preview can safely use the same change model:
  1. Clone material for preview.
  2. Build a temporary `MaterialData` from GUI state.
  3. Apply with `MaterialManipulator`.

### Texture System

File: `Visuals/DataManagers/TextureDataManager.cs`

Existing behavior:

- Textures are stored in `WMRecipeCust.assetPathTextures` as `.png` files.
- `LoadTexture(name)` loads `Textures/{name}.png`.
- `GetTexture(name)` caches loaded textures in `textureCache`.
- `SaveTexture(...)` can export material textures to `.png`.

Implication for GUI:

- Texture browser can start by listing `*.png` in `WMRecipeCust.assetPathTextures`.
- Assigning textures should populate `MaterialData.textures`.
- The existing `TextureConverter` handles YAML texture serialization/deserialization.

### Visual Controller / Prefab Material References

File: `Visuals/VisualController.cs`

Existing behavior:

- Hooks material manager events.
- `UpdatePrefab(string name, CustomVisual visual)` updates item visual material references.
- Uses `PrefabAssistant.GetRenderers(...)`, `PrefabAssistant.UpdateMaterialReference(...)`, and armor-specific helpers.

Implication for GUI:

- MVP should use simple top-level `material` where possible.
- Later, advanced armor slot support can use `CustomVisual`.
- `customVisual` should be phase 2 or 3, not MVP, because armor material routing is special.

### Prefab / Renderer Inspection

File: `Visuals/PrefabAssistant.cs`

Useful existing APIs:

- `GetRenderers(GameObject item)`
  - Checks `attach_skin`, `attach`, and drop child.
- `Describe(string prefabName)`
  - Already inspects renderers, materials, shader names, shader properties, property types, ranges, and values.
- `SaveMaterial(string materialName)`
  - Can write a cloned material YAML from an existing material.

Important discovery:

- `Describe(...)` already uses shader property APIs:
  - `shader.GetPropertyCount()`
  - `shader.GetPropertyType(k)`
  - `shader.GetPropertyName(k)`
  - `shader.GetPropertyRangeLimits(k)`

Implication for GUI:

- We do **not** need to guess all property names.
- The editor can discover shader properties dynamically, then initially expose only color properties in MVP.
- Later, expose texture/float/range properties.

### YAML Loader / Writer

File: `GetData/YamlLoader.cs`

Existing behavior:

- Uses `YamlDotNet` with:
  - `ColorConverter`
  - `TextureConverter`
  - `ValheimTimeConverter`
- `Write<T>(file, data)` serializes and writes YAML.

Implication for GUI:

- `WackyDbYamlExporter` should use `YamlLoader.Write(...)`.
- Avoid manual YAML strings except maybe small preview/debug text.

### Item / Piece YAML Models

Files:

- `Datas/ItemData.cs`
- `Datas/PieceData.cs`

Important fields:

`WItemData`:

- `name`
- `m_name`
- `clonePrefabName`
- `customIcon`
- `material`
- `materials`
- `customVisual`
- `snapshotOnMaterialChange`

`PieceData`:

- `name`
- `piecehammer`
- `m_name`
- `clonePrefabName`
- `material`
- `damagedMaterial`
- `customIcon`
- `piecehammerCategory`

Implication for GUI:

- Item overwrite output can be very small:
  - `name`
  - `m_weight` may be required by current model/comment, so verify before implementation.
  - `material`
- Piece overwrite output needs at least:
  - `name`
  - `piecehammer`
  - `material`
- Clone output uses `clonePrefabName` and new `name`.

### Existing Command Registration

File: `PatchClasses/console.cs`

Existing commands are registered in `Terminal.InitTerminal` postfix.

Implication for GUI:

- Add `wackydb_create` command in `Console_Patch.Postfix()`.
- The command should call something like:
  - `WackyDbCreateWindow.Toggle()`
  - `WackyDbCreateWindow.Open(optionalPrefabName)`

### Project Structure / Compilation

File: `WackysRecipeCustomization.csproj`

The project uses explicit `Compile Include` entries.

Implication:

- Any new `.cs` file must be added to the `.csproj`.
- Recommended new folder:
  - `VisualEditor/`

---

## Proposed New Files

```text
VisualEditor/
  WackyDbCreateWindow.cs
  WackyDbEditorSession.cs
  WackyDbObjectSelector.cs
  WackyDbMaterialLibrary.cs
  WackyDbMaterialSlotInspector.cs
  WackyDbPreviewRenderer.cs
  WackyDbYamlExporter.cs
  WackyDbGuiStyles.cs
```

Optional later:

```text
VisualEditor/
  WackyDbTextureBrowser.cs
  WackyDbColorUtility.cs
  WackyDbSharedMaterialUsageScanner.cs
```

---

## Proposed Core Classes

### `WackyDbCreateWindow`

Main `OnGUI` controller.

Responsibilities:

- Window open/close state.
- Draggable window rect.
- Search box.
- Object list.
- Preview area.
- Renderer/material slot panel.
- Material edit panel.
- Save/clone panel.

Important methods:

```csharp
internal static void Open(string prefabName = null);
internal static void Toggle();
private void OnGUI();
private void DrawWindow(int id);
```

Implementation note:

- Since `BaseUnityPlugin` already exists as `WMRecipeCust.context`, either:
  - add a small MonoBehaviour component to the plugin object, or
  - create a hidden GameObject for `WackyDbCreateWindow`.

### `WackyDbEditorSession`

Holds current editor state.

Suggested fields:

```csharp
internal sealed class WackyDbEditorSession
{
    public GameObject SelectedPrefab;
    public string SelectedPrefabName;
    public WackyDbObjectType SelectedObjectType;

    public Renderer SelectedRenderer;
    public int SelectedMaterialSlot;

    public string OriginalMaterialName;
    public string SelectedSharedMaterialName;
    public string NewMaterialName;

    public Material PreviewMaterial;
    public MaterialData WorkingChanges;

    public bool IsEditingExistingSharedMaterial;
    public bool IsCreatingNewMaterial;
    public bool SaveAsClone;

    public string CloneName;
    public string DisplayName;
    public string PieceHammer;
}
```

Suggested enum:

```csharp
internal enum WackyDbObjectType
{
    Unknown,
    Item,
    Piece,
    Prefab
}
```

### `WackyDbObjectSelector`

Builds searchable object lists.

Initial MVP sources:

1. `ObjectDB.instance.m_items`
2. `ZNetScene.instance.m_prefabs`
3. Known piece tables for piece metadata / hammer lookup

Methods:

```csharp
IReadOnlyList<WackyDbObjectCandidate> GetCandidates();
IReadOnlyList<WackyDbObjectCandidate> Search(string text);
WackyDbObjectCandidate Resolve(string prefabName);
```

Candidate structure:

```csharp
internal sealed class WackyDbObjectCandidate
{
    public string Name;
    public string DisplayName;
    public GameObject Prefab;
    public WackyDbObjectType Type;
    public string PieceHammer;
}
```

### `WackyDbMaterialLibrary`

Central shared material registry.

Sources:

- `WMRecipeCust.originalMaterials`
- `MaterialDataManager.Instance.materials`
- `.yml` files in `WMRecipeCust.assetPathMaterials`

Methods:

```csharp
List<string> GetKnownMaterialNames();
Material GetMaterial(string name);
bool HasMaterialYaml(string name);
MaterialInstance LoadMaterialYaml(string name);
bool IsWackyMaterial(string name);
```

Shared material warning support:

```csharp
int CountYamlReferences(string materialName);
```

MVP can implement this by scanning `Items/*.yml` and `Pieces/*.yml` for `material: materialName`.

### `WackyDbMaterialSlotInspector`

Discovers renderers/material slots for the selected prefab.

Methods:

```csharp
List<WackyDbRendererInfo> GetRendererInfos(GameObject prefab);
List<WackyDbShaderPropertyInfo> GetShaderProperties(Material material);
```

Use `PrefabAssistant.GetRenderers(...)` first, but add fallback to `prefab.GetComponentsInChildren<Renderer>(true)` because pieces and world objects may not use item attach paths.

Property discovery should reuse the same shader APIs used by `PrefabAssistant.Describe(...)`.

### `WackyDbPreviewRenderer`

Generic prefab preview.

Responsibilities:

- Clone selected prefab safely.
- Disable network init:
  - `ZNetView.m_forceDisableInit = true` around instantiate.
- Put clone on preview layer.
- Disable colliders/rigidbodies/joints/scripts that could run unwanted behavior.
- Apply preview material changes to cloned renderers only.
- Render to `RenderTexture`.
- Provide rotate/zoom.
- Destroy clone safely when selection changes/window closes.

Do not mutate original prefab materials during preview.

### `WackyDbYamlExporter`

Writes YAML outputs.

Methods:

```csharp
bool SaveMaterial(MaterialInstance material);
bool SaveItemOverwrite(string prefabName, string materialName);
bool SavePieceOverwrite(string prefabName, string pieceHammer, string materialName);
bool SaveItemClone(string originalPrefabName, string cloneName, string displayName, string materialName);
bool SavePieceClone(string originalPrefabName, string cloneName, string displayName, string pieceHammer, string materialName);
```

Use `YamlLoader.Write(...)`.

---

## MVP UI Layout

```text
WackyDB Creator

[Search prefab/item/piece......................]

Left: Object Results
  ArmorIronChest       Item
  piece_woodwall       Piece/Hammer
  SwordIron            Item

Center: Preview
  [RenderTexture]
  Rotate: [<] [>]    Zoom [-] [+]

Right: Material Editor
  Renderer:
    attach_skin/body
  Material Slots:
    Slot 0: ArmorIronChest_mat
    Slot 1: Leather_mat

  Shared Material:
    [Existing material dropdown/search]
    [Use Selected]
    [New Shared Material]
    [Duplicate Current]

  Color Properties:
    _Color [R] [G] [B] [A]
    _EmissionColor [R] [G] [B] [A]

Bottom: Save
  Default: Overwrite Existing
  [Save Overwrite YAML]

  Clone:
    Clone Name: ArmorIronChest_Blue
    Display Name: Blue Iron Chest
    [Clone New Object]

  [Save Material YAML]
  [Save + Reload]
```

---

## Save Behavior

### Applying Existing Shared Material

If user selects an existing material and does not edit it:

- Save only item/piece YAML.
- Do not duplicate material YAML.

Item example:

```yaml
name: ArmorIronChest
material: WackyBlueIron
```

Piece example:

```yaml
name: piece_woodwall
piecehammer: Hammer
material: WackyBlueIron
```

### Creating New Shared Material

Save one material YAML:

```yaml
name: WackyBlueIron
original: ArmorIronChest_mat
overwrite: false
changes:
  colors:
    _Color: [0.1, 0.2, 1, 1]
```

Then save item/piece YAML referencing it.

### Editing Existing Shared Material

Before save, show warning:

```text
This material may be used by multiple objects.
Editing it may affect all objects that reference it.

[Edit Shared Material] [Duplicate Instead] [Cancel]
```

MVP fallback:

- Always recommend `Duplicate Instead` when `CountYamlReferences(materialName) > 1`.

### Clone New Object

Item clone:

```yaml
name: ArmorIronChest_Blue
clonePrefabName: ArmorIronChest
m_name: Blue Iron Chest
material: WackyBlueIron
```

Piece clone:

```yaml
name: piece_woodwall_blue
clonePrefabName: piece_woodwall
piecehammer: Hammer
m_name: Blue Wood Wall
material: WackyBlueIron
```

---

## Implementation Phases

### Phase 1 — Command + Window Skeleton

Deliverables:

- Add `VisualEditor/WackyDbCreateWindow.cs`.
- Register `wackydb_create` in `PatchClasses/console.cs`.
- Add new files to `.csproj`.
- Open/close draggable `OnGUI` window.
- Basic search text field.

Validation:

- Command opens/closes window in-game.

### Phase 2 — Object Discovery

Deliverables:

- Add `WackyDbObjectSelector`.
- List `ObjectDB` items and `ZNetScene` prefabs.
- Identify item vs piece where possible.
- Capture `piecehammer` for pieces where possible.

Validation:

- Searching `ArmorIronChest`, `SwordIron`, and a build piece finds candidates.

### Phase 3 — Renderer / Material Slot Inspector

Deliverables:

- Add `WackyDbMaterialSlotInspector`.
- Show renderers and material slots.
- Show shader name.
- Show discovered color properties using shader APIs.

Validation:

- Selecting an item/piece shows material slot names.

### Phase 4 — Generic Preview

Deliverables:

- Add `WackyDbPreviewRenderer`.
- Render selected prefab to `RenderTexture`.
- Rotate/zoom.
- Safe clone cleanup.

Validation:

- Preview renders common items and pieces without `ZNetScene` null refs.

### Phase 5 — Shared Material Picker + Color Editing

Deliverables:

- Add `WackyDbMaterialLibrary`.
- List known materials.
- Select existing material.
- Create new shared material name.
- Edit color properties.
- Apply live preview to preview clone.

Validation:

- Changing `_Color` visibly changes preview only.

### Phase 6 — YAML Save

Deliverables:

- Add `WackyDbYamlExporter`.
- Save material YAML to `Materials`.
- Save item overwrite YAML to `Items`.
- Save piece overwrite YAML to `Pieces`.
- Clone item/piece YAML.
- Optional `Save + Reload` button.

Validation:

- Generated YAML loads through existing WackyDB reload.

### Phase 7 — Texture Browser

Deliverables:

- List textures in `Textures` folder.
- Preview selected texture thumbnail.
- Assign texture property.
- Save texture refs into `MaterialData.textures`.

Validation:

- `_MainTex` texture swap saves and reloads.

### Phase 8 — Advanced / Armor-Specific Support

Deliverables:

- `customVisual` generation.
- Armor-specific material slots: `base_mat`, `chest`, `legs`.
- Optional mannequin preview.

Validation:

- Chest/legs armor material editing works without manual YAML.

---

## Technical Notes / Risks

### 1. Avoid mutating shared runtime materials during preview

Use cloned preview materials only.

Bad:

```csharp
renderer.sharedMaterial.SetColor(...)
```

Good:

```csharp
Material preview = Material.Instantiate(original);
preview.SetColor(...);
previewRenderer.sharedMaterials[slot] = preview;
```

### 2. Network-safe preview clones

Wrap prefab instantiation:

```csharp
ZNetView.m_forceDisableInit = true;
try
{
    clone = UnityEngine.Object.Instantiate(prefab);
}
finally
{
    ZNetView.m_forceDisableInit = false;
}
```

### 3. Use fallback renderer discovery

`PrefabAssistant.GetRenderers(...)` is item-focused. Generic prefab preview should fall back to:

```csharp
prefab.GetComponentsInChildren<Renderer>(true)
```

### 4. Piece metadata is harder than item metadata

Piece save requires `piecehammer`. Object discovery should try to map pieces back to hammer tables.

### 5. Required fields in `WItemData`

`WItemData.m_weight` is currently commented as required. Before coding exporter, verify whether minimal overwrite YAML without `m_weight` is accepted. If not, exporter should preserve current item weight from prefab.

---

## Recommended First PR / First Coding Task

Implement only:

- `wackydb_create`
- OnGUI window skeleton
- object search/select
- renderer/material slot inspector

Do **not** implement preview or saving in the first step.

This validates the hard foundation: selecting a prefab and understanding its material slots.

## Success Criteria for First Task

- Command opens window.
- User can search/select a prefab.
- Window displays:
  - prefab name
  - object type
  - renderer names
  - material slot names
  - shader names
  - color property names

No YAML writes yet.
