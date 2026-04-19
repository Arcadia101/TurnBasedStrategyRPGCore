using System;
using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{

    public static event EventHandler OnAnyGrenadeExploded;
    
    private Action onGrenadeBehaviourComplete;

    private float totalDistance;
    private Vector3 positionXZ;

    [SerializeField] private Transform grenadeExplodeVFXPrefab;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private AnimationCurve arcYAnimationCurve;
    
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float reachedTargetDistance = .2f;
    [SerializeField] private float damageRadius = 4f;
    
    [SerializeField] private int damage = 30;
    
    
    private Vector3 targetposition;

    private void Update()
    {
        MoveToPointAndDestroy(targetposition, moveSpeed, reachedTargetDistance);
        
        
    }

    private void MoveToPointAndDestroy(Vector3 targetposition, float moveSpeed, float reachedTargetDistance)
    {
        if (totalDistance <= 0.0001f)
        {
            transform.position = targetposition;
            ApplyDamageInRadius(targetposition, damage, damageRadius);
            OnAnyGrenadeExploded?.Invoke(this, EventArgs.Empty);
            trailRenderer.transform.parent = null;
            Instantiate(grenadeExplodeVFXPrefab, targetposition + Vector3.up * 1f, Quaternion.identity);
            Destroy(gameObject);
            onGrenadeBehaviourComplete();
            return;
        }

        Vector3 moveDir = (targetposition - positionXZ).normalized;
        positionXZ += moveDir * (moveSpeed * Time.deltaTime);

        float distance = Vector3.Distance(positionXZ, targetposition);
        float distanceNormalized = 1 - distance / totalDistance;
        distanceNormalized = Mathf.Clamp01(distanceNormalized);

        float maxHeight = totalDistance / 4f;
        float positionY = arcYAnimationCurve.Evaluate(distanceNormalized) * maxHeight;

        transform.position = new Vector3(positionXZ.x, positionY, positionXZ.z);

        if (Vector3.Distance(positionXZ, targetposition) <= reachedTargetDistance)
        {
            //Reached Target.
            ApplyDamageInRadius(targetposition, damage, damageRadius);
            OnAnyGrenadeExploded?.Invoke(this, EventArgs.Empty);
            
            //trail renderer unparent.
            trailRenderer.transform.parent = null;
            
            Instantiate(grenadeExplodeVFXPrefab, positionXZ + Vector3.up * 1f, Quaternion.identity);
            Destroy(gameObject);
            
            //Inform logic Complete.
            onGrenadeBehaviourComplete();
        }
    }
    

    private void ApplyDamageInRadius(Vector3 targetposition, int damage, float damageRadius = 4f)
    {
        //select units at range.
        Collider[] colliders = Physics.OverlapSphere(targetposition, damageRadius);
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out Unit targetUnit))
            {
                targetUnit.Damage(damage);
            }
            if (collider.TryGetComponent(out DestructibleCrate targetCrate))
            {
                targetCrate.Damage();
            }
        }
    }

    public void Setup(GridPosition gridPosition, Action onGrenadeBehaviourComplete)
    {
        this.onGrenadeBehaviourComplete = onGrenadeBehaviourComplete;
        targetposition = LevelGrid.Instance.GetWorldPosition(gridPosition);

        positionXZ = transform.position;
        positionXZ.y = 0;

        totalDistance = Vector3.Distance(positionXZ, targetposition);
    }
}
