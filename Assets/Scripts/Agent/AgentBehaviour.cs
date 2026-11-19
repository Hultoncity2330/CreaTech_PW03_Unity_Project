using UnityEngine;
using System.Collections;

public enum SimulationType { Sheep, Wolf }


[RequireComponent(typeof(CharacterController))]
public abstract class AgentBehaviour : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 1f;              // m/s
    [SerializeField] private float rotationSpeed = 120f;    // deg/s

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Perception")]
    [SerializeField] private LayerMask animalMask = ~0;
    [SerializeField] protected float visionRadius = 50f;
    [SerializeField] private float minSearchInterval = 0.4f;
    [SerializeField] private float maxSearchInterval = 0.6f;

    [Header("Boundaries")]
    [SerializeField] private Vector2 areaCenter = Vector2.zero;
    [SerializeField] private float areaHalfSize = 60f;

    [Header("Reproduction")]
    [SerializeField] private GameObject heartVisual;
    [SerializeField] private GameObject offspringPrefab;
    [SerializeField, Min(0f)] private float reproductionCooldown = 180f;
    [SerializeField] private float reproductionDistance = 2f;
    [SerializeField] private float reproductionDuration = 3f;

    [Header("Needs")]
    [SerializeField] protected float hungerLossRate = 1f;   // %/s

    [Header("Death")]
    [SerializeField] private Transform deathVisual;
    [SerializeField] private float fallDuration = 0.5f;
    [SerializeField] private float sinkDuration = 2f;
    [SerializeField] private float sinkDistance = 3f;


    // Reused by OverlapSphereNonAlloc: no new collider array each frame.
    private readonly Collider[] animalColliders = new Collider[512];
    private readonly Collider[] mateColliders = new Collider[512];
    private AgentBehaviour reproductionPartner;
    private bool reproductionLeader;
    private float reproductionTimer;
    private float reproductionReadyTime;
    private float nextMateSearchTime;
    private AgentBehaviour cachedClosestSheep;
    private AgentBehaviour cachedClosestWolf;
    private float nextAnimalSearchTime;

    private float nextTurnTime;
    private Quaternion targetRot;
    private CharacterController characterController;
    private Vector3 movement;
    private float verticalSpeed;

    private float hunger = 100f;
    private float energy = 100f;
    private bool isExhausted;
    private bool isDead;
    private bool isBeingEaten;

    public float Hunger => hunger;
    public float Energy => energy;
    public float BreedingCooldownRemaining => Mathf.Max(0f, reproductionReadyTime - Time.time);
    public bool CanBreedNow => CanStartReproduction();
    public bool IsDead => isDead;
    public bool IsBeingEaten => isBeingEaten;
    protected bool IsExhausted => isExhausted;
    protected bool IsReproducing => reproductionPartner != null;
    

    public abstract SimulationType GetSimType();


//------------------------------//------------------------------//


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        characterController = GetComponent<CharacterController>();
        targetRot = transform.rotation;
        nextTurnTime = Time.time + Random.Range(5f, 9f);
        nextAnimalSearchTime = Time.time + Random.Range(0f, 0.2f);
        nextMateSearchTime = Time.time + Random.Range(0f, 0.2f);
        hunger = 100f;
        energy = 100f;
        SetHeartVisible(false);
    }


    // Update is called once per frame
    private void Update()
    {
        if (!isDead && !isBeingEaten)
        {
            hunger = Mathf.Max(0f, hunger - hungerLossRate * Time.deltaTime);
            if (hunger <= 0f) Death();
            else
            {
                movement = Vector3.zero;
                if (IsOutsideArea())
                    CancelReproduction();
                UpdateBehaviour();

                // Outside the square: steer back toward its centre.
                if (IsOutsideArea())
                    ReturnToCenter();

                ApplyMovement();
            }
        }
    }


    protected abstract void UpdateBehaviour();


//------------------------------//------------------------------//


    protected void UseEnergy(float amountPerSecond)
    {
        energy = Mathf.Max(0f, energy - amountPerSecond * Time.deltaTime);

        if (energy <= 0f)
            isExhausted = true;
    }

    protected void RecoverEnergy(float amountPerSecond)
    {
        energy = Mathf.Min(100f, energy + amountPerSecond * Time.deltaTime);

        if (isExhausted && energy > 50f)
            isExhausted = false;
    }

    protected void GainHunger(float amount)
    {
        hunger = Mathf.Clamp(hunger + amount, 0f, 100f);
    }
    
    
    protected void Move(float speedMultiplier = 1f)
    {
        TurnTowardsTarget();
        movement = speed * speedMultiplier * transform.forward;
    }

    private void TurnTowardsTarget()
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );

    }


    /// <summary>Checks whether the agent has crossed the square boundary on X or Z.</summary>
    private bool IsOutsideArea()
    {
        Vector3 position = transform.position;

        return Mathf.Abs(position.x - areaCenter.x) > areaHalfSize
            || Mathf.Abs(position.z - areaCenter.y) > areaHalfSize;
    }


    /// <summary>Steers an agent outside the area toward its centre.</summary>
    private void ReturnToCenter()
    {
        Vector3 center = new Vector3(areaCenter.x, transform.position.y, areaCenter.y);

        SetDirection(center - transform.position);
        Move();
    }


    private void ApplyMovement()
    {
        if (characterController.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;
        else
            verticalSpeed += gravity * Time.deltaTime;

        movement.y = verticalSpeed;
        characterController.Move(movement * Time.deltaTime);
    }


    protected void Wander()
    {
        if (Time.time >= nextTurnTime)
        {
            targetRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            nextTurnTime = Time.time + Random.Range(5f, 9f);
        }
    }


    protected void SetDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            targetRot = Quaternion.LookRotation(direction);
    }


    /// <summary>
    /// Returns the nearest living agent of a requested type.
    /// One masked NonAlloc physics query refreshes both species caches every 0.4–0.6 s.
    /// </summary>
    protected AgentBehaviour FindClosestAgent(SimulationType targetType)
    {
        if (Time.time >= nextAnimalSearchTime)
        {
            cachedClosestSheep = null;
            cachedClosestWolf = null;
            float sheepDistance = float.MaxValue;
            float wolfDistance = float.MaxValue;

            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                visionRadius,
                animalColliders,
                animalMask,
                QueryTriggerInteraction.Collide
            );

            for (int i = 0; i < count; i++)
            {
                AgentBehaviour agent = animalColliders[i].GetComponentInParent<AgentBehaviour>();

                if (agent != null && agent != this && !agent.IsDead && !agent.IsBeingEaten)
                {
                    float distance = (transform.position - agent.transform.position).sqrMagnitude;

                    if (agent.GetSimType() == SimulationType.Sheep && distance < sheepDistance)
                    {
                        sheepDistance = distance;
                        cachedClosestSheep = agent;
                    }
                    else if (agent.GetSimType() == SimulationType.Wolf && distance < wolfDistance)
                    {
                        wolfDistance = distance;
                        cachedClosestWolf = agent;
                    }
                }
            }

            nextAnimalSearchTime = Time.time + Random.Range(minSearchInterval, maxSearchInterval);
        }

        AgentBehaviour closest = targetType == SimulationType.Sheep
            ? cachedClosestSheep : cachedClosestWolf;

        if (closest != null && (closest.IsDead || closest.IsBeingEaten))
            closest = null;

        return closest;
    }


    /// <summary>Displays the heart only while the agent has a reproduction partner.</summary>
    private void SetHeartVisible(bool visible)
    {
        if (heartVisual != null)
            heartVisual.SetActive(visible);
    }


    /// <summary>Implemented by each species: only an eligible Idle animal can start pairing.</summary>
    protected abstract bool CanStartReproduction();

    /// <summary>
    /// Finds the nearest available animal of the same species and reserves the pair.
    /// Searches are spaced and use a reused NonAlloc buffer, as in the workshop.
    /// </summary>
    protected void TryFindReproductionPartner()
    {
        if (reproductionPartner == null && CanStartReproduction() && Time.time >= nextMateSearchTime)
        {
            AgentBehaviour closest = null;
            float closestDistance = float.MaxValue;
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, visionRadius, mateColliders,
                animalMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                AgentBehaviour candidate = mateColliders[i].GetComponentInParent<AgentBehaviour>();
                if (candidate != null && candidate != this &&
                    candidate.GetSimType() == GetSimType() && candidate.CanStartReproduction())
                {
                    float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closest = candidate;
                    }
                }
            }

            nextMateSearchTime = Time.time + Random.Range(0.4f, 0.6f);
            if (closest != null && CanStartReproduction() && closest.CanStartReproduction())
            {
                reproductionPartner = closest;
                closest.reproductionPartner = this;
                reproductionLeader = true;
                closest.reproductionLeader = false;
                reproductionTimer = 0f;
                closest.reproductionTimer = 0f;
                SetHeartVisible(true);
                closest.SetHeartVisible(true);
            }
        }
    }

    /// <summary>True while both partners are close enough to remain stationary.</summary>
    protected bool IsReproductionPairClose()
    {
        if (reproductionPartner == null)
            return false;

        Vector3 toMate = reproductionPartner.transform.position - transform.position;
        toMate.y = 0f;
        return toMate.sqrMagnitude <= reproductionDistance * reproductionDistance;
    }

    /// <summary>Moves partners face to face, waits three seconds, then creates one baby.</summary>
    protected void DoReproduce()
    {
        if (reproductionPartner != null)
        {
            if (reproductionPartner.IsDead || reproductionPartner.IsBeingEaten ||
                reproductionPartner.reproductionPartner != this)
            {
                CancelReproduction();
            }
            else
            {
                Vector3 toPartner = reproductionPartner.transform.position - transform.position;
                toPartner.y = 0f;
                SetDirection(toPartner);

                float distanceSquared = toPartner.sqrMagnitude;
                if (distanceSquared > reproductionDistance * reproductionDistance)
                {
                    // Normal movement speed while approaching a mate.
                    Move();
                    reproductionTimer = 0f;
                }
                else
                {
                    // Stop moving, but continue rotating to face the partner.
                    TurnTowardsTarget();
                    if (reproductionLeader)
                    {
                        reproductionTimer += Time.deltaTime;
                        if (reproductionTimer >= reproductionDuration)
                        {
                            if (offspringPrefab != null)
                            {
                                AgentBehaviour mate = reproductionPartner;
                                Vector3 middle = (transform.position + mate.transform.position) * 0.5f;
                                Instantiate(offspringPrefab, middle, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

                                // Only a successful birth costs 20 energy points to each parent.
                                energy = Mathf.Max(0f, energy - 20f);
                                mate.energy = Mathf.Max(0f, mate.energy - 20f);

                                CancelReproduction();
                                reproductionReadyTime = Time.time + reproductionCooldown;
                                mate.reproductionReadyTime = Time.time + mate.reproductionCooldown;
                            }
                            else
                            {
                                Debug.LogWarning($"No Offspring Prefab configured on {name}.");
                                CancelReproduction();
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>Releases both partners if danger, predation or death interrupts mating.</summary>
    protected void CancelReproduction()
    {
        AgentBehaviour mate = reproductionPartner;
        reproductionPartner = null;
        reproductionLeader = false;
        reproductionTimer = 0f;
        SetHeartVisible(false);
        if (mate != null && mate.reproductionPartner == this)
        {
            mate.reproductionPartner = null;
            mate.reproductionLeader = false;
            mate.reproductionTimer = 0f;
            mate.SetHeartVisible(false);
        }
    }

    protected bool MeetsReproductionNeeds()
    {
        return !isDead && !isBeingEaten && !isExhausted &&
            reproductionPartner == null && Time.time >= reproductionReadyTime &&
            energy >= 99.999f && hunger >= 20f;
    }


    public bool TryBeginBeingEaten()
    {
        bool canBeEaten = !isDead && !isBeingEaten;

        if (canBeEaten)
        {
            CancelReproduction();
            isBeingEaten = true;
        }

        return canBeEaten;
    }


    public void CancelBeingEaten()
    {
        if (!isDead)
            isBeingEaten = false;
    }


    public void Death()
    {
        if (!isDead)
        {
            CancelReproduction();
            isDead = true;
            isBeingEaten = false;
            OnDeath();

            // The controller must not block the sinking animation.
            characterController.enabled = false;

            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            StartCoroutine(DeathAnimation());
        }
    }


    protected virtual void OnDeath() { }


    private IEnumerator DeathAnimation()
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.enabled = false;

        // Rotate around the animal's forward axis, not the world's Z axis.
        // Use the root if Death Visual was not assigned in the Inspector.
        Transform visual = deathVisual != null ? deathVisual : transform;
        Quaternion startRotation = visual.rotation;
        Quaternion endRotation = Quaternion.AngleAxis(90f, transform.forward) * startRotation;

        // 1. Fall onto the side BEFORE sinking into the ground.
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fallDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            visual.rotation = Quaternion.Slerp(startRotation, endRotation, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        visual.rotation = endRotation;

        // 2. Sink under the terrain once the fall is complete.
        Vector3 startPosition = transform.position;
        elapsed = 0f;
        duration = Mathf.Max(0.01f, sinkDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = startPosition + Vector3.down * sinkDistance * Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        Destroy(gameObject);
    }
}
