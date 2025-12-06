namespace dfd2wasm.Models;

public class Node
{
    public int Id { get; init; }
    public string Text { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 120;
    public double Height { get; set; } = 60;
    public NodeShape Shape { get; set; } = NodeShape.Rectangle;
    public string StrokeColor { get; set; } = "#475569";
    public string? Icon { get; set; } = null; // Icon identifier (e.g., "user", "database", "cloud")
}

public enum NodeShape
{
    Rectangle,
    Ellipse,
    Diamond,
    Parallelogram,
    Cylinder
}
