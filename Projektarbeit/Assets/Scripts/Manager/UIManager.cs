using Controller;
using Saving;
using Shooting;
using TMPro;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// Central UI controller for panels, overlays, and text.
    /// Handles pause, inventory, mini-map, and game-over screens,
    /// and enables/disables player controls as needed.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the UIManager, ensuring there is only one instance in the scene.
        /// Provides global access to UI management functionality.
        /// </summary>
        public static UIManager Instance { get; private set; }

        /// <summary>
        /// Text element used for small on-screen messages.
        /// </summary>
        [SerializeField]
        private TMP_Text tmpText;

        /// <summary>
        /// Pause menu root.
        /// </summary>
        [SerializeField]
        private RectTransform pause;
    
        /// <summary>
        /// Game-over screen root.
        /// </summary>
        [SerializeField]
        private RectTransform gameOver;
    
        /// <summary>
        /// Level text on the pause menu.
        /// </summary>
        [SerializeField] 
        private TMP_Text pauseLevelText;
    
        /// <summary>
        /// Level text on the game-over screen.
        /// </summary>
        [SerializeField] 
        private TMP_Text gameOverLevelText;
    
        /// <summary>
        /// Mini-map panel root.
        /// </summary>
        [SerializeField] 
        private GameObject miniMapPanel;

        /// <summary>
        /// Internal flag to print game-over info only once.
        /// </summary>
        private bool _gameOverShown;
    
        /// <summary>
        /// True while combat locks out pause/inventory.
        /// </summary>
        private bool _isCombatLocked;
        
        /// <summary>
        /// True while the pause menu is visible.
        /// </summary>
        private bool _isPauseVisible;
    
        /// <summary>
        /// Cached player reference.
        /// </summary>
        private GameObject _player;
    
        /// <summary>
        /// Cached movement controller on the player.
        /// </summary>
        private FirstPersonPlayerController _controller;
    
        /// <summary>
        /// Cached shooting component on the player.
        /// </summary>
        private PlayerShooting _shooter;
        
        /// <summary>
        /// True while the inventory UI is visible.
        /// </summary>
        private bool _isInvVisible;
        
        /// <summary>
        /// Reference to the inventory items grid object.
        /// </summary>
        private GameObject _itemUI;
        
        /// <summary>
        /// Inventory UI logic component.
        /// </summary>
        private InventoryManager _inventoryManager;

        /// <summary>
        /// Ensures there is only one instance of the UIManager in the scene.
        /// If another instance exists, it will be destroyed to maintain the singleton pattern.
        /// Persists this instance across scenes to provide consistent UI management.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        
            _inventoryManager = GetComponentInChildren<InventoryManager>(true);
        }

        /// <summary>
        /// Assigns the player reference and caches important components.
        /// </summary>
        /// <param name="newPlayer">The spawned player object.</param>
        public void SetPlayer(GameObject newPlayer)
        {
            _player = newPlayer;
        
            _controller = newPlayer.GetComponent<FirstPersonPlayerController>();
            _shooter    = newPlayer.GetComponent<PlayerShooting>();
        
            if (_inventoryManager != null)
            {
                _inventoryManager.SetPlayer(newPlayer);
            }
        }
    
        /// <summary>
        /// Finds the inventory grid under the UI hierarchy.
        /// </summary>
        private void Start()
        {
            _itemUI = GameObject.Find("UIManager").transform.Find("Inventory").transform.Find("Items").gameObject;
        }

        /// <summary>
        /// Updates the text on the UI panel and makes the panel visible.
        /// </summary>
        /// <param name="newText">The new text to display on the UI panel.</param>
        public void ShowPanel(string newText)
        {
            if (tmpText == null)
            {
                Debug.LogError("TMP_Text component not found on GameObject with tag 'UI'!");
                return;
            }

            tmpText.text = newText;
            tmpText.gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the UI panel by deactivating the GameObject that contains the TMP_Text component.
        /// </summary>
        public void HidePanel()
        {
            tmpText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Per-frame UI/state handling:
        /// - Game-over when health &lt; 1
        /// - Toggle pause with P
        /// - Toggle inventory with I
        /// - Toggle mini-map with M
        /// </summary>
        private void Update()
        {
            if (_player is null) return;
        
            // Game over logic
            // Check if the player is dead
            if (_player.GetComponent<Stats>().GetCurStats(0) < 1)
            {
                // Pause the game
                DisableGameplay();
            
                // Lock the cursor in the game view
                HidePanel();
                CloseAllUIExcept(null);
            
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
                return;
            }

            if (!_isPauseVisible && !_isInvVisible) // only resume if not paused or in inventory
            {
                _gameOverShown = false;
            
                // Resume the game time
                Time.timeScale = 1;
            
                // Unlock the cursor
                EnableGameplay();
            }

            //Check weather "P" is pressed to toggle the pause menu
            if (Input.GetKeyDown(KeyCode.P) && !_isCombatLocked)
            {
                if (_isPauseVisible)
                {
                    //_player.SetActive(true);
                    pause.gameObject.SetActive(false);
                
                    //Lock Cursor in the game view
                    GameInputManager.Instance.MouseLocked(true);
                
                    //Resume time to normal value
                    Time.timeScale = 1;
                
                    EnableGameplay();
                
                    _isPauseVisible=false;
                }
                else
                {
                    // Only one overlay at a time
                    CloseAllUIExcept("pause");
                
                    // Set level text on the pause menu
                    if (pauseLevelText is null && pause is not null)
                    {
                        var t = pause.transform.Find("Level");
                        if (t is not null) pauseLevelText = t.GetComponent<TMP_Text>();
                    }
                    if (pauseLevelText is not null)
                        pauseLevelText.text = $"Level: {SaveSystemManager.GetLevel()}";
                
                    DisableGameplay();
                    HidePanel();
                
                    pause.gameObject.SetActive(true);

                    //Make the cursor moveable within the game window
                    GameInputManager.Instance.MouseLocked(false);

                    //Pause the game
                    Time.timeScale = 0;
                    _isPauseVisible=true;
                }
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                if (_isPauseVisible) return;
                if (!_isInvVisible)
                {
                    HidePanel();

                    DisableGameplay();
                
                    CloseAllUIExcept("inventory");

                    _itemUI.transform.parent.gameObject.SetActive(true);

                    //Make the cursor moveable within the game window
                    GameInputManager.Instance.MouseLocked(false);

                    //Pause the game
                    Time.timeScale = 0;

                    _isInvVisible = true;
                }
                else
                {
                    EnableGameplay();

                    _itemUI.transform.parent.gameObject.SetActive(false);

                    //Make the cursor unmoveable within the game window
                    GameInputManager.Instance.MouseLocked(true);

                    //Pause the game
                    Time.timeScale = 1;

                    _isInvVisible = false;
                }
            }
        
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (miniMapPanel is null) return;
            
                // don't allow the map while game over or pause
                if ((gameOver is not null && gameOver.gameObject.activeSelf) || _isPauseVisible) return;
            
                var isActive = miniMapPanel.activeSelf;
            
                if (!isActive)
                {
                    // only one overlay at a time
                    CloseAllUIExcept("map");

                    // Ensure gameplay is running (the map does not pause)
                    ResumeGameplayIfNotGameOver();

                    miniMapPanel.SetActive(true);
                }
                else
                {
                    miniMapPanel.SetActive(false);
                }
            }
        }
    
        /// <summary>
        /// Subscribes to door events to lock/unlock combat overlays.
        /// </summary>
        private void OnEnable()
        {
            EventManager.OnCloseDoors     += HandleCloseDoors;      // Enemy Room: Battle begins
            EventManager.OnOpenDoors      += HandleOpenDoors;       // Enemy Room: Fight over
            EventManager.OnCloseBossDoors += HandleCloseBossDoors;  // Boss Room: Battle begins
            EventManager.OnOpenBossDoors  += HandleOpenBossDoors;   // Boss Room: Fight over
        }

        /// <summary>
        /// Unsubscribes from door events.
        /// </summary>
        private void OnDisable()
        {
            EventManager.OnCloseDoors     -= HandleCloseDoors;
            EventManager.OnOpenDoors      -= HandleOpenDoors;
            EventManager.OnCloseBossDoors -= HandleCloseBossDoors;
            EventManager.OnOpenBossDoors  -= HandleOpenBossDoors;
        }
    
        /// <summary>
        /// Called when regular doors close (combat start).
        /// </summary>
        private void HandleCloseDoors()     => EnterCombatLock();
        
        /// <summary>
        /// Called when regular doors open (combat end).
        /// </summary>
        private void HandleOpenDoors()      => ExitCombatLock();
        
        /// <summary>
        /// Called when boss doors close (boss start).
        /// </summary>
        private void HandleCloseBossDoors() => EnterCombatLock();
        
        /// <summary>
        /// Called when boss doors open (boss end).
        /// </summary>
        private void HandleOpenBossDoors()  => ExitCombatLock();

        /// <summary>
        /// Enables combat lock. Closes pause if it is open.
        /// </summary>
        private void EnterCombatLock()
        {
            _isCombatLocked = true;

            if (!_isPauseVisible) return;
            pause.gameObject.SetActive(false);
            GameInputManager.Instance.MouseLocked(true);
            Time.timeScale = 1;
            _isPauseVisible = false;
        }

        /// <summary>
        /// Disables combat lock.
        /// </summary>
        private void ExitCombatLock()
        {
            _isCombatLocked = false;
        }
    
        /// <summary>
        /// Closes all overlays except the named one.
        /// </summary>
        /// <param name="except">
        /// Overlay to keep open: "pause", "inventory", or "map".
        /// Use <c>null</c> to close all.
        /// </param>
        private void CloseAllUIExcept(string except)
        {
            if (except != "pause" && pause != null)
            {
                pause.gameObject.SetActive(false);
                _isPauseVisible = false;
            }
            if (except != "inventory" && _itemUI != null)
            {
                _itemUI.transform.parent.gameObject.SetActive(false);
                _isInvVisible = false;
            }
            if (except != "map" && miniMapPanel != null)
            {
                miniMapPanel.SetActive(false);
            }
        }
    
        /// <summary>
        /// Ensures gameplay is running (if not in game-over).
        /// </summary>
        private void ResumeGameplayIfNotGameOver()
        {
            if (gameOver != null && gameOver.gameObject.activeSelf) return;

            Time.timeScale = 1;
            EnableGameplay();
            GameInputManager.Instance.MouseLocked(true);
        }
    
        /// <summary>
        /// Turns on movement and shooting controls (if present).
        /// </summary>
        private void EnableGameplay()
        {
            if (_controller != null) _controller.enabled = true;
            if (_shooter    != null) _shooter.enabled    = true;
        }

        /// <summary>
        /// Turns off movement and shooting controls (if present).
        /// </summary>
        private void DisableGameplay()
        {
            if (_controller != null) _controller.enabled = false;
            if (_shooter    != null) _shooter.enabled    = false;
        }
    }
}
