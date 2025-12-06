using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using dfd2wasm.Models;

namespace dfd2wasm.Pages;

public partial class DFDEditor
{
    private void HandleCanvasMouseDown(MouseEventArgs e)
    {
        try
        {
            Console.WriteLine($"HandleCanvasMouseDown - Mode: {mode}, PrintAreaSelection: {isPrintAreaSelection}");

            if (isPrintAreaSelection)
            {
                isSelecting = true;
                selectionStart = (e.OffsetX, e.OffsetY);
                Console.WriteLine($"Print area selection started at ({e.OffsetX}, {e.OffsetY})");
            }
            else if (mode == EditorMode.Select && !e.ShiftKey && !e.CtrlKey)
            {
                isSelecting = true;
                selectionStart = (e.OffsetX, e.OffsetY);
                Console.WriteLine($"Selection started at ({e.OffsetX}, {e.OffsetY})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION in HandleCanvasMouseDown: {ex.Message}");
        }
    }

    private void HandleCanvasMouseMove(MouseEventArgs e)
    {
        currentMousePosition = (e.OffsetX, e.OffsetY);
        lastMouseX = e.OffsetX;
        lastMouseY = e.OffsetY;
        
        svgMouseX = e.OffsetX;
        svgMouseY = e.OffsetY;

        if (isSelecting && selectionStart.HasValue)
        {
            StateHasChanged();
        }
        else if (resizingNodeId != null)
        {
            var node = nodes.FirstOrDefault(n => n.Id == resizingNodeId);
            if (node != null)
            {
                double newWidth = Math.Max(40, e.OffsetX - node.X);
                double newHeight = Math.Max(30, e.OffsetY - node.Y);
                
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
                    X = e.OffsetX,
                    Y = e.OffsetY
                };
                UpdateEdgePath(edge);
                StateHasChanged();
            }
        }
    }

    private void HandleCanvasMouseUp(MouseEventArgs e)
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

                foreach (var node in nodes)
                {
                    if (node.X >= rect.X && node.X <= rect.X + rect.Width &&
                        node.Y >= rect.Y && node.Y <= rect.Y + rect.Height)
                    {
                        selectedNodes.Add(node.Id);
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
            
            if (mode == EditorMode.Select)
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

                    var newNode = new Node
                    {
                        Id = nextId++,
                        X = nodeX,
                        Y = nodeY,
                        Width = 120,
                        Height = 60,
                        Text = $"Node {nodes.Count + 1}",
                        Shape = selectedShape
                    };

                    nodes.Add(newNode);
                    Console.WriteLine($"Array node {i + 1} created at ({nodeX}, {nodeY})");
                }
            }
            else
            {
                var newNode = new Node
                {
                    Id = nextId++,
                    X = clickX - 60,
                    Y = clickY - 30,
                    Width = 120,
                    Height = 60,
                    Text = $"Node {nodes.Count + 1}",
                    Shape = selectedShape
                };

                Console.WriteLine($"Node created with ID: {newNode.Id}");
                nodes.Add(newNode);
            }

            Console.WriteLine($"Total nodes after placement: {nodes.Count}");
            StateHasChanged();

            Console.WriteLine("=== HandleCanvasClick END ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine("!!! EXCEPTION !!!");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Type: {ex.GetType().Name}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
        }
    }

    private void HandleNodeClick(int nodeId, MouseEventArgs e)
    {
        if (mode != EditorMode.Select) return;

        if (chainMode)
        {
            selectedEdges.Clear();
            
            if (lastChainedNodeId.HasValue && lastChainedNodeId.Value != nodeId)
            {
                UndoService.SaveState(nodes, edges, edgeLabels);

                var fromNode = nodes.FirstOrDefault(n => n.Id == lastChainedNodeId.Value);
                var toNode = nodes.FirstOrDefault(n => n.Id == nodeId);

                if (fromNode != null && toNode != null)
                {
                    var (fromConn, toConn) = GeometryService.GetOptimalConnectionPoints(fromNode, toNode);

                    var newEdge = new Edge
                    {
                        Id = nextEdgeId++,
                        From = lastChainedNodeId.Value,
                        To = nodeId,
                        FromConnection = fromConn,
                        ToConnection = toConn,
                        IsOrthogonal = useOrthoPlacement,
                        StrokeWidth = defaultStrokeWidth,
                        StrokeColor = defaultStrokeColor,
                        StrokeDashArray = defaultStrokeDashArray,
                        IsDoubleLine = defaultIsDoubleLine
                    };

                    newEdge.PathData = PathService.GetEdgePath(newEdge, nodes);
                    edges.Add(newEdge);
                }
            }
            
            lastChainedNodeId = nodeId;
            selectedNodes.Clear();
            selectedNodes.Add(nodeId);
            return;
        }

        if (pendingConnectionNodeId.HasValue && pendingConnection != null)
        {
            if (pendingConnectionNodeId.Value == nodeId)
            {
                pendingConnectionNodeId = null;
                pendingConnection = null;
                return;
            }

            UndoService.SaveState(nodes, edges, edgeLabels);

            var node = nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null) return;
            
            var mousePos = GetLastMousePosition();
            var toConnection = GeometryService.FindClosestConnectionPoint(node, mousePos.X, mousePos.Y);

            var newEdge = new Edge
            {
                Id = nextEdgeId++,
                From = pendingConnectionNodeId.Value,
                To = nodeId,
                FromConnection = new ConnectionPoint
                {
                    Side = pendingConnection.Side,
                    Position = pendingConnection.Position
                },
                ToConnection = toConnection,
                IsOrthogonal = useOrthoPlacement,
                StrokeWidth = defaultStrokeWidth,
                StrokeColor = defaultStrokeColor,
                StrokeDashArray = defaultStrokeDashArray,
                IsDoubleLine = defaultIsDoubleLine
            };

            newEdge.PathData = PathService.GetEdgePath(newEdge, nodes);

            edges.Add(newEdge);
            pendingConnectionNodeId = null;
            pendingConnection = null;
            selectedNodes.Clear();
            return;
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

            var newEdge = new Edge
            {
                Id = nextEdgeId++,
                From = selectedNodes[0],
                To = nodeId,
                FromConnection = fromConn,
                ToConnection = toConn,
                IsOrthogonal = useOrthoPlacement,
                StrokeWidth = defaultStrokeWidth,
                StrokeColor = defaultStrokeColor,
                StrokeDashArray = defaultStrokeDashArray,
                IsDoubleLine = defaultIsDoubleLine
            };

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
        if (mode != EditorMode.Select) return;
        if (resizingNodeId != null) return;

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
        if (mode != EditorMode.Select) return;

        if (pendingConnectionNodeId.HasValue && pendingConnection != null)
        {
            if (pendingConnectionNodeId.Value == nodeId)
            {
                pendingConnectionNodeId = null;
                pendingConnection = null;
                return;
            }

            UndoService.SaveState(nodes, edges, edgeLabels);

            var newEdge = new Edge
            {
                Id = nextEdgeId++,
                From = pendingConnectionNodeId.Value,
                To = nodeId,
                FromConnection = new ConnectionPoint
                {
                    Side = pendingConnection.Side,
                    Position = pendingConnection.Position
                },
                ToConnection = new ConnectionPoint
                {
                    Side = side,
                    Position = position
                },
                IsOrthogonal = useOrthoPlacement,
                StrokeWidth = defaultStrokeWidth,
                StrokeColor = defaultStrokeColor,
                StrokeDashArray = defaultStrokeDashArray,
                IsDoubleLine = defaultIsDoubleLine
            };

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
                defaultStrokeWidth = edge.StrokeWidth ?? 2;
                defaultStrokeColor = edge.StrokeColor ?? "#374151";
                defaultStrokeDashArray = edge.StrokeDashArray ?? "";
                defaultIsDoubleLine = edge.IsDoubleLine;
            }
            
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
