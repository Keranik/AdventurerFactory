using ForgeFlow.Core.Entities;
using ForgeFlow.Core.Proto;
using ForgeFlow.Core.Proto.Logic;

namespace ForgeFlow.Core.Utilities;

public sealed class Pathfinding
{
    public List<ulong>? FindPath(
        GridPosRPG start,
        GridPosRPG goal,
        Dictionary<GridPosRPG, ulong> pathPositionIndex,
        Dictionary<ulong, PathSegmentLogic> pathSegments)
    {
        if (!pathPositionIndex.TryGetValue(start, out var startSegmentId))
            return null;

        var visited = new HashSet<ulong>();
        var path = new List<ulong>();
        var queue = new Queue<(ulong segmentId, List<ulong> currentPath)>();
        queue.Enqueue((startSegmentId, new List<ulong> { startSegmentId }));

        while (queue.Count > 0)
        {
            var (currentId, currentPath) = queue.Dequeue();
            if (visited.Contains(currentId)) continue;
            visited.Add(currentId);

            if (!pathSegments.TryGetValue(currentId, out var segment)) continue;

            if (segment.Position == goal)
            {
                return currentPath;
            }

            if (segment.NextSegmentId.HasValue && !visited.Contains(segment.NextSegmentId.Value))
            {
                var nextPath = new List<ulong>(currentPath) { segment.NextSegmentId.Value };
                queue.Enqueue((segment.NextSegmentId.Value, nextPath));
            }
        }

        return null;
    }

    public bool IsReachable(
        GridPosRPG start,
        GridPosRPG goal,
        Dictionary<GridPosRPG, ulong> pathPositionIndex,
        Dictionary<ulong, PathSegmentLogic> pathSegments)
    {
        return FindPath(start, goal, pathPositionIndex, pathSegments) != null;
    }

    public List<ulong> FindPathToNearestStructure(
        GridPosRPG start,
        string targetCategory,
        Dictionary<GridPosRPG, ulong> pathPositionIndex,
        Dictionary<ulong, PathSegmentLogic> pathSegments,
        Dictionary<ulong, Structure> structures,
        Dictionary<GridPosRPG, ulong> structurePositionIndex)
    {
        if (!pathPositionIndex.TryGetValue(start, out var startSegmentId))
            return new List<ulong>();

        var visited = new HashSet<ulong>();
        var queue = new Queue<(ulong segmentId, List<ulong> currentPath)>();
        queue.Enqueue((startSegmentId, new List<ulong> { startSegmentId }));

        while (queue.Count > 0)
        {
            var (currentId, currentPath) = queue.Dequeue();
            if (visited.Contains(currentId)) continue;
            visited.Add(currentId);

            if (!pathSegments.TryGetValue(currentId, out var segment)) continue;

            if (structurePositionIndex.TryGetValue(segment.Position, out var structureId) &&
                structures.TryGetValue(structureId, out var structure) &&
                structure.GetCategoryName() == targetCategory)
            {
                return currentPath;
            }

            if (segment.NextSegmentId.HasValue && !visited.Contains(segment.NextSegmentId.Value))
            {
                var nextPath = new List<ulong>(currentPath) { segment.NextSegmentId.Value };
                queue.Enqueue((segment.NextSegmentId.Value, nextPath));
            }
        }

        return new List<ulong>();
    }
}
