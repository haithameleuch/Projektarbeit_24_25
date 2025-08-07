using UnityEngine;

/// <summary>
/// Manages player input for movement and camera control using the Input System.
/// Handles input locking and cursor visibility for immersive gameplay.
/// </summary>
public class GameInputManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the GameInputManager, ensuring there is only one instance in the scene.
    /// Provides global access to game input management functionality.
    /// </summary>
    public static GameInputManager Instance { get; private set; }
    
    /// <summary>
    /// Instance of the input action map for handling player controls.
    /// </summary>
    private InputSystem_Actions _inputSystemActions;

    /// <summary>
    /// Ensures there is only one instance of the GameManager in the scene.
    /// If another instance exists, it will be destroyed.
    /// Persists the instance across scenes for consistent game state management.
    /// 
    /// Initializes input actions and locks the cursor when the script awakens.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        //DontDestroyOnLoad(gameObject);
        
        // Initialize and enable the input action map
        _inputSystemActions = new InputSystem_Actions();
        _inputSystemActions.Player.Enable();

        // Lock the cursor for gameplay
        MouseLocked(true);
    }

    /// <summary>
    /// Retrieves normalized movement input as a 2D vector.
    /// </summary>
    /// <returns>
    /// A normalized <see cref="Vector2"/> representing the player's movement direction 
    /// (x for horizontal, y for vertical).
    /// </returns>
    public Vector2 GetMovementVectorNormalized()
    {
        // Read movement input and normalize the vector
        Vector2 inputVector = _inputSystemActions.Player.Move.ReadValue<Vector2>();
        return inputVector.normalized;
    }

    /// <summary>
    /// Retrieves the player's input for camera look direction.
    /// </summary>
    /// <returns>
    /// A <see cref="Vector2"/> representing the camera look direction 
    /// (x for horizontal rotation, y for vertical rotation).
    /// </returns>
    public Vector2 GetLookDelta()
    {
        // Read look input for camera rotation
        return _inputSystemActions.Player.Look.ReadValue<Vector2>();
    }

    /// <summary>
    /// Locks or unlocks the cursor based on the specified state.
    /// </summary>
    /// <param name="locked">If true, the cursor is locked and hidden; if false, the cursor is unlocked and visible.</param>
    public void MouseLocked(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Ensures the cursor is unlocked and visible when the object is destroyed, typically when exiting the game.
    /// </summary>
    private void OnDestroy()
    {
        if (_inputSystemActions != null)
        {
            _inputSystemActions.Player.Disable();
            _inputSystemActions.Dispose();
            _inputSystemActions = null;
        }

        if (Instance == this)
            Instance = null;

        MouseLocked(false);
    }
}
