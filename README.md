# DFD Editor Refactoring Package

## Package Contents (Flat Structure - Won't Overwrite!)

```
dfd-refactored/
├── refactor-dfdeditor.ps1           # CSS/JS extraction + new UI features
├── patch-importservice.ps1          # Mermaid label fix
├── patch-edge-bundling.ps1          # Edge fan-out fix
├── patch-rearrangemode.ps1          # Add rearrangeMode field
├── new-files/
│   ├── LayoutOptimizationService.cs # → copy to Services\
│   └── DFDEditor.LayoutOptimization.cs # → copy to Pages\
└── README.md
```

**Note:** New files are in `new-files/` folder - won't overwrite your existing Services/Pages folders!

---

## Quick Install (5 Steps)

```powershell
# 1. Copy new files
cd R:\dfd2wasmTwin
Copy-Item "path\to\new-files\LayoutOptimizationService.cs" "Services\" -Force
Copy-Item "path\to\new-files\DFDEditor.LayoutOptimization.cs" "Pages\" -Force
Copy-Item "path\to\*.ps1" ".\" -Force

# 2. Run all patches
.\patch-rearrangemode.ps1    # Add missing field
.\refactor-dfdeditor.ps1      # CSS/JS + UI updates
.\patch-importservice.ps1     # Mermaid fix
.\patch-edge-bundling.ps1     # Edge fan-out fix

# 3. Manually add bundling call (see below)

# 4. Build
dotnet build

# 5. Deploy
cd ..\dfd2wasm && dotnet publish -c Release
xcopy /E /Y "..\dfd2wasmTwin\bin\Release\net9.0\browser-wasm\publish\wwwroot\*" "."
```

---

## Manual Step: Add Edge Bundling Call

After running patches, edit `DFDEditor.razor.cs`:

**In LoadDiagram method** (after creating nodes/edges from import):
```csharp
GeometryService.BundleAllEdges(nodes, edges);
RecalculateAllEdgePaths();
```

**In LoadExample method** (before StateHasChanged):
```csharp
GeometryService.BundleAllEdges(nodes, edges);
```

---

## Changes Included

### 1. Code Refactoring (`refactor-dfdeditor.ps1`)
- Extracts CSS to `DFDEditor.razor.css` (Blazor CSS isolation)
- Extracts JS to `wwwroot/js/dfdeditor.js`
- Adds Rearrange/Move mode to Select dropdown
- Adds Layout Optimization section (4 buttons)

### 2. Mermaid Label Fix (`patch-importservice.ps1`)
- `N1[Customer]` now shows "Customer" instead of "N1"
- Two-pass parsing: node definitions first, then edges

### 3. Edge Fan-Out Fix (`patch-edge-bundling.ps1`)
- Multiple edges leaving same side of a node now share the CENTER point
- Creates clean fan-out pattern instead of messy parallel lines

**Before:**
```
    ╭───────╮
    │ Node  │──→
    │       │───→
    │       │────→
    ╰───────╯─────→
```

**After:**
```
    ╭───────╮     →
    │ Node  │────→
    │       │    →
    ╰───────╯    →
```

### 4. Layout Optimization (`LayoutOptimizationService.cs`)
- Simulated annealing algorithm
- Fitness function with multiple metrics
- Quick overlap removal
- Layout compaction

---

## Cleanup

After installation, you can delete:
- The extracted `dfd-refactored` folder
- The `.ps1` scripts from project root (optional, keep for re-running)

---

## Troubleshooting

### Build errors after patching?
```powershell
# Restore original files
git checkout -- Pages/DFDEditor.razor
git checkout -- Services/ImportService.cs
git checkout -- Services/GeometryService.cs
```

### Edge bundling not working?
Make sure to call `GeometryService.BundleAllEdges(nodes, edges)` after:
- Loading from JSON
- Importing from Mermaid/DOT
- Loading examples

### CSS not applying?
Blazor CSS isolation requires the file to be named exactly:
`DFDEditor.razor.css` (same name as component + `.css`)
