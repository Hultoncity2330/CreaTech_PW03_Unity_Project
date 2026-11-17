using UnityEngine;

public class SheepBehaviour : AgentBehaviour
{
    [Header("Sheep Settings")]
    [SerializeField] private float fleeDistance = 8f;

    private enum SheepState
    {
        Explore,
        Flee
    }

    private SheepState currentState = SheepState.Explore;

    protected override void UpdateBehaviour()
    {
        AgentBehaviour wolf = FindClosestAgent(SimulationType.Wolf);

        if (wolf != null && Vector3.Distance(transform.position, wolf.transform.position) <= fleeDistance)
        {
            currentState = SheepState.Flee;

            Vector3 direction = transform.position - wolf.transform.position;
            SetDirection(direction);
        }
        else
        {
            currentState = SheepState.Explore;
            Wander();
        }

        Move();
    }
}
