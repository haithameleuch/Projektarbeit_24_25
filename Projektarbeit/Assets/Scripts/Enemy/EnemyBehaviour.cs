using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float speed;
    [SerializeField] private float constantYPosition;

    public void setSpeed(float speed)
    {
        this.speed = speed;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = GameObject.Find("Player").transform.GetChild(0).gameObject.transform;
        constantYPosition = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {

        Vector3 targetDirection = (target.position - transform.position).normalized;
        Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, 2.0f * speed * Time.deltaTime, 0.0f);

        transform.forward = newDirection;
        transform.Translate(transform.forward * speed * Time.deltaTime, Space.World);
        transform.position = new Vector3(transform.position.x, constantYPosition, transform.position.z);


        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }

}
