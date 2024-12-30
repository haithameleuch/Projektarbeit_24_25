using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class ToggleInventory : MonoBehaviour
{

    private bool isOpen = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        toggleInv();
    }

    public void toggleInv()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isOpen)
            {
                transform.GetChild(0).gameObject.SetActive(true);
                isOpen = true;
            }
            else
            {
                transform.GetChild(0).gameObject.SetActive(false);
                isOpen = false;
            }
        }
    }
}