using Microsoft.AspNetCore.Components.Web;

namespace dfd2wasm.Pages;

public partial class DFDEditor
{
    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        // Escape - cancel current operation (smart handling for 1:N mode)
        if (e.Key == "Escape")
        {
            CancelCurrentOperation();
            return;
        }

        // Delete - delete selected items
        if (e.Key == "Delete" || e.Key == "Backspace")
        {
            if (selectedNodes.Any() || selectedEdges.Any() || selectedLabels.Any())
            {
                DeleteSelected();
            }
            return;
        }

        // Ctrl+Z - Undo
        if (e.CtrlKey && e.Key == "z")
        {
            HandleUndo();
            return;
        }

        // Ctrl+A - Select all
        if (e.CtrlKey && e.Key == "a")
        {
            SelectAll();
            return;
        }

        // Ctrl+C - Copy
        if (e.CtrlKey && e.Key == "c")
        {
            await CopySelected();
            return;
        }

        // Ctrl+V - Paste
        if (e.CtrlKey && e.Key == "v")
        {
            await PasteNodes();
            return;
        }

        // +/= - Zoom in
        if (e.Key == "+" || e.Key == "=")
        {
            ZoomIn();
            return;
        }

        // - - Zoom out
        if (e.Key == "-")
        {
            ZoomOut();
            return;
        }

        // 0 - Reset zoom
        if (e.Key == "0" && e.CtrlKey)
        {
            ResetZoom();
            return;
        }

        // Mode shortcuts (only when not using Ctrl/Alt modifiers)
        if (!e.CtrlKey && !e.AltKey)
        {
            switch (e.Key.ToLowerInvariant())
            {
                case "s":
                    mode = Models.EditorMode.Select;
                    chainMode = false;
                    connectionMode = ConnectionModeType.Normal;
                    ClearOneToNState();
                    StateHasChanged();
                    return;
                case "a":
                    mode = Models.EditorMode.AddNode;
                    chainMode = false;
                    multiConnectMode = false;
                    ClearMultiConnectState();
                    StateHasChanged();
                    return;
                case "c":
                    // Toggle chain connect mode
                    mode = Models.EditorMode.Select;
                    chainMode = !chainMode;
                    if (chainMode)
                    {
                        connectionMode = ConnectionModeType.Normal;
                        ClearOneToNState();
                    }
                    lastChainedNodeId = null;
                    StateHasChanged();
                    return;
            }
        }

        // Arrow keys - nudge selected nodes
        if (selectedNodes.Any())
        {
            double dx = 0, dy = 0;
            double step = e.ShiftKey ? 10 : (snapToGrid ? GridSize : 1);

            switch (e.Key)
            {
                case "ArrowUp": dy = -step; break;
                case "ArrowDown": dy = step; break;
                case "ArrowLeft": dx = -step; break;
                case "ArrowRight": dx = step; break;
            }

            if (dx != 0 || dy != 0)
            {
                NudgeSelectedNodes(dx, dy);
            }
        }
    }

    private void CancelCurrentOperation()
    {
        // Special handling for 1:N modes - just reset source, stay in mode
        if (connectionMode == ConnectionModeType.OneToN || connectionMode == ConnectionModeType.OneToNArea)
        {
            // If we have a source selected, just clear it (stay in mode)
            if (oneToNSourceNode.HasValue)
            {
                oneToNSourceNode = null;
                oneToNSourcePoint = null;
                oneToNSourceCoords = null;
                isOneToNAreaSelecting = false;
                oneToNAreaStart = null;
                StateHasChanged();
                return;
            }
            // If no source, exit the mode entirely
            connectionMode = ConnectionModeType.Normal;
            StateHasChanged();
            return;
        }
        
        // Cancel any pending connections
        pendingConnectionNodeId = null;
        pendingConnection = null;
        pendingConnectionPoint = null;
        
        // Cancel chain mode
        chainMode = false;
        lastChainedNodeId = null;
        
        // Cancel selection
        isSelecting = false;
        selectionStart = null;
        
        // Cancel edge reconnection
        pendingEdgeReconnectId = null;
        pendingEdgeReconnectEnd = null;
        
        // Clear text editing
        showTextEditDialog = false;
        editingTextNodeId = null;
        editingTextLabelId = null;
        
        StateHasChanged();
    }

    private void SelectAll()
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

    private void NudgeSelectedNodes(double dx, double dy)
    {
        UndoService.SaveState(nodes, edges, edgeLabels);

        foreach (var nodeId in selectedNodes)
        {
            var node = nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                node.X += dx;
                node.Y += dy;
            }
        }

        RecalculateEdgePaths();
        StateHasChanged();
    }

    private const int GridSize = 20;
}
