using System.Collections.Generic;
using UnityEngine;

public static class GridOutlineGenerator
{
    private struct Edge3D
    {
        public Vector3 start;
        public Vector3 end;
        public GridPosition sourceGridPos;
        public int edgeIndex;
        public PointKey startKey;
        public PointKey endKey;

        public Edge3D(Vector3 start, Vector3 end, GridPosition sourceGridPos, int edgeIndex)
        {
            this.start = start;
            this.end = end;
            this.sourceGridPos = sourceGridPos;
            this.edgeIndex = edgeIndex;
            this.startKey = new PointKey(start);
            this.endKey = new PointKey(end);
        }
    }

    private struct PointKey : System.IEquatable<PointKey>
    {
        private readonly long x;
        private readonly long y;
        private readonly long z;

        public PointKey(Vector3 point)
        {
            x = Quantize(point.x);
            y = Quantize(point.y);
            z = Quantize(point.z);
        }

        public bool Equals(PointKey other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        public override bool Equals(object obj)
        {
            return obj is PointKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + x.GetHashCode();
                hash = hash * 31 + y.GetHashCode();
                hash = hash * 31 + z.GetHashCode();
                return hash;
            }
        }

        private static long Quantize(float value)
        {
            return Mathf.RoundToInt(value * 10000f);
        }
    }

    public static List<List<Vector3>> GenerateOutlineLoops(
        List<GridPosition> validPositions,
        LevelGrid gridContext,
        float heightOffset = 0.05f,
        bool debugReturnRawBoundaryEdges = false,
        bool debugTraceBoundaryEdges = false)
    {
        HashSet<GridPosition> selectedPositions = new HashSet<GridPosition>(validPositions);

        GridSystemVisual activeGridVisual = gridContext is ToroidLevelGrid
            ? ToroidGridSystemVisual.ToroidInstance
            : GridSystemVisual.Instance;

        float probeDistance = Mathf.Max(0.08f, gridContext.GetCellSize() * 0.08f);
        List<Edge3D> boundaryEdges = new List<Edge3D>();

        foreach (GridPosition gridPos in validPositions)
        {
            GridObject gridObject = gridContext.GetGridSystem().GetGridObject(gridPos) as GridObject;
            if (gridObject == null)
            {
                continue;
            }

            int edgeCount = gridObject.GetTileType() == TileType.Rhombus ? 4 : 8;

            for (int edgeIndex = 0; edgeIndex < edgeCount; edgeIndex++)
            {
                if (!TryGetTileEdge(gridPos, gridContext, activeGridVisual, edgeIndex, heightOffset, out Vector3 edgeStart, out Vector3 edgeEnd))
                {
                    continue;
                }

                if (IsBoundaryEdge(gridPos, edgeIndex, edgeStart, edgeEnd, gridContext, selectedPositions, probeDistance, debugTraceBoundaryEdges))
                {
                    boundaryEdges.Add(new Edge3D(edgeStart, edgeEnd, gridPos, edgeIndex));
                }
            }
        }

        if (debugReturnRawBoundaryEdges)
        {
            LogBoundaryEdgeSummary(boundaryEdges);
            return BuildRawSegments(boundaryEdges, validPositions.Count);
        }

        List<List<Vector3>> completedLoops = new List<List<Vector3>>();
        Dictionary<PointKey, List<int>> pointToEdgeIndices = new Dictionary<PointKey, List<int>>();
        Dictionary<PointKey, Vector3> pointRepresentatives = new Dictionary<PointKey, Vector3>();

        for (int i = 0; i < boundaryEdges.Count; i++)
        {
            Edge3D edge = boundaryEdges[i];
            AddPointEdge(pointToEdgeIndices, edge.startKey, i);
            AddPointEdge(pointToEdgeIndices, edge.endKey, i);
            AddPointRepresentative(pointRepresentatives, edge.startKey, edge.start);
            AddPointRepresentative(pointRepresentatives, edge.endKey, edge.end);
        }

        HashSet<int> visitedEdges = new HashSet<int>();

        for (int edgeStartIndex = 0; edgeStartIndex < boundaryEdges.Count; edgeStartIndex++)
        {
            if (visitedEdges.Contains(edgeStartIndex))
            {
                continue;
            }

            List<PointKey> loopKeys = BuildBoundaryLoopKeys(
                edgeStartIndex,
                boundaryEdges,
                pointToEdgeIndices,
                visitedEdges);

            if (loopKeys.Count >= 3)
            {
                List<Vector3> currentLoop = new List<Vector3>(loopKeys.Count);
                for (int i = 0; i < loopKeys.Count; i++)
                {
                    if (pointRepresentatives.TryGetValue(loopKeys[i], out Vector3 point))
                    {
                        currentLoop.Add(point);
                    }
                }

                if (currentLoop.Count >= 3)
                {
                    completedLoops.Add(currentLoop);
                }
            }
        }

        if (visitedEdges.Count != boundaryEdges.Count)
        {
            Debug.LogWarning($"[GridOutlineGenerator] Boundary loop stitching incomplete: visited={visitedEdges.Count}, edges={boundaryEdges.Count}. Falling back to raw segments.");
            return BuildRawSegments(boundaryEdges, validPositions.Count);
        }

        return completedLoops;
    }

    private static List<List<Vector3>> BuildRawSegments(List<Edge3D> boundaryEdges, int validPositionCount)
    {
        List<List<Vector3>> rawSegments = new List<List<Vector3>>(boundaryEdges.Count);
        foreach (Edge3D edge in boundaryEdges)
        {
            rawSegments.Add(new List<Vector3> { edge.start, edge.end });
        }

        Debug.Log($"[GridOutlineGenerator] Raw boundary edges: positions={validPositionCount}, edges={boundaryEdges.Count}");
        return rawSegments;
    }

    private static void AddPointEdge(Dictionary<PointKey, List<int>> pointToEdgeIndices, PointKey key, int edgeIndex)
    {
        if (!pointToEdgeIndices.TryGetValue(key, out List<int> list))
        {
            list = new List<int>();
            pointToEdgeIndices[key] = list;
        }

        list.Add(edgeIndex);
    }

    private static void AddPointRepresentative(Dictionary<PointKey, Vector3> pointRepresentatives, PointKey key, Vector3 point)
    {
        if (!pointRepresentatives.ContainsKey(key))
        {
            pointRepresentatives[key] = point;
        }
    }

    private static List<PointKey> BuildBoundaryLoopKeys(
        int startEdgeIndex,
        List<Edge3D> boundaryEdges,
        Dictionary<PointKey, List<int>> pointToEdgeIndices,
        HashSet<int> visitedEdges)
    {
        List<PointKey> circuit = new List<PointKey>();
        Edge3D startEdge = boundaryEdges[startEdgeIndex];
        PointKey startKey = startEdge.startKey;

        Stack<PointKey> vertexStack = new Stack<PointKey>();
        vertexStack.Push(startKey);
        visitedEdges.Add(startEdgeIndex);

        PointKey initialNeighbor = startEdge.endKey;
        vertexStack.Push(initialNeighbor);

        while (vertexStack.Count > 0)
        {
            PointKey currentKey = vertexStack.Peek();
            int nextEdgeIndex = FindUnusedIncidentEdge(pointToEdgeIndices, visitedEdges, currentKey);

            if (nextEdgeIndex >= 0)
            {
                visitedEdges.Add(nextEdgeIndex);
                Edge3D nextEdge = boundaryEdges[nextEdgeIndex];
                PointKey nextKey = nextEdge.startKey.Equals(currentKey) ? nextEdge.endKey : nextEdge.startKey;
                vertexStack.Push(nextKey);
                continue;
            }

            circuit.Add(currentKey);
            vertexStack.Pop();
        }

        circuit.Reverse();
        return circuit;
    }

    private static int FindUnusedIncidentEdge(
        Dictionary<PointKey, List<int>> pointToEdgeIndices,
        HashSet<int> visitedEdges,
        PointKey currentKey)
    {
        if (!pointToEdgeIndices.TryGetValue(currentKey, out List<int> candidates))
        {
            return -1;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            int edgeIndex = candidates[i];
            if (!visitedEdges.Contains(edgeIndex))
            {
                return edgeIndex;
            }
        }

        return -1;
    }

    private static bool IsBoundaryEdge(
        GridPosition sourceGridPos,
        int edgeIndex,
        Vector3 edgeStart,
        Vector3 edgeEnd,
        LevelGrid gridContext,
        HashSet<GridPosition> selectedPositions,
        float probeDistance,
        bool debugTraceBoundaryEdges)
    {
        Vector3 center = gridContext.GetWorldPosition(sourceGridPos);
        Vector3 edgeMidPoint = (edgeStart + edgeEnd) * 0.5f;
        Vector3 outward = edgeMidPoint - center;
        outward.y = 0f;

        if (outward.sqrMagnitude < 0.000001f)
        {
            Vector3 tangent = edgeEnd - edgeStart;
            outward = Vector3.Cross(Vector3.up, tangent);
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.000001f)
            {
                return true;
            }
        }

        outward.Normalize();

        bool shouldTrace = debugTraceBoundaryEdges && (edgeMidPoint.x > center.x);
        for (int i = 0; i < 3; i++)
        {
            Vector3 probePoint = edgeMidPoint + outward * (probeDistance * (i + 1));
            GridPosition probedPosition = gridContext.GetGridPosition(probePoint);

            if (gridContext is ToroidLevelGrid toroidGrid)
            {
                probedPosition = toroidGrid.GetWrappedGridPosition(probedPosition);
            }

            if (probedPosition == sourceGridPos)
            {
                if (shouldTrace)
                {
                    Debug.Log($"[GridOutlineGenerator] probe-skip pos=({sourceGridPos.x},{sourceGridPos.z}) edgeIdx={edgeIndex} edge={FormatVector(edgeStart)}->{FormatVector(edgeEnd)} step={i} probe={FormatVector(probePoint)} hitSelf={probedPosition}");
                }
                continue;
            }

            if (shouldTrace)
            {
                Debug.Log($"[GridOutlineGenerator] probe pos=({sourceGridPos.x},{sourceGridPos.z}) edgeIdx={edgeIndex} edge={FormatVector(edgeStart)}->{FormatVector(edgeEnd)} step={i} mid={FormatVector(edgeMidPoint)} outward={FormatVector(outward)} probe={FormatVector(probePoint)} gridHit=({probedPosition.x},{probedPosition.z}) selected={selectedPositions.Contains(probedPosition)}");
            }
            return !selectedPositions.Contains(probedPosition);
        }

        if (shouldTrace)
        {
            Debug.Log($"[GridOutlineGenerator] probe-default-boundary pos=({sourceGridPos.x},{sourceGridPos.z}) edgeIdx={edgeIndex} edge={FormatVector(edgeStart)}->{FormatVector(edgeEnd)}");
        }
        return true;
    }

    private static void LogBoundaryEdgeSummary(List<Edge3D> boundaryEdges)
    {
        Debug.Log($"[GridOutlineGenerator] Raw boundary edges: edges={boundaryEdges.Count}");

        boundaryEdges.Sort((a, b) =>
        {
            int compareMaxX = GetEdgeMaxX(b).CompareTo(GetEdgeMaxX(a));
            if (compareMaxX != 0) return compareMaxX;

            int compareZ = GetEdgeMidZ(b).CompareTo(GetEdgeMidZ(a));
            if (compareZ != 0) return compareZ;

            return a.edgeIndex.CompareTo(b.edgeIndex);
        });

        int logCount = Mathf.Min(12, boundaryEdges.Count);
        for (int i = 0; i < logCount; i++)
        {
            Edge3D edge = boundaryEdges[i];
            Debug.Log($"[GridOutlineGenerator] edge[{i}] pos=({edge.sourceGridPos.x},{edge.sourceGridPos.z}) idx={edge.edgeIndex} start={FormatVector(edge.start)} end={FormatVector(edge.end)}");
        }
    }

    private static float GetEdgeMaxX(Edge3D edge)
    {
        return Mathf.Max(edge.start.x, edge.end.x);
    }

    private static float GetEdgeMidZ(Edge3D edge)
    {
        return (edge.start.z + edge.end.z) * 0.5f;
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
    }

    private static bool TryGetTileEdge(
        GridPosition gridPos,
        LevelGrid gridContext,
        GridSystemVisual activeGridVisual,
        int edgeIndex,
        float heightOffset,
        out Vector3 edgeStart,
        out Vector3 edgeEnd)
    {
        edgeStart = default;
        edgeEnd = default;

        GridTileGeometry tileGeo = null;
        if (activeGridVisual != null)
        {
            GridSystemVisualSingle visualSingle = activeGridVisual.GetGridSystemVisualSingleAtPosition(gridPos);
            if (visualSingle != null)
            {
                tileGeo = visualSingle.GetComponent<GridTileGeometry>();
            }
        }

        if (tileGeo != null)
        {
            (edgeStart, edgeEnd) = tileGeo.GetEdge(edgeIndex, heightOffset);
            return true;
        }

        GridObject gridObject = gridContext.GetGridSystem().GetGridObject(gridPos) as GridObject;
        if (gridObject == null)
        {
            return false;
        }

        Vector3 centerPos = gridContext.GetWorldPosition(gridPos) + Vector3.up * heightOffset;
        if (gridObject.GetTileType() == TileType.Rhombus)
        {
            Vector3[] corners = new Vector3[4];
            corners[0] = centerPos + new Vector3(0f, 0f, 0.5f);
            corners[1] = centerPos + new Vector3(0.5f, 0f, 0f);
            corners[2] = centerPos + new Vector3(0f, 0f, -0.5f);
            corners[3] = centerPos + new Vector3(-0.5f, 0f, 0f);
            edgeStart = corners[edgeIndex];
            edgeEnd = corners[(edgeIndex + 1) % 4];
            return true;
        }

        Vector3[] octagonCorners = new Vector3[8];
        float ext = 0.5f;
        float cut = 0.207f;
        octagonCorners[0] = centerPos + new Vector3(-cut, 0f, ext);
        octagonCorners[1] = centerPos + new Vector3(cut, 0f, ext);
        octagonCorners[2] = centerPos + new Vector3(ext, 0f, cut);
        octagonCorners[3] = centerPos + new Vector3(ext, 0f, -cut);
        octagonCorners[4] = centerPos + new Vector3(cut, 0f, -ext);
        octagonCorners[5] = centerPos + new Vector3(-cut, 0f, -ext);
        octagonCorners[6] = centerPos + new Vector3(-ext, 0f, -cut);
        octagonCorners[7] = centerPos + new Vector3(-ext, 0f, cut);
        edgeStart = octagonCorners[edgeIndex];
        edgeEnd = octagonCorners[(edgeIndex + 1) % 8];
        return true;
    }

}
