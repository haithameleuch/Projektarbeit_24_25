using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// This class is used to show the players inventory to the screen using the ui toolkit
/// </summary>
public class InventoryUIV2 : MonoBehaviour
{
    //The inventory which will be shown
    [SerializeField]
    private Inventory inventoryManager;

    //The template used for the itemslots
    [SerializeField]
    private VisualTreeAsset itemTemplate;

    //Main container of the UI
    private VisualElement inventoryContainer;

    //The root Element (UI document)
    private VisualElement rootElement;

    private bool isUIVisible = false;

    //Currently selected Item


    /// <summary>
    /// Initialize the variables root and inventoryContainer for later use (Maybe OnCreate or smth would be better)
    /// </summary>
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        rootElement = root;
        rootElement.style.display = DisplayStyle.None;
        inventoryContainer = root.Q<VisualElement>("MainContainer");

    }

    /// <summary>
    /// This method updates the UI by removing everything and paint an instance of the slot-prefab with set values to the screen
    /// </summary>
    public void RefreshUI()
    {
        inventoryContainer.Clear();
        foreach (var item in inventoryManager.getInventory())
        {
            TemplateContainer itemElement = itemTemplate.CloneTree();
            itemElement.Q<Label>("Name").text = item.itemData.spawnName;
            itemElement.Q<VisualElement>("Top").style.backgroundImage = new StyleBackground(item.itemData.spawnSprite);
            itemElement.Q<Label>("ItemCount").text = "" + item.itemQuantity;
            itemElement.AddToClassList("TemplateContainer");
            inventoryContainer.Add(itemElement);
        }
    }

    /// <summary>
    /// This method is used to check for user-inputs and react to them
    /// </summary>
    private void Update()
    {
        //Check wether "i" is pressed to toggle the inventory
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleUIDocument();
        }
        //If the UI is visible check which button is pressed to interact with the item the mouse is hovering over
        if (isUIVisible)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                throwItemAway();
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                useItem();
            }
        }
    }

    /// <summary>
    /// This method activates and deactivates the inventory
    /// </summary>
    private void ToggleUIDocument()
    {
        // Umschalten der Sichtbarkeit
        if (isUIVisible)
        {
            //Disable UI
            rootElement.style.display = DisplayStyle.None;

            //Lock Cursor in the game view
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;

            //Resume time to normal value
            Time.timeScale = 1;
        }
        else
        {
            //Enable UI
            rootElement.style.display = DisplayStyle.Flex;

            RefreshUI();

            //Make the cursor moveable within the game window
            UnityEngine.Cursor.lockState = CursorLockMode.Confined;
            UnityEngine.Cursor.visible = true;

            //Pause the game
            Time.timeScale = 0;
        }

        //Save the current state of the UI
        isUIVisible = !isUIVisible;
    }

    /// <summary>
    /// This Method will handle the button press "q" to remove an item from the inventory.
    /// </summary>
    private void throwItemAway()
    {
        ItemInstance toRemove = getItemUnderMouse();
        if (!toRemove.itemData.spawnName.Equals(""))
        {
            inventoryManager.RemoveItem(toRemove, 1);
            RefreshUI();
        }    
    }

    private void useItem()
    {
        ItemInstance toUse = getItemUnderMouse();
        if (!toUse.itemData.spawnName.Equals(""))
        {
            //TODO code to use Item
            inventoryManager.RemoveItem(toUse, 1);
            RefreshUI();
        }
    }

    private ItemInstance getItemUnderMouse()
    {
        //Get mouse location
        Vector2 mousePosition = Input.mousePosition;

        //Invert the y axis of the mouse-position since the UI is counting from top to bottom
        mousePosition.y = Screen.height - mousePosition.y;

        //Get the Element under the mouse
        VisualElement elementUnderMouse = rootElement.panel.Pick(mousePosition);


        if (elementUnderMouse != null)
        {
            //If the element under the mouse is the MainContainer of the UI we skip the rest
            if (elementUnderMouse.name != "MainContainer")
            {

                //Get the main component of the slot-prefab
                var itemPanel = elementUnderMouse.parent;

                //Special-case if the mouse is directly on the slot-prefab (this is like a 4px gap but it happened)
                if (elementUnderMouse.name == "Slot")
                {
                    itemPanel = elementUnderMouse;
                }

                //Special-case if the mouse is over the number, since the number component is a child of the icon
                if (elementUnderMouse.name == "ItemCount")
                {
                    itemPanel = elementUnderMouse.parent.parent;
                }

                //Get the name of the item from the name-panel
                string name = itemPanel.Q<Label>("Name").text;

                return inventoryManager.GetItem(name);

            }
        }
        return new ItemInstance("",new GameObject(),0, null,0);
    }
}
