using dfd2wasm.Models;

namespace dfd2wasm.Services
{
    public class PathService
    {
        private readonly GeometryService _geometryService;
        private const double OBSTACLE_MARGIN = 15; // Clearance from nodes

        public PathService(GeometryService geometryService)
        {
            _geometryService = geometryService;
        }

        public string GetEdgePath(Edge edge, List<Node> nodes)
        {
            var fromNode = nodes.FirstOrDefault(n => n.Id == edge.From);
            var toNode = nodes.FirstOrDefault(n => n.Id == edge.To);

            if (fromNode == null || toNode == null) return "";

            var (fromX, fromY) = _geometryService.GetConnectionPointCoordinates(
                fromNode, edge.FromConnection.Side, edge.FromConnection.Position);
            var (toX, toY) = _geometryService.GetConnectionPointCoordinates(
                toNode, edge.ToConnection.Side, edge.ToConnection.Position);

            // If edge has waypoints, render them (manual path)
            if (edge.Waypoints.Count > 0)
            {
                var path = $"M {fromX} {fromY}";
                foreach (var wp in edge.Waypoints)
                {
                    path += $" L {wp.X} {wp.Y}";
                }
                path += $" L {toX} {toY}";
                return path;
            }

            // Get obstacles (all nodes except source and target)
            var obstacles = nodes.Where(n => n.Id != edge.From && n.Id != edge.To).ToList();

            // Use EdgeStyle if set, otherwise fall back to IsOrthogonal for compatibility
            var style = edge.Style;
            if (style == EdgeStyle.Direct && edge.IsOrthogonal)
            {
                style = EdgeStyle.Ortho;
            }

            return style switch
            {
                EdgeStyle.Direct => GetDirectPath(fromX, fromY, toX, toY),
                EdgeStyle.Ortho => GetOrthogonalPathWithObstacles(fromX, fromY, toX, toY, 
                    edge.FromConnection.Side, edge.ToConnection.Side, obstacles),
                EdgeStyle.OrthoRound => GetOrthoRoundPathWithObstacles(fromX, fromY, toX, toY, 
                    edge.FromConnection.Side, edge.ToConnection.Side, obstacles),
                EdgeStyle.Bezier => GetBezierPath(fromX, fromY, toX, toY, 
                    edge.FromConnection.Side, edge.ToConnection.Side),
                EdgeStyle.Arc => GetArcPath(fromX, fromY, toX, toY,
                    edge.FromConnection.Side, edge.ToConnection.Side),
                EdgeStyle.Circuit => GetCircuitPath(fromX, fromY, toX, toY,
                    edge.FromConnection.Side, edge.ToConnection.Side, obstacles),
                EdgeStyle.Stylized => GetStylizedPath(fromX, fromY, toX, toY, 
                    edge.FromConnection.Side, edge.ToConnection.Side),
                _ => GetDirectPath(fromX, fromY, toX, toY)
            };
        }

        // ============================================
        // DIRECT - Straight line
        // ============================================
        private string GetDirectPath(double fromX, double fromY, double toX, double toY)
        {
            return $"M {fromX} {fromY} L {toX} {toY}";
        }

        // ============================================
        // ORTHOGONAL WITH OBSTACLE AVOIDANCE
        // ============================================
        private string GetOrthogonalPathWithObstacles(double fromX, double fromY, double toX, double toY, 
            string fromSide, string toSide, List<Node> obstacles)
        {
            var points = GetOrthogonalPoints(fromX, fromY, toX, toY, fromSide, toSide, 25);
            
            // Adjust points to avoid obstacles
            points = AdjustPathForObstacles(points, obstacles);
            
            var path = $"M {points[0].x} {points[0].y}";
            for (int i = 1; i < points.Count; i++)
            {
                path += $" L {points[i].x} {points[i].y}";
            }
            return path;
        }

        private string GetOrthoRoundPathWithObstacles(double fromX, double fromY, double toX, double toY, 
            string fromSide, string toSide, List<Node> obstacles)
        {
            var points = GetOrthogonalPoints(fromX, fromY, toX, toY, fromSide, toSide, 25);
            
            // Adjust points to avoid obstacles
            points = AdjustPathForObstacles(points, obstacles);
            
            if (points.Count < 3)
            {
                return $"M {fromX} {fromY} L {toX} {toY}";
            }

            double radius = 10;
            var path = $"M {points[0].x} {points[0].y}";

            for (int i = 1; i < points.Count - 1; i++)
            {
                var prev = points[i - 1];
                var curr = points[i];
                var next = points[i + 1];

                double distPrev = Math.Sqrt(Math.Pow(curr.x - prev.x, 2) + Math.Pow(curr.y - prev.y, 2));
                double distNext = Math.Sqrt(Math.Pow(next.x - curr.x, 2) + Math.Pow(next.y - curr.y, 2));
                
                double maxRadius = Math.Min(distPrev, distNext) / 2;
                double r = Math.Min(radius, maxRadius);

                if (r < 2)
                {
                    path += $" L {curr.x} {curr.y}";
                    continue;
                }

                double dx1 = (curr.x - prev.x) / distPrev;
                double dy1 = (curr.y - prev.y) / distPrev;
                double dx2 = (next.x - curr.x) / distNext;
                double dy2 = (next.y - curr.y) / distNext;

                double arcStartX = curr.x - dx1 * r;
                double arcStartY = curr.y - dy1 * r;
                double arcEndX = curr.x + dx2 * r;
                double arcEndY = curr.y + dy2 * r;

                path += $" L {arcStartX} {arcStartY}";
                path += $" Q {curr.x} {curr.y} {arcEndX} {arcEndY}";
            }

            var last = points[points.Count - 1];
            path += $" L {last.x} {last.y}";

            return path;
        }

        /// <summary>
        /// Adjust path points to avoid obstacles
        /// </summary>
        private List<(double x, double y)> AdjustPathForObstacles(List<(double x, double y)> points, List<Node> obstacles)
        {
            if (obstacles.Count == 0 || points.Count < 2)
                return points;

            var result = new List<(double x, double y)>();
            result.Add(points[0]);

            for (int i = 0; i < points.Count - 1; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];

                // Check if this segment passes through any obstacle
                var crossedNodes = GetCrossedNodes(p1.x, p1.y, p2.x, p2.y, obstacles);

                if (crossedNodes.Count == 0)
                {
                    // No obstacles - keep the endpoint
                    if (i < points.Count - 1)
                        result.Add(p2);
                }
                else
                {
                    // Route around each obstacle
                    var current = p1;
                    foreach (var node in crossedNodes)
                    {
                        var detour = GetDetourPoints(current, p2, node);
                        foreach (var dp in detour)
                        {
                            result.Add(dp);
                        }
                        current = detour.Count > 0 ? detour[detour.Count - 1] : current;
                    }
                    
                    // Add endpoint if not the last segment
                    if (i < points.Count - 2)
                        result.Add(p2);
                }
            }

            // Always add the final point
            result.Add(points[points.Count - 1]);

            return result;
        }

        /// <summary>
        /// Get nodes that a line segment crosses
        /// </summary>
        private List<Node> GetCrossedNodes(double x1, double y1, double x2, double y2, List<Node> obstacles)
        {
            var crossed = new List<Node>();

            foreach (var node in obstacles)
            {
                // Expand node bounds by margin
                double left = node.X - OBSTACLE_MARGIN;
                double right = node.X + node.Width + OBSTACLE_MARGIN;
                double top = node.Y - OBSTACLE_MARGIN;
                double bottom = node.Y + node.Height + OBSTACLE_MARGIN;

                if (LineIntersectsRect(x1, y1, x2, y2, left, top, right, bottom))
                {
                    crossed.Add(node);
                }
            }

            // Sort by distance from start
            crossed.Sort((a, b) =>
            {
                double distA = Math.Abs(a.X + a.Width / 2 - x1) + Math.Abs(a.Y + a.Height / 2 - y1);
                double distB = Math.Abs(b.X + b.Width / 2 - x1) + Math.Abs(b.Y + b.Height / 2 - y1);
                return distA.CompareTo(distB);
            });

            return crossed;
        }

        /// <summary>
        /// Check if a line segment intersects a rectangle
        /// </summary>
        private bool LineIntersectsRect(double x1, double y1, double x2, double y2,
            double left, double top, double right, double bottom)
        {
            // Quick rejection
            if ((x1 < left && x2 < left) || (x1 > right && x2 > right)) return false;
            if ((y1 < top && y2 < top) || (y1 > bottom && y2 > bottom)) return false;

            // Check if either point is inside
            if (x1 >= left && x1 <= right && y1 >= top && y1 <= bottom) return true;
            if (x2 >= left && x2 <= right && y2 >= top && y2 <= bottom) return true;

            // Check line intersection with rectangle edges
            return LineIntersectsLine(x1, y1, x2, y2, left, top, right, top) ||
                   LineIntersectsLine(x1, y1, x2, y2, left, bottom, right, bottom) ||
                   LineIntersectsLine(x1, y1, x2, y2, left, top, left, bottom) ||
                   LineIntersectsLine(x1, y1, x2, y2, right, top, right, bottom);
        }

        private bool LineIntersectsLine(double x1, double y1, double x2, double y2,
            double x3, double y3, double x4, double y4)
        {
            double denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
            if (Math.Abs(denom) < 0.0001) return false;

            double ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denom;
            double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / denom;

            return ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1;
        }

        /// <summary>
        /// Generate detour points to route around an obstacle (orthogonally)
        /// </summary>
        private List<(double x, double y)> GetDetourPoints((double x, double y) from, (double x, double y) to, Node obstacle)
        {
            var detour = new List<(double x, double y)>();

            double left = obstacle.X - OBSTACLE_MARGIN;
            double right = obstacle.X + obstacle.Width + OBSTACLE_MARGIN;
            double top = obstacle.Y - OBSTACLE_MARGIN;
            double bottom = obstacle.Y + obstacle.Height + OBSTACLE_MARGIN;

            // Determine if we're moving primarily horizontal or vertical
            bool isHorizontal = Math.Abs(to.x - from.x) > Math.Abs(to.y - from.y);

            if (isHorizontal)
            {
                // Moving horizontally - go above or below
                double nodeCenter = obstacle.Y + obstacle.Height / 2;
                double pathY = (from.y + to.y) / 2;

                if (pathY < nodeCenter)
                {
                    // Route above
                    detour.Add((from.x, top));
                    detour.Add((to.x, top));
                }
                else
                {
                    // Route below
                    detour.Add((from.x, bottom));
                    detour.Add((to.x, bottom));
                }
            }
            else
            {
                // Moving vertically - go left or right
                double nodeCenter = obstacle.X + obstacle.Width / 2;
                double pathX = (from.x + to.x) / 2;

                if (pathX < nodeCenter)
                {
                    // Route left
                    detour.Add((left, from.y));
                    detour.Add((left, to.y));
                }
                else
                {
                    // Route right
                    detour.Add((right, from.y));
                    detour.Add((right, to.y));
                }
            }

            return detour;
        }

        // ============================================
        // ORTHOGONAL - Basic path points (no obstacles)
        // ============================================
        private List<(double x, double y)> GetOrthogonalPoints(double fromX, double fromY, 
            double toX, double toY, string fromSide, string toSide, double offset)
        {
            var points = new List<(double x, double y)>();
            points.Add((fromX, fromY));

            bool fromHorizontal = fromSide == "left" || fromSide == "right";
            bool toHorizontal = toSide == "left" || toSide == "right";

            double entryX = fromX, entryY = fromY;
            switch (fromSide)
            {
                case "top": entryY = fromY - offset; break;
                case "bottom": entryY = fromY + offset; break;
                case "left": entryX = fromX - offset; break;
                case "right": entryX = fromX + offset; break;
            }

            double exitX = toX, exitY = toY;
            switch (toSide)
            {
                case "top": exitY = toY - offset; break;
                case "bottom": exitY = toY + offset; break;
                case "left": exitX = toX - offset; break;
                case "right": exitX = toX + offset; break;
            }

            double midX = (entryX + exitX) / 2;
            double midY = (entryY + exitY) / 2;

            points.Add((entryX, entryY));

            if (fromHorizontal && toHorizontal)
            {
                points.Add((midX, entryY));
                points.Add((midX, exitY));
            }
            else if (!fromHorizontal && !toHorizontal)
            {
                points.Add((entryX, midY));
                points.Add((exitX, midY));
            }
            else if (fromHorizontal && !toHorizontal)
            {
                points.Add((exitX, entryY));
            }
            else
            {
                points.Add((entryX, exitY));
            }

            points.Add((exitX, exitY));
            points.Add((toX, toY));
            
            return points;
        }

        // ============================================
        // BEZIER - Smooth S-curve
        // ============================================
        private string GetBezierPath(double fromX, double fromY, double toX, double toY, 
            string fromSide, string toSide)
        {
            double distance = Math.Sqrt(Math.Pow(toX - fromX, 2) + Math.Pow(toY - fromY, 2));
            double controlOffset = Math.Max(50, distance * 0.4);

            double cp1X = fromX, cp1Y = fromY;
            switch (fromSide)
            {
                case "top": cp1Y = fromY - controlOffset; break;
                case "bottom": cp1Y = fromY + controlOffset; break;
                case "left": cp1X = fromX - controlOffset; break;
                case "right": cp1X = fromX + controlOffset; break;
            }

            double cp2X = toX, cp2Y = toY;
            switch (toSide)
            {
                case "top": cp2Y = toY - controlOffset; break;
                case "bottom": cp2Y = toY + controlOffset; break;
                case "left": cp2X = toX - controlOffset; break;
                case "right": cp2X = toX + controlOffset; break;
            }

            return $"M {fromX} {fromY} C {cp1X} {cp1Y}, {cp2X} {cp2Y}, {toX} {toY}";
        }

        // ============================================
        // ARC - Single curved arc
        // ============================================
        private string GetArcPath(double fromX, double fromY, double toX, double toY,
            string fromSide, string toSide)
        {
            double dx = toX - fromX;
            double dy = toY - fromY;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            bool isAligned = IsConnectionAligned(fromSide, toSide, dx, dy);
            
            if (isAligned || distance < 50)
            {
                double controlOffset = distance * 0.15;
                double perpX = -dy / distance * controlOffset;
                double perpY = dx / distance * controlOffset;
                
                double midX = (fromX + toX) / 2 + perpX;
                double midY = (fromY + toY) / 2 + perpY;
                
                return $"M {fromX} {fromY} Q {midX} {midY} {toX} {toY}";
            }
            
            double curveStrength = Math.Min(distance * 0.3, 80);
            double perpDx = -dy / distance;
            double perpDy = dx / distance;
            int curveDir = GetArcDirection(fromSide, toSide, dx, dy);
            
            double ctrlX = (fromX + toX) / 2 + perpDx * curveStrength * curveDir;
            double ctrlY = (fromY + toY) / 2 + perpDy * curveStrength * curveDir;
            
            return $"M {fromX} {fromY} Q {ctrlX} {ctrlY} {toX} {toY}";
        }

        private bool IsConnectionAligned(string fromSide, string toSide, double dx, double dy)
        {
            if (fromSide == "right" && toSide == "left" && dx > 0) return true;
            if (fromSide == "left" && toSide == "right" && dx < 0) return true;
            if (fromSide == "bottom" && toSide == "top" && dy > 0) return true;
            if (fromSide == "top" && toSide == "bottom" && dy < 0) return true;
            
            double angle = Math.Atan2(Math.Abs(dy), Math.Abs(dx)) * 180 / Math.PI;
            if (angle > 30 && angle < 60) return true;
            
            return false;
        }

        private int GetArcDirection(string fromSide, string toSide, double dx, double dy)
        {
            return fromSide switch
            {
                "right" => dy >= 0 ? 1 : -1,
                "left" => dy >= 0 ? -1 : 1,
                "bottom" => dx >= 0 ? -1 : 1,
                "top" => dx >= 0 ? 1 : -1,
                _ => 1
            };
        }

        // ============================================
        // CIRCUIT / BUS - Orthogonal bus routing with semicircular jumps
        // ============================================
        private string GetCircuitPath(double fromX, double fromY, double toX, double toY,
            string fromSide, string toSide, List<Node> obstacles)
        {
            // Determine bus Y coordinate (snap to grid to create clean horizontal buses)
            int grid = 40;
            double busY = Math.Round(((fromY + toY) / 2) / grid) * grid;

            var path = $"M {fromX} {fromY} L {fromX} {busY} ";

            // Horizontal travel from fromX to toX along busY, inserting semicircular jumps
            double x1 = Math.Min(fromX, toX);
            double x2 = Math.Max(fromX, toX);

            // Find obstacles that intersect the horizontal band at busY
            var crossed = new List<(double cx, Node node)>();
            foreach (var node in obstacles)
            {
                double left = node.X - OBSTACLE_MARGIN;
                double right = node.X + node.Width + OBSTACLE_MARGIN;
                double top = node.Y - OBSTACLE_MARGIN;
                double bottom = node.Y + node.Height + OBSTACLE_MARGIN;

                if (busY >= top && busY <= bottom && right >= x1 && left <= x2)
                {
                    var cx = Math.Min(Math.Max((node.X + node.Width / 2), x1), x2);
                    crossed.Add((cx, node));
                }
            }

            // Sort crossings left-to-right
            crossed.Sort((a, b) => a.cx.CompareTo(b.cx));

            double curX = fromX;
            int dir = toX >= fromX ? 1 : -1;

            foreach (var (cx, node) in crossed)
            {
                // leave small margin around crossing
                double r = Math.Min(node.Width, node.Height) / 2 + 8;
                double segX1 = cx - r * dir;
                double segX2 = cx + r * dir;

                // Line to start of jump
                path += $"L {segX1} {busY} ";

                // Choose jump direction (up or down) based on bus vs node center
                double nodeCenterY = node.Y + node.Height / 2;
                double jumpHeight = Math.Max(12, r * 0.9);
                double controlY = busY < nodeCenterY ? busY - jumpHeight : busY + jumpHeight;

                // Quadratic arch over obstacle: from segX1 -> arch peak -> segX2
                double peakX = cx;
                path += $"Q {peakX} {controlY} {segX2} {busY} ";

                curX = segX2;
            }

            // finish horizontal and drop to target
            path += $"L {toX} {busY} L {toX} {toY}";

            return path;
        }

        // ============================================
        // STYLIZED - Fancy with embellishments
        // ============================================
        private string GetStylizedPath(double fromX, double fromY, double toX, double toY, 
            string fromSide, string toSide)
        {
            double distance = Math.Sqrt(Math.Pow(toX - fromX, 2) + Math.Pow(toY - fromY, 2));
            double controlOffset = Math.Max(60, distance * 0.5);
            double flourishSize = 15;

            double flourishX = fromX, flourishY = fromY;
            double cp1X = fromX, cp1Y = fromY;
            
            switch (fromSide)
            {
                case "top":
                    flourishY = fromY - flourishSize;
                    flourishX = fromX + flourishSize * 0.5;
                    cp1Y = fromY - controlOffset;
                    cp1X = fromX + controlOffset * 0.3;
                    break;
                case "bottom":
                    flourishY = fromY + flourishSize;
                    flourishX = fromX + flourishSize * 0.5;
                    cp1Y = fromY + controlOffset;
                    cp1X = fromX + controlOffset * 0.3;
                    break;
                case "left":
                    flourishX = fromX - flourishSize;
                    flourishY = fromY - flourishSize * 0.5;
                    cp1X = fromX - controlOffset;
                    cp1Y = fromY - controlOffset * 0.3;
                    break;
                case "right":
                    flourishX = fromX + flourishSize;
                    flourishY = fromY - flourishSize * 0.5;
                    cp1X = fromX + controlOffset;
                    cp1Y = fromY - controlOffset * 0.3;
                    break;
            }

            double cp2X = toX, cp2Y = toY;
            switch (toSide)
            {
                case "top": cp2Y = toY - controlOffset * 0.8; break;
                case "bottom": cp2Y = toY + controlOffset * 0.8; break;
                case "left": cp2X = toX - controlOffset * 0.8; break;
                case "right": cp2X = toX + controlOffset * 0.8; break;
            }

            return $"M {fromX} {fromY} " +
                   $"Q {flourishX} {flourishY} {flourishX} {(fromY + flourishY) / 2} " +
                   $"C {cp1X} {cp1Y}, {cp2X} {cp2Y}, {toX} {toY}";
        }
    }
}
