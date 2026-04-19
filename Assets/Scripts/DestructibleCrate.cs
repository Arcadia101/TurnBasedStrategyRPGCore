using System;
using UnityEngine;

public class DestructibleCrate : MonoBehaviour
{
    public static event EventHandler OnAnyDestructibleCrateDestroyed;

    [SerializeField] private Transform crateDestroyedPrefab;
    
    
    private GridPosition gridPosition;

    private void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
    }

    public GridPosition GetGridPosition()
    {
        return gridPosition;
    }

    public void Damage()
    {
        Transform crateDestroyedTransform = Instantiate(crateDestroyedPrefab, transform.position, Quaternion.identity);
        ApplyExplotionToCrateParts(crateDestroyedTransform, 200, transform.position, 10);
        Destroy(gameObject);
        OnAnyDestructibleCrateDestroyed?.Invoke(this, EventArgs.Empty);
    }
    
    private void ApplyExplotionToCrateParts(Transform root, float explosionForce, Vector3 explosionPosition, float explosionRange)
    {
        foreach (Transform child in root)
        {
            if (child.TryGetComponent<Rigidbody>(out Rigidbody childRigidbody))
            {
                childRigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRange);
            }
            
            ApplyExplotionToCrateParts(child, explosionForce, explosionPosition, explosionRange);
        }
    }
}
