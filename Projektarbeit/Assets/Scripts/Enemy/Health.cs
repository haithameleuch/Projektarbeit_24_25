using UnityEngine;
using UnityEngine.Serialization;

namespace Enemy
{
    public class Health : MonoBehaviour
    {
        [FormerlySerializedAs("_maxHealth")] public float maxHealth;
        [FormerlySerializedAs("_currentHealth")] public float currentHealth;
    }
}
