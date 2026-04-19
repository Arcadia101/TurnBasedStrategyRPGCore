using System;
using UnityEngine;

public class InteractSphere : MonoBehaviour, IInteractable
{
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material redMaterial;
    [SerializeField] private MeshRenderer meshRenderer;
    
    private GridPosition gridPosition;
    private Action OnInteractComplete;
    private bool isActive;
    private float timer;


    public bool isGreen;

    private void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.SetInteractableAtGridPosition(gridPosition, this);
        
        SetColorGreen();
    }
    
    private void Update()
    {
        if (!isActive)
        {
            return;
        }
        
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            isActive = false;
            OnInteractComplete();
        }

    }

    private void SetColorGreen()
    {
        isGreen = true;
        meshRenderer.material = greenMaterial;
    }
    
    private void SetColorRed()
    {
        isGreen = false;
        meshRenderer.material = redMaterial;   
    }

    public void Interact(Action OnInteractComplete)
    {
        this.OnInteractComplete = OnInteractComplete;
        isActive = true;
        timer = .5f;
        if (isGreen)
        {
            SetColorRed();
        }
        else
        {
            SetColorGreen();
        }
    }
}
