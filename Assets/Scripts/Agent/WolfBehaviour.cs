using UnityEngine;

public class WolfBehaviour : AgentBehaviour
{
    private enum WolfState
    {
        Explore,
        Chase
    }

    private WolfState currentState = WolfState.Explore;

    protected override void UpdateBehaviour()
    {
        AgentBehaviour sheep = FindClosestAgent(SimulationType.Sheep);

        if (sheep != null)
        {
            currentState = WolfState.Chase;

            Vector3 direction = sheep.transform.position - transform.position;
            SetDirection(direction);
        }
        else
        {
            currentState = WolfState.Explore;
            Wander();
        }

        Move();
    }
}
