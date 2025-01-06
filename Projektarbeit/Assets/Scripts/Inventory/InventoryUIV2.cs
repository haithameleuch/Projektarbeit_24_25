using UnityEngine;
using UnityEngine.UIElements;

public class InventoryUIV2 : MonoBehaviour
{
    [SerializeField]
    private Inventory inventoryManager;
    [SerializeField]
    private VisualTreeAsset itemTemplate;
    private VisualElement inventoryContainer;
    private VisualElement rootElement;
    private bool isUIVisible = false;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        rootElement = root;
        rootElement.style.display = DisplayStyle.None;
        inventoryContainer = root.Q<VisualElement>("MainContainer");
    }

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

    private void Update()
    {
        // Überprüfen, ob die "E"-Taste gedrückt wurde
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleUIDocument();
        }
    }

    private void ToggleUIDocument()
    {
        // Umschalten der Sichtbarkeit
        if (isUIVisible)
        {
            rootElement.style.display = DisplayStyle.None; // UI ausblenden
            UnityEngine.Cursor.lockState= CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            Time.timeScale = 1;
        }
        else
        {
            rootElement.style.display = DisplayStyle.Flex; // UI einblenden
            RefreshUI();
            UnityEngine.Cursor.lockState = CursorLockMode.Confined;
            UnityEngine.Cursor.visible = true;
            Time.timeScale = 0;
        }

        // Den aktuellen Status speichern
        isUIVisible = !isUIVisible;
    }
}
