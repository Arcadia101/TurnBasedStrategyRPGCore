using System.Collections.Generic;
using UnityEngine;

public class GridOutlineRenderer : MonoBehaviour
{
    public static GridOutlineRenderer Instance { get; private set; }

    [Header("Configuración de la Línea")]
    [SerializeField] private Material outlineMaterial; // Tu material Neón
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private float floatHeight = 0.03f; // Separación del suelo
    [SerializeField] private bool debugReturnRawBoundaryEdges = false;
    [SerializeField] private bool debugDumpTileGeometry = false;
    [SerializeField] private bool debugTraceBoundaryEdges = false;

    // Lista para reciclar los LineRenderers de un frame a otro y evitar lag de Garbage Collector
    private List<LineRenderer> activeLines = new List<LineRenderer>();
    private List<LineRenderer> linePool = new List<LineRenderer>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Nos suscribimos al evento global para actualizar la línea cuando cambie la acción o unidad
        UnitActionSystem.Instance.OnSelectedActionChanged += (s, e) => RefreshOutline();
        UnitActionSystem.Instance.OnActionStarted += (s, e) => ClearOutline();
        LevelGrid.Instance.OnAnyUnitMovedGridPosition += (s, e) => RefreshOutline();
        if (ToroidLevelGrid.ToroidInstance != null)
        {
            ToroidLevelGrid.ToroidInstance.OnAnyUnitMovedGridPosition += (s, e) => RefreshOutline();
        }
    }

    public void RefreshOutline()
    {
        ClearOutline();

        Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
        if (selectedUnit == null) return;

        BaseAction selectedAction = UnitActionSystem.Instance.GetSelectedAction();
        if (selectedAction == null) return;

        // --- EXCEPCIÓN: SOLO DIBUJAMOS LA SILUETA SI LA ACCIÓN ES EL MOVIMIENTO ---
        if (selectedAction is not MoveAction) return;

        // 1. Obtenemos las casillas válidas de la acción de movimiento actual
        List<GridPosition> validPositions = new List<GridPosition>(selectedAction.GetValidActionGridPositionList());
        GridPosition originGridPosition = selectedUnit.GetGridPosition();
        if (!validPositions.Contains(originGridPosition))
        {
            validPositions.Add(originGridPosition);
        }
        LevelGrid gridContext = selectedAction.GetGridContext();

        if (debugDumpTileGeometry)
        {
            foreach (GridPosition gridPos in validPositions)
            {
                GridSystemVisual activeGridVisual = gridContext is ToroidLevelGrid
                    ? ToroidGridSystemVisual.ToroidInstance
                    : GridSystemVisual.Instance;

                if (activeGridVisual == null)
                {
                    continue;
                }

                GridSystemVisualSingle visualSingle = activeGridVisual.GetGridSystemVisualSingleAtPosition(gridPos);
                if (visualSingle == null)
                {
                    continue;
                }

                GridTileGeometry geometry = visualSingle.GetComponent<GridTileGeometry>();
                if (geometry != null)
                {
                    geometry.DumpGeometry(floatHeight);
                }
            }
        }

        // 2. Calculamos los bucles perimetrales ordenados secuencialmente
        List<List<Vector3>> loops = GridOutlineGenerator.GenerateOutlineLoops(
            validPositions,
            gridContext,
            floatHeight,
            debugReturnRawBoundaryEdges,
            debugTraceBoundaryEdges);

        // 3. Mandamos a pintar cada isla de forma independiente
        bool forceClosedLoop = gridContext is not ToroidLevelGrid;
        foreach (List<Vector3> loopPoints in loops)
        {
            DrawLoop(loopPoints, forceClosedLoop);
        }
    }

    private void DrawLoop(List<Vector3> points, bool forceClosedLoop)
    {
        LineRenderer line = GetOrCreateLineRenderer();

        // Si el loop ya viene cerrado, quitamos el último punto repetido.
        bool endpointsMeet = points.Count > 2 && Vector3.Distance(points[0], points[points.Count - 1]) < 0.001f;
        if (endpointsMeet)
        {
            points = points.GetRange(0, points.Count - 1);
        }
        
        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
        line.loop = points.Count > 2 && (forceClosedLoop || endpointsMeet);
        line.transform.rotation = Quaternion.LookRotation(Vector3.up);
        line.gameObject.SetActive(true);
        activeLines.Add(line);
    }

    private LineRenderer GetOrCreateLineRenderer()
    {
        if (linePool.Count > 0)
        {
            LineRenderer pooledLine = linePool[linePool.Count - 1];
            linePool.RemoveAt(linePool.Count - 1);
            return pooledLine;
        }

        // Si la piscina está vacía, creamos un nuevo hilo dinámico
        GameObject lineObj = new GameObject("Outline_Loop_Segment");
        lineObj.transform.SetParent(transform);
        lineObj.transform.rotation = Quaternion.LookRotation(Vector3.up);
        
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = outlineMaterial;
        
        // Configuraciones estratégicas para esquinas suaves neón
        lr.textureMode = LineTextureMode.Tile;
        lr.alignment = LineAlignment.TransformZ;
        lr.numCornerVertices = 3;
        lr.numCapVertices = 3;

        return lr;
    }

    public void ClearOutline()
    {
        foreach (LineRenderer line in activeLines)
        {
            line.gameObject.SetActive(false);
            linePool.Add(line);
        }
        activeLines.Clear();
    }
}
