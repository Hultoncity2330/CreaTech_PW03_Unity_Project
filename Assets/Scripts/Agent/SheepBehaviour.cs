using UnityEngine;

public enum SheepState { Idle, Flee, Dead }


public class SheepBehaviour : AgentBehaviour
{
    [Header("Sheep Settings")]
    [SerializeField] private float fleeDistance = 10f;
    [SerializeField] private float fleeEnergyLoss = 8f;
    [SerializeField] private float energyRecovery = 10f;

    [Header("Grass")]
    [SerializeField] private LayerMask foodMask = ~0;
    [SerializeField] private float seekGrassBelowHunger = 90f;
    [SerializeField] private float eatDistance = 1.2f;
    [SerializeField] private float eatDuration = 1f;
    [SerializeField] private float grassHungerGain = 10f;

    private readonly Collider[] foodColliders = new Collider[512];
    private GrassBehaviour closestGrass;
    private float nextFoodSearchTime;
    private float eatingTimer;
    private Animator animator;
    private bool isGrazing;

    private SheepState currentState = SheepState.Idle;
    private AgentBehaviour closestWolf;

    public override SimulationType GetSimType()
    {
        return SimulationType.Sheep;
    }


//------------------------------//------------------------------//


    protected override void Start()
    {
        base.Start();
        nextFoodSearchTime = Time.time + Random.Range(0f, 0.2f);
        animator = GetComponentInChildren<Animator>();
    }


    protected override void UpdateBehaviour()
    {
        // 1. Perception
        closestWolf = FindClosestAgent(SimulationType.Wolf);
        FindClosestGrass();

        // 2. State selection
        PickState();

        // 3. State execution
        isGrazing = false;

        switch (currentState)
        {
            case SheepState.Idle:
                DoIdle();
                break;

            case SheepState.Flee:
                eatingTimer = 0f;
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

        if (animator != null)
            animator.SetBool("IsGrazing", isGrazing);
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

        // Grazing is an Idle action: the sheep stops instead of wandering.
        if (Hunger < seekGrassBelowHunger && closestGrass != null)
        {
            Vector3 toGrass = closestGrass.transform.position - transform.position;
            toGrass.y = 0f;

            if (toGrass.sqrMagnitude <= eatDistance * eatDistance)
            {
                isGrazing = true;
                eatingTimer += Time.deltaTime;

                if (eatingTimer >= eatDuration)
                {
                    if (closestGrass.TryConsume())
                        GainHunger(grassHungerGain);

                    closestGrass = null;
                    eatingTimer = 0f;
                    nextFoodSearchTime = Time.time;
                }
            }
            else
            {
                eatingTimer = 0f;
                SetDirection(toGrass);
                Move();
            }
        }
        else
        {
            eatingTimer = 0f;
            Wander();
            Move();
        }
    }


    /// <summary>Searches Food colliders periodically without allocating a new array.</summary>
    private void FindClosestGrass()
    {
        if (Time.time >= nextFoodSearchTime)
        {
            closestGrass = null;
            float minDistance = float.MaxValue;

            if (Hunger < seekGrassBelowHunger)
            {
                int count = Physics.OverlapSphereNonAlloc(
                    transform.position,
                    visionRadius,
                    foodColliders,
                    foodMask,
                    QueryTriggerInteraction.Collide
                );

                for (int i = 0; i < count; i++)
                {
                    GrassBehaviour grass = foodColliders[i].GetComponentInParent<GrassBehaviour>();

                    if (grass != null && !grass.IsConsumed)
                    {
                        float distance = (grass.transform.position - transform.position).sqrMagnitude;

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestGrass = grass;
                        }
                    }
                }
            }

            nextFoodSearchTime = Time.time + Random.Range(0.4f, 0.6f);
        }

        if (closestGrass != null && closestGrass.IsConsumed)
            closestGrass = null;
    }


    protected override void OnDeath()
    {
        currentState = SheepState.Dead;
        closestGrass = null;
        isGrazing = false;

        if (animator != null)
            animator.SetBool("IsGrazing", false);
    }
}

