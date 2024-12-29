using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject enemy1 = Instantiate(enemyPrefab, new Vector3(0, 1, 3), Quaternion.identity);
        enemy1.GetComponent<EnemyBehaviour>().setSpeed(2.0f);
        GameObject enemy2 = Instantiate(enemyPrefab, new Vector3(-3, 1, 3), Quaternion.identity);
        enemy2.GetComponent<EnemyBehaviour>().setSpeed(2.5f);
        GameObject enemy3 = Instantiate(enemyPrefab, new Vector3(-6, 1, 3), Quaternion.identity);
        enemy3.GetComponent<EnemyBehaviour>().setSpeed(3.0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
