using UnityEngine;
using System;

/// <summary>
/// Manages global events for opening and closing doors, allowing other scripts to listen and respond to these actions.
/// </summary>
public class EventManager : MonoBehaviour
{
    /// <summary>
    /// Event triggered to open doors. 
    /// Other scripts can subscribe to this event to perform actions when doors need to be opened.
    /// </summary>
    public static event Action OnOpenDoors;

    /// <summary>
    /// Event triggered to close doors. 
    /// Other scripts can subscribe to this event to perform actions when doors need to be closed.
    /// </summary>
    public static event Action OnCloseDoors;

    /// <summary>
    /// Monitors player input to trigger the door-related events.
    /// </summary>
    private void Update()
    {
        // Trigger the "OpenDoors" event when the left mouse button is clicked
        if (Input.GetMouseButtonDown(0))
        {
            OnOpenDoors?.Invoke();      // Safely invoke the event if there are subscribers
        }

        // Trigger the "CloseDoors" event when the right mouse button is clicked
        if (Input.GetMouseButtonDown(1))
        {
            OnCloseDoors?.Invoke();     // Safely invoke the event if there are subscribers
        }
    }
}