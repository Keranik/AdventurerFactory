namespace ForgeFlow.Core.Utilities;

public sealed class TrafficManager
{
    private readonly Dictionary<ulong, HashSet<ulong>> _segmentOccupants = new();
    private readonly int _maxOccupantsPerSegment;

    public TrafficManager(int maxOccupantsPerSegment = 1)
    {
        _maxOccupantsPerSegment = maxOccupantsPerSegment;
    }

    public bool CanEnterSegment(ulong segmentId, ulong heroId)
    {
        if (!_segmentOccupants.TryGetValue(segmentId, out var occupants))
            return true;

        if (occupants.Contains(heroId))
            return true;

        return occupants.Count < _maxOccupantsPerSegment;
    }

    public void EnterSegment(ulong segmentId, ulong heroId)
    {
        if (!_segmentOccupants.TryGetValue(segmentId, out var occupants))
        {
            occupants = new HashSet<ulong>();
            _segmentOccupants[segmentId] = occupants;
        }
        occupants.Add(heroId);
    }

    public void LeaveSegment(ulong segmentId, ulong heroId)
    {
        if (_segmentOccupants.TryGetValue(segmentId, out var occupants))
        {
            occupants.Remove(heroId);
            if (occupants.Count == 0)
            {
                _segmentOccupants.Remove(segmentId);
            }
        }
    }

    public int GetOccupantCount(ulong segmentId)
    {
        return _segmentOccupants.TryGetValue(segmentId, out var occupants) ? occupants.Count : 0;
    }

    public bool IsSegmentFull(ulong segmentId)
    {
        return GetOccupantCount(segmentId) >= _maxOccupantsPerSegment;
    }

    public void SetMaxOccupants(int max)
    {
        // Used when party-lane research is unlocked
    }

    public void Clear() => _segmentOccupants.Clear();
}
