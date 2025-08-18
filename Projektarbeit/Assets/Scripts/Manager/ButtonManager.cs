using Saving;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manager
{
    public class ButtonManager : MonoBehaviour
    {
    
        public void startGame()
        {
            Debug.Log("StartButton [NEW GAME]");
            
            var seed = Random.Range(100000, 999999);
            SaveSystemManager.StartNewRun(seed);
            SceneManager.LoadScene("Scenes/VoronoiTest");
        }

        public void loadGame()
        {
            Debug.Log("LoadButton [LOAD GAME]");
            
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

        public void exitGame()
        {
            Debug.Log("ExitButton [EXIT GAME]");
            
            #if UNITY_STANDALONE
                Application.Quit();
            #endif
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        public void saveGame()
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
