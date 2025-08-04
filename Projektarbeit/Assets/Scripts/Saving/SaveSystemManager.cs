using UnityEngine;
using System.IO;

public static class SaveSystemManager
{
    private static string SaveFileName = "save.json";
    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static SaveData SaveData { get; private set; }

    public static void Load()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            SaveData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("Save loaded from: " + SavePath);
        }
        else
        {
            Debug.Log("No save found. Creating default save.");
            //SaveData = CreateDefaultSave();
            //Save();
        }
    }

    public static void Save()
    {
        string json = JsonUtility.ToJson(SaveData, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("Save written to: " + SavePath);
    }

    public static void StartNewRun(int newSeed)
    {
        SaveData = new SaveData
        {
            seed = newSeed,
            level = 1,
            playerPosition = Vector3.zero
        };
        Save();
    }

    // Getter and Setter
    public static int GetSeed() => SaveData.seed;
    public static int GetLevel() => SaveData.level;
    public static Vector3 GetPlayerPosition() => SaveData.playerPosition;
    public static void SetPlayerPosition(Vector3 pos) => SaveData.playerPosition = pos;
    
    public static float GetPlayerRotation() => SaveData.playerRotation;
    public static void SetPlayerRotation(Quaternion q) => SaveData.playerRotation = q.eulerAngles.y;
    
    public static float GetCamRotation() => SaveData.cameraRotation;
    public static void SetCamRotation(Quaternion q) => SaveData.cameraRotation = q.eulerAngles.x;

    private static SaveData CreateDefaultSave()
    {
        return new SaveData
        {
            seed = Random.Range(0, 999999),
            level = 1,
            playerPosition = Vector3.zero
        };
    }
}
