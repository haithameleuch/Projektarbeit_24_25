using UnityEngine;

[CreateAssetMenu(fileName = "Spawnable", menuName = "Scriptable Objects/Spawnable")]
public class Spawnable : ScriptableObject
{
    [SerializeField]
    public string _name = "";
    [SerializeField]
    public GameObject _model;
    [SerializeField]
    [Range(0, 100)]
    public float rarity = 50.0f;
}
