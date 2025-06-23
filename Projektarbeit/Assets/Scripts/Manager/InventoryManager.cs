using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Manages the inventory UI and logic, including displaying and updating item slots,
/// handling user interactions, and connecting to the player's inventory system.
/// </summary>
public class InventoryManager : MonoBehaviour, IPointerClickHandler
{
    /// <summary>
    /// Reference to the player GameObject.
    /// </summary>
    private GameObject _player;

    /// <summary>
    /// 2D array storing item inventory.
    /// </summary>
    private ItemStack[,] _playerInventory;
    // private ItemInstance[,] playerInventory;

    /// <summary>
    /// 2D array storing equipped items.
    /// </summary>
    private ItemStack[,] _playerEquipment;
    // private ItemInstance[,] playerEquipment;
    
    /// <summary>
    /// Cached reference to the player's inventory component.
    /// </summary>
    private Inventory_V3 _inventory;

    /// <summary>
    /// Reference to the item UI panel.
    /// </summary>
    [SerializeField] private GameObject itemUI;
    
    /// <summary>
    /// Reference to the equipment UI panel.
    /// </summary>
    [SerializeField] private GameObject equipUI;
    
    /// <summary>
    /// Reference to the stat text field.
    /// </summary>
    [SerializeField] private TMP_Text statText;
    
    /// <summary>
    /// Currently selected slot (row, column).
    /// </summary>
    private (int, int) _selectedSlot = (-1, -1);

    /// <summary>
    /// Placeholder icons for each equipment slot.
    /// </summary>
    [SerializeField] private Sprite helmet;
    [SerializeField] private Sprite body;
    [SerializeField] private Sprite legs;
    [SerializeField] private Sprite boots;
    [SerializeField] private Sprite rightHand;
    [SerializeField] private Sprite leftHand;
    
    /// <summary>
    /// Internal dummy sprite matrix used to initialize equipment slots.
    /// </summary>
    private readonly Sprite[,] _dummies = new Sprite[3, 2];

    /// <summary>
    /// Row prefab used for dynamically creating rows in the UI.
    /// </summary>
    [SerializeField] private GameObject rowPrefab;
    
    /// <summary>
    /// Slot prefab used for dynamically creating item slots.
    /// </summary>
    [SerializeField] private GameObject slotPrefab;

    /// <summary>
    /// Indicates whether the inventory has already been initialized.
    /// </summary>
    private bool _isSetup;
    
    /// <summary>
    /// Receives the player GameObject reference from the UIManager.
    /// </summary>
    /// <param name="newPlayer">The player GameObject.</param>
    public void SetPlayer(GameObject newPlayer)
    {
        _player = newPlayer;
        _inventory = _player.GetComponent<Inventory_V3>();
    }
    
    /// <summary>
    /// Initializes the inventory UI when the object is enabled and player is available.
    /// </summary>
    private void OnEnable()
    {
        if (_isSetup || _player == null) return;
        
        _playerInventory = _inventory.getInventory();
        _playerEquipment = _inventory.getEquipment();
        
        _dummies[0, 0] = helmet;
        _dummies[0, 1] = body;
        _dummies[1, 0] = legs;
        _dummies[1, 1] = boots;
        _dummies[2, 0] = rightHand;
        _dummies[2, 1] = leftHand;

        SetupUI();
        _isSetup = true;
    }
    
    /// <summary>
    /// Handles inventory logic such as using or removing items based on player input.
    /// </summary>
    private void Update()
    {
        if (!_isSetup) return;
        
        UpdateUI();
        
        if (_selectedSlot.Item1 != -1)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (_selectedSlot.Item1 < 4)
                {
                    if (_playerInventory[_selectedSlot.Item1, _selectedSlot.Item2] != null)
                    {
                        _inventory.useItem(_selectedSlot.Item1, _selectedSlot.Item2);
                    }

                }
                else
                {
                    if (_playerEquipment[_selectedSlot.Item1 - 4, _selectedSlot.Item2] != null)
                    {
                        _inventory.useItem(_selectedSlot.Item1, _selectedSlot.Item2);
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                if (_selectedSlot.Item1 < 4)
                {
                    _inventory.removeItem(_selectedSlot.Item1, _selectedSlot.Item2);
                }
                else
                {
                    _inventory.removeEquip(_selectedSlot.Item1 - 4, _selectedSlot.Item2);
                }
            }
        }
    }

    /// <summary>
    /// Instantiates all item and equipment UI slots dynamically.
    /// </summary>
    private void SetupUI()
    {
        // Create the item UI
        for (var i = 0; i < _playerInventory.GetLength(0); i++)
        {
            // Create a new row
            GameObject row = Instantiate(rowPrefab, itemUI.transform);

            // Create the slots of a row
            for (var j = 0; j < _playerInventory.GetLength(1); j++)
            {
                GameObject slot = Instantiate(slotPrefab, row.transform);
                slot.transform.Find("Icon").gameObject.GetComponent<Image>().enabled = false;
                slot.transform.Find("Name").gameObject.transform.Find("Name").GetComponent<TMP_Text>().SetText("");
                slot.GetComponent<ItemSlotNumber>().row = i;
                slot.GetComponent<ItemSlotNumber>().col = j;
            }
        }

        // Create equip UI
        for (var i = 0; i < _playerEquipment.GetLength(0); i++)
        {
            // Create a new row
            GameObject row = Instantiate(rowPrefab, equipUI.transform);

            // Create the slots of a row
            for (var j = 0; j < _playerEquipment.GetLength(1); j++)
            {
                GameObject slot = Instantiate(slotPrefab, row.transform);
                slot.transform.Find("Icon").gameObject.GetComponent<Image>().sprite = _dummies[i, j];
                slot.transform.Find("Name").gameObject.transform.Find("Name").GetComponent<TMP_Text>().SetText("");
                slot.GetComponent<ItemSlotNumber>().row = i + 4;
                slot.GetComponent<ItemSlotNumber>().col = j;
            }
        }

        // Set stat Text
        statText.SetText("Health:10\nDamage:10\nSpeed:10");
    }

    /// <summary>
    /// Updates the inventory and equipment UI slots with current item data.
    /// </summary>
    private void UpdateUI()
    {
        // Set the info of the items
        for (int i = 0; i < _playerInventory.GetLength(0); i++)
        {
            for (int j = 0; j < _playerInventory.GetLength(1); j++)
            {
                var slot = itemUI.transform.GetChild(i).GetChild(j);
                
                if (_playerInventory[i, j] != null)
                {
                    // Set Background
                    slot.GetComponent<Image>().color = _playerInventory[i, j].item.getRarityColor();
                    // Set Icon
                    slot.GetChild(0).GetComponent<Image>().sprite = _playerInventory[i, j].item.item_icon;
                    slot.GetChild(0).GetComponent<Image>().enabled = true;

                    // Set Name and Quantity
                    slot.GetChild(1).GetChild(0).GetComponent<TMP_Text>()
                        .SetText(_playerInventory[i, j].item._name + " (x" + _playerInventory[i, j].amount + ")");
                }
                else
                {
                    // Disabled background
                    slot.GetComponent<Image>().color = new Color32(125, 125, 125, 100);
                    // Set Icon
                    slot.GetChild(0).GetComponent<Image>().enabled = false;
                    // Set Name and Quantity
                    slot.GetChild(1).GetChild(0).GetComponent<TMP_Text>()
                        .SetText("");
                }
            }
        }

        // Set Equipment slots
        for (int i = 0; i < _playerEquipment.GetLength(0); i++)
        {
            for (int j = 0; j < _playerEquipment.GetLength(1); j++)
            {
                var slot = equipUI.transform.GetChild(i).GetChild(j);
                
                if (_playerEquipment[i, j] != null)
                {
                    // Set Icon
                    slot.GetChild(0).GetComponent<Image>().sprite = _playerEquipment[i, j].item.item_icon;
                    slot.GetChild(0).GetComponent<Image>().enabled = true;

                    slot.GetComponent<Image>().color = _playerEquipment[i, j].item.getRarityColor();

                    // Set Name and Quantity
                    slot.GetChild(1).GetChild(0).GetComponent<TMP_Text>()
                        .SetText(_playerEquipment[i, j].item._name + " (x" + _playerEquipment[i, j].amount + ")");
                }
                else
                {
                    // Disabled background
                    slot.GetComponent<Image>().color = new Color32(125, 125, 125, 100);
                    // Set Icon
                    slot.GetChild(0).GetComponent<Image>().sprite = _dummies[i, j];
                    // Set Name and Quantity
                    slot.GetChild(1).GetChild(0).GetComponent<TMP_Text>()
                        .SetText("");
                }
            }
        }
        if (_selectedSlot.Item1 > -1)
        {
            if (_selectedSlot.Item1 < 4)
            {
                Color32 tmp = itemUI.transform.GetChild(_selectedSlot.Item1).GetChild(_selectedSlot.Item2).GetComponent<Image>().color;
                tmp.a = 255;
                itemUI.transform.GetChild(_selectedSlot.Item1).GetChild(_selectedSlot.Item2).GetComponent<Image>().color = tmp;
            }
            else
            {
                Color32 tmp = equipUI.transform.GetChild(_selectedSlot.Item1 - 4).GetChild(_selectedSlot.Item2).GetComponent<Image>().color;
                tmp.a = 255;
                equipUI.transform.GetChild(_selectedSlot.Item1 - 4).GetChild(_selectedSlot.Item2).GetComponent<Image>().color = tmp;
            }
        }
        // Set new Stat text
        // Todo calculate
        statText.SetText("Health:10\nDamage:10\nSpeed:10");
    }

    /// <summary>
    /// Handles click events on inventory slots to select/deselect items.
    /// </summary>
    /// <param name="eventData">Pointer click event data.</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (_selectedSlot.Item1 != -1)
            {
                if (_selectedSlot.Item1 < 4)
                {
                    Color32 tmp = itemUI.transform.GetChild(_selectedSlot.Item1).GetChild(_selectedSlot.Item2).GetComponent<Image>().color;
                    tmp.a = 100;
                    itemUI.transform.GetChild(_selectedSlot.Item1).GetChild(_selectedSlot.Item2).GetComponent<Image>().color = tmp;
                }
                else
                {
                    Color32 tmp = equipUI.transform.GetChild(_selectedSlot.Item1 - 4).GetChild(_selectedSlot.Item2).GetComponent<Image>().color;
                    tmp.a = 100;
                    equipUI.transform.GetChild(_selectedSlot.Item1 - 4).GetChild(_selectedSlot.Item2).GetComponent<Image>().color = tmp;
                }
            }

            GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
            if (clickedObject.name == "ItemSlot(Clone)")
            {
                _selectedSlot = (clickedObject.GetComponent<ItemSlotNumber>().row, clickedObject.GetComponent<ItemSlotNumber>().col);
                Color32 tmp = clickedObject.GetComponent<Image>().color;
                tmp.a = 255;
                clickedObject.GetComponent<Image>().color = tmp;
            }
            else
            {
                _selectedSlot = (-1, -1);
            }
        }
    }
}
