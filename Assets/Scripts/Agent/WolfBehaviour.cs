using UnityEngine;

public enum WolfState { Idle, Chase, Attack, Eat, Dead }


public class WolfBehaviour : AgentBehaviour
{
    [Header("Wolf Settings")]
    [SerializeField] private float chaseEnergyLoss = 15f;
    [SerializeField] private float energyRecovery = 10f;
    [SerializeField] private float hungerThreshold = 50f;

    [Header("Eating")]
    [SerializeField] private float attackDuration = 1f;
    [SerializeField] private float eatDuration = 2f;
    [SerializeField] private float hungerGain = 50f;

    private WolfState currentState = WolfState.Idle;
    private AgentBehaviour closestSheep;
    private SheepBehaviour prey;
    private Animator animator;
    private float stateTimer;

    public override SimulationType GetSimType()
    {
        return SimulationType.Wolf;
    }


//------------------------------//------------------------------//


    protected override void Start()
    {
        base.Start();
        hungerLossRate = 2f;
        animator = GetComponentInChildren<Animator>();
    }


    protected override void UpdateBehaviour()
    {
        // 1. Perception and state selection (not while attacking or eating)
        if (currentState == WolfState.Idle || currentState == WolfState.Chase)
        {
            closestSheep = FindClosestAgent(SimulationType.Sheep);

            WolfState previousState = currentState;
            PickState();

            if (animator != null && previousState != currentState)
                animator.SetBool("IsRunning", currentState == WolfState.Chase);
        }

        // 2. State execution
        switch (currentState)
        {
            case WolfState.Idle:
                DoIdle();
                break;

            case WolfState.Chase:
                UseEnergy(chaseEnergyLoss);

                if (IsExhausted)
                {
                    currentState = WolfState.Idle;
                    if (animator != null)
                        animator.SetBool("IsRunning", false);
                    DoIdle();
                }
                else
                {
                    SetDirection(closestSheep.transform.position - transform.position);
                    Move(4f);
                }
                break;

            case WolfState.Attack:
                DoAttack();
                break;

            case WolfState.Eat:
                DoEat();
                break;

            case WolfState.Dead:
                break;
        }
    }


//------------------------------//------------------------------//


    private void PickState()
    {
        currentState = WolfState.Idle;

        if (!IsExhausted && Hunger < hungerThreshold && closestSheep != null)
            currentState = WolfState.Chase;
    }


    private void DoIdle()
    {
        RecoverEnergy(energyRecovery);
        Wander();
        Move();
    }


    private void DoAttack()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer >= attackDuration)
        {
            stateTimer = 0f;
            currentState = WolfState.Eat;
        }
    }


    private void DoEat()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer >= eatDuration)
        {
            if (prey != null && !prey.IsDead)
            {
                prey.Death();
                GainHunger(hungerGain);
            }

            prey = null;
            stateTimer = 0f;
            currentState = WolfState.Idle;

            if (animator != null)
                animator.SetBool("IsRunning", false);
        }
    }


    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TryEat(hit.collider);
    }


    private void TryEat(Collider other)
    {
        if (currentState == WolfState.Chase && !IsDead && Hunger < hungerThreshold)
        {
            SheepBehaviour sheep = other.GetComponentInParent<SheepBehaviour>();

            if (sheep != null && sheep.TryBeginBeingEaten())
            {
                prey = sheep;
                stateTimer = 0f;
                currentState = WolfState.Attack;

                if (animator != null)
                {
                    animator.SetBool("IsRunning", false);
                    animator.SetTrigger("IsAttacking");
                }
            }
        }
    }


    protected override void OnDeath()
    {
        currentState = WolfState.Dead;

        if (prey != null && !prey.IsDead)
            prey.CancelBeingEaten();

        prey = null;

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.ResetTrigger("IsAttacking");
        }
    }
}

