namespace dfd2wasm.Models
{
    // Edge.cs - Required structure for your Edge class

    public class Edge
    {
        public int Id { get; set; }
        public int From { get; set; }
        public int To { get; set; }
        public ConnectionPoint FromConnection { get; set; }
        public ConnectionPoint ToConnection { get; set; }
        public int? StrokeWidth { get; set; }
        public string? StrokeColor { get; set; }
        public string? StrokeDashArray { get; set; }
        public bool IsDoubleLine { get; set; }
        public bool IsOrthogonal { get; set; }

        // CRITICAL: Must have these two!
        public string PathData { get; set; } = "";
        public string Label { get; set; } = "";

        // Add these two lines to your Edge class:
        public string? CustomFromSide { get; set; }
        public string? CustomToSide { get; set; }

        public List<Waypoint> Waypoints { get; set; } = new();
    }
    // Waypoint.cs - For edge waypoints
    public class Waypoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    // Node.cs - Your node structure should include
 

    // EditorMode.cs - Enum for editor modes
    public enum EditorMode
    {
        Select,
        AddNode
    }
}
