using UnityEngine;
using System.Collections;

public enum SimulationType { Sheep, Wolf }


[RequireComponent(typeof(CharacterController))]
public abstract class AgentBehaviour : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 1f;            // m/s
    [SerializeField] private float rotationSpeed = 120f;  // deg/s

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Perception")]
    [SerializeField] protected float visionRadius = 30f;

    [Header("Needs")]
    [SerializeField] protected float hungerLossRate = 1f;   // %/s

    [Header("Death")]
    [SerializeField] private Transform deathVisual;
    [SerializeField] private float fallDuration = 0.5f;
    [SerializeField] private float sinkDuration = 2f;
    [SerializeField] private float sinkDistance = 3f;


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
    public bool IsDead => isDead;
    public bool IsBeingEaten => isBeingEaten;
    protected bool IsExhausted => isExhausted;
    

    public abstract SimulationType GetSimType();


//------------------------------//------------------------------//


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        characterController = GetComponent<CharacterController>();
        targetRot = transform.rotation;
        nextTurnTime = Time.time + Random.Range(5f, 9f);
        hunger = 100f;
        energy = 100f;
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
                UpdateBehaviour();
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
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );

        movement = speed * speedMultiplier * transform.forward;
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


    protected AgentBehaviour FindClosestAgent(SimulationType targetType)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, visionRadius);
        AgentBehaviour closestAgent = null;
        float minDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            AgentBehaviour agent = collider.GetComponentInParent<AgentBehaviour>();

            if (agent != null && agent != this && !agent.IsDead && !agent.IsBeingEaten && agent.GetSimType() == targetType)
            {
                float distance = (transform.position - agent.transform.position).sqrMagnitude;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestAgent = agent;
                }
            }
        }

        if (closestAgent != null && Time.frameCount % 60 == 0)
        {
            Debug.Log($"{GetSimType()} detected {closestAgent.GetSimType()}: {closestAgent.name}");
        }

        return closestAgent;
    }


    public bool TryBeginBeingEaten()
    {
        bool canBeEaten = !isDead && !isBeingEaten;

        if (canBeEaten)
            isBeingEaten = true;

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
