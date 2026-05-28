using System;
using System.Collections.Generic;
using UnityEngine;

public class ToroidPathfinding : Pathfinding
{
    private void Awake()
    {
        // Al heredar, nos adueñamos de la instancia global polimórficamente
        if (Instance != null)
        {
            Debug.LogError("There is already a Pathfinding (Toroid) in the scene!");
            Destroy(gameObject);
        }
        Instance = this;
    }

    // --- 1. CÁLCULO DE DISTANCIA MODULAR (ATALOS PAC-MAN) ---
    public override int CalculateDistance(GridPosition gridPositionA, GridPosition gridPositionB)
    {
        // En una escena Toroidal, necesitamos la Grid para saber las dimensiones (ej: 5x5)
        int width = ToroidLevelGrid.Instance.GetWidth();
        int height = ToroidLevelGrid.Instance.GetHeight();

        // Distancia directa clásica
        int directX = Mathf.Abs(gridPositionA.x - gridPositionB.x);
        int directZ = Mathf.Abs(gridPositionA.z - gridPositionB.z);

        // Distancia cruzando los bordes del Toroide (Wrapping)
        int toroidX = width - directX;
        int toroidZ = height - directZ;

        // Nos quedamos siempre con el camino más corto absoluto en ambos ejes
        int shortestX = Mathf.Min(directX, toroidX);
        int shortestZ = Mathf.Min(directZ, toroidZ);

        // Aplicamos tus mismos costos matemáticos (MOVE_STRAIGHT / MOVE_DIAGONAL)
        int remaining = Mathf.Abs(shortestX - shortestZ);
        
        // Usamos tus valores fijos: 10 para rectas, 10 para diagonales del array
        return 10 * Mathf.Min(shortestX, shortestZ) + 10 * remaining;
    }

    // --- 2. VECINOS CON ENVOLTURA AUTOMÁTICA ---
    protected override List<PathNode> GetNeighbourList(PathNode currentNode)
    {
        List<PathNode> neighbourList = new List<PathNode>();
        GridPosition gridPosition = currentNode.GetGridPosition();

        int width = ToroidLevelGrid.Instance.GetWidth();
        int height = ToroidLevelGrid.Instance.GetHeight();
        bool isOctagon = (gridPosition.x + gridPosition.z) % 2 == 0;

        PathNode GetWrappedNode(int x, int z)
        {
            int wrappedX = (x % width + width) % width;
            int wrappedZ = (z % height + height) % height;
        
            // SI HUBO WRAP: Metemos un Log para verlo en consola
            if (x != wrappedX || z != wrappedZ)
            {
                Debug.Log($"[CEREBRO] ¡Conectando borde! Vecino virtual ({x},{z}) envuelto a casilla real ({wrappedX},{wrappedZ})");
            }

            return GetNode(wrappedX, wrappedZ); 
        }

        // ... (El resto de las conexiones X/Z y diagonales se quedan igual) ...
        neighbourList.Add(GetWrappedNode(gridPosition.x - 1, gridPosition.z));
        neighbourList.Add(GetWrappedNode(gridPosition.x + 1, gridPosition.z));
        neighbourList.Add(GetWrappedNode(gridPosition.x, gridPosition.z - 1));
        neighbourList.Add(GetWrappedNode(gridPosition.x, gridPosition.z + 1));

        if (isOctagon)
        {
            neighbourList.Add(GetWrappedNode(gridPosition.x - 1, gridPosition.z - 1));
            neighbourList.Add(GetWrappedNode(gridPosition.x - 1, gridPosition.z + 1));
            neighbourList.Add(GetWrappedNode(gridPosition.x + 1, gridPosition.z - 1));
            neighbourList.Add(GetWrappedNode(gridPosition.x + 1, gridPosition.z + 1));
        }

        return neighbourList;
    }

    public override List<GridPosition> FindPath(GridPosition startGridPosition, GridPosition endGridPosition, out int pathLength)
    {
        List<PathNode> openList = new List<PathNode>();
        List<PathNode> closedList = new List<PathNode>();

        PathNode startNode = GetNode(startGridPosition.x, startGridPosition.z);
        PathNode endNode = GetNode(endGridPosition.x, endGridPosition.z);
        
        openList.Add(startNode);

        int width = ToroidLevelGrid.Instance.GetWidth();
        int height = ToroidLevelGrid.Instance.GetHeight();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                PathNode pathNode = GetNode(x, z);
                pathNode.SetGCost(int.MaxValue);
                pathNode.SetHCost(0);
                pathNode.CalculateFCost();
                pathNode.ResetCameFromPathNode();
            }
        }
    
        startNode.SetGCost(0);
        // Usamos el CalculateDistance polimórfico del Toroide
        startNode.SetHCost(CalculateDistance(startGridPosition, endGridPosition));
        startNode.CalculateFCost();

        while (openList.Count > 0)
        {
            PathNode currentNode = GetLowestFCostPathNode(openList);

            if (currentNode == endNode)
            {
                pathLength = endNode.GetGCost();
                return CalculatePath(endNode);
            }
        
            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (PathNode neighbourNode in GetNeighbourList(currentNode))
            {
                if (closedList.Contains(neighbourNode)) continue;
                if (!neighbourNode.IsWalkable())
                {
                    closedList.Add(neighbourNode);
                    continue;
                }
            
                // Calculamos el costo de movernos usando la distancia modular del Toroide (siempre dará 10 para vecinos directos)
                int tentativeGCost = currentNode.GetGCost() + CalculateDistance(currentNode.GetGridPosition(), neighbourNode.GetGridPosition());

                if (tentativeGCost < neighbourNode.GetGCost())
                {
                    neighbourNode.SetCameFormPathNode(currentNode);
                    neighbourNode.SetGCost(tentativeGCost);
                
                    // ¡CRÍTICO! Calculamos la distancia estimada restante usando la matemática del Toroide hacia el objetivo
                    neighbourNode.SetHCost(CalculateDistance(neighbourNode.GetGridPosition(), endGridPosition));
                    neighbourNode.CalculateFCost();

                    if (!openList.Contains(neighbourNode))
                    {
                        openList.Add(neighbourNode);
                    }
                }
            }
        }
    
        pathLength = 0;
        return null;
}

// Método auxiliar heredado necesario para que compile el bucle A*
private PathNode GetLowestFCostPathNode(List<PathNode> pathNodeList)
{
    PathNode lowestFCostPathNode = pathNodeList[0];
    for (int i = 0; i < pathNodeList.Count; i++)
    {
        if (pathNodeList[i].GetFCost() < lowestFCostPathNode.GetFCost())
        {
            lowestFCostPathNode = pathNodeList[i];
        }
    }
    return lowestFCostPathNode;
}

// Método auxiliar heredado necesario para reconstruir la ruta
private List<GridPosition> CalculatePath(PathNode endNode)
{
    List<PathNode> pathNodeList = new List<PathNode> { endNode };
    PathNode currentNode = endNode;
    while (currentNode.GetCameFromPathNode() != null)
    {
        pathNodeList.Add(currentNode.GetCameFromPathNode());
        currentNode = currentNode.GetCameFromPathNode();
    }
    pathNodeList.Reverse();
    
    List<GridPosition> gridPositionList = new List<GridPosition>();
    foreach (PathNode pathNode in pathNodeList)
    {
        gridPositionList.Add(pathNode.GetGridPosition());
    }
    return gridPositionList;
}
    
    public override List<GridPosition> GetReachableGridPositionList(GridPosition startGridPosition, int maxDistance)
    {
        List<GridPosition> reachableGridPositionList = new List<GridPosition>();

        // Usamos el método GetNode que heredamos de la base de forma segura
        PathNode startNode = GetNode(startGridPosition.x, startGridPosition.z);

        Queue<PathNode> queue = new Queue<PathNode>();
        Dictionary<PathNode, int> distanceByNode = new Dictionary<PathNode, int>();

        queue.Enqueue(startNode);
        distanceByNode[startNode] = 0;

        while (queue.Count > 0)
        {
            PathNode currentNode = queue.Dequeue();
            int currentDistance = distanceByNode[currentNode];

            if (currentDistance >= maxDistance) continue;

            // Llama automáticamente a tu GetNeighbourList() toroidal que ya loguea en consola
            foreach (PathNode neighbourNode in GetNeighbourList(currentNode))
            {
                if (distanceByNode.ContainsKey(neighbourNode)) continue;
                if (!neighbourNode.IsWalkable()) continue;

                // Verificamos si se puede añadir la unidad usando la Grid del Toroide
                GridPosition neighbourGridPosition = neighbourNode.GetGridPosition();
                if (!LevelGrid.Instance.CanAddUnitAtGridPosition(neighbourGridPosition)) continue;

                // En el Toroide, cada paso a un vecino directo SIEMPRE cuesta exactamente 1 de distancia
                int neighbourDistance = currentDistance + 1;

                distanceByNode[neighbourNode] = neighbourDistance;
                queue.Enqueue(neighbourNode);
            
                if (!reachableGridPositionList.Contains(neighbourGridPosition))
                {
                    reachableGridPositionList.Add(neighbourGridPosition);
                }
            }
        }

        return reachableGridPositionList;
    }
}