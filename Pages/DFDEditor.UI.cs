using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace dfd2wasm.Pages;

public partial class DFDEditor
{
    #region Keyboard Handling

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        // Don't handle delete/backspace if user is typing in an input field
        if (e.Key == "Delete" || e.Key == "Backspace")
        {
            var isInputFocused = await JS.InvokeAsync<bool>("eval", "document.activeElement.tagName === 'INPUT' || document.activeElement.tagName === 'TEXTAREA'");
            if (isInputFocused) return;
        }
        
        // Ctrl+Z or Cmd+Z for Undo
        if ((e.CtrlKey || e.MetaKey) && e.Key == "z")
        {
            HandleUndo();
        }
        // Ctrl+A or Cmd+A to select all
        else if ((e.CtrlKey || e.MetaKey) && e.Key == "a")
        {
            selectedNodes.Clear();
            selectedEdges.Clear();
            selectedLabels.Clear();
            
            foreach (var node in nodes)
            {
                selectedNodes.Add(node.Id);
            }
            
            StateHasChanged();
        }
        // Delete or Backspace key to delete selected edges/nodes
        else if (e.Key == "Delete" || e.Key == "Backspace")
        {
            if (selectedEdges.Count > 0)
            {
                UndoService.SaveState(nodes, edges, edgeLabels);
                
                foreach (var edgeId in selectedEdges.ToList())
                {
                    var edge = edges.FirstOrDefault(ed => ed.Id == edgeId);
                    if (edge != null)
                    {
                        edges.Remove(edge);
                        
                        var labels = edgeLabels.Where(l => l.EdgeId == edgeId).ToList();
                        foreach (var label in labels)
                        {
                            edgeLabels.Remove(label);
                        }
                    }
                }
                
                selectedEdges.Clear();
                StateHasChanged();
            }
            else if (selectedNodes.Count > 0)
            {
                UndoService.SaveState(nodes, edges, edgeLabels);
                
                foreach (var nodeId in selectedNodes.ToList())
                {
                    var node = nodes.FirstOrDefault(n => n.Id == nodeId);
                    if (node != null)
                    {
                        nodes.Remove(node);
                        
                        var connectedEdges = edges.Where(e => e.From == nodeId || e.To == nodeId).ToList();
                        foreach (var edge in connectedEdges)
                        {
                            edges.Remove(edge);
                            
                            var labels = edgeLabels.Where(l => l.EdgeId == edge.Id).ToList();
                            foreach (var label in labels)
                            {
                                edgeLabels.Remove(label);
                            }
                        }
                    }
                }
                
                selectedNodes.Clear();
                StateHasChanged();
            }
        }
        // Escape key to cancel selection or clear selected items
        else if (e.Key == "Escape")
        {
            if (isPrintAreaSelection)
            {
                isPrintAreaSelection = false;
                isSelecting = false;
                selectionStart = null;
            }
            else if (selectedNodes.Count > 0 || selectedEdges.Count > 0 || selectedLabels.Count > 0)
            {
                selectedNodes.Clear();
                selectedEdges.Clear();
                selectedLabels.Clear();
                StateHasChanged();
            }
            else if (printArea.HasValue)
            {
                printArea = null;
                StateHasChanged();
            }
        }
    }

    private void HandleUndo()
    {
        var previousState = UndoService.Undo();
        if (previousState != null)
        {
            nodes = previousState.Nodes;
            edges = previousState.Edges;
            edgeLabels = previousState.EdgeLabels;
            
            RecalculateEdgePaths(null);
            
            selectedNodes.Clear();
            selectedEdges.Clear();
            selectedLabels.Clear();
            
            StateHasChanged();
        }
    }

    #endregion

    #region Text Editing

    private void HandleTextEditKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            CancelTextEdit();
        }
        else if (e.Key == "Enter" && e.CtrlKey)
        {
            SaveTextEdit();
        }
    }

    private void SaveTextEdit()
    {
        if (editingTextNodeId.HasValue)
        {
            var node = nodes.FirstOrDefault(n => n.Id == editingTextNodeId.Value);
            if (node != null)
            {
                UndoService.SaveState(nodes, edges, edgeLabels);
                node.Text = editingText;
            }
        }
        else if (editingTextLabelId.HasValue)
        {
            var label = edgeLabels.FirstOrDefault(l => l.Id == editingTextLabelId.Value);
            if (label != null)
            {
                UndoService.SaveState(nodes, edges, edgeLabels);
                label.Text = editingText;
            }
        }

        CloseTextEditDialog();
    }

    private void CancelTextEdit()
    {
        CloseTextEditDialog();
    }

    private void CloseTextEditDialog()
    {
        showTextEditDialog = false;
        editingTextNodeId = null;
        editingTextLabelId = null;
        editingText = "";
        StateHasChanged();
    }

    #endregion

    #region Dialogs and UI

    private void ConfirmClear()
    {
        UndoService.SaveState(nodes, edges, edgeLabels);
        
        nodes.Clear();
        edges.Clear();
        edgeLabels.Clear();
        selectedNodes.Clear();
        selectedEdges.Clear();
        selectedLabels.Clear();
        nextId = 1;
        nextEdgeId = 1;
        nextLabelId = 1;
        
        showClearConfirm = false;
        StateHasChanged();
    }

    private void ToggleOrthoMode()
    {
        useOrthoPlacement = !useOrthoPlacement;

        foreach (var edge in edges)
        {
            edge.IsOrthogonal = useOrthoPlacement;
        }

        StateHasChanged();
    }

    private void UpdateSwimlaneLabel(int index, string? label)
    {
        if (string.IsNullOrEmpty(label)) return;

        while (swimlaneLabels.Count <= index)
        {
            swimlaneLabels.Add($"Lane {swimlaneLabels.Count + 1}");
        }

        swimlaneLabels[index] = label;
    }

    private void UpdateColumnLabel(int index, string? label)
    {
        if (string.IsNullOrEmpty(label)) return;

        while (columnLabels.Count <= index)
        {
            columnLabels.Add($"Column {columnLabels.Count + 1}");
        }

        columnLabels[index] = label;
    }

    #endregion

    #region Zoom

    private void ZoomIn()
    {
        var currentIndex = Array.IndexOf(zoomLevels, zoomLevel);
        if (currentIndex < 0) currentIndex = 3;
        if (currentIndex < zoomLevels.Length - 1)
        {
            zoomLevel = zoomLevels[currentIndex + 1];
        }
    }

    private void ZoomOut()
    {
        var currentIndex = Array.IndexOf(zoomLevels, zoomLevel);
        if (currentIndex < 0) currentIndex = 3;
        if (currentIndex > 0)
        {
            zoomLevel = zoomLevels[currentIndex - 1];
        }
    }

    #endregion
}
