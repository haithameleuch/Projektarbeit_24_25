using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField]
    private GameObject slotPrefab;
    [SerializeField]
    private Transform contentPanel;
    [SerializeField]
    private Inventory inventory;

    void Start()
    {
        UpdateUI();
    }
    void Update()
    {
        UpdateUI();  
    }

    public void UpdateUI()
    {
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
        foreach (Item item in inventory.items)
        {
            GameObject newSlot = Instantiate(slotPrefab,contentPanel);
            Image icon = newSlot.transform.Find("TopHalf").Find("Icon").GetComponent<Image>();
            icon.sprite = item.itemIcon;
            Text itemquantity = newSlot.transform.Find("TopHalf").Find("ItemQuantity").GetComponent<Text>();
            itemquantity.text = ""+item.itemQuantity;
            Text textfield = newSlot.transform.Find("TextField").GetComponent<Text>();
            textfield.text = item.itemName;
        }
    }

    
}