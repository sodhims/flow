using Microsoft.AspNetCore.Components;
using dfd2wasm.Models;

namespace dfd2wasm.Pages;

public partial class DFDEditor
{
    private string GetParallelPath(string pathData, double offset)
    {
        // Simple implementation - returns same path for double line effect
        return pathData;
    }

    private RenderFragment RenderEdgeLabel(Edge edge, (double X, double Y) midpoint) => builder =>
    {
        builder.OpenElement(0, "text");
        builder.AddAttribute(1, "x", midpoint.X);
        builder.AddAttribute(2, "y", midpoint.Y);
        builder.AddAttribute(3, "text-anchor", "middle");
        builder.AddAttribute(4, "dominant-baseline", "middle");
        builder.AddAttribute(5, "fill", "#374151");
        builder.AddAttribute(6, "font-size", "14");
        builder.AddAttribute(7, "font-weight", "bold");
        builder.AddAttribute(8, "style", "pointer-events: none; user-select: none;");
        builder.AddContent(9, edge.Label);
        builder.CloseElement();
    };

    private RenderFragment RenderRowGuideLabel(double y, int rowNumber) => builder =>
    {
        builder.OpenElement(0, "text");
        builder.AddAttribute(1, "x", "10");
        builder.AddAttribute(2, "y", (y - 5).ToString());
        builder.AddAttribute(3, "fill", "#ef4444");
        builder.AddAttribute(4, "font-size", "12");
        builder.AddAttribute(5, "font-weight", "bold");
        builder.AddContent(6, $"Row {rowNumber} — {y} px");
        builder.CloseElement();
    };

    private RenderFragment RenderColumnGuideLabel(double x, int columnNumber) => builder =>
    {
        builder.OpenElement(0, "text");
        builder.AddAttribute(1, "x", (x + 5).ToString());
        builder.AddAttribute(2, "y", "20");
        builder.AddAttribute(3, "fill", "#3b82f6");
        builder.AddAttribute(4, "font-size", "12");
        builder.AddAttribute(5, "font-weight", "bold");
        builder.AddContent(6, $"Col {columnNumber} — {x} px");
        builder.CloseElement();
    };

    private RenderFragment RenderNodeText(Node node) => builder =>
    {
        var textLines = node.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var lineHeight = 16.0;
        var centerX = node.Width / 2;
        
        if (textLines.Length <= 1)
        {
            builder.OpenElement(0, "text");
            builder.AddAttribute(1, "x", centerX.ToString());
            builder.AddAttribute(2, "y", (node.Height / 2).ToString());
            builder.AddAttribute(3, "text-anchor", "middle");
            builder.AddAttribute(4, "dominant-baseline", "middle");
            builder.AddAttribute(5, "fill", "#374151");
            builder.AddAttribute(6, "font-size", "14");
            builder.AddAttribute(7, "style", "pointer-events: none; user-select: none;");
            builder.AddContent(8, node.Text);
            builder.CloseElement();
        }
        else
        {
            var totalHeight = textLines.Length * lineHeight;
            var startY = (node.Height - totalHeight) / 2 + lineHeight / 2;
            
            for (int i = 0; i < textLines.Length; i++)
            {
                var lineY = startY + i * lineHeight;
                builder.OpenElement(0, "text");
                builder.AddAttribute(1, "x", centerX.ToString());
                builder.AddAttribute(2, "y", lineY.ToString());
                builder.AddAttribute(3, "text-anchor", "middle");
                builder.AddAttribute(4, "dominant-baseline", "middle");
                builder.AddAttribute(5, "fill", "#374151");
                builder.AddAttribute(6, "font-size", "14");
                builder.AddAttribute(7, "style", "pointer-events: none; user-select: none;");
                builder.AddContent(8, textLines[i]);
                builder.CloseElement();
            }
        }
    };

    private RenderFragment CreateSvgText(string x, string y, string content, string? fill = "#374151", string? fontSize = "14", string? fontWeight = "bold") => builder =>
    {
        builder.OpenElement(0, "text");
        builder.AddAttribute(1, "x", x);
        builder.AddAttribute(2, "y", y);
        builder.AddAttribute(3, "fill", fill);
        builder.AddAttribute(4, "font-size", fontSize);
        builder.AddAttribute(5, "font-weight", fontWeight);
        builder.AddAttribute(6, "style", "pointer-events: none; user-select: none;");
        builder.AddContent(7, content);
        builder.CloseElement();
    };

    private RenderFragment RenderSvgText(string content, double x, double y, 
        string textAnchor, string dominantBaseline, string fontSize, string fontWeight, 
        string fill, string? transform = null) => builder =>
    {
        builder.OpenElement(0, "text");
        builder.AddAttribute(1, "x", x);
        builder.AddAttribute(2, "y", y);
        builder.AddAttribute(3, "text-anchor", textAnchor);
        builder.AddAttribute(4, "dominant-baseline", dominantBaseline);
        builder.AddAttribute(5, "font-size", fontSize);
        builder.AddAttribute(6, "font-weight", fontWeight);
        builder.AddAttribute(7, "fill", fill);
        if (!string.IsNullOrEmpty(transform))
        {
            builder.AddAttribute(8, "transform", transform);
        }
        builder.AddAttribute(9, "style", "pointer-events: none;");
        builder.AddContent(10, content);
        builder.CloseElement();
    };
}
