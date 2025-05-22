using TMPro; // Import TextMeshPro namespace for using TMP_Text components
using Unity.VisualScripting;
using UnityEngine;

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

    // Needed to check wether the pause screen is displayed
    bool isPauseVisible = false;

    // Needed to check wether the inventory screen is displayed
    bool isInvVisible = false;

    [SerializeField]
    private GameObject player;

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
            if (isPauseVisible)
            {
                //player.GetComponent<FirstPersonPlayerController>().enabled = true;
                player.SetActive(true);
                pause.gameObject.SetActive(false);

                //Lock Cursor in the game view
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;

                //Resume time to normal value
                Time.timeScale = 1;
                isPauseVisible=false;
            }
            else
            {
                //player.GetComponent<FirstPersonPlayerController>().enabled = false;
                player.SetActive(false);
                HidePanel();
                pause.gameObject.SetActive(true);

                //Make the cursor moveable within the game window
                UnityEngine.Cursor.lockState = CursorLockMode.Confined;
                UnityEngine.Cursor.visible = true;

                //Pause the game
                Time.timeScale = 0;
                isPauseVisible=true;
            }
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            GameObject inv = gameObject.transform.Find("Inventory").gameObject;

            if (!isInvVisible)
            {
                HidePanel();

                player.SetActive(false);

                renderInv(inv);
                inv.SetActive(true);

                //Make the cursor moveable within the game window
                UnityEngine.Cursor.lockState = CursorLockMode.Confined;
                UnityEngine.Cursor.visible = true;

                //Pause the game
                Time.timeScale = 0;

                isInvVisible = true;
            }
            else
            {

                player.SetActive(true);

                inv.SetActive(false);

                deRenderInv(inv);

                //Make the cursor unmoveable within the game window
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;

                //Pause the game
                Time.timeScale = 1;

                isInvVisible = false;
            }
        }
    }

    private void deRenderInv(GameObject inv)
    {
        GameObject items = inv.transform.Find("Items").gameObject;
        foreach (Transform child in items.transform)
        {
            Destroy(child.gameObject);
        }

        GameObject equipslots = inv.transform.Find("Equipment").gameObject.transform.Find("EquipmentSlots").gameObject;

        foreach (Transform child in equipslots.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void renderInv(GameObject inv)
    {
        GameObject items = inv.transform.Find("Items").gameObject;
        for (int i = 0; i < 4; i++)
        {
            GameObject row = Instantiate(rowPrefab,items.transform);
            for (int j=0; j<5;j++)
            {
                GameObject slot = Instantiate(emptyPrefab, row.transform);
            }
        }

        GameObject equip = inv.transform.Find("Equipment").gameObject;

        GameObject equipslots = equip.transform.Find("EquipmentSlots").gameObject;

        TMP_Text stats = equip.transform.Find("Stats").gameObject.transform.Find("StatDetails").GetComponent<TMP_Text>();

        stats.SetText("Health:10\nDamage:10\nSpeed:10");

        for (int i = 0; i < 3; i++)
        {
            GameObject row = Instantiate(rowPrefab, equipslots.transform);
            for (int j = 0; j < 2; j++)
            {
                GameObject slot = Instantiate(emptyPrefab, row.transform);
            }
        }
    }
}
