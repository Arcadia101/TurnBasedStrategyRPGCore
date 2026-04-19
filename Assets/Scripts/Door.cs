using System;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] private bool isOpen;
    
    private GridPosition gridPosition;
    private Animator animator;
    private Action OnInteractComplete;
    private bool isActive;
    private float timer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.SetInteractableAtGridPosition(gridPosition, this);

        if (isOpen)
        {
            OpenDoor();
        }
        else
        {
            CloseDoor();
        }
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

    public void Interact(Action OnInteractComplete)
    {
        Debug.Log("Door.Interact() called!");
        this.OnInteractComplete = OnInteractComplete;
        isActive = true;
        timer = .5f;
        if (isOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }
    }

    private void OpenDoor()
    {
        isOpen = true;
        Pathfinding.Instance.SetIsWalkableGridPosition(gridPosition, true);
        animator.SetBool("IsOpen", true);
    }
    
    private void CloseDoor()
    {
        isOpen = false;
        Pathfinding.Instance.SetIsWalkableGridPosition(gridPosition, false);
        animator.SetBool("IsOpen", false);
    }
}
