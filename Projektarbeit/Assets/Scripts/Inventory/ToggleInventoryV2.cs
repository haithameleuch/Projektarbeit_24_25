using UnityEngine;
using UnityEngine.UIElements;

public class ToggleInventoryV2 : MonoBehaviour
{
    private VisualElement rootElement;
    private bool isUIVisible = false; // Bool, um den aktuellen Zustand zu speichern

    private void OnEnable()
    {
        // Holen des rootVisualElement
        var root = GetComponent<UIDocument>().rootVisualElement;
        rootElement = root;

        // Das UI zu Beginn ausblenden
        rootElement.style.display = DisplayStyle.None;
    }

    private void Update()
    {
        // Überprüfen, ob die "E"-Taste gedrückt wurde
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleUIDocument();
        }
    }

    // Funktion zum Umschalten der Sichtbarkeit des UI
    private void ToggleUIDocument()
    {
        // Umschalten der Sichtbarkeit
        if (isUIVisible)
        {
            rootElement.style.display = DisplayStyle.None; // UI ausblenden
        }
        else
        {
            rootElement.style.display = DisplayStyle.Flex; // UI einblenden
        }

        // Den aktuellen Status speichern
        isUIVisible = !isUIVisible;
    }
}
