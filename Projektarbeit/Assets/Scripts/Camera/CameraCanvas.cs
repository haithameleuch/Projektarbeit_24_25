using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
// using static Draw;

public class TriggerCanvas : MonoBehaviour
{
    public CinemachineCamera cameraCanvas; // Canvas for the alternate view
    public bool Action = false; // Indicates if the player is in the trigger zone

     void Start()
    {
        if (cameraCanvas != null)
        {
            cameraCanvas.Priority = 0; // Ensure the canvas camera is off initially
        }
        else
        {
            UnityEngine.Debug.Log("cameraCanvas is not assigned in the inspector.");
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            Action = true; // Player is in the trigger zone
            UnityEngine.Debug.Log("Player entered the trigger zone.");
        }
    }

    void OnTriggerExit(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (cameraCanvas != null)
            {
                cameraCanvas.Priority = 0; // Lower the priority to disable the canvas camera
            }
            Action = false; // Player left the trigger zone
        }
    }

    void Update()
    {
        if (cameraCanvas != null)
        {
            if (Input.GetKeyDown(KeyCode.P) && Action)
            {
                // Switch to the canvas camera by increasing its priority
                cameraCanvas.Priority = 10; // Set a higher priority to activate this camera
                UnityEngine.Debug.Log("Switched to the alternate camera.");
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                // Switch back to the main camera by lowering its priority
                cameraCanvas.Priority = 0; // Set a lower priority to deactivate this camera
                UnityEngine.Debug.Log("Switched back to the main camera.");
            }
        }
    }
}
