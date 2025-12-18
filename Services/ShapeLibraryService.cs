using System.Collections.Generic;
using System.Linq;
using System.Text;
using dfd2wasm.Models;

namespace dfd2wasm.Services;

public class ShapeLibraryService
{
    // RenderFunc returns an SVG fragment (inner markup) for the node
    public record ShapeDescriptor(string Id, string DisplayName, Func<Node, string> Render);
    public record Template(string Id, string DisplayName, List<ShapeDescriptor> Shapes);

    private readonly Dictionary<string, Template> templates = new();

    // Helper to render circuit component label next to the shape
    private static string RenderCircuitLabel(Node node, double labelX, double labelY, string? anchor = null)
    {
        if (string.IsNullOrEmpty(node.ComponentLabel)) return "";
        var sb = new StringBuilder();
        var textAnchor = anchor ?? "start";
        sb.Append($"<text x=\"{labelX}\" y=\"{labelY}\" font-size=\"10\" fill=\"{node.StrokeColor}\" font-family=\"sans-serif\" text-anchor=\"{textAnchor}\">");
        sb.Append(node.ComponentLabel);
        if (!string.IsNullOrEmpty(node.ComponentValue))
        {
            sb.Append($"<tspan x=\"{labelX}\" dy=\"12\" font-size=\"9\" fill=\"#6b7280\">{node.ComponentValue}</tspan>");
        }
        sb.Append("</text>");
        return sb.ToString();
    }

    public ShapeLibraryService()
    {
        RegisterFlowchartTemplate();
        RegisterCircuitTemplate();
        RegisterICDTemplate();
        RegisterNetworkTemplate();
        RegisterBPMNTemplate();
    }

    private void RegisterFlowchartTemplate()
    {
        var shapes = new List<ShapeDescriptor>
        {
            new ShapeDescriptor("process", "Process", node =>
            {
                return $"<rect x=\"0\" y=\"0\" width=\"{node.Width}\" height=\"{node.Height}\" rx=\"6\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />";
            }),

            new ShapeDescriptor("decision", "Decision", node =>
            {
                var midX = node.Width / 2;
                var midY = node.Height / 2;
                var points = $"{midX},0 {node.Width},{midY} {midX},{node.Height} 0,{midY}";
                return $"<polygon points=\"{points}\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />";
            }),

            new ShapeDescriptor("terminator", "Terminator", node =>
            {
                return $"<rect x=\"0\" y=\"0\" width=\"{node.Width}\" height=\"{node.Height}\" rx=\"{node.Height / 2}\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />";
            }),

            new ShapeDescriptor("data", "Data (I/O)", node =>
            {
                var skew = 15.0;
                var points = $"{skew},0 {node.Width},0 {node.Width - skew},{node.Height} 0,{node.Height}";
                return $"<polygon points=\"{points}\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />";
            }),

            new ShapeDescriptor("database", "Database", node =>
            {
                var rx = node.Width / 2;
                var ellipseRy = 10.0;
                var cy1 = ellipseRy;
                var cy2 = node.Height - ellipseRy;
                var sb = new StringBuilder();
                sb.Append($"<g style=\"cursor:inherit;\">");
                sb.Append($"<path d=\"M 0,{cy1} Q 0,{cy1 - ellipseRy} {rx},{cy1 - ellipseRy} Q {node.Width},{cy1 - ellipseRy} {node.Width},{cy1} L {node.Width},{cy2} Q {node.Width},{cy2 + ellipseRy} {rx},{cy2 + ellipseRy} Q 0,{cy2 + ellipseRy} 0,{cy2} Z\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                sb.Append($"<path d=\"M 0,{cy1} Q 0,{cy1 + ellipseRy / 2} {rx},{cy1 + ellipseRy / 2} Q {node.Width},{cy1 + ellipseRy / 2} {node.Width},{cy1}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                sb.Append("</g>");
                return sb.ToString();
            }),

            new ShapeDescriptor("document", "Document", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var waveHeight = h * 0.15;
                return $"<path d=\"M 0,0 L {w},0 L {w},{h - waveHeight} Q {w * 0.75},{h - waveHeight * 2} {w * 0.5},{h - waveHeight} Q {w * 0.25},{h} 0,{h - waveHeight} Z\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />";
            }),

            new ShapeDescriptor("predefined", "Predefined Process", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var barWidth = w * 0.1;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                sb.Append($"<line x1=\"{barWidth}\" y1=\"0\" x2=\"{barWidth}\" y2=\"{h}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                sb.Append($"<line x1=\"{w - barWidth}\" y1=\"0\" x2=\"{w - barWidth}\" y2=\"{h}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                return sb.ToString();
            })
        };

        templates["flowchart"] = new Template("flowchart", "Flowchart", shapes);
    }

    private void RegisterCircuitTemplate()
    {
        var shapes = new List<ShapeDescriptor>
        {
            // Basic Components
            new ShapeDescriptor("resistor", "Resistor", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var cy = h / 2;
                var segment = w / 8.0;
                var zigHeight = h / 3;
                var pathSb = new StringBuilder();
                pathSb.Append($"M 0,{cy} L {segment},{cy} ");
                pathSb.Append($"L {segment * 1.5},{cy - zigHeight} ");
                pathSb.Append($"L {segment * 2.5},{cy + zigHeight} ");
                pathSb.Append($"L {segment * 3.5},{cy - zigHeight} ");
                pathSb.Append($"L {segment * 4.5},{cy + zigHeight} ");
                pathSb.Append($"L {segment * 5.5},{cy - zigHeight} ");
                pathSb.Append($"L {segment * 6.5},{cy + zigHeight} ");
                pathSb.Append($"L {segment * 7},{cy} ");
                pathSb.Append($"L {w},{cy}");
                var sb = new StringBuilder();
                sb.Append($"<path d=\"{pathSb}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                sb.Append(RenderCircuitLabel(node, w / 2, cy - zigHeight - 8, "middle"));
                return sb.ToString();
            }),

            new ShapeDescriptor("capacitor", "Capacitor", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var cx = w / 2;
                var cy = h / 2;
                var plateHeight = h * 0.6;
                var gap = w * 0.08;
                var sb = new StringBuilder();
                sb.Append($"<line x1=\"0\" y1=\"{cy}\" x2=\"{cx - gap}\" y2=\"{cy}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<line x1=\"{cx - gap}\" y1=\"{cy - plateHeight / 2}\" x2=\"{cx - gap}\" y2=\"{cy + plateHeight / 2}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<line x1=\"{cx + gap}\" y1=\"{cy - plateHeight / 2}\" x2=\"{cx + gap}\" y2=\"{cy + plateHeight / 2}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<line x1=\"{cx + gap}\" y1=\"{cy}\" x2=\"{w}\" y2=\"{cy}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append(RenderCircuitLabel(node, cx, cy - plateHeight / 2 - 8, "middle"));
                return sb.ToString();
            }),

            new ShapeDescriptor("inductor", "Inductor", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var cy = h / 2;
                var loopCount = 4;
                var loopWidth = (w - 20) / loopCount;
                var loopRadius = loopWidth / 2;
                var pathSb = new StringBuilder();
                pathSb.Append($"M 0,{cy} L 10,{cy} ");
                for (int i = 0; i < loopCount; i++)
                {
                    var startX = 10 + i * loopWidth;
                    pathSb.Append($"A {loopRadius},{loopRadius} 0 0 1 {startX + loopWidth},{cy} ");
                }
                pathSb.Append($"L {w},{cy}");
                var sb = new StringBuilder();
                sb.Append($"<path d=\"{pathSb}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append(RenderCircuitLabel(node, w / 2, cy - loopRadius - 8, "middle"));
                return sb.ToString();
            }),

            new ShapeDescriptor("diode", "Diode", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var cx = w / 2;
                var cy = h / 2;
                var triSize = Math.Min(w, h) * 0.4;
                var sb = new StringBuilder();
                sb.Append($"<line x1=\"0\" y1=\"{cy}\" x2=\"{cx - triSize / 2}\" y2=\"{cy}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<polygon points=\"{cx - triSize / 2},{cy - triSize / 2} {cx - triSize / 2},{cy + triSize / 2} {cx + triSize / 2},{cy}\" fill=\"{node.FillColor ?? "white"}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<line x1=\"{cx + triSize / 2}\" y1=\"{cy - triSize / 2}\" x2=\"{cx + triSize / 2}\" y2=\"{cy + triSize / 2}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<line x1=\"{cx + triSize / 2}\" y1=\"{cy}\" x2=\"{w}\" y2=\"{cy}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append(RenderCircuitLabel(node, cx, cy - triSize / 2 - 8, "middle"));
                return sb.ToString();
            }),

            new ShapeDescriptor("transistor-npn", "NPN Transistor", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var cx = w / 2;
                var cy = h / 2;
                var radius = Math.Min(w, h) * 0.35;
                var sb = new StringBuilder();
                sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{radius}\" fill=\"{node.FillColor ?? "white"}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                // Base line
                sb.Append($"<line x1=\"0\" y1=\"{cy}\" x2=\"{cx - radius * 0.3}\" y2=\"{cy}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<line x1=\"{cx - radius * 0.3}\" y1=\"{cy - radius * 0.5}\" x2=\"{cx - radius * 0.3}\" y2=\"{cy + radius * 0.5}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                // Emitter (with arrow)
                sb.Append($"<line x1=\"{cx - radius * 0.3}\" y1=\"{cy + radius * 0.25}\" x2=\"{cx + radius * 0.5}\" y2=\"{cy + radius * 0.7}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<line x1=\"{cx + radius * 0.5}\" y1=\"{cy + radius * 0.7}\" x2=\"{cx + radius * 0.5}\" y2=\"{h}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                // Collector
                sb.Append($"<line x1=\"{cx - radius * 0.3}\" y1=\"{cy - radius * 0.25}\" x2=\"{cx + radius * 0.5}\" y2=\"{cy - radius * 0.7}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<line x1=\"{cx + radius * 0.5}\" y1=\"{cy - radius * 0.7}\" x2=\"{cx + radius * 0.5}\" y2=\"0\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append(RenderCircuitLabel(node, cx + radius + 5, cy - radius, "start"));
                return sb.ToString();
            }),

            new ShapeDescriptor("ground", "Ground", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var cx = w / 2;
                var sb = new StringBuilder();
                sb.Append($"<line x1=\"{cx}\" y1=\"0\" x2=\"{cx}\" y2=\"{h * 0.4}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<line x1=\"{cx - w * 0.3}\" y1=\"{h * 0.4}\" x2=\"{cx + w * 0.3}\" y2=\"{h * 0.4}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<line x1=\"{cx - w * 0.2}\" y1=\"{h * 0.55}\" x2=\"{cx + w * 0.2}\" y2=\"{h * 0.55}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<line x1=\"{cx - w * 0.1}\" y1=\"{h * 0.7}\" x2=\"{cx + w * 0.1}\" y2=\"{h * 0.7}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append(RenderCircuitLabel(node, cx + w * 0.35, h * 0.5, "start"));
                return sb.ToString();
            }),

            new ShapeDescriptor("vcc", "VCC/Power", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var cx = w / 2;
                var sb = new StringBuilder();
                sb.Append($"<line x1=\"{cx}\" y1=\"{h}\" x2=\"{cx}\" y2=\"{h * 0.4}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<polygon points=\"{cx},{h * 0.15} {cx - w * 0.15},{h * 0.4} {cx + w * 0.15},{h * 0.4}\" fill=\"{node.StrokeColor}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append(RenderCircuitLabel(node, cx + w * 0.2, h * 0.25, "start"));
                return sb.ToString();
            }),

            // Logic Gates
            new ShapeDescriptor("and-gate", "AND Gate", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<path d=\"M 0,0 L {w * 0.5},0 A {w * 0.5},{h / 2} 0 0 1 {w * 0.5},{h} L 0,{h} Z\" fill=\"{node.FillColor ?? "white"}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                // Input lines
                sb.Append($"<line x1=\"-10\" y1=\"{h * 0.3}\" x2=\"0\" y2=\"{h * 0.3}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<line x1=\"-10\" y1=\"{h * 0.7}\" x2=\"0\" y2=\"{h * 0.7}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                // Output line
                sb.Append($"<line x1=\"{w}\" y1=\"{h / 2}\" x2=\"{w + 10}\" y2=\"{h / 2}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append(RenderCircuitLabel(node, w / 2, -5, "middle"));
                return sb.ToString();
            }),

            new ShapeDescriptor("or-gate", "OR Gate", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<path d=\"M 0,0 Q {w * 0.3},0 {w * 0.5},{h * 0.1} Q {w},{h * 0.3} {w},{h / 2} Q {w},{h * 0.7} {w * 0.5},{h * 0.9} Q {w * 0.3},{h} 0,{h} Q {w * 0.2},{h / 2} 0,0 Z\" fill=\"{node.FillColor ?? "white"}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append(RenderCircuitLabel(node, w / 2, -5, "middle"));
                return sb.ToString();
            }),

            new ShapeDescriptor("not-gate", "NOT Gate", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var triWidth = w * 0.8;
                var circleR = w * 0.08;
                var sb = new StringBuilder();
                sb.Append($"<polygon points=\"0,0 {triWidth},{h / 2} 0,{h}\" fill=\"{node.FillColor ?? "white"}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append($"<circle cx=\"{triWidth + circleR}\" cy=\"{h / 2}\" r=\"{circleR}\" fill=\"{node.FillColor ?? "white"}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                sb.Append(RenderCircuitLabel(node, w / 2, -5, "middle"));
                return sb.ToString();
            }),

            new ShapeDescriptor("ic-chip", "IC Chip", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var pinCount = 4;
                var pinWidth = 8;
                var pinHeight = 4;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"{pinWidth}\" y=\"0\" width=\"{w - pinWidth * 2}\" height=\"{h}\" rx=\"2\" fill=\"{node.FillColor ?? "white"}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                // Notch
                sb.Append($"<circle cx=\"{pinWidth + 10}\" cy=\"10\" r=\"4\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                // Pins on left
                for (int i = 0; i < pinCount; i++)
                {
                    var py = (h / (pinCount + 1)) * (i + 1);
                    sb.Append($"<rect x=\"0\" y=\"{py - pinHeight / 2}\" width=\"{pinWidth}\" height=\"{pinHeight}\" fill=\"{node.StrokeColor}\" />");
                }
                // Pins on right
                for (int i = 0; i < pinCount; i++)
                {
                    var py = (h / (pinCount + 1)) * (i + 1);
                    sb.Append($"<rect x=\"{w - pinWidth}\" y=\"{py - pinHeight / 2}\" width=\"{pinWidth}\" height=\"{pinHeight}\" fill=\"{node.StrokeColor}\" />");
                }
                sb.Append(RenderCircuitLabel(node, w / 2, -5, "middle"));
                return sb.ToString();
            }),

            new ShapeDescriptor("op-amp", "Op-Amp", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<polygon points=\"0,0 {w},{h / 2} 0,{h}\" fill=\"{node.FillColor ?? "white"}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{node.StrokeWidth ?? 2}\" />");
                // + and - inputs
                sb.Append($"<text x=\"8\" y=\"{h * 0.35}\" font-size=\"14\" fill=\"{node.StrokeColor}\">+</text>");
                sb.Append($"<text x=\"8\" y=\"{h * 0.75}\" font-size=\"14\" fill=\"{node.StrokeColor}\">−</text>");
                sb.Append(RenderCircuitLabel(node, w / 2, -5, "middle"));
                return sb.ToString();
            })
        };

        templates["circuit"] = new Template("circuit", "Circuit Diagram", shapes);
    }

    private void RegisterICDTemplate()
    {
        var shapes = new List<ShapeDescriptor>
        {
            // System Blocks
            new ShapeDescriptor("system", "System", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" rx=\"4\" fill=\"{(node.FillColor ?? "#e0f2fe")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"18\" rx=\"4\" fill=\"{node.StrokeColor}\" />");
                sb.Append($"<text x=\"{w / 2}\" y=\"13\" text-anchor=\"middle\" font-size=\"10\" fill=\"white\" font-weight=\"bold\">SYSTEM</text>");
                return sb.ToString();
            }),

            new ShapeDescriptor("subsystem", "Subsystem", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" rx=\"4\" fill=\"{(node.FillColor ?? "#fef3c7")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" stroke-dasharray=\"5,3\" />");
                sb.Append($"<text x=\"5\" y=\"14\" font-size=\"9\" fill=\"{node.StrokeColor}\" font-style=\"italic\">subsystem</text>");
                return sb.ToString();
            }),

            new ShapeDescriptor("external-system", "External System", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" rx=\"0\" fill=\"{(node.FillColor ?? "#fee2e2")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 3)}\" />");
                sb.Append($"<line x1=\"0\" y1=\"18\" x2=\"{w}\" y2=\"18\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                sb.Append($"<text x=\"{w / 2}\" y=\"13\" text-anchor=\"middle\" font-size=\"9\" fill=\"{node.StrokeColor}\">EXTERNAL</text>");
                return sb.ToString();
            }),

            new ShapeDescriptor("hardware", "Hardware Component", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" fill=\"{(node.FillColor ?? "#d1fae5")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // 3D effect
                sb.Append($"<path d=\"M {w},0 L {w + 8},{-6} L {w + 8},{h - 6} L {w},{h}\" fill=\"{(node.FillColor ?? "#a7f3d0")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                sb.Append($"<path d=\"M 0,0 L 8,-6 L {w + 8},-6 L {w},0\" fill=\"{(node.FillColor ?? "#6ee7b7")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("software", "Software Component", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" rx=\"8\" fill=\"{(node.FillColor ?? "#dbeafe")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // Code icon
                sb.Append($"<text x=\"8\" y=\"{h / 2 + 4}\" font-size=\"12\" fill=\"{node.StrokeColor}\" font-family=\"monospace\">&lt;/&gt;</text>");
                return sb.ToString();
            }),

            // Interface Types
            new ShapeDescriptor("data-interface", "Data Interface", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var arrowSize = 12;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"{arrowSize}\" y=\"0\" width=\"{w - arrowSize * 2}\" height=\"{h}\" fill=\"{(node.FillColor ?? "#f0fdf4")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // Left arrow
                sb.Append($"<polygon points=\"0,{h / 2} {arrowSize},{h / 4} {arrowSize},{h * 3 / 4}\" fill=\"{node.StrokeColor}\" />");
                // Right arrow
                sb.Append($"<polygon points=\"{w},{h / 2} {w - arrowSize},{h / 4} {w - arrowSize},{h * 3 / 4}\" fill=\"{node.StrokeColor}\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("control-interface", "Control Interface", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" fill=\"{(node.FillColor ?? "#fef9c3")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // Control symbol (gear-like)
                var cx = w - 15;
                var cy = 15;
                sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"8\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"2\" />");
                sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"3\" fill=\"{node.StrokeColor}\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("power-interface", "Power Interface", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" fill=\"{(node.FillColor ?? "#fee2e2")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // Lightning bolt
                sb.Append($"<path d=\"M {w - 20},{5} L {w - 25},{h / 2} L {w - 18},{h / 2} L {w - 22},{h - 5} L {w - 12},{h / 2 - 2} L {w - 18},{h / 2 - 2} L {w - 14},{5} Z\" fill=\"{node.StrokeColor}\" />");
                return sb.ToString();
            }),

            // Connectors
            new ShapeDescriptor("connector-serial", "Serial Port", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" rx=\"4\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // DB9-like pins
                var pinRows = 2;
                var pinsPerRow = new int[] { 5, 4 };
                for (int row = 0; row < pinRows; row++)
                {
                    var pins = pinsPerRow[row];
                    var rowY = h / 3 + row * (h / 3);
                    var startX = (w - (pins * 8 + (pins - 1) * 4)) / 2;
                    for (int p = 0; p < pins; p++)
                    {
                        sb.Append($"<circle cx=\"{startX + p * 12 + 4}\" cy=\"{rowY}\" r=\"3\" fill=\"{node.StrokeColor}\" />");
                    }
                }
                return sb.ToString();
            }),

            new ShapeDescriptor("connector-ethernet", "Ethernet Port", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" rx=\"2\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // RJ45-like shape
                var portW = w * 0.6;
                var portH = h * 0.5;
                var portX = (w - portW) / 2;
                var portY = (h - portH) / 2;
                sb.Append($"<rect x=\"{portX}\" y=\"{portY}\" width=\"{portW}\" height=\"{portH}\" fill=\"{node.StrokeColor}\" rx=\"2\" />");
                sb.Append($"<rect x=\"{portX + 2}\" y=\"{portY + 2}\" width=\"{portW - 4}\" height=\"{portH * 0.4}\" fill=\"{(node.FillColor ?? "white")}\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("connector-usb", "USB Port", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" rx=\"4\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // USB symbol
                var cx = w / 2;
                var cy = h / 2;
                sb.Append($"<circle cx=\"{cx}\" cy=\"{cy - 8}\" r=\"4\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"2\" />");
                sb.Append($"<line x1=\"{cx}\" y1=\"{cy - 4}\" x2=\"{cx}\" y2=\"{cy + 10}\" stroke=\"{node.StrokeColor}\" stroke-width=\"2\" />");
                sb.Append($"<line x1=\"{cx - 8}\" y1=\"{cy + 2}\" x2=\"{cx + 8}\" y2=\"{cy + 2}\" stroke=\"{node.StrokeColor}\" stroke-width=\"2\" />");
                sb.Append($"<rect x=\"{cx - 10}\" y=\"{cy}\" width=\"4\" height=\"6\" fill=\"{node.StrokeColor}\" />");
                sb.Append($"<rect x=\"{cx + 6}\" y=\"{cy}\" width=\"4\" height=\"6\" fill=\"{node.StrokeColor}\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("connector-wireless", "Wireless Interface", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" rx=\"4\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // WiFi symbol
                var cx = w / 2;
                var cy = h / 2 + 5;
                sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"3\" fill=\"{node.StrokeColor}\" />");
                sb.Append($"<path d=\"M {cx - 8},{cy - 8} A 12,12 0 0 1 {cx + 8},{cy - 8}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"2\" />");
                sb.Append($"<path d=\"M {cx - 14},{cy - 14} A 20,20 0 0 1 {cx + 14},{cy - 14}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"2\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("interface-block", "Interface Block", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                // Main block with notched corner
                sb.Append($"<path d=\"M 0,0 L {w - 15},0 L {w},15 L {w},{h} L 0,{h} Z\" fill=\"{(node.FillColor ?? "#f3e8ff")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                sb.Append($"<path d=\"M {w - 15},0 L {w - 15},15 L {w},15\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                return sb.ToString();
            })
        };

        templates["icd"] = new Template("icd", "Interface Control Diagram", shapes);
    }

    private void RegisterNetworkTemplate()
    {
        var shapes = new List<ShapeDescriptor>
        {
            new ShapeDescriptor("router", "Router", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<circle cx=\"{w / 2}\" cy=\"{h / 2}\" r=\"{Math.Min(w, h) / 2 - 2}\" fill=\"{(node.FillColor ?? "#dbeafe")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // Arrow cross pattern
                var cx = w / 2;
                var cy = h / 2;
                var arrowLen = Math.Min(w, h) * 0.25;
                sb.Append($"<line x1=\"{cx}\" y1=\"{cy - arrowLen}\" x2=\"{cx}\" y2=\"{cy + arrowLen}\" stroke=\"{node.StrokeColor}\" stroke-width=\"2\" />");
                sb.Append($"<line x1=\"{cx - arrowLen}\" y1=\"{cy}\" x2=\"{cx + arrowLen}\" y2=\"{cy}\" stroke=\"{node.StrokeColor}\" stroke-width=\"2\" />");
                // Arrow heads
                sb.Append($"<polygon points=\"{cx},{cy - arrowLen - 5} {cx - 4},{cy - arrowLen + 2} {cx + 4},{cy - arrowLen + 2}\" fill=\"{node.StrokeColor}\" />");
                sb.Append($"<polygon points=\"{cx},{cy + arrowLen + 5} {cx - 4},{cy + arrowLen - 2} {cx + 4},{cy + arrowLen - 2}\" fill=\"{node.StrokeColor}\" />");
                sb.Append($"<polygon points=\"{cx - arrowLen - 5},{cy} {cx - arrowLen + 2},{cy - 4} {cx - arrowLen + 2},{cy + 4}\" fill=\"{node.StrokeColor}\" />");
                sb.Append($"<polygon points=\"{cx + arrowLen + 5},{cy} {cx + arrowLen - 2},{cy - 4} {cx + arrowLen - 2},{cy + 4}\" fill=\"{node.StrokeColor}\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("switch", "Switch", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" rx=\"4\" fill=\"{(node.FillColor ?? "#d1fae5")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // Port indicators
                var portCount = 4;
                var portW = 8;
                var portH = 6;
                var spacing = (w - portCount * portW) / (portCount + 1);
                for (int i = 0; i < portCount; i++)
                {
                    var px = spacing + i * (portW + spacing);
                    sb.Append($"<rect x=\"{px}\" y=\"{h - portH - 4}\" width=\"{portW}\" height=\"{portH}\" fill=\"{node.StrokeColor}\" rx=\"1\" />");
                }
                // Arrows showing switching
                sb.Append($"<path d=\"M {w * 0.3},{h * 0.3} L {w * 0.7},{h * 0.3}\" stroke=\"{node.StrokeColor}\" stroke-width=\"2\" marker-end=\"url(#arrowhead)\" />");
                sb.Append($"<path d=\"M {w * 0.7},{h * 0.5} L {w * 0.3},{h * 0.5}\" stroke=\"{node.StrokeColor}\" stroke-width=\"2\" marker-end=\"url(#arrowhead)\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("server", "Server", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                var unitH = h / 3;
                for (int i = 0; i < 3; i++)
                {
                    var y = i * unitH;
                    sb.Append($"<rect x=\"0\" y=\"{y}\" width=\"{w}\" height=\"{unitH - 2}\" rx=\"2\" fill=\"{(node.FillColor ?? "#e5e7eb")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                    // LED indicators
                    sb.Append($"<circle cx=\"{w - 12}\" cy=\"{y + unitH / 2}\" r=\"3\" fill=\"#22c55e\" />");
                    sb.Append($"<circle cx=\"{w - 24}\" cy=\"{y + unitH / 2}\" r=\"3\" fill=\"#f59e0b\" />");
                    // Drive slots
                    sb.Append($"<rect x=\"8\" y=\"{y + 4}\" width=\"{w * 0.4}\" height=\"{unitH - 10}\" fill=\"{node.StrokeColor}\" rx=\"1\" opacity=\"0.3\" />");
                }
                return sb.ToString();
            }),

            new ShapeDescriptor("firewall", "Firewall", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" fill=\"{(node.FillColor ?? "#fee2e2")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // Brick pattern
                var brickH = h / 4;
                var brickW = w / 3;
                for (int row = 0; row < 4; row++)
                {
                    var offset = (row % 2 == 0) ? 0 : brickW / 2;
                    for (double x = offset; x < w; x += brickW)
                    {
                        sb.Append($"<rect x=\"{x}\" y=\"{row * brickH}\" width=\"{Math.Min(brickW - 2, w - x)}\" height=\"{brickH - 2}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                    }
                }
                return sb.ToString();
            }),

            new ShapeDescriptor("cloud", "Cloud", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<path d=\"M {w * 0.25},{h * 0.7} ");
                sb.Append($"A {w * 0.2},{h * 0.25} 0 1 1 {w * 0.35},{h * 0.4} ");
                sb.Append($"A {w * 0.15},{h * 0.2} 0 1 1 {w * 0.55},{h * 0.25} ");
                sb.Append($"A {w * 0.2},{h * 0.25} 0 1 1 {w * 0.8},{h * 0.45} ");
                sb.Append($"A {w * 0.15},{h * 0.2} 0 1 1 {w * 0.75},{h * 0.7} Z\" ");
                sb.Append($"fill=\"{(node.FillColor ?? "#dbeafe")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("database-server", "Database Server", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var rx = w / 2;
                var ellipseRy = h * 0.12;
                var sb = new StringBuilder();
                // Multiple stacked cylinders
                for (int i = 2; i >= 0; i--)
                {
                    var yOffset = i * 8;
                    var cy1 = ellipseRy + yOffset;
                    var cy2 = h - ellipseRy - (2 - i) * 8;
                    if (i == 0)
                    {
                        sb.Append($"<path d=\"M 0,{cy1} Q 0,{cy1 - ellipseRy} {rx},{cy1 - ellipseRy} Q {w},{cy1 - ellipseRy} {w},{cy1} L {w},{cy2} Q {w},{cy2 + ellipseRy} {rx},{cy2 + ellipseRy} Q 0,{cy2 + ellipseRy} 0,{cy2} Z\" fill=\"{(node.FillColor ?? "#fef3c7")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                        sb.Append($"<path d=\"M 0,{cy1} Q 0,{cy1 + ellipseRy} {rx},{cy1 + ellipseRy} Q {w},{cy1 + ellipseRy} {w},{cy1}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                    }
                }
                return sb.ToString();
            }),

            new ShapeDescriptor("workstation", "Workstation", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                // Monitor
                var monitorH = h * 0.65;
                sb.Append($"<rect x=\"{w * 0.1}\" y=\"0\" width=\"{w * 0.8}\" height=\"{monitorH}\" rx=\"4\" fill=\"{(node.FillColor ?? "#e5e7eb")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                sb.Append($"<rect x=\"{w * 0.15}\" y=\"4\" width=\"{w * 0.7}\" height=\"{monitorH - 12}\" fill=\"#1e293b\" />");
                // Stand
                sb.Append($"<rect x=\"{w * 0.4}\" y=\"{monitorH}\" width=\"{w * 0.2}\" height=\"{h * 0.15}\" fill=\"{node.StrokeColor}\" />");
                sb.Append($"<rect x=\"{w * 0.25}\" y=\"{h * 0.85}\" width=\"{w * 0.5}\" height=\"{h * 0.08}\" rx=\"2\" fill=\"{node.StrokeColor}\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("laptop", "Laptop", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                // Screen
                sb.Append($"<rect x=\"{w * 0.1}\" y=\"0\" width=\"{w * 0.8}\" height=\"{h * 0.6}\" rx=\"4\" fill=\"{(node.FillColor ?? "#e5e7eb")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                sb.Append($"<rect x=\"{w * 0.14}\" y=\"4\" width=\"{w * 0.72}\" height=\"{h * 0.52}\" fill=\"#1e293b\" />");
                // Keyboard base
                sb.Append($"<path d=\"M 0,{h * 0.65} L {w * 0.1},{h * 0.6} L {w * 0.9},{h * 0.6} L {w},{h * 0.65} L {w},{h} L 0,{h} Z\" fill=\"{(node.FillColor ?? "#d1d5db")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("mobile", "Mobile Device", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" rx=\"8\" fill=\"{(node.FillColor ?? "#e5e7eb")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // Screen
                sb.Append($"<rect x=\"4\" y=\"{h * 0.1}\" width=\"{w - 8}\" height=\"{h * 0.75}\" fill=\"#1e293b\" rx=\"2\" />");
                // Home button
                sb.Append($"<circle cx=\"{w / 2}\" cy=\"{h * 0.92}\" r=\"{Math.Min(w, h) * 0.06}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"2\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("printer", "Printer", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                // Paper tray top
                sb.Append($"<rect x=\"{w * 0.15}\" y=\"0\" width=\"{w * 0.7}\" height=\"{h * 0.2}\" fill=\"white\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                // Main body
                sb.Append($"<rect x=\"0\" y=\"{h * 0.2}\" width=\"{w}\" height=\"{h * 0.5}\" rx=\"4\" fill=\"{(node.FillColor ?? "#e5e7eb")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // Paper tray bottom
                sb.Append($"<rect x=\"{w * 0.1}\" y=\"{h * 0.7}\" width=\"{w * 0.8}\" height=\"{h * 0.25}\" fill=\"{(node.FillColor ?? "#f3f4f6")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("internet", "Internet", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var cx = w / 2;
                var cy = h / 2;
                var r = Math.Min(w, h) / 2 - 2;
                var sb = new StringBuilder();
                sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{r}\" fill=\"{(node.FillColor ?? "#dbeafe")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // Globe lines
                sb.Append($"<ellipse cx=\"{cx}\" cy=\"{cy}\" rx=\"{r * 0.4}\" ry=\"{r}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                sb.Append($"<line x1=\"{cx - r}\" y1=\"{cy}\" x2=\"{cx + r}\" y2=\"{cy}\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                sb.Append($"<path d=\"M {cx - r},{cy - r * 0.5} Q {cx},{cy - r * 0.4} {cx + r},{cy - r * 0.5}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                sb.Append($"<path d=\"M {cx - r},{cy + r * 0.5} Q {cx},{cy + r * 0.4} {cx + r},{cy + r * 0.5}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                return sb.ToString();
            })
        };

        templates["network"] = new Template("network", "Network Diagram", shapes);
    }

    private void RegisterBPMNTemplate()
    {
        var shapes = new List<ShapeDescriptor>
        {
            new ShapeDescriptor("task", "Task", node =>
            {
                var w = node.Width;
                var h = node.Height;
                return $"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" rx=\"8\" fill=\"{(node.FillColor ?? "#dbeafe")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />";
            }),

            new ShapeDescriptor("start-event", "Start Event", node =>
            {
                var cx = node.Width / 2;
                var cy = node.Height / 2;
                var r = Math.Min(node.Width, node.Height) / 2 - 2;
                return $"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{r}\" fill=\"{(node.FillColor ?? "#d1fae5")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />";
            }),

            new ShapeDescriptor("end-event", "End Event", node =>
            {
                var cx = node.Width / 2;
                var cy = node.Height / 2;
                var r = Math.Min(node.Width, node.Height) / 2 - 2;
                var sb = new StringBuilder();
                sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{r}\" fill=\"{(node.FillColor ?? "#fee2e2")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 4)}\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("intermediate-event", "Intermediate Event", node =>
            {
                var cx = node.Width / 2;
                var cy = node.Height / 2;
                var r = Math.Min(node.Width, node.Height) / 2 - 2;
                var sb = new StringBuilder();
                sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{r}\" fill=\"{(node.FillColor ?? "#fef3c7")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{r * 0.8}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("exclusive-gateway", "Exclusive Gateway (XOR)", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var cx = w / 2;
                var cy = h / 2;
                var sb = new StringBuilder();
                sb.Append($"<polygon points=\"{cx},0 {w},{cy} {cx},{h} 0,{cy}\" fill=\"{(node.FillColor ?? "#fef3c7")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // X mark
                var markSize = Math.Min(w, h) * 0.2;
                sb.Append($"<line x1=\"{cx - markSize}\" y1=\"{cy - markSize}\" x2=\"{cx + markSize}\" y2=\"{cy + markSize}\" stroke=\"{node.StrokeColor}\" stroke-width=\"3\" />");
                sb.Append($"<line x1=\"{cx + markSize}\" y1=\"{cy - markSize}\" x2=\"{cx - markSize}\" y2=\"{cy + markSize}\" stroke=\"{node.StrokeColor}\" stroke-width=\"3\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("parallel-gateway", "Parallel Gateway (AND)", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var cx = w / 2;
                var cy = h / 2;
                var sb = new StringBuilder();
                sb.Append($"<polygon points=\"{cx},0 {w},{cy} {cx},{h} 0,{cy}\" fill=\"{(node.FillColor ?? "#dbeafe")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // + mark
                var markSize = Math.Min(w, h) * 0.2;
                sb.Append($"<line x1=\"{cx}\" y1=\"{cy - markSize}\" x2=\"{cx}\" y2=\"{cy + markSize}\" stroke=\"{node.StrokeColor}\" stroke-width=\"3\" />");
                sb.Append($"<line x1=\"{cx - markSize}\" y1=\"{cy}\" x2=\"{cx + markSize}\" y2=\"{cy}\" stroke=\"{node.StrokeColor}\" stroke-width=\"3\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("inclusive-gateway", "Inclusive Gateway (OR)", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var cx = w / 2;
                var cy = h / 2;
                var sb = new StringBuilder();
                sb.Append($"<polygon points=\"{cx},0 {w},{cy} {cx},{h} 0,{cy}\" fill=\"{(node.FillColor ?? "#d1fae5")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // Circle mark
                sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{Math.Min(w, h) * 0.18}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"3\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("subprocess", "Sub-Process", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" rx=\"8\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                // Collapse marker
                var markerSize = 12;
                var mx = (w - markerSize) / 2;
                var my = h - markerSize - 4;
                sb.Append($"<rect x=\"{mx}\" y=\"{my}\" width=\"{markerSize}\" height=\"{markerSize}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                sb.Append($"<line x1=\"{mx + markerSize / 2}\" y1=\"{my + 2}\" x2=\"{mx + markerSize / 2}\" y2=\"{my + markerSize - 2}\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                sb.Append($"<line x1=\"{mx + 2}\" y1=\"{my + markerSize / 2}\" x2=\"{mx + markerSize - 2}\" y2=\"{my + markerSize / 2}\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("pool", "Pool", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var headerW = 30;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{headerW}\" height=\"{h}\" fill=\"{(node.FillColor ?? "#f3f4f6")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("lane", "Lane", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 1)}\" stroke-dasharray=\"5,3\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("data-object", "Data Object", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var foldSize = 15;
                var sb = new StringBuilder();
                sb.Append($"<path d=\"M 0,0 L {w - foldSize},0 L {w},{foldSize} L {w},{h} L 0,{h} Z\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                sb.Append($"<path d=\"M {w - foldSize},0 L {w - foldSize},{foldSize} L {w},{foldSize}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("data-store", "Data Store", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var rx = w / 2;
                var ellipseRy = 8;
                var sb = new StringBuilder();
                sb.Append($"<path d=\"M 0,{ellipseRy} Q 0,0 {rx},0 Q {w},0 {w},{ellipseRy} L {w},{h - ellipseRy} Q {w},{h} {rx},{h} Q 0,{h} 0,{h - ellipseRy} Z\" fill=\"{(node.FillColor ?? "white")}\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                sb.Append($"<path d=\"M 0,{ellipseRy} Q 0,{ellipseRy * 2} {rx},{ellipseRy * 2} Q {w},{ellipseRy * 2} {w},{ellipseRy}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"1\" />");
                return sb.ToString();
            }),

            new ShapeDescriptor("annotation", "Annotation", node =>
            {
                var w = node.Width;
                var h = node.Height;
                var sb = new StringBuilder();
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{w}\" height=\"{h}\" fill=\"{(node.FillColor ?? "#fefce8")}\" stroke=\"none\" />");
                sb.Append($"<path d=\"M 10,0 L 0,0 L 0,{h} L 10,{h}\" fill=\"none\" stroke=\"{node.StrokeColor}\" stroke-width=\"{(node.StrokeWidth ?? 2)}\" />");
                return sb.ToString();
            })
        };

        templates["bpmn"] = new Template("bpmn", "BPMN (Business Process)", shapes);
    }

    public IEnumerable<Template> GetTemplates() => templates.Values;

    public Template? GetTemplate(string id)
    {
        templates.TryGetValue(id, out var tpl);
        return tpl;
    }

    public ShapeDescriptor? GetShape(string templateId, string shapeId)
    {
        var tpl = GetTemplate(templateId);
        return tpl?.Shapes.FirstOrDefault(s => s.Id == shapeId);
    }
}
