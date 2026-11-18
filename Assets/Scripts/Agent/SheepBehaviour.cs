using UnityEngine;

public enum SheepState { Idle, Flee, Dead }


public class SheepBehaviour : AgentBehaviour
{
    [Header("Sheep Settings")]
    [SerializeField] private float fleeDistance = 10f;
    [SerializeField] private float fleeEnergyLoss = 10f;
    [SerializeField] private float energyRecovery = 10f;

    private SheepState currentState = SheepState.Idle;
    private AgentBehaviour closestWolf;

    public override SimulationType GetSimType()
    {
        return SimulationType.Sheep;
    }


//------------------------------//------------------------------//


    protected override void UpdateBehaviour()
    {
        // 1. Perception
        closestWolf = FindClosestAgent(SimulationType.Wolf);

        // 2. State selection
        PickState();

        // 3. State execution
        switch (currentState)
        {
            case SheepState.Idle:
                DoIdle();
                break;

            case SheepState.Flee:
                UseEnergy(fleeEnergyLoss);

                if (IsExhausted)
                {
                    currentState = SheepState.Idle;
                    DoIdle();
                }
                else
                {
                    SetDirection(transform.position - closestWolf.transform.position);
                    Move(2f);
                }
                break;

            case SheepState.Dead:
                break;
        }
    }


//------------------------------//------------------------------//


    private void PickState()
    {
        currentState = SheepState.Idle;

        if (!IsExhausted && closestWolf != null)
        {
            float distance = (transform.position - closestWolf.transform.position).sqrMagnitude;

            if (distance <= fleeDistance * fleeDistance)
                currentState = SheepState.Flee;
        }
    }

    private void DoIdle()
    {
        RecoverEnergy(energyRecovery);
        Wander();
        Move();
    }

    protected override void OnDeath()
    {
        currentState = SheepState.Dead;
    }
}

