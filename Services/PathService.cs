using dfd2wasm.Models;
namespace dfd2wasm.Services
{
    using dfd2wasm.Services;

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

            // If edge has waypoints, render them
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

            // Draw path based on mode
            if (edge.IsOrthogonal)
            {
                return GetOrthogonalPath(fromX, fromY, toX, toY, 
                    edge.FromConnection.Side, edge.ToConnection.Side);
            }
            else
            {
                return $"M {fromX} {fromY} L {toX} {toY}";
            }
        }

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
    }
}
