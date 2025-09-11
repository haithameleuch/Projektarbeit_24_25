using System;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// Manages global events for opening and closing doors, allowing other scripts to listen and respond to these actions.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the EventManager, ensuring there is only one instance in the scene.
        /// Provides global access to event management functionality.
        /// </summary>
        public static EventManager Instance { get; private set; }
    
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
        /// Event triggered to open Boss doors.
        /// Other scripts can subscribe to this event to perform actions when doors need to be closed.
        /// </summary>
        public static event Action OnOpenBossDoors;
    
        /// <summary>
        /// Event triggered to close Boss doors.
        /// Other scripts can subscribe to this event to perform actions when doors need to be closed.
        /// </summary>
        public static event Action OnCloseBossDoors;

        /// <summary>
        /// Event triggered to activate the Canvas View.
        /// </summary>
        public static event Action OnActivateCanvasView;
    
        /// <summary>
        /// Ensures there is only one instance of the EventManager in the scene.
        /// If another instance exists, it will be destroyed.
        /// Persists the instance across scenes for consistent event state management.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Monitors player input to trigger the door-related events.
        /// </summary>
        private void Update()
        {
            // Trigger the "OpenDoors" event when the left mouse button is clicked
            if (Input.GetMouseButtonDown(0))
            {
                OnOpenDoors?.Invoke(); // Safely invoke the event if there are subscribers
            }

            // Trigger the "CloseDoors" event when the right mouse button is clicked
            if (Input.GetMouseButtonDown(1))
            {
                OnCloseDoors?.Invoke(); // Safely invoke the event if there are subscribers
            }
        
            //  Press K to Open Boss a door (only for debugging)
            /*if (Input.GetKeyDown(KeyCode.K))
            {
                OnOpenBossDoors?.Invoke();
                SaveSystemManager.SetBossRoomOpen(true);
            }*/
        }

        /// <summary>
        /// Method to manually trigger the Canvas View activation event.
        /// </summary>
        public void TriggerCanvasView()
        {
            OnActivateCanvasView?.Invoke();
        }

        /// <summary>
        /// Manually triggers the "CloseDoors" event.
        /// This can be called by other scripts to close doors.
        /// </summary>
        public void TriggerCloseDoors()
        {
            OnCloseDoors?.Invoke();
        }
    
        /// <summary>
        /// Manually triggers the "OpenDoors" event.
        /// This can be called by other scripts to open doors.
        /// </summary>
        public void TriggerOpenDoors()
        {
            OnOpenDoors?.Invoke();
        }
    
        /// <summary>
        /// Manually triggers the "CloseBossDoors" event.
        /// This can be called by other scripts to close boss doors.
        /// </summary>
        public void TriggerCloseBossDoors()  => OnCloseBossDoors?.Invoke();
        
        /// <summary>
        /// Manually triggers the "OpenBossDoors" event.
        /// This can be called by other scripts to open boss doors.
        /// </summary>
        public void TriggerOpenBossDoors()   => OnOpenBossDoors?.Invoke();
    }
}
