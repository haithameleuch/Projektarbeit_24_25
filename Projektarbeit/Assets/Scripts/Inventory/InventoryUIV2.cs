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
            var itemElement = itemTemplate.CloneTree();
            itemElement.Q<Label>("Name").text = item.itemName;
            itemElement.Q<VisualElement>("Top").style.backgroundImage = new StyleBackground(item.itemIcon);
            itemElement.Q<Label>("ItemCount").text = ""+item.itemQuantity;
            itemElement.AddToClassList("TemplateContainer");
            inventoryContainer.Add(itemElement);
        }
    }

    /// <summary>
    /// This method is used to check for user-inputs and react to them
    /// </summary>
    private void Update()
    {
        // Überprüfen, ob die "E"-Taste gedrückt wurde
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleUIDocument();
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
            UnityEngine.Cursor.lockState= CursorLockMode.Locked;
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
}
