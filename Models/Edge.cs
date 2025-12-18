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
        Stylized,    // Fancy with embellishments
        Circuit      // Circuit/bus-style orthogonal routing with jumps
    }

    // ArrowDirection enum for arrow placement
    public enum ArrowDirection
    {
        End,         // ---->  (default, arrow at destination)
        Start,       // <----  (arrow at source)
        Both,        // <--->  (arrows at both ends)
        None         // -----  (no arrows)
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
        
        // Arrow direction
        public ArrowDirection ArrowDirection { get; set; } = ArrowDirection.End;

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
        // Layer 0 = normal, 1 = via (jump)
        public int Layer { get; set; } = 0;
    }

    public enum EditorMode
    {
        Select,
        AddNode
    }
}
