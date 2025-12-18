using dfd2wasm.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace dfd2wasm.Pages;

public partial class DFDEditor
{
    #region Multi-Connect Mode

    // Handle connection point click in multi-connect mode
    // Returns true if handled, false if should use normal behavior
    private bool HandleMultiConnectClick(int nodeId, string side, int position)
    {
        if (!multiConnectMode) return false;

        var connection = new ConnectionPoint { Side = side, Position = position };

        if (multiConnectSourceNode == null)
        {
            // First click - set source
            oneToNSourceNode = nodeId;
            oneToNSourcePoint = connection;
            
            // Get coordinates for visual feedback
            var node = nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                oneToNSourceCoords = GeometryService.GetConnectionPointCoordinates(node, side, position);
            }
            
            StateHasChanged();
            return true;
        }
        else
        {
            // Subsequent clicks - create edge to this destination
            if (nodeId != oneToNSourceNode) // Don't connect to self
            {
                CreateEdgeFromMultiConnect(nodeId, connection);
            }
            return true;
        }
    }

    private void CreateEdgeFromMultiConnect(int destNodeId, ConnectionPoint destConnection)
    {
        if (oneToNSourceNode == null || multiConnectSourcePoint == null) return;

        UndoService.SaveState(nodes, edges, edgeLabels);

        var newEdge = new Edge
        {
            Id = nextEdgeId++,
            From = oneToNSourceNode.Value,
            To = destNodeId,
            FromConnection = oneToNSourcePoint,
            ToConnection = destConnection,
            StrokeWidth = defaultStrokeWidth,
            StrokeColor = defaultStrokeColor,
            StrokeDashArray = defaultStrokeDashArray,
            IsDoubleLine = defaultIsDoubleLine,
            Style = defaultEdgeStyle,
            IsOrthogonal = defaultEdgeStyle == EdgeStyle.Ortho || defaultEdgeStyle == EdgeStyle.OrthoRound,
            ArrowDirection = ArrowDirection.End // Default arrow direction
        };

        newEdge.PathData = PathService.GetEdgePath(newEdge, nodes);
        edges.Add(newEdge);
        StateHasChanged();
    }

    private void CancelMultiConnect()
    {
        ClearMultiConnectState();
        multiConnectMode = false;
        StateHasChanged();
    }

    #endregion

    #region Edge Styles

    private void ApplyEdgeStylesToSelected()
    {
        if (selectedEdges.Count == 0)
        {
            return;
        }

        foreach (var edgeId in selectedEdges)
        {
            var edge = edges.FirstOrDefault(e => e.Id == edgeId);
            if (edge != null)
            {
                edge.StrokeWidth = editStrokeWidth;
                edge.StrokeColor = editStrokeColor;
                edge.StrokeDashArray = editStrokeDashArray;
                edge.IsDoubleLine = editIsDoubleLine;
                edge.Style = editEdgeStyle;
                edge.IsOrthogonal = editEdgeStyle == EdgeStyle.Ortho || editEdgeStyle == EdgeStyle.OrthoRound;
                edge.ArrowDirection = editArrowDirection;
                
                edge.PathData = PathService.GetEdgePath(edge, nodes);
            }
        }

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
            edge.Style = editEdgeStyle;
            edge.IsOrthogonal = editEdgeStyle == EdgeStyle.Ortho || editEdgeStyle == EdgeStyle.OrthoRound;
            edge.ArrowDirection = editArrowDirection;
            
            edge.PathData = PathService.GetEdgePath(edge, nodes);
        }

        showEdgeStylePanel = false;
        StateHasChanged();
    }

    // Called when connector style dropdown changes - updates immediately
    private void OnEdgeStyleChanged(ChangeEventArgs e)
    {
        if (Enum.TryParse<EdgeStyle>(e.Value?.ToString(), out var newStyle))
        {
            editEdgeStyle = newStyle;
            ApplyEdgeStylesToSelected();
        }
    }

    // Called when any edge property changes - updates immediately
    private void OnEdgePropertyChanged()
    {
        ApplyEdgeStylesToSelected();
    }

    #endregion

    #region Waypoints

    // Double-click on midpoint handle to add a waypoint there
    private void AddWaypointAtMidpoint(int edgeId)
    {
        var edge = edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge == null) return;

        UndoService.SaveState(nodes, edges, edgeLabels);

        var mid = GetEdgeMidpoint(edge);
        edge.Waypoints.Add(new Waypoint { X = mid.X, Y = mid.Y });
        edge.PathData = PathService.GetEdgePath(edge, nodes);
        StateHasChanged();
    }

    // Double-click on edge to add a waypoint
    private void AddWaypointToEdge(int edgeId, MouseEventArgs e)
    {
        var edge = edges.FirstOrDefault(ed => ed.Id == edgeId);
        if (edge == null) return;

        UndoService.SaveState(nodes, edges, edgeLabels);

        // Get click position (adjusted for any transforms)
        double x = e.OffsetX;
        double y = e.OffsetY;

        // Create new waypoint at click position
        var newWaypoint = new Waypoint { X = x, Y = y };

        // Insert waypoint in the right position (find nearest segment)
        if (edge.Waypoints.Count == 0)
        {
            edge.Waypoints.Add(newWaypoint);
        }
        else
        {
            // Find best insertion point based on distance to existing waypoints
            int insertIndex = FindBestWaypointInsertIndex(edge, x, y);
            edge.Waypoints.Insert(insertIndex, newWaypoint);
        }

        // Recalculate path
        edge.PathData = PathService.GetEdgePath(edge, nodes);
        StateHasChanged();
    }

    private int FindBestWaypointInsertIndex(Edge edge, double x, double y)
    {
        // Simple approach: find where this point fits best in the sequence
        var fromNode = nodes.FirstOrDefault(n => n.Id == edge.From);
        var toNode = nodes.FirstOrDefault(n => n.Id == edge.To);
        
        if (fromNode == null || toNode == null)
            return 0;

        double fromX = fromNode.X + fromNode.Width / 2;
        double fromY = fromNode.Y + fromNode.Height / 2;
        double toX = toNode.X + toNode.Width / 2;
        double toY = toNode.Y + toNode.Height / 2;

        // Build list of all points
        var points = new List<(double x, double y)>();
        points.Add((fromX, fromY));
        foreach (var wp in edge.Waypoints)
        {
            points.Add((wp.X, wp.Y));
        }
        points.Add((toX, toY));

        // Find which segment the new point is closest to
        double minDist = double.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i < points.Count - 1; i++)
        {
            double dist = DistanceToSegment(x, y, points[i].x, points[i].y, points[i + 1].x, points[i + 1].y);
            if (dist < minDist)
            {
                minDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private double DistanceToSegment(double px, double py, double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        double lengthSquared = dx * dx + dy * dy;

        if (lengthSquared == 0)
            return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));

        double t = Math.Max(0, Math.Min(1, ((px - x1) * dx + (py - y1) * dy) / lengthSquared));
        double projX = x1 + t * dx;
        double projY = y1 + t * dy;

        return Math.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
    }

    // Right-click on waypoint to delete it
    private void DeleteWaypoint(int edgeId, Waypoint waypoint)
    {
        var edge = edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge == null) return;

        UndoService.SaveState(nodes, edges, edgeLabels);

        edge.Waypoints.Remove(waypoint);
        edge.PathData = PathService.GetEdgePath(edge, nodes);
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
