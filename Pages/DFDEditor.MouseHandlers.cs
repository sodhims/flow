using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using dfd2wasm.Models;

namespace dfd2wasm.Pages;

public partial class DFDEditor
{
    private async Task HandleCanvasMouseDown(MouseEventArgs e)
    {
        try
        {
            Console.WriteLine($"HandleCanvasMouseDown - Mode: {mode}, PrintAreaSelection: {isPrintAreaSelection}");

            var scrollInfo = await JS.InvokeAsync<double[]>("getScrollInfo", canvasRef);
            double scrollOffsetX = scrollInfo?[0] ?? 0;
            double scrollOffsetY = scrollInfo?[1] ?? 0;

            double diagX = (e.OffsetX + scrollOffsetX) / zoomLevel;
            double diagY = (e.OffsetY + scrollOffsetY) / zoomLevel;

            if (isPrintAreaSelection)
            {
                isSelecting = true;
                selectionStart = (diagX, diagY);
                Console.WriteLine($"Print area selection started at ({diagX}, {diagY})");
            }
            else if ((mode == EditorMode.Select || selectToolActive) && !e.ShiftKey && !e.CtrlKey)
            {
                isSelecting = true;
                selectionStart = (diagX, diagY);
                Console.WriteLine($"Selection started at ({diagX}, {diagY})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION in HandleCanvasMouseDown: {ex.Message}");
        }
    }

    private async Task HandleCanvasMouseMove(MouseEventArgs e)
    {
        try
        {
            var scrollInfo = await JS.InvokeAsync<double[]>("getScrollInfo", canvasRef);
            double scrollOffsetX = scrollInfo?[0] ?? 0;
            double scrollOffsetY = scrollInfo?[1] ?? 0;

            double diagX = (e.OffsetX + scrollOffsetX) / zoomLevel;
            double diagY = (e.OffsetY + scrollOffsetY) / zoomLevel;

            currentMousePosition = (diagX, diagY);
            lastMouseX = diagX;
            lastMouseY = diagY;

            svgMouseX = diagX;
            svgMouseY = diagY;
        }
        catch
        {
            // If JS call fails, fall back to raw offsets
            currentMousePosition = (e.OffsetX, e.OffsetY);
            lastMouseX = e.OffsetX;
            lastMouseY = e.OffsetY;
            svgMouseX = e.OffsetX;
            svgMouseY = e.OffsetY;
        }

        if (isSelecting && selectionStart.HasValue)
        {
            StateHasChanged();
        }
        else if (resizingNodeId != null)
        {
            var node = nodes.FirstOrDefault(n => n.Id == resizingNodeId);
            if (node != null)
            {
                double newWidth = Math.Max(40, currentMousePosition.X - node.X);
                double newHeight = Math.Max(30, currentMousePosition.Y - node.Y);
                
                node.Width = newWidth;
                node.Height = newHeight;
                
                RecalculateEdgePaths(resizingNodeId.Value);
                StateHasChanged();
            }
        }
        else if (draggingNodeId != null)
        {
            var node = nodes.FirstOrDefault(n => n.Id == draggingNodeId);
            if (node != null)
            {
                double newX = e.ClientX - dragOffsetX;
                double newY = e.ClientY - dragOffsetY;

                double deltaX = newX - node.X;
                double deltaY = newY - node.Y;

                if (e.ShiftKey)
                {
                    var dx = Math.Abs(newX - dragStartX);
                    var dy = Math.Abs(newY - dragStartY);
                    if (dx > dy)
                    {
                        deltaY = 0;
                        newY = node.Y;
                    }
                    else
                    {
                        deltaX = 0;
                        newX = node.X;
                    }
                }

                node.X = newX;
                node.Y = newY;
                
                if (selectedNodes.Count > 1)
                {
                    foreach (var selectedNodeId in selectedNodes)
                    {
                        if (selectedNodeId != draggingNodeId.Value)
                        {
                            var selectedNode = nodes.FirstOrDefault(n => n.Id == selectedNodeId);
                            if (selectedNode != null)
                            {
                                selectedNode.X += deltaX;
                                selectedNode.Y += deltaY;
                                RecalculateEdgePaths(selectedNodeId);
                            }
                        }
                    }
                }
                
                RecalculateEdgePaths(draggingNodeId.Value);
                StateHasChanged();
            }
        }
        else if (draggingEdgeId != null && draggingWaypointIndex >= 0)
        {
            var edge = edges.FirstOrDefault(e => e.Id == draggingEdgeId);
            if (edge != null && draggingWaypointIndex < edge.Waypoints.Count)
            {
                edge.Waypoints[draggingWaypointIndex] = new Waypoint
                {
                    X = currentMousePosition.X,
                    Y = currentMousePosition.Y
                };
                UpdateEdgePath(edge);
                StateHasChanged();
            }
        }
    }

    private async Task HandleCanvasMouseUp(MouseEventArgs e)
    {
        if (isSelecting)
        {
            if (isPrintAreaSelection)
            {
                var rect = GetSelectionRectangle();
                printArea = (rect.X, rect.Y, rect.Width, rect.Height);
                
                isSelecting = false;
                selectionStart = null;
                isPrintAreaSelection = false;
                
                _ = ExportToPDF();
            }
            else
            {
                var rect = GetSelectionRectangle();
                selectedNodes.Clear();
                selectedEdges.Clear();

                // Select nodes within rectangle
                foreach (var node in nodes)
                {
                    if (node.X >= rect.X && node.X <= rect.X + rect.Width &&
                        node.Y >= rect.Y && node.Y <= rect.Y + rect.Height)
                    {
                        selectedNodes.Add(node.Id);
                    }
                }

                // Select edges where BOTH endpoints are within the selection
                foreach (var edge in edges)
                {
                    if (selectedNodes.Contains(edge.From) && selectedNodes.Contains(edge.To))
                    {
                        selectedEdges.Add(edge.Id);
                    }
                }

                isSelecting = false;
                selectionStart = null;
            }
        }

        draggingNodeId = null;
        resizingNodeId = null;
        draggingEdgeId = null;
        draggingWaypointIndex = -1;
    }

    private async Task HandleCanvasClick(MouseEventArgs e)
    {
        try
        {
            var scrollInfo = await JS.InvokeAsync<double[]>("getScrollInfo", canvasRef);
            double scrollOffsetX = scrollInfo?[0] ?? 0;
            double scrollOffsetY = scrollInfo?[1] ?? 0;
            
            double clickX = (e.OffsetX + scrollOffsetX) / zoomLevel;
            double clickY = (e.OffsetY + scrollOffsetY) / zoomLevel;
            
            Console.WriteLine("=== HandleCanvasClick START ===");
            Console.WriteLine($"Mode: {mode}");
            Console.WriteLine($"ClickX: {clickX}, ClickY: {clickY} (scroll: {scrollOffsetX}, {scrollOffsetY}, zoom: {zoomLevel})");
            
            if (mode == EditorMode.Select || selectToolActive)
            {
                if (selectedEdges.Count > 0)
                {
                    selectedEdges.Clear();
                    StateHasChanged();
                }
                return;
            }

            if (mode != EditorMode.AddNode)
            {
                Console.WriteLine("Not in AddNode mode, returning");
                return;
            }

            Console.WriteLine($"Array Mode: {arrayMode}, Count: {arrayCount}, Orientation: {arrayOrientation}");

            UndoService.SaveState(nodes, edges, edgeLabels);

            if (arrayMode)
            {
                for (int i = 0; i < arrayCount; i++)
                {
                    double nodeX, nodeY;

                    if (arrayOrientation == "horizontal")
                    {
                        nodeX = clickX - 60 + (i * arraySpacing);
                        nodeY = clickY - 30;
                    }
                    else
                    {
                        nodeX = clickX - 60;
                        nodeY = clickY - 30 + (i * arraySpacing);
                    }

                    // Generate component label for circuit components
                    string? componentLabel = null;
                    string nodeText = $"Node {nodes.Count + 1}";
                    if (selectedTemplateId == "circuit" && !string.IsNullOrEmpty(selectedTemplateShapeId))
                    {
                        componentLabel = GetNextComponentLabel(selectedTemplateShapeId);
                        nodeText = componentLabel;
                    }

                    var newNode = new Node
                    {
                        Id = nextId++,
                        X = nodeX,
                        Y = nodeY,
                        Width = 120,
                        Height = 60,
                        Text = nodeText,
                        Shape = selectedShape,
                        TemplateId = selectedTemplateId,
                        TemplateShapeId = selectedTemplateShapeId,
                        ComponentLabel = componentLabel
                    };

                    nodes.Add(newNode);
                    Console.WriteLine($"Array node {i + 1} created at ({nodeX}, {nodeY})");
                }
            }
            else
            {
                // Generate component label for circuit components
                string? componentLabel = null;
                string nodeText = $"Node {nodes.Count + 1}";
                if (selectedTemplateId == "circuit" && !string.IsNullOrEmpty(selectedTemplateShapeId))
                {
                    componentLabel = GetNextComponentLabel(selectedTemplateShapeId);
                    nodeText = componentLabel;
                }

                var newNode = new Node
                {
                    Id = nextId++,
                    X = clickX - 60,
                    Y = clickY - 30,
                    Width = 120,
                    Height = 60,
                    Text = nodeText,
                    Shape = selectedShape,
                    TemplateId = selectedTemplateId,
                    TemplateShapeId = selectedTemplateShapeId,
                    ComponentLabel = componentLabel
                };

                Console.WriteLine($"Node created with ID: {newNode.Id}");
                nodes.Add(newNode);
            }

            Console.WriteLine("=== HandleCanvasClick END ===");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION in HandleCanvasClick: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }
    }

    private async void HandleNodeClick(int nodeId, MouseEventArgs e)
    {
        Console.WriteLine($"HandleNodeClick - NodeId: {nodeId}, Mode: {mode}");
        Console.WriteLine($"  chainMode={chainMode}, lastChainedNodeId={lastChainedNodeId}");

        if (!(mode == EditorMode.Select || selectToolActive)) return;

        // Ctrl+click on node with PDF attachment opens the PDF viewer
        if (e.CtrlKey)
        {
            var node = nodes.FirstOrDefault(n => n.Id == nodeId);
            var pdfAttachment = node?.Attachments?.FirstOrDefault(a => a.FileType == AttachmentType.Pdf);
            if (pdfAttachment != null)
            {
                await JS.InvokeVoidAsync("openPdfViewer", pdfAttachment.DataUri);
                return;
            }
        }

        // In Rearrange mode, just select/deselect nodes - no connection logic
        if (connectionMode == ConnectionModeType.Rearrange)
        {
            if (e.ShiftKey)
            {
                if (selectedNodes.Contains(nodeId))
                    selectedNodes.Remove(nodeId);
                else
                    selectedNodes.Add(nodeId);
            }
            else
            {
                selectedNodes.Clear();
                selectedNodes.Add(nodeId);
            }
            selectedEdges.Clear();
            StateHasChanged();
            return;
        }

        // Handle chain mode
        if (chainMode)
        {
            Console.WriteLine($"  CHAIN MODE: lastChainedNodeId={lastChainedNodeId}");

            if (lastChainedNodeId.HasValue && lastChainedNodeId.Value != nodeId)
            {
                Console.WriteLine($"  CREATING EDGE: {lastChainedNodeId.Value} -> {nodeId}");

                UndoService.SaveState(nodes, edges, edgeLabels);

                var fromNode = nodes.FirstOrDefault(n => n.Id == lastChainedNodeId.Value);
                var toNode = nodes.FirstOrDefault(n => n.Id == nodeId);

                if (fromNode != null && toNode != null)
                {
                    var (fromConn, toConn) = GeometryService.GetOptimalConnectionPoints(fromNode, toNode);

                    var newEdge = CreateEdgeWithDefaults(lastChainedNodeId.Value, nodeId, fromConn, toConn);
                    newEdge.PathData = PathService.GetEdgePath(newEdge, nodes);
                    edges.Add(newEdge);
                }
            }

            lastChainedNodeId = nodeId;
            selectedNodes.Clear();
            selectedNodes.Add(nodeId);
            StateHasChanged();
            return;
        }

        // Handle 1:N mode - click on nodes to connect
        if (connectionMode == ConnectionModeType.OneToN)
        {
            if (HandleOneToNNodeClick(nodeId))
                return;
        }
        // Handle 1:N Area mode - first click sets source, subsequent are area selects
        else if (connectionMode == ConnectionModeType.OneToNArea)
        {
            if (HandleOneToNAreaNodeClick(nodeId))
                return;
        }

        // Handle pending connection from connection point
        if (pendingConnectionNodeId.HasValue && pendingConnection != null)
        {
            if (pendingConnectionNodeId.Value == nodeId)
            {
                pendingConnectionNodeId = null;
                pendingConnection = null;
                return;
            }

            UndoService.SaveState(nodes, edges, edgeLabels);

            var toNode = nodes.FirstOrDefault(n => n.Id == nodeId);
            if (toNode != null)
            {
                var fromNode = nodes.FirstOrDefault(n => n.Id == pendingConnectionNodeId.Value);
                var (_, toConn) = GeometryService.GetOptimalConnectionPoints(fromNode!, toNode);

                var newEdge = CreateEdgeWithDefaults(pendingConnectionNodeId.Value, nodeId, pendingConnection!, toConn);
                newEdge.PathData = PathService.GetEdgePath(newEdge, nodes);

                edges.Add(newEdge);
                pendingConnectionNodeId = null;
                pendingConnection = null;
                selectedNodes.Clear();
                return;
            }
        }

        selectedEdges.Clear();

        if (e.ShiftKey)
        {
            if (selectedNodes.Contains(nodeId))
            {
                selectedNodes.Remove(nodeId);
            }
            else
            {
                selectedNodes.Add(nodeId);
            }
            return;
        }

        if (selectedNodes.Contains(nodeId))
        {
            selectedNodes.Remove(nodeId);
        }
        else if (selectedNodes.Count == 0)
        {
            selectedNodes.Add(nodeId);
        }
        else if (selectedNodes.Count == 1)
        {
            UndoService.SaveState(nodes, edges, edgeLabels);

            var fromNode = nodes.First(n => n.Id == selectedNodes[0]);
            var toNode = nodes.First(n => n.Id == nodeId);

            var (fromConn, toConn) = GeometryService.GetOptimalConnectionPoints(fromNode, toNode);

            var newEdge = CreateEdgeWithDefaults(selectedNodes[0], nodeId, fromConn, toConn);
            newEdge.PathData = PathService.GetEdgePath(newEdge, nodes);

            edges.Add(newEdge);
            selectedNodes.Clear();
        }
        else
        {
            selectedNodes.Clear();
            selectedNodes.Add(nodeId);
        }
    }

    private void HandleNodeMouseDown(int nodeId, MouseEventArgs e)
    {
        if (!(mode == EditorMode.Select || selectToolActive)) return;
        if (resizingNodeId != null) return;
        if (chainMode) return;  // Don't start drag in chain mode

        if (e.Detail == 2)
        {
            EnableTextEdit(nodeId);
            return;
        }

        var node = nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node == null) return;

        UndoService.SaveState(nodes, edges, edgeLabels);

        draggingNodeId = nodeId;
        dragOffsetX = e.ClientX - node.X;
        dragOffsetY = e.ClientY - node.Y;
        dragStartX = node.X;
        dragStartY = node.Y;
    }

    private void HandleConnectionPointClick(int nodeId, string side, int position, MouseEventArgs e)
    {
        if (!(mode == EditorMode.Select || selectToolActive)) return;

        // Check for multi-connect mode first
        if (HandleMultiConnectClick(nodeId, side, position))
            return;

        if (pendingConnectionNodeId.HasValue && pendingConnection != null)
        {
            if (pendingConnectionNodeId.Value == nodeId)
            {
                pendingConnectionNodeId = null;
                pendingConnection = null;
                return;
            }

            UndoService.SaveState(nodes, edges, edgeLabels);

            var fromConn = new ConnectionPoint
            {
                Side = pendingConnection.Side,
                Position = pendingConnection.Position
            };
            var toConn = new ConnectionPoint
            {
                Side = side,
                Position = position
            };

            var newEdge = CreateEdgeWithDefaults(pendingConnectionNodeId.Value, nodeId, fromConn, toConn);
            newEdge.PathData = PathService.GetEdgePath(newEdge, nodes);
            edges.Add(newEdge);

            pendingConnectionNodeId = null;
            pendingConnection = null;
            pendingConnectionPoint = null;
        }
        else
        {
            pendingConnectionNodeId = nodeId;
            pendingConnection = new ConnectionPoint
            {
                Side = side,
                Position = position
            };

            var node = nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                pendingConnectionPoint = GeometryService.GetConnectionPointCoordinates(node, side, position);
            }
        }

        StateHasChanged();
    }

    private void HandleEdgeClick(int edgeId, MouseEventArgs e)
    {
        if (e.ShiftKey)
        {
            if (selectedEdges.Contains(edgeId))
            {
                selectedEdges.Remove(edgeId);
            }
            else
            {
                selectedEdges.Add(edgeId);
            }
            
            StateHasChanged();
        }
        else
        {
            selectedEdges.Clear();
            selectedEdges.Add(edgeId);
            selectedNodes.Clear();
            
            var edge = edges.FirstOrDefault(ed => ed.Id == edgeId);
            if (edge != null)
            {
                editStrokeWidth = edge.StrokeWidth ?? 2;
                editStrokeColor = edge.StrokeColor ?? "#374151";
                editStrokeDashArray = edge.StrokeDashArray ?? "";
                editIsDoubleLine = edge.IsDoubleLine;
                editEdgeStyle = edge.Style;
                editArrowDirection = edge.ArrowDirection;
            }
            
            showEdgeStylePanel = true;
            StateHasChanged();
        }
    }

    private void StartDraggingWaypoint(int edgeId, Waypoint waypoint, MouseEventArgs e)
    {
        draggingEdgeId = edgeId;
        var edge = edges.FirstOrDefault(ed => ed.Id == edgeId);
        if (edge != null)
        {
            draggingWaypointIndex = edge.Waypoints.IndexOf(waypoint);
        }
    }

    private async Task HandleCanvasScroll()
    {
        var scrollInfo = await JS.InvokeAsync<double[]>("getScrollInfo", canvasRef);
        if (scrollInfo != null && scrollInfo.Length >= 4)
        {
            scrollX = scrollInfo[0];
            scrollY = scrollInfo[1];
            viewportWidth = scrollInfo[2];
            viewportHeight = scrollInfo[3];
            StateHasChanged();
        }
    }

    private async Task HandleMinimapClick(MouseEventArgs e)
    {
        try
        {
            var minimapBounds = await JS.InvokeAsync<double[]>("getMinimapBounds", minimapRef);
            if (minimapBounds == null || minimapBounds.Length < 4) return;

            double minimapWidth = minimapBounds[2];
            double minimapHeight = minimapBounds[3];

            double scaleX = 4000.0 / minimapWidth;
            double scaleY = 4000.0 / minimapHeight;

            double canvasX = e.OffsetX * scaleX;
            double canvasY = e.OffsetY * scaleY;

            double targetScrollX = canvasX - (viewportWidth / 2);
            double targetScrollY = canvasY - (viewportHeight / 2);

            targetScrollX = Math.Max(0, Math.Min(targetScrollX, 4000 - viewportWidth));
            targetScrollY = Math.Max(0, Math.Min(targetScrollY, 4000 - viewportHeight));

            await JS.InvokeVoidAsync("scrollCanvasTo", canvasRef, targetScrollX, targetScrollY);

            scrollX = targetScrollX;
            scrollY = targetScrollY;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HandleMinimapClick: {ex.Message}");
        }
    }

    private void EnableTextEdit(int nodeId) { /* Implement text editing */ }
}
