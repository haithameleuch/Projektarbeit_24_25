using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float currentSpeed;
    [SerializeField] private float defaultSpeed;
    [SerializeField] private float constantYPosition;

    public void setSpeed(float speed)
    {
        this.currentSpeed = speed;
    }

    public float getDefaultSpeed()
    {
        return this.defaultSpeed;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = GameObject.Find("Player").transform.GetChild(0).gameObject.transform;
        constantYPosition = transform.position.y;
        defaultSpeed = 2;
        currentSpeed = 2;
    }

    // Update is called once per frame
    void Update()
    {
        if(GetComponent<EnemyInteraction>().CanMove())
        {
            Vector3 targetDirection = (target.position - transform.position).normalized;
            Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, 2.0f * currentSpeed * Time.deltaTime, 0.0f);

            transform.forward = newDirection;
            transform.Translate(transform.forward * currentSpeed * Time.deltaTime, Space.World);
            transform.position = new Vector3(transform.position.x, constantYPosition, transform.position.z);


            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }
    }

}
