using Microsoft.AspNetCore.Components;
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

    // Guide state
    private bool showRowGuide = false;
    private bool showColumnGuide = false;
    private bool showClearConfirm = false;
    private bool showBackgroundConfig = false;

    // Text edit dialog state
    private bool showTextEditDialog = false;
    private int? editingTextNodeId = null;
    private int? editingTextLabelId = null;
    private string editingText = "";

    // Selection state
    private bool isSelecting = false;
    private (double X, double Y)? selectionStart = null;
    private (double X, double Y) currentMousePosition = (0, 0);

    // Print area selection
    private bool isPrintAreaSelection = false;
    private (double X, double Y, double Width, double Height)? printArea = null;

    // Edge style editing
    private int editStrokeWidth = 2;
    private string editStrokeColor = "#374151";
    private string editStrokeDashArray = "";
    private bool editIsDoubleLine = false;

    // Edge style defaults
    private int defaultStrokeWidth = 2;
    private string defaultStrokeColor = "#374151";
    private string defaultStrokeDashArray = "";
    private bool defaultIsDoubleLine = false;
    private string defaultArrowStyle = "filled";

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

    // Array placement state
    private bool arrayMode = false;
    private string arrayOrientation = "horizontal";
    private int arrayCount = 3;
    private int arraySpacing = 150;

    // Chain mode state
    private bool chainMode = false;
    private int? lastChainedNodeId = null;

    // Preset colors
    private readonly List<string> presetColors = new()
    {
        "#374151", "#ef4444", "#3b82f6", "#10b981",
        "#f59e0b", "#8b5cf6", "#ec4899", "#000000"
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
    private readonly double[] zoomLevels = { 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 2.0 };

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
        ["getting-started"] = ("Getting Started", "🚀"),
        ["shapes"] = ("Shapes & Meanings", "⬡"),
        ["connections"] = ("Connections & Edges", "↗"),
        ["icons"] = ("Icons Library", "🎨"),
        ["dfd"] = ("Data Flow Diagrams", "📊"),
        ["flowcharts"] = ("Flowcharts", "📋"),
        ["swimlanes"] = ("Swimlane Diagrams", "🏊"),
        ["architecture"] = ("Architecture Diagrams", "🏗"),
        ["keyboard"] = ("Keyboard Shortcuts", "⌨"),
        ["export"] = ("Export Options", "💾"),
        ["tips"] = ("Tips & Tricks", "💡"),
    };

    private void OpenHelp() => showHelpModal = true;
    private void CloseHelp() => showHelpModal = false;
    private void SetHelpSection(string section) => activeHelpSection = section;

    // ============================================
    // EXAMPLE GENERATOR - Password Protected
    // ============================================
    private bool showExampleGenerator = false;
    private string examplePassword = "";
    private bool examplePasswordVerified = false;
    private string generatedExampleCode = "";
    private string newExampleKey = "myexample";
    private string newExampleName = "My Example";
    private string newExampleDescription = "Description here";

    // Change this password to whatever you want
    private const string EXAMPLE_GENERATOR_PASSWORD = "dfd2025";

    // Secret key combination: Ctrl+Shift+E to open generator
    // This should be called from your existing HandleKeyDown method
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

    private void CloseExampleGenerator()
    {
        showExampleGenerator = false;
        examplePasswordVerified = false;
        examplePassword = "";
        generatedExampleCode = "";
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
}
