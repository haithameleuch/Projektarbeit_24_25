using UnityEngine;

[CreateAssetMenu(fileName = "NewSpawnableData", menuName = "Spawnable/SpawnableData")]
public class SpawnableData<T> : ScriptableObject
{
    [SerializeField]
    public string spawnableName = "";
    
    [SerializeField] 
    public GameObject prefab;

    [Range(1, 100)] 
    public int spawnProbability = 0;
}