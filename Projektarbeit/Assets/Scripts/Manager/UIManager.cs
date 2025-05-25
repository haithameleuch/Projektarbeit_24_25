using TMPro; // Import TextMeshPro namespace for using TMP_Text components
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Manages the UI elements, including panels and text updates.
/// Provides methods for showing and hiding UI panels, and updates text dynamically.
/// </summary>
public class UIManager : MonoBehaviour, IPointerClickHandler
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

    // Toggle bools for UIs
    bool isPauseVisible = false;
    bool isInvVisible = false;

    // Reference to important onjects
    private GameObject player;
    private ItemInstance[,] playerInventory;
    private ItemInstance[,] playerEquipment;
    private GameObject itemUI;
    private GameObject equipUI;
    private TMP_Text statText;
    private (int, int) selectedSlot = (-1, -1);

    // UI-Prefabs
    [SerializeField]
    private GameObject rowPrefab;
    [SerializeField]
    private GameObject slotPrefab;
    [SerializeField]
    private GameObject emptyPrefab;

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

        DontDestroyOnLoad(gameObject); // Ensure this GameObject persists across scenes.
    }

    private void Start()
    {
        player = GameObject.Find("Player");
        playerInventory = player.GetComponent<Inventory>().getInventory();
        playerEquipment = player.GetComponent<Inventory>().getEquipment();
        itemUI = GameObject.Find("UIManager").transform.Find("Inventory").transform.Find("Items").gameObject;
        equipUI = GameObject.Find("UIManager").transform.Find("Inventory").transform.Find("Equipment").transform.Find("EquipmentSlots").gameObject;
        statText = GameObject.Find("UIManager").transform.Find("Inventory").transform.Find("Equipment").transform.Find("DetailPanel").transform.Find("StatDetails").gameObject.GetComponent<TMP_Text>();
        setupUI();
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
        //Check wether "P" is pressed to toggle the pause menu
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (!isInvVisible)
            {
                if (isPauseVisible)
                {
                    player.GetComponent<FirstPersonPlayerController>().enabled = true;
                    player.GetComponent<PlayerShooting>().enabled = true;

                    pause.gameObject.SetActive(false);
                    updateUI();

                    //Lock Cursor in the game view
                    UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                    UnityEngine.Cursor.visible = false;

                    //Resume time to normal value
                    Time.timeScale = 1;
                    isPauseVisible = false;
                }
                else
                {
                    player.GetComponent<FirstPersonPlayerController>().enabled = false;
                    player.GetComponent<PlayerShooting>().enabled = false;

                    HidePanel();
                    pause.gameObject.SetActive(true);

                    //Make the cursor moveable within the game window
                    UnityEngine.Cursor.lockState = CursorLockMode.Confined;
                    UnityEngine.Cursor.visible = true;

                    //Pause the game
                    Time.timeScale = 0;
                    isPauseVisible = true;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!isPauseVisible)
            {
                if (!isInvVisible)
                {
                    HidePanel();

                    player.GetComponent<FirstPersonPlayerController>().enabled = false;
                    player.GetComponent<PlayerShooting>().enabled = false;

                    updateUI();
                    itemUI.transform.parent.gameObject.SetActive(true);

                    //Make the cursor moveable within the game window
                    UnityEngine.Cursor.lockState = CursorLockMode.Confined;
                    UnityEngine.Cursor.visible = true;

                    //Pause the game
                    Time.timeScale = 0;

                    isInvVisible = true;
                }
                else
                {

                    player.GetComponent<FirstPersonPlayerController>().enabled = true;
                    player.GetComponent<PlayerShooting>().enabled = true;

                    itemUI.transform.parent.gameObject.SetActive(false);

                    //Make the cursor unmoveable within the game window
                    UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                    UnityEngine.Cursor.visible = false;

                    //Pause the game
                    Time.timeScale = 1;

                    isInvVisible = false;
                }
            }
        }
        
        if (selectedSlot.Item1 != -1)
        {
            if (isInvVisible) {
                if (Input.GetKeyDown(KeyCode.E)) 
                {
                    if (selectedSlot.Item1 < 4)
                    {
                        player.GetComponent<Inventory>().removeItem(selectedSlot.Item1, selectedSlot.Item2);
                    }
                    else
                    {
                        player.GetComponent<Inventory>().removeEquip(selectedSlot.Item1 - 4, selectedSlot.Item2);
                    }
                    updateUI();
                }
                if (Input.GetKeyDown(KeyCode.O))
                {
                    if (selectedSlot.Item1 < 4) {
                        player.GetComponent<Inventory>().removeItem(selectedSlot.Item1, selectedSlot.Item2);
                    }
                    else
                    {
                        player.GetComponent<Inventory>().removeEquip(selectedSlot.Item1 - 4, selectedSlot.Item2);
                    }
                    updateUI();
                }
            }
        }
    }

    public void setupUI()
    {
        // Create the item UI
        for (int i = 0; i < playerInventory.GetLength(0); i++)
        {
            // Create a new row
            GameObject row = Instantiate(rowPrefab,itemUI.transform);

            // Create the slots of a row
            for(int j = 0; j < playerInventory.GetLength(1); j++)
            {
                GameObject slot = Instantiate(slotPrefab, row.transform);
                slot.transform.Find("Icon").gameObject.GetComponent<Image>().enabled = false;
                slot.transform.Find("Name").gameObject.transform.Find("Name").GetComponent<TMP_Text>().SetText("");
                slot.GetComponent<ItemSlotNumber>().row = i;
                slot.GetComponent<ItemSlotNumber>().col = j;
            }
        }

        // Create the equip UI
        for (int i = 0; i < playerEquipment.GetLength(0); i++)
        {
            // Create a new row
            GameObject row = Instantiate(rowPrefab, equipUI.transform);

            // Create the slots of a row
            for (int j = 0; j < playerEquipment.GetLength(1); j++)
            {
                GameObject slot = Instantiate(slotPrefab, row.transform);
                slot.transform.Find("Icon").gameObject.GetComponent<Image>().enabled = false;
                slot.transform.Find("Name").gameObject.transform.Find("Name").GetComponent<TMP_Text>().SetText("");
                slot.GetComponent<ItemSlotNumber>().row = i+4;
                slot.GetComponent<ItemSlotNumber>().col = j;
            }
        }

        // Set stat Text
        statText.SetText("Health:10\nDamage:10\nSpeed:10");
    }

    private void updateUI()
    {
        // Set the info of the items
        for (int i = 0; i < playerInventory.GetLength(0); i++)
        {
            for(int j = 0;j < playerInventory.GetLength(1); j++)
            {
                if (playerInventory[i,j] != null)
                {
                    // Set Icon
                    itemUI.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<Image>().sprite = playerInventory[i,j].itemData.spawnSprite;
                    itemUI.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<Image>().enabled = true;

                    // Set Name and Quantity
                    itemUI.transform.GetChild(i).GetChild(j).GetChild(1).GetChild(0).GetComponent<TMP_Text>()
                        .SetText(playerInventory[i,j].itemData.name + " (x"+ playerInventory[i,j].itemQuantity+")");
                }
                else
                {
                    // Set Icon
                    itemUI.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<Image>().enabled = false;
                    // Set Name and Quantity
                    itemUI.transform.GetChild(i).GetChild(j).GetChild(1).GetChild(0).GetComponent<TMP_Text>()
                        .SetText("");
                }
            }
        }

        // Set Equipment slots
        for (int i = 0;i < playerEquipment.GetLength(0); i++)
        {
            for( int j = 0; j < playerEquipment.GetLength(1); j++)
            {
                if (playerEquipment[i, j] != null)
                {
                    // Set Icon
                    equipUI.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<Image>().sprite = playerInventory[i, j].itemData.spawnSprite;
                    equipUI.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<Image>().enabled = true;

                    // Set Name and Quantity
                    equipUI.transform.GetChild(i).GetChild(j).GetChild(1).GetChild(0).GetComponent<TMP_Text>()
                        .SetText(playerInventory[i, j].itemData.name + " (x" + playerInventory[i, j].itemQuantity + ")");
                }
                else
                {
                    // Set Icon
                    equipUI.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<Image>().enabled = false;
                    // Set Name and Quantity
                    equipUI.transform.GetChild(i).GetChild(j).GetChild(1).GetChild(0).GetComponent<TMP_Text>()
                        .SetText("");
                }
            }
        }

        // Set new Stat text
        // Todo calculate
        statText.SetText("Health:10\nDamage:10\nSpeed:10");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (selectedSlot.Item1 != -1)
            {
                if (selectedSlot.Item1 < 4)
                {
                    itemUI.transform.GetChild(selectedSlot.Item1).GetChild(selectedSlot.Item2).GetComponent<Image>().color = new Color32(0x00, 0x8E, 0x11, 100);
                }
                else
                {
                    equipUI.transform.GetChild(selectedSlot.Item1-4).GetChild(selectedSlot.Item2).GetComponent<Image>().color = new Color32(0x00, 0x8E, 0x11, 100);
                }
            }

            GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
            if (clickedObject.name == "ItemSlot(Clone)")
            {
                clickedObject.GetComponent<Image>().color = new Color32(0x28, 0xB6, 0x39, 100);
                selectedSlot = (clickedObject.GetComponent<ItemSlotNumber>().row, clickedObject.GetComponent<ItemSlotNumber>().col);
            }
            else
            {
                selectedSlot = (-1,-1);
            }
        }
    }
}
