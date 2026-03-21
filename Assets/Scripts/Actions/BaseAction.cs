using UnityEngine;

public abstract class BaseAction : MonoBehaviour
{
    protected Unit unit;
    protected bool isActive;
    
    protected virtual void Awake()
    {
        unit = GetComponent<Unit>();
    }
}
