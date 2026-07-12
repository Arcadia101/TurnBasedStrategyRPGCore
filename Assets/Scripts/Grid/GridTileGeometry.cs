using System.Collections.Generic;
using UnityEngine;

public class GridTileGeometry : MonoBehaviour
{
    [Header("Configuración Geométrica")]
    [SerializeField] private TileType tileType; //enum: Octagon o Rhombus

    [Header("Radios de la Casilla (Ajustar según escala del modelo)")]
    [SerializeField] private float outerRadius = 1f; //Distancia del centro a las esquinas principales

    // Lista de vectores locales con las esquinas del polígono
    private List<Vector3> localCorners = new List<Vector3>();

    private void Awake()
    {
        GenerateLocalCorners();
    }
    
    // Genera la matriz de puntos locales alrededor del centro (0,0,0) de la casilla.
    private void GenerateLocalCorners()
    {
        localCorners.Clear();

        if (tileType == TileType.Rhombus)
        {
            // UN ROMBO TIENE 4 VÉRTICES
            // Los colocamos en orden horario (Norte, Este, Sur, Oeste)
            localCorners.Add(new Vector3(0f, 0f, outerRadius));  // Norte
            localCorners.Add(new Vector3(outerRadius, 0f, 0f));  // Este
            localCorners.Add(new Vector3(0f, 0f, -outerRadius)); // Sur
            localCorners.Add(new Vector3(-outerRadius, 0f, 0f)); // Oeste
        }
        else // TileType.Octagon
        {
            // UN OCTÁGONO TIENE 8 VÉRTICES
            // CORRECCIÓN: Ajustamos los ángulos para que las ARISTAS (lados entre vértices) 
            // se alineen perfectamente con las direcciones de los vecinos de tu GridOutlineGenerator:
            // Lado 0: Norte, Lado 1: Noreste, Lado 2: Este, etc.
            
            // Para que la primera arista (entre el vértice 0 y el 1) mire plano al Norte,
            // empezamos desfasados hacia la izquierda a -22.5°
            float startAngle = -22.5f;
            for (int i = 0; i < 8; i++)
            {
                float angleRad = Mathf.Deg2Rad * (startAngle + (i * 45f));
                localCorners.Add(new Vector3(Mathf.Sin(angleRad) * outerRadius, 0f, Mathf.Cos(angleRad) * outerRadius));
            }
        }
    }
    
    // Devuelve las esquinas de esta casilla específica en coordenadas globales del mundo 3D.
    public List<Vector3> GetWorldCorners(float heightOffset = 0.02f)
    {
        List<Vector3> worldCorners = new List<Vector3>();
        foreach (Vector3 localCorner in localCorners)
        {
            // Respetamos escala y rotación del prefab en lugar de asumir solo traslación.
            Vector3 worldCorner = transform.TransformPoint(localCorner);
            worldCorners.Add(worldCorner + Vector3.up * heightOffset);
        }
        return worldCorners;
    }
    
    // Devuelve un par de puntos (Arista/Lado) específico del polígono según su índice.
    public (Vector3 start, Vector3 end) GetEdge(int index, float heightOffset = 0.02f)
    {
        List<Vector3> corners = GetWorldCorners(heightOffset);
        Vector3 start = corners[index];
        Vector3 end = corners[(index + 1) % corners.Count]; // El último lado conecta de vuelta con el primero
        return (start, end);
    }

    public int GetEdgeCount()
    {
        return tileType == TileType.Rhombus ? 4 : 8;
    }

    public void DumpGeometry(float heightOffset = 0.02f)
    {
        List<Vector3> corners = GetWorldCorners(heightOffset);
        Debug.Log($"[GridTileGeometry] {name} tileType={tileType} edgeCount={GetEdgeCount()}");

        for (int i = 0; i < corners.Count; i++)
        {
            Vector3 current = corners[i];
            Vector3 next = corners[(i + 1) % corners.Count];
            Debug.Log($"[GridTileGeometry] {name} corner[{i}]={current} edge[{i}]={current} -> {next}");
        }
    }
}
