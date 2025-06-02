using UnityEngine;

/// <summary>
/// Listens for open and close events from the EventManager and toggles the door's state accordingly.
/// </summary>
public class OpenDoor : MonoBehaviour
{
    /// <summary>
    /// Collider on the parent object, used to control collision when the door is opened or closed.
    /// </summary>
    private Collider _parentCollider;

    /// <summary>
    /// The child GameObject representing the actual door to enable or disable.
    /// </summary>
    private GameObject _doorChild;
    
    /// <summary>
    /// Flag to check a boss door
    /// </summary>
    public bool isBossDoor = false;

    /// <summary>
    /// Subscribes to the open and close door events and initializes references to the collider and door child.
    /// </summary>
    private void Start()
    {
        // Cache the parent Collider and the door child GameObject
        _parentCollider = GetComponent<Collider>();
        _doorChild = transform.GetChild(0).gameObject;
        
        if (isBossDoor)
        {                     
            // Boss door starting disabled
            _doorChild.SetActive(true);
            _parentCollider.enabled = true;

            EventManager.OnOpenBossDoors  += Open;
            EventManager.OnCloseBossDoors += Close;
        }
        else
        {
            // Disable the door child by default
            _doorChild.SetActive(false);
            _parentCollider.enabled = false;

            // Subscribe to events from the EventManager
            EventManager.OnOpenDoors += Open;
            EventManager.OnCloseDoors += Close;
        }
    }

    /// <summary>
    /// Unsubscribes from the events to avoid memory leaks when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (isBossDoor)
        {
            EventManager.OnOpenBossDoors  -= Open;
            EventManager.OnCloseBossDoors -= Close;
        }
        else
        {
            EventManager.OnOpenDoors  -= Open;
            EventManager.OnCloseDoors -= Close;
        }
    }

    /// <summary>
    /// Opens the door by disabling the child door GameObject and the parent's collider.
    /// </summary>
    private void Open()
    {
        if (_doorChild.activeSelf)
        {
            _parentCollider.enabled = false;    // Disable collision
            _doorChild.SetActive(false);        // Hide the door object
        }
    }

    /// <summary>
    /// Closes the door by enabling the child door GameObject and the parent's collider.
    /// </summary>
    private void Close()
    {
        if (!_doorChild.activeSelf)
        {
            _parentCollider.enabled = true;     // Enable collision
            _doorChild.SetActive(true);         // Show the door object
        }
    }
}