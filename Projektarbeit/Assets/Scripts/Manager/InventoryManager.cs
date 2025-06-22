using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour, IPointerClickHandler
{
    // Reference to important onjects
    private GameObject player;
    //private ItemInstance[,] playerInventory;
    private ItemStack[,] playerInventory;
    //private ItemInstance[,] playerEquipment;
    private ItemStack[,] playerEquipment;
    private GameObject itemUI;
    private GameObject equipUI;
    private TMP_Text statText;
    private (int, int) selectedSlot = (-1, -1);

    // Dummy sprites for equipment
    [SerializeField] private Sprite helmet;
    [SerializeField] private Sprite body;
    [SerializeField] private Sprite legs;
    [SerializeField] private Sprite boots;
    [SerializeField] private Sprite rightHand;
    [SerializeField] private Sprite leftHand;
    private Sprite[,] dummies = new Sprite[3, 2];

    // UI-Prefabs
    [SerializeField]
    private GameObject rowPrefab;
    [SerializeField]
    private GameObject slotPrefab;

    private bool isSetup = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        equipUI = GameObject.Find("UIManager").transform.Find("Inventory").transform.Find("Equipment").transform.Find("EquipmentSlots").gameObject;
        itemUI = GameObject.Find("UIManager").transform.Find("Inventory").transform.Find("Items").gameObject;
        statText = GameObject.Find("UIManager").transform.Find("Inventory").transform.Find("Equipment").transform.Find("DetailPanel").transform.Find("StatDetails").gameObject.GetComponent<TMP_Text>();
        dummies[0, 0] = helmet;
        dummies[0, 1] = body;
        dummies[1, 0] = legs;
        dummies[1, 1] = boots;
        dummies[2, 0] = rightHand;
        dummies[2, 1] = leftHand;
    }
 
    // Update is called once per frame
    void Update()
    {
        if (!isSetup)
        {
            player = GameObject.Find("Player(Clone)");
            playerInventory = player.GetComponent<Inventory_V3>().getInventory();
            playerEquipment = player.GetComponent<Inventory_V3>().getEquipment();
            setupUI();
            isSetup = true;
        }
        updateUI();
        if (selectedSlot.Item1 != -1)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (selectedSlot.Item1 < 4)
                {
                    if (playerInventory[selectedSlot.Item1, selectedSlot.Item2] != null)
                    {
                        player.GetComponent<Inventory_V3>().useItem(selectedSlot.Item1, selectedSlot.Item2);
                    }

                }
                else
                {
                    if (playerEquipment[selectedSlot.Item1 - 4, selectedSlot.Item2] != null)
                    {
                        player.GetComponent<Inventory_V3>().useItem(selectedSlot.Item1, selectedSlot.Item2);
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                if (selectedSlot.Item1 < 4)
                {
                    player.GetComponent<Inventory_V3>().removeItem(selectedSlot.Item1, selectedSlot.Item2);
                }
                else
                {
                    player.GetComponent<Inventory_V3>().removeEquip(selectedSlot.Item1 - 4, selectedSlot.Item2);
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
            GameObject row = Instantiate(rowPrefab, itemUI.transform);

            // Create the slots of a row
            for (int j = 0; j < playerInventory.GetLength(1); j++)
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
                slot.transform.Find("Icon").gameObject.GetComponent<Image>().sprite = dummies[i, j];
                slot.transform.Find("Name").gameObject.transform.Find("Name").GetComponent<TMP_Text>().SetText("");
                slot.GetComponent<ItemSlotNumber>().row = i + 4;
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
            for (int j = 0; j < playerInventory.GetLength(1); j++)
            {
                if (playerInventory[i, j] != null)
                {
                    // Set Background
                    itemUI.transform.GetChild(i).GetChild(j).GetComponent<Image>().color = playerInventory[i, j].item.getRarityColor();
                    // Set Icon
                    itemUI.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<Image>().sprite = playerInventory[i, j].item.item_icon;
                    itemUI.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<Image>().enabled = true;

                    // Set Name and Quantity
                    itemUI.transform.GetChild(i).GetChild(j).GetChild(1).GetChild(0).GetComponent<TMP_Text>()
                        .SetText(playerInventory[i, j].item._name + " (x" + playerInventory[i, j].amount + ")");
                }
                else
                {
                    // Disabled background
                    itemUI.transform.GetChild(i).GetChild(j).GetComponent<Image>().color = new Color32(125, 125, 125, 100);
                    // Set Icon
                    itemUI.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<Image>().enabled = false;
                    // Set Name and Quantity
                    itemUI.transform.GetChild(i).GetChild(j).GetChild(1).GetChild(0).GetComponent<TMP_Text>()
                        .SetText("");
                }
            }
        }

        // Set Equipment slots
        for (int i = 0; i < playerEquipment.GetLength(0); i++)
        {
            for (int j = 0; j < playerEquipment.GetLength(1); j++)
            {
                if (playerEquipment[i, j] != null)
                {
                    // Set Icon
                    equipUI.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<Image>().sprite = playerEquipment[i, j].item.item_icon;
                    equipUI.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<Image>().enabled = true;

                    equipUI.transform.GetChild(i).GetChild(j).GetComponent<Image>().color = playerEquipment[i, j].item.getRarityColor();

                    // Set Name and Quantity
                    equipUI.transform.GetChild(i).GetChild(j).GetChild(1).GetChild(0).GetComponent<TMP_Text>()
                        .SetText(playerEquipment[i, j].item._name + " (x" + playerEquipment[i, j].amount + ")");
                }
                else
                {
                    // Disabled background
                    equipUI.transform.GetChild(i).GetChild(j).GetComponent<Image>().color = new Color32(125, 125, 125, 100);
                    // Set Icon
                    equipUI.transform.GetChild(i).GetChild(j).GetChild(0).GetComponent<Image>().sprite = dummies[i, j];
                    // Set Name and Quantity
                    equipUI.transform.GetChild(i).GetChild(j).GetChild(1).GetChild(0).GetComponent<TMP_Text>()
                        .SetText("");
                }
            }
        }
        if (selectedSlot.Item1 > -1)
        {
            if (selectedSlot.Item1 < 4)
            {
                Color32 tmp = itemUI.transform.GetChild(selectedSlot.Item1).GetChild(selectedSlot.Item2).GetComponent<Image>().color;
                tmp.a = (byte)255;
                itemUI.transform.GetChild(selectedSlot.Item1).GetChild(selectedSlot.Item2).GetComponent<Image>().color = tmp;
            }
            else
            {
                Color32 tmp = equipUI.transform.GetChild(selectedSlot.Item1 - 4).GetChild(selectedSlot.Item2).GetComponent<Image>().color;
                tmp.a = (byte)255;
                equipUI.transform.GetChild(selectedSlot.Item1 - 4).GetChild(selectedSlot.Item2).GetComponent<Image>().color = tmp;
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
                    Color32 tmp = itemUI.transform.GetChild(selectedSlot.Item1).GetChild(selectedSlot.Item2).GetComponent<Image>().color;
                    tmp.a = (byte)100;
                    itemUI.transform.GetChild(selectedSlot.Item1).GetChild(selectedSlot.Item2).GetComponent<Image>().color = tmp;
                }
                else
                {
                    Color32 tmp = equipUI.transform.GetChild(selectedSlot.Item1 - 4).GetChild(selectedSlot.Item2).GetComponent<Image>().color;
                    tmp.a = (byte)100;
                    equipUI.transform.GetChild(selectedSlot.Item1 - 4).GetChild(selectedSlot.Item2).GetComponent<Image>().color = tmp;
                }
            }

            GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
            if (clickedObject.name == "ItemSlot(Clone)")
            {
                selectedSlot = (clickedObject.GetComponent<ItemSlotNumber>().row, clickedObject.GetComponent<ItemSlotNumber>().col);
                Color32 tmp = clickedObject.GetComponent<Image>().color;
                tmp.a = (byte)255;
                clickedObject.GetComponent<Image>().color = tmp;
            }
            else
            {
                selectedSlot = (-1, -1);
            }
        }
    }
}
