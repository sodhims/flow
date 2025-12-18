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
    public string? FillColor { get; set; }
    public int? StrokeWidth { get; set; }
    public string? StrokeDashArray { get; set; }
    // Optional template id and shape id allow using a shape library (e.g. "flowchart", "circuit").
    // When set, the renderer will prefer the template shape over the `NodeShape` enum.
    public string? TemplateId { get; set; }
    public string? TemplateShapeId { get; set; }
    // Component-specific label (e.g., "R1", "C2", "L1" for circuit components)
    public string? ComponentLabel { get; set; }
    // Component value (e.g., "10kΩ", "100µF", "5V")
    public string? ComponentValue { get; set; }
}
public enum NodeShape
{
    Rectangle,
    Ellipse,
    Diamond,
    Parallelogram,
    Cylinder
}
