using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SpawnableData", menuName = "SpawnableData")]
public class SpawnableData : ScriptableObject
{
    [SerializeField]
    public string spawnName = "";
    
    [SerializeField] 
    public GameObject spawnObject;

    [SerializeField]
    [Range(0, 100)]
    public float spawnProbability = 50.0f;
    
    [SerializeField]
    public Sprite spawnSprite = null;
}