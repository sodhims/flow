using dfd2wasm.Models;

namespace dfd2wasm.Pages;

public partial class DFDEditor
{
    #region Edge Styles

    private void ApplyEdgeStylesToSelected()
    {
        if (selectedEdges.Count == 0)
        {
            Console.WriteLine("ApplyEdgeStylesToSelected: No edges selected");
            return;
        }

        Console.WriteLine($"ApplyEdgeStylesToSelected: Applying to {selectedEdges.Count} edge(s)");
        Console.WriteLine($"  Width: {defaultStrokeWidth}, Color: {defaultStrokeColor}, Dash: {defaultStrokeDashArray}, Double: {defaultIsDoubleLine}");

        UndoService.SaveState(nodes, edges, edgeLabels);

        foreach (var edgeId in selectedEdges)
        {
            var edge = edges.FirstOrDefault(e => e.Id == edgeId);
            if (edge != null)
            {
                Console.WriteLine($"  Updating edge {edgeId}: Width {edge.StrokeWidth} → {defaultStrokeWidth}, Double {edge.IsDoubleLine} → {defaultIsDoubleLine}");
                edge.StrokeWidth = defaultStrokeWidth;
                edge.StrokeColor = defaultStrokeColor;
                edge.StrokeDashArray = defaultStrokeDashArray;
                edge.IsDoubleLine = defaultIsDoubleLine;
                
                edge.PathData = PathService.GetEdgePath(edge, nodes);
            }
        }

        Console.WriteLine("ApplyEdgeStylesToSelected: Calling StateHasChanged");
        InvokeAsync(StateHasChanged);
    }

    private void ApplyEdgeStylesToAll()
    {
        UndoService.SaveState(nodes, edges, edgeLabels);

        foreach (var edge in edges)
        {
            edge.StrokeWidth = editStrokeWidth;
            edge.StrokeColor = editStrokeColor;
            edge.StrokeDashArray = editStrokeDashArray;
            edge.IsDoubleLine = editIsDoubleLine;
        }

        showEdgeStylePanel = false;
        StateHasChanged();
    }

    #endregion

    #region Edge Labels

    private void UpdateLabelText(int labelId, string newText)
    {
        var label = edgeLabels.FirstOrDefault(l => l.Id == labelId);
        if (label != null)
        {
            label.Text = newText;
            StateHasChanged();
        }
    }

    private void StartEditingSelectedLabel()
    {
        if (selectedLabels.Count != 1) return;
        var labelId = selectedLabels.First();
        var label = edgeLabels.FirstOrDefault(l => l.Id == labelId);
        if (label == null) return;

        editingTextLabelId = labelId;
        editingTextNodeId = null;
        editingText = label.Text;
        showTextEditDialog = true;
    }

    #endregion
}
