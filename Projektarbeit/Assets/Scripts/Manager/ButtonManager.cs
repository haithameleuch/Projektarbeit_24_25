using Unity.Cinemachine;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    
    public void startGame()
    {
        Debug.Log("Start");
        int seed = Random.Range(100000, 999999);
        SaveSystemManager.StartNewRun(seed);
        SceneManager.LoadScene("Scenes/VoronoiTest");
    }

    public void loadGame()
    {
        Debug.Log("Load");
        SaveSystemManager.Load();
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
        Debug.Log("Exit");
        #if UNITY_STANDALONE
                Application.Quit();
        #endif
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #endif

    }

    public void saveGame()
    {
        Debug.Log("Save");
        GameObject player = GameObject.Find("Player(Clone)");
        SaveSystemManager.SetPlayerRotation(player.transform.rotation);
        SaveSystemManager.SetPlayerPosition(player.transform.position);
        SaveSystemManager.SetCamRotation(player.transform.Find("FirstPersonCam").transform.rotation);
        SaveSystemManager.Save();
    }
}
