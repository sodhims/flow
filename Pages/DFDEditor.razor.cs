using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using dfd2wasm.Models;
using dfd2wasm.Services;

namespace dfd2wasm.Pages;

public partial class DFDEditor
{
    // Injected services - these override the @inject in razor file
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;
    [Inject] protected GeometryService GeometryService { get; set; } = default!;
    [Inject] protected PathService PathService { get; set; } = default!;
    [Inject] protected UndoService UndoService { get; set; } = default!;
    [Inject] protected ExportService ExportService { get; set; } = default!;
    [Inject] protected ShapeLibraryService shapeLibrary { get; set; } = default!;

    // Core state
    private List<Node> nodes = new();
    private List<Edge> edges = new();
    private List<EdgeLabel> edgeLabels = new();
    private List<int> selectedNodes = new();
    private List<int> selectedEdges = new();
    private List<int> selectedLabels = new();

    // Element references
    private ElementReference canvasRef;
    private ElementReference minimapRef;
    private ElementReference textInputRef;
    private ElementReference textEditTextarea;

    // Editor mode
    private EditorMode mode = EditorMode.Select;
    private NodeShape selectedShape = NodeShape.Rectangle;
    private bool useOrthoPlacement = false;
    private bool snapToGrid = false;

    // Connection state
    private int? pendingConnectionNodeId = null;
    private ConnectionPoint? pendingConnection = null;
    private (double X, double Y)? pendingConnectionPoint = null;
    private int? hoveredNodeId = null;

    // Edge reconnection state
    private int? pendingEdgeReconnectId = null;
    private string? pendingEdgeReconnectEnd = null;

    // Drag state
    private int? draggingNodeId = null;
    private double dragOffsetX = 0;
    private double dragOffsetY = 0;
    private double dragStartX = 0;
    private double dragStartY = 0;

    // Resize/drag state
    private int? resizingNodeId = null;
    private int? draggingEdgeId = null;
    private int draggingWaypointIndex = -1;
    private int? draggingLabelId = null;
    private int? resizingLabelId = null;

    // Editing state
    private int? editingNodeId = null;
    private int? editingLabelId = null;

    // ID counters
    private int nextId = 1;
    private int nextEdgeId = 1;
    private int nextLabelId = 1;

    // Dialog state
    private bool showExportDialog = false;
    private bool showLoadDialog = false;
    private bool showHelpDialog = false;
    private bool showAboutDialog = false;
    private string exportedContent = "";
    private string exportDialogTitle = "";
    private string exportDialogDescription = "";
    private string loadDiagramJson = "";
    private string loadErrorMessage = "";
    private string importFormat = "auto";


    // Panel collapse states (entire panels)
    private bool isLeftPanelCollapsed = false;
    private bool isRightPanelCollapsed = false;

    // Panel section collapse states (sections within panels)
    private bool collapseCanvas = false;
    private bool collapseShape = false;
    private bool collapseEdgeStyle = false;
    private bool collapseProperties = false;
    private bool collapseMinimap = false;
    private bool collapseInfo = false;
    private bool collapseNodeProperties = false;

    // Template selection state (used by the properties UI)
    private string? selectedTemplateId = null;
    private string? selectedTemplateShapeId = null;

    // Circuit component counters for auto-labeling (R1, R2, C1, C2, etc.)
    private Dictionary<string, int> circuitComponentCounters = new()
    {
        ["resistor"] = 0,
        ["capacitor"] = 0,
        ["inductor"] = 0,
        ["diode"] = 0,
        ["transistor-npn"] = 0,
        ["ground"] = 0,
        ["vcc"] = 0,
        ["and-gate"] = 0,
        ["or-gate"] = 0,
        ["not-gate"] = 0,
        ["ic-chip"] = 0,
        ["op-amp"] = 0
    };

    // Component prefix mapping for circuit labels
    private static readonly Dictionary<string, string> CircuitComponentPrefixes = new()
    {
        ["resistor"] = "R",
        ["capacitor"] = "C",
        ["inductor"] = "L",
        ["diode"] = "D",
        ["transistor-npn"] = "Q",
        ["ground"] = "GND",
        ["vcc"] = "VCC",
        ["and-gate"] = "U",
        ["or-gate"] = "U",
        ["not-gate"] = "U",
        ["ic-chip"] = "U",
        ["op-amp"] = "U"
    };

    // Template-specific edge style defaults
    public record TemplateEdgeDefaults(
        ArrowDirection ArrowDirection,
        EdgeStyle EdgeStyle,
        int StrokeWidth,
        string StrokeColor,
        string StrokeDashArray,
        bool IsDoubleLine
    );

    private static readonly Dictionary<string, TemplateEdgeDefaults> TemplateEdgeStyles = new()
    {
        // Circuit: no arrows, ortho routing, thinner lines
        ["circuit"] = new TemplateEdgeDefaults(
            ArrowDirection.None,
            EdgeStyle.Ortho,
            2,
            "#374151",
            "",
            false
        ),
        // Flowchart: standard arrows at end
        ["flowchart"] = new TemplateEdgeDefaults(
            ArrowDirection.End,
            EdgeStyle.Direct,
            2,
            "#374151",
            "",
            false
        ),
        // ICD: bidirectional arrows
        ["icd"] = new TemplateEdgeDefaults(
            ArrowDirection.Both,
            EdgeStyle.Direct,
            2,
            "#475569",
            "",
            false
        ),
        // Network: no arrows (connections are bidirectional)
        ["network"] = new TemplateEdgeDefaults(
            ArrowDirection.None,
            EdgeStyle.Direct,
            2,
            "#374151",
            "",
            false
        ),
        // BPMN: standard arrows
        ["bpmn"] = new TemplateEdgeDefaults(
            ArrowDirection.End,
            EdgeStyle.Direct,
            2,
            "#374151",
            "",
            false
        )
    };

    // Get current edge defaults based on selected template
    private TemplateEdgeDefaults GetCurrentEdgeDefaults()
    {
        if (!string.IsNullOrEmpty(selectedTemplateId) && TemplateEdgeStyles.TryGetValue(selectedTemplateId, out var defaults))
        {
            return defaults;
        }
        // Default: standard arrows at end
        return new TemplateEdgeDefaults(ArrowDirection.End, EdgeStyle.Direct, 2, "#374151", "", false);
    }

    // Helper to create an edge with template-appropriate defaults
    private Edge CreateEdgeWithDefaults(int fromId, int toId, ConnectionPoint fromConn, ConnectionPoint toConn)
    {
        var defaults = GetCurrentEdgeDefaults();
        return new Edge
        {
            Id = nextEdgeId++,
            From = fromId,
            To = toId,
            FromConnection = fromConn,
            ToConnection = toConn,
            IsOrthogonal = defaults.EdgeStyle == EdgeStyle.Ortho || useOrthoPlacement,
            Style = defaults.EdgeStyle,
            StrokeWidth = defaults.StrokeWidth,
            StrokeColor = defaults.StrokeColor,
            StrokeDashArray = defaults.StrokeDashArray,
            IsDoubleLine = defaults.IsDoubleLine,
            ArrowDirection = defaults.ArrowDirection
        };
    }

    private string GetNextComponentLabel(string shapeId)
    {
        if (!CircuitComponentPrefixes.TryGetValue(shapeId, out var prefix))
            return $"Node {nodes.Count + 1}";

        if (!circuitComponentCounters.ContainsKey(shapeId))
            circuitComponentCounters[shapeId] = 0;

        circuitComponentCounters[shapeId]++;
        return $"{prefix}{circuitComponentCounters[shapeId]}";
    }

    private IEnumerable<ShapeLibraryService.Template> GetAvailableTemplates() => shapeLibrary?.GetTemplates() ?? Enumerable.Empty<ShapeLibraryService.Template>();

    private IEnumerable<ShapeLibraryService.ShapeDescriptor> GetShapesForTemplate(string? templateId)
    {
        if (string.IsNullOrEmpty(templateId)) return Enumerable.Empty<ShapeLibraryService.ShapeDescriptor>();
        var tpl = shapeLibrary.GetTemplate(templateId);
        return tpl?.Shapes ?? Enumerable.Empty<ShapeLibraryService.ShapeDescriptor>();
    }

    private void OnTemplateChanged(ChangeEventArgs e)
    {
        selectedTemplateId = e.Value?.ToString();
        // When template changes, auto-select first shape in that template
        if (!string.IsNullOrEmpty(selectedTemplateId))
        {
            var shapes = GetShapesForTemplate(selectedTemplateId);
            selectedTemplateShapeId = shapes.FirstOrDefault()?.Id;
            // Auto-activate Add Shape mode when template is selected
            mode = EditorMode.AddNode;
            chainMode = false;
            ClearMultiConnectState();
        }
        else
        {
            selectedTemplateShapeId = null;
        }
        StateHasChanged();
    }

    // Called when shape selection changes within a template
    private void OnTemplateShapeChanged(ChangeEventArgs e)
    {
        selectedTemplateShapeId = e.Value?.ToString();
        // Auto-activate Add Shape mode when shape is selected
        if (!string.IsNullOrEmpty(selectedTemplateShapeId))
        {
            mode = EditorMode.AddNode;
            chainMode = false;
            ClearMultiConnectState();
        }
        StateHasChanged();
    }

    private void ApplyTemplateToSelectedNodes()
    {
        if (selectedNodes.Count == 0) return;

        foreach (var nodeId in selectedNodes)
        {
            var node = nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null) continue;
            node.TemplateId = selectedTemplateId;
            node.TemplateShapeId = selectedTemplateShapeId;
            RecalculateEdgePaths(node.Id);
        }
        StateHasChanged();
    }

    private void ClearTemplateFromSelectedNodes()
    {
        foreach (var nodeId in selectedNodes)
        {
            var node = nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null) continue;
            node.TemplateId = null;
            node.TemplateShapeId = null;
            RecalculateEdgePaths(node.Id);
        }
        StateHasChanged();
    }




    // Guide state
    private bool showRowGuide = false;
    private bool showColumnGuide = false;
    private bool showClearConfirm = false;
    private bool showBackgroundConfig = false;

    // Optimization settings
    private bool showOptimizationSettings = false;
    private int annealingIterations = 5000;
    private double annealingCooling = 0.995;

    // Circuit layout interactive settings
    private bool showCircuitSettings = false;
    private double circuitGridSpacing = 40.0;
    private double circuitObstacleMargin = 12.0;
    private double circuitBendPenalty = 6.0;
    private double circuitViaPenalty = 30.0;
    private double circuitProximityPenalty = 10.0;
    private int circuitMaxGridSize = 300;
    private int circuitRowSpacing = 140;
    private int circuitColSpacing = 220;
    private int circuitStartX = 100;
    private int circuitStartY = 100;

    private void ApplyQuickPreset() { annealingIterations = 1000; annealingCooling = 0.99; }
    private void ApplyBalancedPreset() { annealingIterations = 5000; annealingCooling = 0.995; }
    private void ApplyThoroughPreset() { annealingIterations = 15000; annealingCooling = 0.998; }

    // Text edit dialog state
    private bool showTextEditDialog = false;
    private int? editingTextNodeId = null;
    private int? editingTextLabelId = null;
    private string editingText = "";

    // Selection state
    private bool isSelecting = false;
    private bool justFinishedAreaSelect = false;
    private (double X, double Y)? selectionStart = null;
    private (double X, double Y) currentMousePosition = (0, 0);

    // Toolbar select toggle (allows the Select button to be active without changing `mode`)
    private bool selectToolActive = false;

    // Print area selection
    private bool isPrintAreaSelection = false;
    private (double X, double Y, double Width, double Height)? printArea = null;

    // Edge style editing
    private int editStrokeWidth = 2;
    private string editStrokeColor = "#374151";
    private string editStrokeDashArray = "";
    private bool editIsDoubleLine = false;
    private EdgeStyle editEdgeStyle = EdgeStyle.Direct;

    // Edge style defaults
    private int defaultStrokeWidth = 2;
    private string defaultStrokeColor = "#374151";
    private string defaultStrokeDashArray = "";
    private bool defaultIsDoubleLine = false;
    private string defaultArrowStyle = "filled";
    private EdgeStyle defaultEdgeStyle = EdgeStyle.Direct;

    // Mouse tracking
    private double svgMouseX = 0;
    private double svgMouseY = 0;
    private double lastMouseX = 0;
    private double lastMouseY = 0;

    // Edge attribute variables
    private int strokeWidth = 2;
    private string selectedStrokeColor = "#374151";
    private string strokeDashArray = "";
    private bool isDoubleLine = false;
    private bool showEdgeStylePanel = false;
    private ArrowDirection editArrowDirection = ArrowDirection.End;

    // Array placement state
    private bool arrayMode = false;
    private string arrayOrientation = "horizontal";
    private int arrayCount = 3;
    private int arraySpacing = 150;

    // Chain mode state
    private bool chainMode = false;
    private bool rearrangeMode = false;
    private int? lastChainedNodeId = null;

    // ============================================
    // CONNECTION MODE SYSTEM (replaces old multiConnect)
    // ============================================
    
    /// <summary>
    /// Connection modes for creating edges between nodes
    /// </summary>
    public enum ConnectionModeType
    {
        /// <summary>Normal: Click source, click target, edge created, done</summary>
        Normal,
        /// <summary>1:N Click: Click source once, then click each target to connect</summary>
        OneToN,
        /// <summary>1:N Area: Click source once, then area-select targets</summary>
        OneToNArea,
        /// <summary>Rearrange: Drag nodes freely without creating connections</summary>
        Rearrange
    }

    private ConnectionModeType connectionMode = ConnectionModeType.Normal;
    
    // 1:N mode state
    private int? oneToNSourceNode = null;
    private ConnectionPoint? oneToNSourcePoint = null;
    private (double X, double Y)? oneToNSourceCoords = null;
    
    // 1:N Area mode state  
    private bool isOneToNAreaSelecting = false;
    private (double X, double Y)? oneToNAreaStart = null;

    // Legacy multi-connect (keep for compatibility, but redirect to new system)
    private bool multiConnectMode 
    { 
        get => connectionMode != ConnectionModeType.Normal;
        set 
        {
            if (!value) connectionMode = ConnectionModeType.Normal;
            // If setting to true, default to OneToN
            else if (connectionMode == ConnectionModeType.Normal) connectionMode = ConnectionModeType.OneToN;
        }
    }
    private int? multiConnectSourceNode => oneToNSourceNode;
    private ConnectionPoint? multiConnectSourcePoint => oneToNSourcePoint;
    private (double X, double Y)? multiConnectSourceCoords => oneToNSourceCoords;

    // Helper to get current select mode label
private string GetSelectModeLabel()
{
    if (connectionMode == ConnectionModeType.Rearrange) return "(Move)";
    if (connectionMode == ConnectionModeType.OneToN) return "(1:N)";
    if (connectionMode == ConnectionModeType.OneToNArea) return "(1:N▢)";
    if (chainMode) return "(Chain)";
    if (multiConnectMode) return "(Multi)";
    return "";
}
    // Helper to get connection mode button class
    private string GetConnectionModeClass(ConnectionModeType modeType)
    {
        return connectionMode == modeType ? "active" : "";
    }

    // Set connection mode
private void SetConnectionMode(ConnectionModeType newMode)
{
    // If clicking the same mode, toggle off
    if (connectionMode == newMode)
    {
        connectionMode = ConnectionModeType.Normal;
    }
    else
    {
        connectionMode = newMode;
    }
    
    // Clear any existing connection state when changing modes
    ClearOneToNState();
    
    // Only clear chain mode when switching to a non-Normal connection mode
    if (newMode != ConnectionModeType.Normal)
    {
        chainMode = false;
        lastChainedNodeId = null;
    }
    
    StateHasChanged();
}
    // Helper to clear 1:N state
    private void ClearOneToNState()
    {
        oneToNSourceNode = null;
        oneToNSourcePoint = null;
        oneToNSourceCoords = null;
        isOneToNAreaSelecting = false;
        oneToNAreaStart = null;
    }

    // Legacy helper - redirects to new system
    private void ClearMultiConnectState()
    {
        ClearOneToNState();
    }


    private readonly GraphLayoutService _layoutService = new();
    private readonly EdgeRoutingService _routingService = new();


    // Check if we have an active 1:N source
    private bool HasOneToNSource => oneToNSourceNode.HasValue;

    // Preset colors
    private readonly List<string> presetColors = new()
    {
        "#374151", "#ef4444", "#3b82f6", "#10b981",
        "#f59e0b", "#8b5cf6", "#ec4899", "#000000"
    };

    // Node fill color presets (lighter colors work better as fills)
    private readonly string[] nodeFillColors = new[]
    {
        "#ffffff", "#f3f4f6", "#fef3c7", "#d1fae5", "#dbeafe", 
        "#ede9fe", "#fce7f3", "#fee2e2", "#e0f2fe", "#f0fdf4"
    };

    // Line style options
    private readonly Dictionary<string, string> lineStyles = new()
    {
        { "", "Solid" },
        { "5,5", "Dashed" },
        { "2,2", "Dotted" },
        { "10,5,2,5", "Dash-Dot" }
    };

    // Canvas configuration
    private string canvasBackground = "grid";
    private double canvasWidth = 4000;
    private double canvasHeight = 4000;

    // Viewport tracking for minimap
    private double scrollX = 0;
    private double scrollY = 0;
    private double viewportWidth = 800;
    private double viewportHeight = 600;

    // Zoom
    private double zoomLevel = 1.0;
    private readonly double[] zoomLevels = { 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0, 2.5, 3.0 };

    // Swimlane/column configuration
    private int swimlaneCount = 3;
    private int columnCount = 4;
    private List<string> swimlaneLabels = new() { "Lane 1", "Lane 2", "Lane 3" };
    private List<string> columnLabels = new() { "Column 1", "Column 2", "Column 3", "Column 4" };

    // ============================================
    // HELP SYSTEM
    // ============================================
    private bool showHelpModal = false;
    private string activeHelpSection = "getting-started";

    private static readonly Dictionary<string, (string Title, string Icon)> HelpSections = new()
    {
        ["getting-started"] = ("Getting Started", "??"),
        ["shapes"] = ("Shapes & Meanings", "?"),
        ["connections"] = ("Connections & Edges", "?"),
        ["icons"] = ("Icons Library", "??"),
        ["dfd"] = ("Data Flow Diagrams", "??"),
        ["flowcharts"] = ("Flowcharts", "??"),
        ["swimlanes"] = ("Swimlane Diagrams", "??"),
        ["architecture"] = ("Architecture Diagrams", "??"),
        ["keyboard"] = ("Keyboard Shortcuts", "?"),
        ["export"] = ("Export Options", "??"),
        ["tips"] = ("Tips & Tricks", "??"),
    };

    private void OpenHelp() => showHelpModal = true;
    private void CloseHelp() => showHelpModal = false;
    private void SetHelpSection(string section) => activeHelpSection = section;

    private void ToggleSelectTool()
    {
        selectToolActive = !selectToolActive;
        StateHasChanged();
    }

    // ============================================
    // EXAMPLE GENERATOR - Password Protected
    // ============================================
    private bool showExampleGenerator = false;
    private string examplePassword = "";
    private bool examplePasswordVerified = false;
    private string generatedExampleCode = "";
    private string exampleName = "MyDiagram";
    private string newExampleKey = "myexample";
    private string newExampleName = "My Example";
    private string newExampleDescription = "Description here";
    private const string EXAMPLE_PASSWORD = "dfd2025";
    private const string EXAMPLE_GENERATOR_PASSWORD = "dfd2025";

    private void ToggleExampleGenerator()
    {
        showExampleGenerator = !showExampleGenerator;
        if (!showExampleGenerator)
        {
            examplePasswordVerified = false;
            examplePassword = "";
            generatedExampleCode = "";
        }
    }

    private void CloseExampleGenerator()
    {
        showExampleGenerator = false;
        examplePasswordVerified = false;
        examplePassword = "";
        generatedExampleCode = "";
    }

    private void CheckExampleGeneratorShortcut(KeyboardEventArgs e)
    {
        if (e.CtrlKey && e.ShiftKey && e.Key == "E")
        {
            showExampleGenerator = true;
            StateHasChanged();
        }
    }

    private void VerifyExamplePassword()
    {
        examplePasswordVerified = (examplePassword == EXAMPLE_GENERATOR_PASSWORD);
        if (examplePasswordVerified)
        {
            GenerateExampleCode();
        }
    }

    private void GenerateExampleCode()
    {
        var sb = new System.Text.StringBuilder();

        // Generate method signature
        string methodName = ToPascalCase(newExampleKey);
        sb.AppendLine($"    // Add this to the Examples dictionary:");
        sb.AppendLine($"    // [\"{newExampleKey}\"] = (\"{newExampleName}\", \"{newExampleDescription}\", Load{methodName}),");
        sb.AppendLine();
        sb.AppendLine($"    private static void Load{methodName}(DFDEditor editor)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Helper function");
        sb.AppendLine("        ConnectionPoint CP(string side, int pos) => new ConnectionPoint { Side = side, Position = pos };");
        sb.AppendLine();

        // Generate nodes
        sb.AppendLine("        // Nodes");
        foreach (var node in nodes.OrderBy(n => n.Id))
        {
            sb.Append($"        editor.nodes.Add(new Node {{ ");
            sb.Append($"Id = {node.Id}, ");
            sb.Append($"Text = \"{EscapeString(node.Text)}\", ");
            sb.Append($"X = {node.X}, ");
            sb.Append($"Y = {node.Y}, ");
            sb.Append($"Width = {node.Width}, ");
            sb.Append($"Height = {node.Height}, ");
            sb.Append($"Shape = NodeShape.{node.Shape}, ");
            sb.Append($"StrokeColor = \"{node.StrokeColor}\"");
            if (!string.IsNullOrEmpty(node.Icon))
            {
                sb.Append($", Icon = \"{node.Icon}\"");
            }
            sb.AppendLine(" });");
        }

        if (nodes.Any())
        {
            sb.AppendLine($"        editor.nextId = {nodes.Max(n => n.Id) + 1};");
        }

        sb.AppendLine();

        // Generate edges
        if (edges.Any())
        {
            sb.AppendLine("        // Edges");
            foreach (var edge in edges.OrderBy(e => e.Id))
            {
                sb.Append($"        editor.edges.Add(new Edge {{ ");
                sb.Append($"Id = {edge.Id}, ");
                sb.Append($"From = {edge.From}, ");
                sb.Append($"To = {edge.To}, ");
                sb.Append($"FromConnection = CP(\"{edge.FromConnection?.Side ?? "right"}\", {edge.FromConnection?.Position ?? 0}), ");
                sb.Append($"ToConnection = CP(\"{edge.ToConnection?.Side ?? "left"}\", {edge.ToConnection?.Position ?? 0})");

                if (edge.IsDoubleLine)
                    sb.Append(", IsDoubleLine = true");
                if (edge.IsOrthogonal)
                    sb.Append(", IsOrthogonal = true");
                if (!string.IsNullOrEmpty(edge.StrokeColor))
                    sb.Append($", StrokeColor = \"{edge.StrokeColor}\"");
                if (edge.StrokeWidth.HasValue)
                    sb.Append($", StrokeWidth = {edge.StrokeWidth}");
                if (!string.IsNullOrEmpty(edge.StrokeDashArray))
                    sb.Append($", StrokeDashArray = \"{edge.StrokeDashArray}\"");

                sb.AppendLine(" });");
            }
            sb.AppendLine($"        editor.nextEdgeId = {edges.Max(e => e.Id) + 1};");
        }

        // Generate edge labels
        var labelsForEdges = edgeLabels.Where(l => !string.IsNullOrEmpty(l.Text)).ToList();
        if (labelsForEdges.Any())
        {
            sb.AppendLine();
            sb.AppendLine("        // Labels");
            int labelId = 1;
            foreach (var label in labelsForEdges.OrderBy(l => l.EdgeId))
            {
                sb.AppendLine($"        editor.edgeLabels.Add(new EdgeLabel {{ Id = {labelId++}, EdgeId = {label.EdgeId}, Text = \"{EscapeString(label.Text)}\" }});");
            }
        }

        sb.AppendLine("    }");

        generatedExampleCode = sb.ToString();
    }

    private string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return "Example";

        var words = input.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w =>
            char.ToUpper(w[0]) + (w.Length > 1 ? w.Substring(1).ToLower() : "")
        ));
    }

    private string EscapeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private async Task CopyGeneratedCode()
    {
        await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", generatedExampleCode);
    }

    // ============================================
    // LIFECYCLE METHODS
    // ============================================
    protected override void OnInitialized()
    {
        try
        {
            Console.WriteLine("=== DFDEditor OnInitialized START ===");
            Console.WriteLine($"Nodes count: {nodes.Count}");
            Console.WriteLine($"Edges count: {edges.Count}");
            Console.WriteLine($"Mode: {mode}");
            Console.WriteLine("=== DFDEditor OnInitialized END ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION in OnInitialized: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Initialize viewport size for minimap
            await Task.Delay(100);
            await HandleCanvasScroll();
        }
    }

    // ============================================
    // HELPER METHODS
    // ============================================
    private (double X, double Y) GetLastMousePosition() => (lastMouseX, lastMouseY);

    private Rectangle GetSelectionRectangle()
    {
        if (!selectionStart.HasValue) return new Rectangle { X = 0, Y = 0, Width = 0, Height = 0 };

        var startX = Math.Min(selectionStart.Value.X, currentMousePosition.X);
        var startY = Math.Min(selectionStart.Value.Y, currentMousePosition.Y);
        var width = Math.Abs(currentMousePosition.X - selectionStart.Value.X);
        var height = Math.Abs(currentMousePosition.Y - selectionStart.Value.Y);

        return new Rectangle { X = startX, Y = startY, Width = width, Height = height };
    }

    // Get selection rectangle for 1:N Area mode
    private Rectangle GetOneToNAreaRectangle()
    {
        if (!oneToNAreaStart.HasValue) return new Rectangle { X = 0, Y = 0, Width = 0, Height = 0 };

        var startX = Math.Min(oneToNAreaStart.Value.X, currentMousePosition.X);
        var startY = Math.Min(oneToNAreaStart.Value.Y, currentMousePosition.Y);
        var width = Math.Abs(currentMousePosition.X - oneToNAreaStart.Value.X);
        var height = Math.Abs(currentMousePosition.Y - oneToNAreaStart.Value.Y);

        return new Rectangle { X = startX, Y = startY, Width = width, Height = height };
    }

    private (double X, double Y) GetEdgeMidpoint(Edge edge)
    {
        if (edge.Waypoints.Count > 0)
        {
            var midIndex = edge.Waypoints.Count / 2;
            return (edge.Waypoints[midIndex].X, edge.Waypoints[midIndex].Y);
        }

        var fromNode = nodes.FirstOrDefault(n => n.Id == edge.From);
        var toNode = nodes.FirstOrDefault(n => n.Id == edge.To);

        if (fromNode == null || toNode == null)
            return (0, 0);

        return (
            (fromNode.X + fromNode.Width / 2 + toNode.X + toNode.Width / 2) / 2,
            (fromNode.Y + fromNode.Height / 2 + toNode.Y + toNode.Height / 2) / 2
        );
    }

    /// <summary>
    /// Get 3 control points along an edge at 25%, 50%, 75% positions
    /// If waypoints exist, return them; otherwise compute from endpoints
    /// </summary>
    private List<(double X, double Y, int Index)> GetEdgeControlPoints(Edge edge)
    {
        var points = new List<(double X, double Y, int Index)>();
        
        // If edge has existing waypoints, return those with their indices
        if (edge.Waypoints.Count > 0)
        {
            for (int i = 0; i < edge.Waypoints.Count; i++)
            {
                points.Add((edge.Waypoints[i].X, edge.Waypoints[i].Y, i));
            }
            return points;
        }
        
        // Otherwise, compute 3 points along the straight line
        var fromNode = nodes.FirstOrDefault(n => n.Id == edge.From);
        var toNode = nodes.FirstOrDefault(n => n.Id == edge.To);
        
        if (fromNode == null || toNode == null)
            return points;
        
        // Get connection point coordinates
        double x1, y1, x2, y2;
        
        if (edge.FromConnection != null)
        {
            var fromCoords = GeometryService.GetConnectionPointCoordinates(fromNode, edge.FromConnection.Side, edge.FromConnection.Position);
            x1 = fromCoords.X;
            y1 = fromCoords.Y;
        }
        else
        {
            x1 = fromNode.X + fromNode.Width / 2;
            y1 = fromNode.Y + fromNode.Height / 2;
        }
        
        if (edge.ToConnection != null)
        {
            var toCoords = GeometryService.GetConnectionPointCoordinates(toNode, edge.ToConnection.Side, edge.ToConnection.Position);
            x2 = toCoords.X;
            y2 = toCoords.Y;
        }
        else
        {
            x2 = toNode.X + toNode.Width / 2;
            y2 = toNode.Y + toNode.Height / 2;
        }
        
        // Calculate 3 points at 25%, 50%, 75%
        points.Add((x1 + (x2 - x1) * 0.25, y1 + (y2 - y1) * 0.25, -1)); // -1 means "needs to be added"
        points.Add((x1 + (x2 - x1) * 0.50, y1 + (y2 - y1) * 0.50, -2));
        points.Add((x1 + (x2 - x1) * 0.75, y1 + (y2 - y1) * 0.75, -3));
        
        return points;
    }

    private void RecalculateEdgePaths(int? movedNodeId = null)
    {
        foreach (var edge in edges)
        {
            if (movedNodeId == null || edge.From == movedNodeId || edge.To == movedNodeId)
            {
                edge.PathData = PathService.GetEdgePath(edge, nodes);
            }
        }
    }

    private void UpdateEdgePath(Edge edge)
    {
        edge.PathData = PathService.GetEdgePath(edge, nodes);
        StateHasChanged();
    }

    private EventCallback<(string side, int position, MouseEventArgs e)> CreateConnectionPointHandler(int nodeId)
    {
        return new EventCallback<(string side, int position, MouseEventArgs e)>(
            this,
            (Action<(string side, int position, MouseEventArgs e)>)(args => HandleConnectionPointClick(nodeId, args.side, args.position, args.e))
        );
    }

    // Helper class for selection rectangle
    private class Rectangle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    private void DeleteSelected()
    {
        if (selectedNodes.Count == 0 && selectedEdges.Count == 0 && selectedLabels.Count == 0)
            return;

        UndoService.SaveState(nodes, edges, edgeLabels);

        // Delete selected nodes and their connected edges
        foreach (var nodeId in selectedNodes.ToList())
        {
            // Remove edges connected to this node
            edges.RemoveAll(e => e.From == nodeId || e.To == nodeId);
            // Remove the node
            nodes.RemoveAll(n => n.Id == nodeId);
        }

        // Delete selected edges
        foreach (var edgeId in selectedEdges.ToList())
        {
            edges.RemoveAll(e => e.Id == edgeId);
        }

        // Delete selected labels
        foreach (var labelId in selectedLabels.ToList())
        {
            edgeLabels.RemoveAll(l => l.Id == labelId);
        }

        selectedNodes.Clear();
        selectedEdges.Clear();
        selectedLabels.Clear();
        StateHasChanged();
    }
    
    private void OnForceLayout()
{
    UndoService.SaveState(nodes, edges, edgeLabels);
    _layoutService.ApplyForceDirectedLayout(nodes, edges);
    RecalculateEdgePaths();
    StateHasChanged();
}
private void HandleOptimizationComplete((List<Node>, List<Edge>) result)
{
    nodes = result.Item1;
    edges = result.Item2;
    RecalculateEdgePaths();
    StateHasChanged();
}
private void ToggleChainMode()
{
    chainMode = !chainMode;
    
    if (chainMode)
    {
        connectionMode = ConnectionModeType.Normal;
        ClearOneToNState();
        multiConnectMode = false;
        ClearMultiConnectState();
    }
    
    lastChainedNodeId = null;
    mode = EditorMode.Select;
    StateHasChanged();
}
// Apply stroke color to selected nodes
    private void ApplyStrokeToSelectedNodes()
    {
        if (selectedNodes.Count == 0) return;
        
        UndoService.SaveState(nodes, edges, edgeLabels);
        
        foreach (var nodeId in selectedNodes)
        {
            var node = nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                node.StrokeColor = defaultStrokeColor;
                node.StrokeWidth = defaultStrokeWidth;
                node.StrokeDashArray = defaultStrokeDashArray;
            }
        }
        StateHasChanged();
    }

    // Apply stroke when clicking color swatch (auto-apply to selection)
    private void ApplyStrokeToSelection()
    {
        if (selectedNodes.Count > 0)
        {
            ApplyStrokeToSelectedNodes();
        }
        else if (selectedEdges.Count > 0)
        {
            ApplyEdgeStylesToSelected();
        }
    }
       /// <summary>
    /// Arranges selected nodes in a grid within their current bounding area.
    /// </summary>
    /// <param name="forceRows">0 = auto (square-ish), 1 = single row, 999 = single column</param>
    private void ArrangeInGrid(int forceRows)
    {
        if (selectedNodes.Count < 2) return;
        
        UndoService.SaveState(nodes, edges, edgeLabels);
        
        var selected = nodes.Where(n => selectedNodes.Contains(n.Id)).ToList();
        if (selected.Count < 2) return;
        
        // Get bounding box of selection
        var minX = selected.Min(n => n.X);
        var minY = selected.Min(n => n.Y);
        var maxX = selected.Max(n => n.X + n.Width);
        var maxY = selected.Max(n => n.Y + n.Height);
        
        var areaWidth = maxX - minX;
        var areaHeight = maxY - minY;
        
        // Calculate grid dimensions
        int count = selected.Count;
        int cols, rows;
        
        if (forceRows == 1)
        {
            // Single row
            rows = 1;
            cols = count;
        }
        else if (forceRows >= count)
        {
            // Single column
            rows = count;
            cols = 1;
        }
        else if (forceRows > 0)
        {
            // Force specific number of rows
            rows = forceRows;
            cols = (int)Math.Ceiling((double)count / rows);
        }
        else
        {
            // Auto: try to make it square-ish
            cols = (int)Math.Ceiling(Math.Sqrt(count));
            rows = (int)Math.Ceiling((double)count / cols);
        }
        
        // Get average node size for spacing calculation
        var avgWidth = selected.Average(n => n.Width);
        var avgHeight = selected.Average(n => n.Height);
        
        // Calculate cell size (ensure minimum spacing)
        var cellWidth = Math.Max(areaWidth / cols, avgWidth + 20);
        var cellHeight = Math.Max(areaHeight / rows, avgHeight + 20);
        
        // If area is too small, expand it
        if (areaWidth < cellWidth * cols) areaWidth = cellWidth * cols;
        if (areaHeight < cellHeight * rows) areaHeight = cellHeight * rows;
        
        // Recalculate cell size with potentially expanded area
        cellWidth = areaWidth / cols;
        cellHeight = areaHeight / rows;
        
        // Sort nodes by their current position (top-left to bottom-right)
        var sortedNodes = selected
            .OrderBy(n => (int)(n.Y / 100))
            .ThenBy(n => n.X)
            .ToList();
        
        // Position nodes in grid
        for (int i = 0; i < sortedNodes.Count; i++)
        {
            var node = sortedNodes[i];
            int row = i / cols;
            int col = i % cols;
            
            // Center node in its cell
            var cellX = minX + col * cellWidth;
            var cellY = minY + row * cellHeight;
            
            node.X = cellX + (cellWidth - node.Width) / 2;
            node.Y = cellY + (cellHeight - node.Height) / 2;
        }
        
        // Recalculate edge paths for affected nodes
        foreach (var node in selected)
        {
            RecalculateEdgePaths(node.Id);
        }

        StateHasChanged();
    }

    // Attachment handling methods
    private async Task HandleAttachmentUpload(InputFileChangeEventArgs e)
    {
        if (selectedNodes.Count != 1) return;
        var selectedNode = nodes.FirstOrDefault(n => n.Id == selectedNodes.First());
        if (selectedNode == null) return;

        var file = e.File;
        if (file == null) return;

        // Determine file type
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        AttachmentType fileType;
        string mimeType;

        switch (extension)
        {
            case ".svg":
                fileType = AttachmentType.Svg;
                mimeType = "image/svg+xml";
                break;
            case ".pdf":
                fileType = AttachmentType.Pdf;
                mimeType = "application/pdf";
                break;
            default:
                return; // Unsupported file type
        }

        // Read file content as Base64
        const long maxFileSize = 5 * 1024 * 1024; // 5MB limit
        using var stream = file.OpenReadStream(maxFileSize);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var base64 = Convert.ToBase64String(memoryStream.ToArray());
        var dataUri = $"data:{mimeType};base64,{base64}";

        // Create attachment
        var attachment = new NodeAttachment
        {
            FileName = file.Name,
            FileType = fileType,
            DataUri = dataUri
        };

        // Add to node
        selectedNode.Attachments ??= new List<NodeAttachment>();
        selectedNode.Attachments.Add(attachment);

        StateHasChanged();
    }

    private void RemoveAttachment(Node node, string attachmentId)
    {
        if (node.Attachments == null) return;
        node.Attachments.RemoveAll(a => a.Id == attachmentId);
        if (node.Attachments.Count == 0)
            node.Attachments = null;
        StateHasChanged();
    }

    private static string TruncateFileName(string fileName, int maxLength)
    {
        if (string.IsNullOrEmpty(fileName) || fileName.Length <= maxLength)
            return fileName;

        var extension = Path.GetExtension(fileName);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var availableLength = maxLength - extension.Length - 3; // 3 for "..."

        if (availableLength <= 0)
            return fileName[..maxLength];

        return name[..Math.Min(availableLength, name.Length)] + "..." + extension;
    }

    // Handle attachment upload from the double-click dialog
    private async Task HandleDialogAttachmentUpload(InputFileChangeEventArgs e, Node targetNode)
    {
        if (targetNode == null) return;

        var file = e.File;
        if (file == null) return;

        // Determine file type
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        AttachmentType fileType;
        string mimeType;

        switch (extension)
        {
            case ".svg":
                fileType = AttachmentType.Svg;
                mimeType = "image/svg+xml";
                break;
            case ".pdf":
                fileType = AttachmentType.Pdf;
                mimeType = "application/pdf";
                break;
            default:
                return; // Unsupported file type
        }

        // Read file content as Base64
        const long maxFileSize = 5 * 1024 * 1024; // 5MB limit
        using var stream = file.OpenReadStream(maxFileSize);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var base64 = Convert.ToBase64String(memoryStream.ToArray());
        var dataUri = $"data:{mimeType};base64,{base64}";

        // Create attachment
        var attachment = new NodeAttachment
        {
            FileName = file.Name,
            FileType = fileType,
            DataUri = dataUri
        };

        // Add to node
        targetNode.Attachments ??= new List<NodeAttachment>();
        targetNode.Attachments.Add(attachment);

        // Auto-expand node to fit SVG if it's too small
        if (fileType == AttachmentType.Svg)
        {
            var minSvgNodeSize = 100.0;
            if (targetNode.Width < minSvgNodeSize)
                targetNode.Width = minSvgNodeSize;
            if (targetNode.Height < minSvgNodeSize + 20) // +20 for text label space
                targetNode.Height = minSvgNodeSize + 20;
        }

        StateHasChanged();
    }

    // Place an SVG attachment as a new node on the canvas
    private void PlaceAttachmentOnCanvas(Node sourceNode, NodeAttachment attachment)
    {
        if (attachment.FileType != AttachmentType.Svg) return;

        // Create a new node sized to display the SVG nicely
        // Default to 120x120 for a square node that shows the SVG well
        var nodeSize = 120.0;

        var newNode = new Node
        {
            Id = nodes.Count > 0 ? nodes.Max(n => n.Id) + 1 : 1,
            X = sourceNode.X + sourceNode.Width + 50, // Place to the right of source node
            Y = sourceNode.Y,
            Width = nodeSize,
            Height = nodeSize + 20, // Extra space for text label at bottom
            Text = Path.GetFileNameWithoutExtension(attachment.FileName), // Cleaner label without extension
            Shape = NodeShape.Rectangle,
            FillColor = "#ffffff",
            StrokeColor = "#374151",
            // Store the SVG data URI for rendering
            Attachments = new List<NodeAttachment>
            {
                new NodeAttachment
                {
                    Id = attachment.Id,
                    FileName = attachment.FileName,
                    FileType = attachment.FileType,
                    DataUri = attachment.DataUri,
                    DisplayWidth = nodeSize - 16, // Match the padding in rendering
                    DisplayHeight = nodeSize - 16
                }
            }
        };

        nodes.Add(newNode);

        // Select the new node
        selectedNodes.Clear();
        selectedNodes.Add(newNode.Id);

        StateHasChanged();
    }
}

