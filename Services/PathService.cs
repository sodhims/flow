using dfd2wasm.Models;

namespace dfd2wasm.Services
{
    public class PathService
    {
        private readonly GeometryService _geometryService;

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

            // Use EdgeStyle if set, otherwise fall back to IsOrthogonal for compatibility
            var style = edge.Style;
            if (style == EdgeStyle.Direct && edge.IsOrthogonal)
            {
                style = EdgeStyle.Ortho;
            }

            return style switch
            {
                EdgeStyle.Direct => GetDirectPath(fromX, fromY, toX, toY),
                EdgeStyle.Ortho => GetOrthogonalPath(fromX, fromY, toX, toY, 
                    edge.FromConnection.Side, edge.ToConnection.Side),
                EdgeStyle.OrthoRound => GetOrthoRoundPath(fromX, fromY, toX, toY, 
                    edge.FromConnection.Side, edge.ToConnection.Side),
                EdgeStyle.Bezier => GetBezierPath(fromX, fromY, toX, toY, 
                    edge.FromConnection.Side, edge.ToConnection.Side),
                EdgeStyle.Arc => GetArcPath(fromX, fromY, toX, toY,
                    edge.FromConnection.Side, edge.ToConnection.Side),
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
        // ORTHOGONAL - Right angles only
        // ============================================
        private string GetOrthogonalPath(double fromX, double fromY, double toX, double toY, 
            string fromSide, string toSide)
        {
            const double offset = 20;

            // Determine exit point from source
            double exitX = fromX, exitY = fromY;
            switch (fromSide)
            {
                case "top": exitY = fromY - offset; exitX = fromX; break;
                case "bottom": exitY = fromY + offset; exitX = fromX; break;
                case "left": exitX = fromX - offset; exitY = fromY; break;
                case "right": exitX = fromX + offset; exitY = fromY; break;
            }

            // Determine entry point to target
            double entryX = toX, entryY = toY;
            switch (toSide)
            {
                case "top": entryY = toY - offset; entryX = toX; break;
                case "bottom": entryY = toY + offset; entryX = toX; break;
                case "left": entryX = toX - offset; entryY = toY; break;
                case "right": entryX = toX + offset; entryY = toY; break;
            }

            var path = $"M {fromX} {fromY} L {exitX} {exitY}";

            bool fromHorizontal = (fromSide == "left" || fromSide == "right");
            bool toHorizontal = (toSide == "left" || toSide == "right");

            if (fromHorizontal && toHorizontal)
            {
                double midX = (exitX + entryX) / 2;
                path += $" L {midX} {exitY} L {midX} {entryY}";
            }
            else if (!fromHorizontal && !toHorizontal)
            {
                double midY = (exitY + entryY) / 2;
                path += $" L {exitX} {midY} L {entryX} {midY}";
            }
            else if (fromHorizontal && !toHorizontal)
            {
                path += $" L {exitX} {entryY}";
            }
            else
            {
                path += $" L {entryX} {exitY}";
            }

            path += $" L {entryX} {entryY} L {toX} {toY}";
            return path;
        }

        // ============================================
        // ORTHO ROUND - Right angles with rounded corners
        // ============================================
        private string GetOrthoRoundPath(double fromX, double fromY, double toX, double toY, 
            string fromSide, string toSide)
        {
            const double offset = 25;
            const double radius = 12; // Corner radius

            // Get the orthogonal path points first
            var points = GetOrthogonalPoints(fromX, fromY, toX, toY, fromSide, toSide, offset);
            
            if (points.Count < 2)
                return $"M {fromX} {fromY} L {toX} {toY}";

            // Build path with rounded corners
            var path = $"M {points[0].x} {points[0].y}";
            
            for (int i = 1; i < points.Count - 1; i++)
            {
                var prev = points[i - 1];
                var curr = points[i];
                var next = points[i + 1];
                
                // Calculate direction vectors
                double dx1 = curr.x - prev.x;
                double dy1 = curr.y - prev.y;
                double dx2 = next.x - curr.x;
                double dy2 = next.y - curr.y;
                
                // Normalize and get the point before the corner
                double len1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
                double len2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                
                if (len1 == 0 || len2 == 0)
                {
                    path += $" L {curr.x} {curr.y}";
                    continue;
                }
                
                // Limit radius to half the shortest segment
                double maxRadius = Math.Min(len1, len2) / 2;
                double r = Math.Min(radius, maxRadius);
                
                // Points before and after the corner
                double beforeX = curr.x - (dx1 / len1) * r;
                double beforeY = curr.y - (dy1 / len1) * r;
                double afterX = curr.x + (dx2 / len2) * r;
                double afterY = curr.y + (dy2 / len2) * r;
                
                // Line to the point before corner, then quadratic curve through corner
                path += $" L {beforeX} {beforeY}";
                path += $" Q {curr.x} {curr.y} {afterX} {afterY}";
            }
            
            // Final line to end point
            path += $" L {points[points.Count - 1].x} {points[points.Count - 1].y}";
            
            return path;
        }

        // Helper to get orthogonal path points
        private List<(double x, double y)> GetOrthogonalPoints(double fromX, double fromY, 
            double toX, double toY, string fromSide, string toSide, double offset)
        {
            var points = new List<(double x, double y)>();
            points.Add((fromX, fromY));

            // Determine exit point from source
            double exitX = fromX, exitY = fromY;
            switch (fromSide)
            {
                case "top": exitY = fromY - offset; break;
                case "bottom": exitY = fromY + offset; break;
                case "left": exitX = fromX - offset; break;
                case "right": exitX = fromX + offset; break;
            }
            points.Add((exitX, exitY));

            // Determine entry point to target
            double entryX = toX, entryY = toY;
            switch (toSide)
            {
                case "top": entryY = toY - offset; break;
                case "bottom": entryY = toY + offset; break;
                case "left": entryX = toX - offset; break;
                case "right": entryX = toX + offset; break;
            }

            bool fromHorizontal = (fromSide == "left" || fromSide == "right");
            bool toHorizontal = (toSide == "left" || toSide == "right");

            if (fromHorizontal && toHorizontal)
            {
                double midX = (exitX + entryX) / 2;
                points.Add((midX, exitY));
                points.Add((midX, entryY));
            }
            else if (!fromHorizontal && !toHorizontal)
            {
                double midY = (exitY + entryY) / 2;
                points.Add((exitX, midY));
                points.Add((entryX, midY));
            }
            else if (fromHorizontal && !toHorizontal)
            {
                points.Add((entryX, exitY));
            }
            else
            {
                points.Add((exitX, entryY));
            }

            points.Add((entryX, entryY));
            points.Add((toX, toY));
            
            return points;
        }

        // ============================================
        // BEZIER - Smooth S-curve
        // ============================================
        private string GetBezierPath(double fromX, double fromY, double toX, double toY, 
            string fromSide, string toSide)
        {
            // Calculate control points based on connection sides
            double distance = Math.Sqrt(Math.Pow(toX - fromX, 2) + Math.Pow(toY - fromY, 2));
            double controlOffset = Math.Max(50, distance * 0.4);

            // Control point 1 - extends from source in direction of connection
            double cp1X = fromX, cp1Y = fromY;
            switch (fromSide)
            {
                case "top": cp1Y = fromY - controlOffset; break;
                case "bottom": cp1Y = fromY + controlOffset; break;
                case "left": cp1X = fromX - controlOffset; break;
                case "right": cp1X = fromX + controlOffset; break;
            }

            // Control point 2 - extends from target in direction of connection
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
            
            // Calculate the angle of the direct line
            double angle = Math.Atan2(dy, dx);
            
            // Determine if connections are roughly aligned (would make a straight-ish line)
            bool isAligned = IsConnectionAligned(fromSide, toSide, dx, dy);
            
            if (isAligned || distance < 50)
            {
                // For aligned connections or short distances, use a subtle curve
                double controlOffset = distance * 0.15;
                double perpX = -dy / distance * controlOffset;
                double perpY = dx / distance * controlOffset;
                
                double midX = (fromX + toX) / 2 + perpX;
                double midY = (fromY + toY) / 2 + perpY;
                
                return $"M {fromX} {fromY} Q {midX} {midY} {toX} {toY}";
            }
            
            // For non-aligned connections, create a more pronounced curve
            double curveStrength = Math.Min(distance * 0.3, 80);
            
            // Control point perpendicular to the line
            double perpDx = -dy / distance;
            double perpDy = dx / distance;
            
            // Determine curve direction based on connection sides
            int curveDir = GetArcDirection(fromSide, toSide, dx, dy);
            
            double ctrlX = (fromX + toX) / 2 + perpDx * curveStrength * curveDir;
            double ctrlY = (fromY + toY) / 2 + perpDy * curveStrength * curveDir;
            
            return $"M {fromX} {fromY} Q {ctrlX} {ctrlY} {toX} {toY}";
        }

        private bool IsConnectionAligned(string fromSide, string toSide, double dx, double dy)
        {
            // Check if the connection sides suggest a natural straight-ish path
            if (fromSide == "right" && toSide == "left" && dx > 0) return true;
            if (fromSide == "left" && toSide == "right" && dx < 0) return true;
            if (fromSide == "bottom" && toSide == "top" && dy > 0) return true;
            if (fromSide == "top" && toSide == "bottom" && dy < 0) return true;
            
            // Also check for diagonal alignment
            double angle = Math.Atan2(Math.Abs(dy), Math.Abs(dx)) * 180 / Math.PI;
            if (angle > 30 && angle < 60) return true; // Roughly 45 degrees
            
            return false;
        }

        private int GetArcDirection(string fromSide, string toSide, double dx, double dy)
        {
            // Determine which way to curve based on layout
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
        // STYLIZED - Fancy with embellishments
        // ============================================
        private string GetStylizedPath(double fromX, double fromY, double toX, double toY, 
            string fromSide, string toSide)
        {
            // Stylized: Start with a small loop/flourish, then smooth curve
            double distance = Math.Sqrt(Math.Pow(toX - fromX, 2) + Math.Pow(toY - fromY, 2));
            double controlOffset = Math.Max(60, distance * 0.5);
            double flourishSize = 15;

            // Calculate flourish point (small decorative curve at start)
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

            // Control point 2 for target
            double cp2X = toX, cp2Y = toY;
            switch (toSide)
            {
                case "top": cp2Y = toY - controlOffset * 0.8; break;
                case "bottom": cp2Y = toY + controlOffset * 0.8; break;
                case "left": cp2X = toX - controlOffset * 0.8; break;
                case "right": cp2X = toX + controlOffset * 0.8; break;
            }

            // Build path: start → flourish → smooth curve to end
            return $"M {fromX} {fromY} " +
                   $"Q {flourishX} {flourishY} {flourishX} {(fromY + flourishY) / 2} " +
                   $"C {cp1X} {cp1Y}, {cp2X} {cp2Y}, {toX} {toY}";
        }
    }
}
