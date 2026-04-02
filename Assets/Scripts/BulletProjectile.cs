using System;
using UnityEngine;

public class BulletProjectile : MonoBehaviour
{
    private Vector3 targetPosition;
    [SerializeField] private float moveSpeed;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private Transform bulletHitVfxPrefab;
    
    

    public void Setup(Vector3 targetPosition)
    {
        this.targetPosition = targetPosition;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 moveDir = (targetPosition - transform.position).normalized;
        float distanceBeforeMoving = Vector3.Distance(transform.position, targetPosition);
        
        transform.position += moveDir * moveSpeed * Time.deltaTime;
        
        float distanceAfterMoving = Vector3.Distance(transform.position, targetPosition);
        
        if (distanceAfterMoving > distanceBeforeMoving)
        {
            transform.position = targetPosition;
            trailRenderer.transform.parent = null;
            Destroy(gameObject);
            
            Instantiate(bulletHitVfxPrefab, targetPosition, Quaternion.identity);
        }
    }
}
