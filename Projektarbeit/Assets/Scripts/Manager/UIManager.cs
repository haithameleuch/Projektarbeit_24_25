using TMPro;
using UnityEngine;
using Saving;

/// <summary>
/// Manages the UI elements, including panels and text updates.
/// Provides methods for showing and hiding UI panels, and updates text dynamically.
/// </summary>
public class UIManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the UIManager, ensuring there is only one instance in the scene.
    /// Provides global access to UI management functionality.
    /// </summary>
    public static UIManager Instance { get; private set; }

    [SerializeField]
    private TMP_Text tmpText; // Reference to the TMP_Text component in the scene used for UI text updates.

    [SerializeField]
    private RectTransform pause; // Reference to the pause screen
    
    [SerializeField]
    private RectTransform gameOver; // Reference to the gameOver screen
    
    [SerializeField] 
    private TMP_Text pauseLevelText; // Text-Object "level" in pause screen
    
    [SerializeField] 
    private TMP_Text gameOverLevelText; // Text-Object "level" in gameOver screen
    
    [SerializeField] 
    private GameObject miniMapPanel;  // Reference to the mini map

    private bool _gameOverShown = false;
    
    private bool isCombatLocked = false;
    
    // Toggle bools for UIs
    bool isPauseVisible = false;
    
    // Player reference
    private GameObject _player;
    
    // Inventory bool
    bool isInvVisible = false;

    // Reference to important onjects
    private GameObject itemUI;
    
    // Inventory manager
    private InventoryManager inventoryManager;

    /// <summary>
    /// Ensures there is only one instance of the UIManager in the scene.
    /// If another instance exists, it will be destroyed to maintain the singleton pattern.
    /// Persists this instance across scenes to provide consistent UI management.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy the duplicate UIManager if one already exists.
            return;
        }

        Instance = this; // Assign this instance as the singleton.

        //DontDestroyOnLoad(gameObject); // Ensure this GameObject persists across scenes.
        
        inventoryManager = GetComponentInChildren<InventoryManager>(true);
    }

    // <summary>
    /// Sets the player reference.
    /// </summary>
    public void SetPlayer(GameObject newPlayer)
    {
        _player = newPlayer;
        
        if (inventoryManager != null)
        {
            inventoryManager.SetPlayer(newPlayer);
        }
    }
    
    private void Start()
    {
        itemUI = GameObject.Find("UIManager").transform.Find("Inventory").transform.Find("Items").gameObject;
    }

    /// <summary>
    /// Updates the text on the UI panel and makes the panel visible.
    /// </summary>
    /// <param name="newText">The new text to display on the UI panel.</param>
    public void ShowPanel(string newText)
    {
        if (tmpText == null) // Check if the TMP_Text component is assigned.
        {
            Debug.LogError("TMP_Text component not found on GameObject with tag 'UI'!"); // Log an error if TMP_Text is missing.
            return;
        }

        tmpText.text = newText; // Update the text content of the TMP_Text component.
        tmpText.gameObject.SetActive(true); // Make the TMP_Text GameObject (UI panel) visible.
    }

    /// <summary>
    /// Hides the UI panel by deactivating the GameObject that contains the TMP_Text component.
    /// </summary>
    public void HidePanel()
    {
        tmpText.gameObject.SetActive(false); // Deactivate the TMP_Text GameObject to hide the panel.
    }

    /// <summary>
    /// Method called every Frame
    /// </summary>
    private void Update()
    {
        // Game over logic
        // Check if the player is dead
        if (_player.GetComponent<Stats>().GetCurStats(0) < 1)
        {
            // Pause the game
            _player.GetComponent<FirstPersonPlayerController>().enabled = false;
            _player.GetComponent<PlayerShooting>().enabled = false;
            
            // Lock the cursor in the game view
            HidePanel();
            
            if (!_gameOverShown)
            {
                if (gameOverLevelText is null && gameOver is not null)
                {
                    var t = gameOver.transform.Find("Level");
                    if (t is not null) gameOverLevelText = t.GetComponent<TMP_Text>();
                }

                if (gameOverLevelText is not null)
                    gameOverLevelText.text = $"Level: {SaveSystemManager.GetLevel()}";

                _gameOverShown = true;
            }
            
            gameOver.gameObject.SetActive(true);
            GameInputManager.Instance.MouseLocked(false);

            // Pause the game time
            Time.timeScale = 0;
        }
        else if (!isPauseVisible && !isInvVisible) // only resume if not paused or in inventory
        {
            _gameOverShown = false;
            
            // Resume the game time
            Time.timeScale = 1;
            
            // Unlock the cursor
            _player.GetComponent<FirstPersonPlayerController>().enabled = true;
            _player.GetComponent<PlayerShooting>().enabled = true;
        }
        
        //Check weather "P" is pressed to toggle the pause menu
        if (Input.GetKeyDown(KeyCode.P) && !isCombatLocked)
        {
            if (isPauseVisible)
            {
                //player.GetComponent<FirstPersonPlayerController>().enabled = true;
                _player.GetComponent<FirstPersonPlayerController>().enabled = true;
                _player.GetComponent<PlayerShooting>().enabled = true;
                //_player.SetActive(true);
                pause.gameObject.SetActive(false);

                //Lock Cursor in the game view
                GameInputManager.Instance.MouseLocked(true);

                //Resume time to normal value
                Time.timeScale = 1;
                isPauseVisible=false;
            }
            else
            {
                //player.GetComponent<FirstPersonPlayerController>().enabled = false;
                //_player.SetActive(false);
                _player.GetComponent<FirstPersonPlayerController>().enabled = false;
                _player.GetComponent<PlayerShooting>().enabled = false;
                HidePanel();
                
                // Set level text on pause menu
                if (pauseLevelText is null && pause is not null)
                {
                    var t = pause.transform.Find("Level");
                    if (t is not null) pauseLevelText = t.GetComponent<TMP_Text>();
                }
                if (pauseLevelText is not null)
                    pauseLevelText.text = $"Level: {SaveSystemManager.GetLevel()}";
                
                pause.gameObject.SetActive(true);

                //Make the cursor moveable within the game window
                GameInputManager.Instance.MouseLocked(false);

                //Pause the game
                Time.timeScale = 0;
                isPauseVisible=true;
            }
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!isPauseVisible)
            {
                if (!isInvVisible)
                {
                    HidePanel();

                    _player.GetComponent<FirstPersonPlayerController>().enabled = false;
                    _player.GetComponent<PlayerShooting>().enabled = false;

                    itemUI.transform.parent.gameObject.SetActive(true);

                    //Make the cursor moveable within the game window
                    GameInputManager.Instance.MouseLocked(false);

                    //Pause the game
                    Time.timeScale = 0;

                    isInvVisible = true;
                }
                else
                {

                    _player.GetComponent<FirstPersonPlayerController>().enabled = true;
                    _player.GetComponent<PlayerShooting>().enabled = true;

                    itemUI.transform.parent.gameObject.SetActive(false);

                    //Make the cursor unmoveable within the game window
                    GameInputManager.Instance.MouseLocked(true);

                    //Pause the game
                    Time.timeScale = 1;

                    isInvVisible = false;
                }
            }
        }
        
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (miniMapPanel is null) return;
            bool isActive = miniMapPanel.activeSelf;
            miniMapPanel.SetActive(!isActive);
        }
    }
    
    private void OnEnable()
    {
        EventManager.OnCloseDoors     += HandleCloseDoors;      // Enemy Room: Battle begins
        EventManager.OnOpenDoors      += HandleOpenDoors;       // Enemy Room: Fight over
        EventManager.OnCloseBossDoors += HandleCloseBossDoors;  // Boss Room: Battle begins
        EventManager.OnOpenBossDoors  += HandleOpenBossDoors;   // Boss Room: Fight over
    }

    private void OnDisable()
    {
        EventManager.OnCloseDoors     -= HandleCloseDoors;
        EventManager.OnOpenDoors      -= HandleOpenDoors;
        EventManager.OnCloseBossDoors -= HandleCloseBossDoors;
        EventManager.OnOpenBossDoors  -= HandleOpenBossDoors;
    }
    
    private void HandleCloseDoors()     => EnterCombatLock();
    private void HandleOpenDoors()      => ExitCombatLock();
    private void HandleCloseBossDoors() => EnterCombatLock();
    private void HandleOpenBossDoors()  => ExitCombatLock();

    private void EnterCombatLock()
    {
        isCombatLocked = true;

        if (!isPauseVisible) return;
        pause.gameObject.SetActive(false);
        GameInputManager.Instance.MouseLocked(true);
        Time.timeScale = 1;
        isPauseVisible = false;
    }

    private void ExitCombatLock()
    {
        isCombatLocked = false;
    }
}
