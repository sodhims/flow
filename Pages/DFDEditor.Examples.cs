using dfd2wasm.Models;

namespace dfd2wasm.Pages;

public partial class DFDEditor
{
    private bool showExamplesMenu = false;

    private static readonly Dictionary<string, (string Name, string Description, Action<DFDEditor> Load)> Examples = new()
    {
        ["context"] = ("Context Diagram", "Simple system with external entities", LoadContextDiagram),
        ["level0"] = ("Level 0 DFD", "Expanded system with data stores", LoadLevel0DFD),
        ["login"] = ("Login Flow", "User authentication process", LoadLoginFlow),
        ["ecommerce"] = ("E-Commerce Flow", "Online shopping process", LoadEcommerceFlow),
        ["software"] = ("Software Architecture", "Typical web app layers", LoadSoftwareArchitecture),
        ["etl"] = ("ETL Pipeline", "Data processing workflow", LoadETLPipeline),
        ["help"] = ("📖 Help Guide", "How to use this editor", OpenHelpFromExamples),
        ["generator"] = ("🔧 Create Example...", "Generate code from current diagram", OpenExampleGenerator),
    };

    // Helper method to create ConnectionPoint easily
    private static ConnectionPoint CP(string side, int position = 0) => new() { Side = side, Position = position };

    private void LoadExample(string key)
    {
        // Handle Help separately
        if (key == "help")
        {
            showHelpModal = true;
            showExamplesMenu = false;
            StateHasChanged();
            return;
        }

        // Handle Example Generator separately
        if (key == "generator")
        {
            showExampleGenerator = true;
            showExamplesMenu = false;
            StateHasChanged();
            return;
        }

        if (Examples.TryGetValue(key, out var example))
        {
            // Clear existing
            nodes.Clear();
            edges.Clear();
            edgeLabels.Clear();
            selectedNodes.Clear();
            selectedEdges.Clear();
            nextId = 1;
            nextEdgeId = 1;
            
            // Load the example
            example.Load(this);
            
            // Recalculate paths
            foreach (var edge in edges)
            {
                edge.PathData = PathService.GetEdgePath(edge, nodes);
            }
            
            StateHasChanged();
        }
        showExamplesMenu = false;
    }

    private static void OpenHelpFromExamples(DFDEditor editor)
    {
        editor.showHelpModal = true;
        editor.showExamplesMenu = false;
    }

    private static void OpenExampleGenerator(DFDEditor editor)
    {
        editor.showExampleGenerator = true;
        editor.showExamplesMenu = false;
    }

    private static void LoadContextDiagram(DFDEditor editor)
    {
        // External entities
        editor.nodes.Add(new Node { Id = 1, Text = "Customer", X = 100, Y = 200, Width = 100, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#059669", Icon = "user" });
        editor.nodes.Add(new Node { Id = 2, Text = "Admin", X = 100, Y = 350, Width = 100, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#059669", Icon = "users" });
        
        // Central process
        editor.nodes.Add(new Node { Id = 3, Text = "Order\nManagement\nSystem", X = 350, Y = 250, Width = 140, Height = 100, Shape = NodeShape.Ellipse, StrokeColor = "#3b82f6", Icon = "gear" });
        
        // External systems
        editor.nodes.Add(new Node { Id = 4, Text = "Payment\nGateway", X = 600, Y = 150, Width = 100, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#8b5cf6", Icon = "credit-card" });
        editor.nodes.Add(new Node { Id = 5, Text = "Shipping\nProvider", X = 600, Y = 300, Width = 100, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#8b5cf6", Icon = "cart" });
        editor.nodes.Add(new Node { Id = 6, Text = "Email\nService", X = 600, Y = 450, Width = 100, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#8b5cf6", Icon = "email" });
        
        editor.nextId = 7;
        
        // Edges
        editor.edges.Add(new Edge { Id = 1, From = 1, To = 3, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 2, From = 2, To = 3, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 3, From = 3, To = 4, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 4, From = 3, To = 5, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 5, From = 3, To = 6, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.nextEdgeId = 6;
        
        // Labels
        editor.edgeLabels.Add(new EdgeLabel { Id = 1, EdgeId = 1, Text = "Orders" });
        editor.edgeLabels.Add(new EdgeLabel { Id = 2, EdgeId = 2, Text = "Reports" });
        editor.edgeLabels.Add(new EdgeLabel { Id = 3, EdgeId = 3, Text = "Payment" });
        editor.edgeLabels.Add(new EdgeLabel { Id = 4, EdgeId = 4, Text = "Shipment" });
        editor.edgeLabels.Add(new EdgeLabel { Id = 5, EdgeId = 5, Text = "Notifications" });
    }

    private static void LoadLevel0DFD(DFDEditor editor)
    {
        // External entity
        editor.nodes.Add(new Node { Id = 1, Text = "User", X = 100, Y = 250, Width = 100, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#059669", Icon = "user" });
        
        // Processes
        editor.nodes.Add(new Node { Id = 2, Text = "1.0\nProcess\nRequest", X = 280, Y = 150, Width = 120, Height = 80, Shape = NodeShape.Ellipse, StrokeColor = "#3b82f6", Icon = "play" });
        editor.nodes.Add(new Node { Id = 3, Text = "2.0\nValidate\nData", X = 280, Y = 320, Width = 120, Height = 80, Shape = NodeShape.Ellipse, StrokeColor = "#3b82f6", Icon = "check" });
        editor.nodes.Add(new Node { Id = 4, Text = "3.0\nStore\nResult", X = 500, Y = 250, Width = 120, Height = 80, Shape = NodeShape.Ellipse, StrokeColor = "#3b82f6", Icon = "database" });
        
        // Data stores
        editor.nodes.Add(new Node { Id = 5, Text = "D1: User Data", X = 480, Y = 80, Width = 140, Height = 50, Shape = NodeShape.Parallelogram, StrokeColor = "#f59e0b", Icon = "storage" });
        editor.nodes.Add(new Node { Id = 6, Text = "D2: Logs", X = 480, Y = 400, Width = 140, Height = 50, Shape = NodeShape.Parallelogram, StrokeColor = "#f59e0b", Icon = "file" });
        
        editor.nextId = 7;
        
        // Edges
        editor.edges.Add(new Edge { Id = 1, From = 1, To = 2, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 2, From = 1, To = 3, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 3, From = 2, To = 4, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 4, From = 3, To = 4, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 5, From = 2, To = 5, FromConnection = CP("top"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 6, From = 4, To = 6, FromConnection = CP("bottom"), ToConnection = CP("left") });
        editor.nextEdgeId = 7;
    }

    private static void LoadLoginFlow(DFDEditor editor)
    {
        // Start/End
        editor.nodes.Add(new Node { Id = 1, Text = "Start", X = 300, Y = 50, Width = 80, Height = 40, Shape = NodeShape.Ellipse, StrokeColor = "#059669", Icon = "play", TemplateId = "flowchart", TemplateShapeId = "terminator" });
        editor.nodes.Add(new Node { Id = 8, Text = "End", X = 300, Y = 650, Width = 80, Height = 40, Shape = NodeShape.Ellipse, StrokeColor = "#dc2626", Icon = "stop", TemplateId = "flowchart", TemplateShapeId = "terminator" });
        
        // Process steps
        editor.nodes.Add(new Node { Id = 2, Text = "Display\nLogin Form", X = 270, Y = 120, Width = 140, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#3b82f6", Icon = "computer", TemplateId = "flowchart", TemplateShapeId = "process" });
        editor.nodes.Add(new Node { Id = 3, Text = "Enter\nCredentials", X = 270, Y = 210, Width = 140, Height = 60, Shape = NodeShape.Parallelogram, StrokeColor = "#8b5cf6", Icon = "user", TemplateId = "flowchart", TemplateShapeId = "data" });
        editor.nodes.Add(new Node { Id = 4, Text = "Valid?", X = 290, Y = 310, Width = 100, Height = 60, Shape = NodeShape.Diamond, StrokeColor = "#f59e0b", Icon = "key", TemplateId = "flowchart", TemplateShapeId = "decision" });
        
        // Branches
        editor.nodes.Add(new Node { Id = 5, Text = "Show\nError", X = 480, Y = 310, Width = 120, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#dc2626", Icon = "error" });
        editor.nodes.Add(new Node { Id = 6, Text = "Create\nSession", X = 270, Y = 420, Width = 140, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#3b82f6", Icon = "shield" });
        editor.nodes.Add(new Node { Id = 7, Text = "Redirect to\nDashboard", X = 270, Y = 530, Width = 140, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#3b82f6", Icon = "home" });
        
        editor.nextId = 9;
        
        // Edges
        editor.edges.Add(new Edge { Id = 1, From = 1, To = 2, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 2, From = 2, To = 3, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 3, From = 3, To = 4, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 4, From = 4, To = 5, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 5, From = 4, To = 6, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 6, From = 5, To = 2, FromConnection = CP("top"), ToConnection = CP("right") });
        editor.edges.Add(new Edge { Id = 7, From = 6, To = 7, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 8, From = 7, To = 8, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.nextEdgeId = 9;
        
        // Labels
        editor.edgeLabels.Add(new EdgeLabel { Id = 1, EdgeId = 4, Text = "No" });
        editor.edgeLabels.Add(new EdgeLabel { Id = 2, EdgeId = 5, Text = "Yes" });
    }

    private static void LoadEcommerceFlow(DFDEditor editor)
    {
        // Customer journey
        editor.nodes.Add(new Node { Id = 1, Text = "Browse\nProducts", X = 100, Y = 150, Width = 120, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#3b82f6", Icon = "search" });
        editor.nodes.Add(new Node { Id = 2, Text = "Add to\nCart", X = 280, Y = 150, Width = 120, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#3b82f6", Icon = "cart" });
        editor.nodes.Add(new Node { Id = 3, Text = "Checkout", X = 460, Y = 150, Width = 120, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#3b82f6", Icon = "credit-card" });
        editor.nodes.Add(new Node { Id = 4, Text = "Payment", X = 640, Y = 150, Width = 120, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#8b5cf6", Icon = "lock" });
        
        // Backend
        editor.nodes.Add(new Node { Id = 5, Text = "Product\nCatalog", X = 100, Y = 300, Width = 120, Height = 50, Shape = NodeShape.Cylinder, StrokeColor = "#f59e0b", Icon = "database" });
        editor.nodes.Add(new Node { Id = 6, Text = "Cart\nService", X = 280, Y = 300, Width = 120, Height = 50, Shape = NodeShape.Ellipse, StrokeColor = "#059669", Icon = "gear" });
        editor.nodes.Add(new Node { Id = 7, Text = "Order\nService", X = 460, Y = 300, Width = 120, Height = 50, Shape = NodeShape.Ellipse, StrokeColor = "#059669", Icon = "gear" });
        editor.nodes.Add(new Node { Id = 8, Text = "Payment\nGateway", X = 640, Y = 300, Width = 120, Height = 50, Shape = NodeShape.Rectangle, StrokeColor = "#dc2626", Icon = "shield" });
        
        // Data stores
        editor.nodes.Add(new Node { Id = 9, Text = "Orders DB", X = 460, Y = 420, Width = 120, Height = 50, Shape = NodeShape.Cylinder, StrokeColor = "#f59e0b", Icon = "database" });
        editor.nodes.Add(new Node { Id = 10, Text = "Notify\nCustomer", X = 640, Y = 420, Width = 120, Height = 50, Shape = NodeShape.Rectangle, StrokeColor = "#8b5cf6", Icon = "email" });
        
        editor.nextId = 11;
        
        // Edges  
        editor.edges.Add(new Edge { Id = 1, From = 1, To = 2, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 2, From = 2, To = 3, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 3, From = 3, To = 4, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 4, From = 1, To = 5, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 5, From = 2, To = 6, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 6, From = 3, To = 7, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 7, From = 4, To = 8, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 8, From = 7, To = 9, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 9, From = 8, To = 10, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.nextEdgeId = 10;
    }

    private static void LoadSoftwareArchitecture(DFDEditor editor)
    {
        // Presentation layer
        editor.nodes.Add(new Node { Id = 1, Text = "Web Browser", X = 100, Y = 80, Width = 120, Height = 50, Shape = NodeShape.Rectangle, StrokeColor = "#3b82f6", Icon = "computer" });
        editor.nodes.Add(new Node { Id = 2, Text = "Mobile App", X = 260, Y = 80, Width = 120, Height = 50, Shape = NodeShape.Rectangle, StrokeColor = "#3b82f6", Icon = "mobile" });
        editor.nodes.Add(new Node { Id = 3, Text = "API Client", X = 420, Y = 80, Width = 120, Height = 50, Shape = NodeShape.Rectangle, StrokeColor = "#3b82f6", Icon = "code" });
        
        // API Gateway
        editor.nodes.Add(new Node { Id = 4, Text = "API Gateway\n/ Load Balancer", X = 220, Y = 180, Width = 160, Height = 60, Shape = NodeShape.Rectangle, StrokeColor = "#8b5cf6", Icon = "cloud" });
        
        // Services
        editor.nodes.Add(new Node { Id = 5, Text = "Auth\nService", X = 80, Y = 300, Width = 100, Height = 60, Shape = NodeShape.Ellipse, StrokeColor = "#059669", Icon = "lock" });
        editor.nodes.Add(new Node { Id = 6, Text = "User\nService", X = 220, Y = 300, Width = 100, Height = 60, Shape = NodeShape.Ellipse, StrokeColor = "#059669", Icon = "user" });
        editor.nodes.Add(new Node { Id = 7, Text = "Order\nService", X = 360, Y = 300, Width = 100, Height = 60, Shape = NodeShape.Ellipse, StrokeColor = "#059669", Icon = "cart" });
        editor.nodes.Add(new Node { Id = 8, Text = "Email\nService", X = 500, Y = 300, Width = 100, Height = 60, Shape = NodeShape.Ellipse, StrokeColor = "#059669", Icon = "email" });
        
        // Databases
        editor.nodes.Add(new Node { Id = 9, Text = "Users DB", X = 150, Y = 430, Width = 100, Height = 50, Shape = NodeShape.Cylinder, StrokeColor = "#f59e0b", Icon = "database" });
        editor.nodes.Add(new Node { Id = 10, Text = "Orders DB", X = 330, Y = 430, Width = 100, Height = 50, Shape = NodeShape.Cylinder, StrokeColor = "#f59e0b", Icon = "database" });
        editor.nodes.Add(new Node { Id = 11, Text = "Cache", X = 500, Y = 430, Width = 100, Height = 50, Shape = NodeShape.Parallelogram, StrokeColor = "#dc2626", Icon = "storage" });
        
        editor.nextId = 12;
        
        // Edges
        editor.edges.Add(new Edge { Id = 1, From = 1, To = 4, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 2, From = 2, To = 4, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 3, From = 3, To = 4, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 4, From = 4, To = 5, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 5, From = 4, To = 6, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 6, From = 4, To = 7, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 7, From = 4, To = 8, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 8, From = 5, To = 9, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 9, From = 6, To = 9, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 10, From = 7, To = 10, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 11, From = 7, To = 11, FromConnection = CP("bottom"), ToConnection = CP("left") });
        editor.nextEdgeId = 12;
    }

    private static void LoadETLPipeline(DFDEditor editor)
    {
        // Sources
        editor.nodes.Add(new Node { Id = 1, Text = "CSV Files", X = 80, Y = 100, Width = 100, Height = 50, Shape = NodeShape.Parallelogram, StrokeColor = "#059669", Icon = "file" });
        editor.nodes.Add(new Node { Id = 2, Text = "API Data", X = 80, Y = 180, Width = 100, Height = 50, Shape = NodeShape.Parallelogram, StrokeColor = "#059669", Icon = "api" });
        editor.nodes.Add(new Node { Id = 3, Text = "Database", X = 80, Y = 260, Width = 100, Height = 50, Shape = NodeShape.Cylinder, StrokeColor = "#059669", Icon = "database" });
        
        // Extract
        editor.nodes.Add(new Node { Id = 4, Text = "Extract", X = 250, Y = 170, Width = 100, Height = 60, Shape = NodeShape.Ellipse, StrokeColor = "#3b82f6", Icon = "cloud-download" });
        
        // Transform
        editor.nodes.Add(new Node { Id = 5, Text = "Clean\nData", X = 400, Y = 100, Width = 100, Height = 50, Shape = NodeShape.Rectangle, StrokeColor = "#8b5cf6", Icon = "refresh" });
        editor.nodes.Add(new Node { Id = 6, Text = "Validate", X = 400, Y = 180, Width = 100, Height = 50, Shape = NodeShape.Diamond, StrokeColor = "#8b5cf6", Icon = "check" });
        editor.nodes.Add(new Node { Id = 7, Text = "Transform", X = 400, Y = 270, Width = 100, Height = 50, Shape = NodeShape.Rectangle, StrokeColor = "#8b5cf6", Icon = "gear" });
        
        // Load
        editor.nodes.Add(new Node { Id = 8, Text = "Load", X = 560, Y = 170, Width = 100, Height = 60, Shape = NodeShape.Ellipse, StrokeColor = "#f59e0b", Icon = "cloud-upload" });
        
        // Destinations
        editor.nodes.Add(new Node { Id = 9, Text = "Data\nWarehouse", X = 700, Y = 120, Width = 110, Height = 50, Shape = NodeShape.Cylinder, StrokeColor = "#dc2626", Icon = "database" });
        editor.nodes.Add(new Node { Id = 10, Text = "Analytics\nDashboard", X = 700, Y = 220, Width = 110, Height = 50, Shape = NodeShape.Rectangle, StrokeColor = "#dc2626", Icon = "computer" });
        
        editor.nextId = 11;
        
        // Edges
        editor.edges.Add(new Edge { Id = 1, From = 1, To = 4, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 2, From = 2, To = 4, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 3, From = 3, To = 4, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 4, From = 4, To = 5, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 5, From = 5, To = 6, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 6, From = 6, To = 7, FromConnection = CP("bottom"), ToConnection = CP("top") });
        editor.edges.Add(new Edge { Id = 7, From = 7, To = 8, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 8, From = 8, To = 9, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.edges.Add(new Edge { Id = 9, From = 8, To = 10, FromConnection = CP("right"), ToConnection = CP("left") });
        editor.nextEdgeId = 10;
    }
}
