using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    
    public void startGame()
    {
        Debug.Log("Start");
        SceneManager.LoadScene("SampleScene");
    }

    public void loadGame()
    {
        Debug.Log("Load");
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
    }
}
