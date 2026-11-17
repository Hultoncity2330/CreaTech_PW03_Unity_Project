using UnityEngine;

public enum SimulationType
{
    Sheep,
    Wolf
};

public class AgentBehaviour : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 2f;          // m/s
    [SerializeField] private float rotationSpeed = 90f; // deg/s

    [Header("Perception")]
    [SerializeField] private float visionRadius = 10f;

    private float nextTurnTime;
    private Quaternion targetRot;

    [SerializeField] private SimulationType type = SimulationType.Sheep;

    public SimulationType GetSimType()
    {
        return type;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        targetRot = transform.rotation;
        nextTurnTime = Time.time + Random.Range(5f, 9f);
    }


    // Update is called once per frame
    void Update()
    {
        UpdateBehaviour();
    }


    protected virtual void UpdateBehaviour()
    {
        Wander();
        Move();
    }


    protected void Move()
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );

        transform.position += speed * Time.deltaTime * transform.forward;
    }


    protected void Wander()
    {
        if (Time.time >= nextTurnTime)
        {
            float angle = Random.Range(0f, 360f);
            Quaternion rot = Quaternion.Euler(0f, angle, 0f);
            targetRot = rot;
            nextTurnTime = Time.time + Random.Range(5f, 9f);
        }
    }


    protected void SetDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude > 0f)
        {
            targetRot = Quaternion.LookRotation(direction);
        }
    }


    protected AgentBehaviour FindClosestAgent(SimulationType targetType)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, visionRadius);

        AgentBehaviour closestAgent = null;
        float minDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            AgentBehaviour agent = collider.GetComponentInParent<AgentBehaviour>();

            if (agent == null || agent == this)
                continue;

            if (agent.GetSimType() != targetType)
                continue;

            float distance = Vector3.Distance(transform.position, agent.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestAgent = agent;
            }
        }

        if (closestAgent != null && Time.frameCount % 60 == 0)
        {
            Debug.Log($"{type} detected {closestAgent.GetSimType()}: {closestAgent.name}");
        }

        return closestAgent;
    }
}

