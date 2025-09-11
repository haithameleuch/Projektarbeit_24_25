using Saving;
using Enemy;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manager
{
    /// <summary>
    /// Handles menu button actions: start, load, save, and exit.
    /// Sets a scene-changing flag to avoid enemy death reports during scene switches.
    /// </summary>
    public class ButtonManager : MonoBehaviour
    {
        /// <summary>
        /// Starts a new game: creates a new save with a random seed and loads the main scene.
        /// </summary>
        public void StartGame()
        {
            Debug.Log("StartButton [NEW GAME]");
            
            // Just for protection
            EnemyDeathReporter.SetSceneChanging(true);
            
            var seed = Random.Range(100000, 999999);
            SaveSystemManager.StartNewRun(seed);
            SceneManager.LoadScene("Scenes/VoronoiTest");
        }

        /// <summary>
        /// Loads an existing save and opens the main scene if a save file exists.
        /// </summary>
        public void LoadGame()
        {
            Debug.Log("LoadButton [LOAD GAME]");
            
            // Just for protection
            EnemyDeathReporter.SetSceneChanging(true);
            
            SaveSystemManager.Load();
            Time.timeScale = 1;
            
            if (SaveSystemManager.SaveData != null)
            {
                SceneManager.LoadScene("Scenes/VoronoiTest");
            }
            else
            {
                Debug.Log("No save found.");
            }
        }

        /// <summary>
        /// Exits the game (or stops Play Mode in the editor).
        /// </summary>
        public void ExitGame()
        {
            Debug.Log("ExitButton [EXIT GAME]");
            
            // Just for protection
            EnemyDeathReporter.SetSceneChanging(true);
            
            #if UNITY_STANDALONE
                Application.Quit();
            #endif
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        /// <summary>
        /// Saves the current player state: position, view direction, camera direction,
        /// inventory, equipment, and stats.
        /// </summary>
        public void SaveGame()
        {
            Debug.Log("SaveButton [SAVE GAME]");
            
            var player = GameObject.Find("Player(Clone)");
            var playerCam = player.transform.Find("FirstPersonCam");
            var inventory = player.GetComponent<Inventory>();
            var playerStats = player.GetComponent<Stats>();
            
            SaveSystemManager.SetPlayerRotation(player.transform.forward);
            SaveSystemManager.SetPlayerPosition(player.transform.position);
            SaveSystemManager.SetCamRotation(playerCam.transform.forward);
            SaveSystemManager.SetInventory(inventory.getInventory());
            SaveSystemManager.SetEquipment(inventory.getEquipment());
            SaveSystemManager.SetStats(playerStats.GetCurStatsList(), playerStats.GetMaxStatsList());
            SaveSystemManager.Save();
        }
    }
}
