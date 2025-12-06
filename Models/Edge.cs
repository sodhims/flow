namespace dfd2wasm.Models
{
    // EdgeStyle enum for different connector types
    public enum EdgeStyle
    {
        Direct,      // Straight line A→B
        Ortho,       // Right angles only
        OrthoRound,  // Right angles with rounded corners
        Bezier,      // Smooth S-curve
        Arc,         // Single curved arc
        Stylized     // Fancy with embellishments
    }

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
        
        // Replace IsOrthogonal with EdgeStyle
        public bool IsOrthogonal { get; set; } // Keep for backward compatibility
        public EdgeStyle Style { get; set; } = EdgeStyle.Direct;

        public string PathData { get; set; } = "";
        public string Label { get; set; } = "";

        public string? CustomFromSide { get; set; }
        public string? CustomToSide { get; set; }

        public List<Waypoint> Waypoints { get; set; } = new();
    }

    public class Waypoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public enum EditorMode
    {
        Select,
        AddNode
    }
}
