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
        Application.Quit();
    }

    public void saveGame()
    {
        Debug.Log("Save");
    }
}
