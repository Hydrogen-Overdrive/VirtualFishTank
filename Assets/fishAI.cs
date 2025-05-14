using UnityEngine;
using System.Linq; 
public class FishAI : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3.0f;
    public float rotationSpeed = 120.0f; // Degrees per second for turning
    public float acceleration = 5.0f;    // Units per second^2 for speeding up/changing direction
    public float deceleration = 8.0f;  // Units per second^2 for slowing down
    private Vector3 currentVelocity = Vector3.zero;

    [Header("Behavior Settings")]
    public FishState currentState = FishState.Wandering;
    public float wanderChangeInterval = 5.0f; // How often to pick a new wander direction
    private float wanderTimer;
    private Vector3 wanderTargetPosition;

    [Header("Food Seeking")]
    public string foodTag = "Food";
    public float foodDetectionRadius = 8.0f;
    private Transform currentFoodTarget;

    [Header("Pursuit")]
    public Transform pursueTarget; 
    public float pursuitDetectionRadius = 12.0f;

    [Header("Obstacle Avoidance")]
    public float obstacleAvoidanceDistance = 2.5f; // Main forward whisker length
    public float obstacleAvoidanceForceMagnitude = 10.0f; // Strength of the avoidance push
    public LayerMask obstacleLayer; // define what are obstacles
    public float whiskerAngle = 30.0f; // Angle for side whiskers (degrees)
    public float sideWhiskerLengthMultiplier = 0.7f; // Side whiskers are shorter

    // Enum to define the fish's possible states
    public enum FishState
    {
        Wandering,
        SeekingFood,
        PursuingTarget
    }

    void Start()
    {
        wanderTimer = 0; // Calculate initial wander target immediately
        CalculateNewWanderTarget(); // Set an initial wander target
    }

    void Update()
    {
        // --- Perception Phase ---
        DetectFood(); // Update currentFoodTarget if food is found

        // --- State Determination Phase ---
        // Priority: Pursue > SeekFood > Wander
        if (pursueTarget != null && Vector3.Distance(transform.position, pursueTarget.position) < pursuitDetectionRadius)
        {
            if (currentState != FishState.PursuingTarget)
            {
                Debug.Log(gameObject.name + " changing to Pursue state for: " + pursueTarget.name);
                currentState = FishState.PursuingTarget;
                currentFoodTarget = null; // Stop seeking food if pursuing a target
            }
        }
        else if (currentFoodTarget != null) // Food is detected and in range (checked in DetectFood)
        {
            if (currentState != FishState.SeekingFood)
            {
                Debug.Log(gameObject.name + " changing to SeekFood state for: " + currentFoodTarget.name);
                currentState = FishState.SeekingFood;
            }
        }
        else
        {
            if (currentState != FishState.Wandering)
            {
                Debug.Log(gameObject.name + " changing to Wander state.");
                currentState = FishState.Wandering;
            }
        }

        // --- Behavior Execution Phase ---
        Vector3 targetDirectionForState = Vector3.zero; // The direction the current state wants to go

        switch (currentState)
        {
            case FishState.Wandering:
                wanderTimer -= Time.deltaTime;
                // If reached wander target or timer is up, pick a new one
                if (wanderTimer <= 0 || Vector3.Distance(transform.position, wanderTargetPosition) < 1.0f)
                {
                    CalculateNewWanderTarget();
                    wanderTimer = wanderChangeInterval;
                }
                targetDirectionForState = (wanderTargetPosition - transform.position).normalized;
                break;

            case FishState.SeekingFood:
                if (currentFoodTarget == null) // Food might have been eaten by another fish or destroyed
                {
                    currentState = FishState.Wandering; // Revert to wandering
                    DetectFood(); // Try to find new food immediately if possible
                    return; // Recalculate next frame
                }
                targetDirectionForState = (currentFoodTarget.position - transform.position).normalized;

                // Check if food is reached
                if (Vector3.Distance(transform.position, currentFoodTarget.position) < 0.8f) // Eating distance
                {
                    Debug.Log(gameObject.name + " reached and 'ate' food: " + currentFoodTarget.name);
                    Destroy(currentFoodTarget.gameObject); // "Eat" the food
                    currentFoodTarget = null;
                    currentState = FishState.Wandering; // Go back to wandering
                }
                break;

            case FishState.PursuingTarget:
                if (pursueTarget == null) // Target might have disappeared
                {
                    currentState = FishState.Wandering; // Revert to wandering
                    return; // Recalculate next frame
                }
                targetDirectionForState = (pursueTarget.position - transform.position).normalized;
                // (Vector3.Distance(transform.position, pursueTarget.position) < 1.0f) { /* Do something */ }
                break;
        }

        // --- Steering Calculation ---
        Vector3 steeringForState = targetDirectionForState * speed; // Desired velocity based on current state
        Vector3 steeringForAvoidance = CalculateObstacleAvoidance(); // Steering adjustment from obstacles

        // Combine steering behaviors
        Vector3 finalDesiredVelocity = steeringForState + steeringForAvoidance;

        // If obstacle avoidance is active, it might significantly alter the desired velocity.
        // We ensure the fish doesn't try to move faster than its max speed.
        if (finalDesiredVelocity.magnitude > speed)
        {
            finalDesiredVelocity = finalDesiredVelocity.normalized * speed;
        }
        // If avoidance is very strong and opposite to state steering, fish might slow down or stop.
        // If finalDesiredVelocity is near zero due to conflicting forces, let deceleration handle it.


        // --- Movement and Rotation Update ---
        // Smoothly change current velocity towards the final desired velocity
        if (finalDesiredVelocity.magnitude > 0.01f || currentVelocity.magnitude > 0.01f)
        {
            // If there's a direction we want to go, or if we are currently moving and need to stop
            if (finalDesiredVelocity.magnitude > 0.01f)
            {
                // Accelerate towards the target velocity
                currentVelocity = Vector3.MoveTowards(currentVelocity, finalDesiredVelocity, acceleration * Time.deltaTime);
            }
            else
            {
                // No desired movement, so decelerate to a stop
                currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Time.deltaTime);
            }
        }


        // Apply movement
        transform.position += currentVelocity * Time.deltaTime;

        // Apply rotation
        if (currentVelocity.magnitude > 0.05f) // Only rotate if moving significantly
        {
            // Rotate to face the direction of the current velocity
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void CalculateNewWanderTarget()
    {
        float wanderRange = 10.0f; // Define how far the fish can pick a new wander point
        Vector2 randomPointInCircle = Random.insideUnitCircle * wanderRange;

        // Assuming fish primarily move on the XZ plane.
        // The new target's Y position will be the fish's current Y position.
        wanderTargetPosition = transform.position + new Vector3(randomPointInCircle.x, 0, randomPointInCircle.y);
        // wanderTargetPosition.y = transform.position.y; // Explicitly keep y same if needed, but obstacle avoidance might change y slightly.

        // If you want true 3D wandering:
        // wanderTargetPosition = transform.position + Random.insideUnitSphere * wanderRange;

        // Optional: Clamp wanderTargetPosition to defined boundaries if your fish has a specific area.
        //wanderTargetPosition.x = Mathf.Clamp(wanderTargetPosition.x, minBounds.x, maxBounds.x);
        //wanderTargetPosition.y = Mathf.Clamp(wanderTargetPosition.y, minBounds.y, maxBounds.y);
        //wanderTargetPosition.z = Mathf.Clamp(wanderTargetPosition.z, minBounds.z, maxBounds.z);

        Debug.Log(gameObject.name + " new wander target: " + wanderTargetPosition);
    }

    void DetectFood()
    {
        // If already has a food target (and not pursuing something else), or currently pursuing, don't look for new food.
        // State machine will handle if currentFoodTarget becomes null (eaten).
        if (currentState == FishState.PursuingTarget)
        {
            currentFoodTarget = null; // Ensure no food target if pursuing
            return;
        }
        if (currentFoodTarget != null && currentState == FishState.SeekingFood) return;


        Collider[] hitColliders = Physics.OverlapSphere(transform.position, foodDetectionRadius);
        Transform closestFood = null;
        float minDistanceSqr = foodDetectionRadius * foodDetectionRadius + 1.0f; // Start with a value greater than max possible squared distance

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag(foodTag))
            {
                float distanceSqr = (transform.position - hitCollider.transform.position).sqrMagnitude;
                if (distanceSqr < minDistanceSqr)
                {
                    minDistanceSqr = distanceSqr;
                    closestFood = hitCollider.transform;
                }
            }
        }

        if (closestFood != null)
        {
            currentFoodTarget = closestFood;
            // The state will be updated in the Update() method's state determination phase.
        }
        else
        {
            currentFoodTarget = null; // Ensure it's null if no food found or current food is out of range/gone
        }
    }

    Vector3 CalculateObstacleAvoidance()
    {
        Vector3 avoidanceSteeringForce = Vector3.zero;
        int detectedObstaclesCount = 0;

        // Define whisker directions based on fish's forward direction
        Vector3 forwardDir = transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, whiskerAngle, 0) * forwardDir;
        Vector3 leftDir = Quaternion.Euler(0, -whiskerAngle, 0) * forwardDir;

        Vector3[] whiskerDirections = {
            forwardDir,
            rightDir,
            leftDir
        };

        float[] whiskerLengths = {
            obstacleAvoidanceDistance,
            obstacleAvoidanceDistance * sideWhiskerLengthMultiplier,
            obstacleAvoidanceDistance * sideWhiskerLengthMultiplier
        };

        for (int i = 0; i < whiskerDirections.Length; i++)
        {
            RaycastHit hit;
            // Ray origin can be slightly in front of the fish's pivot for better results
            Vector3 rayOrigin = transform.position + forwardDir * 0.1f; // Small offset
            Debug.DrawRay(rayOrigin, whiskerDirections[i] * whiskerLengths[i], Color.red);

            if (Physics.Raycast(rayOrigin, whiskerDirections[i], out hit, whiskerLengths[i], obstacleLayer))
            {
                // Obstacle detected by this whisker
                Debug.DrawLine(rayOrigin, hit.point, Color.yellow); // Show hit ray segment

                // The closer the obstacle, the stronger the avoidance force from this whisker.
                // Force should be primarily away from the obstacle's surface (along hit.normal).
                float proximityFactor = 1.0f - (hit.distance / whiskerLengths[i]); // 0 (far) to 1 (close)
                avoidanceSteeringForce += hit.normal * proximityFactor;
                detectedObstaclesCount++;
            }
        }

        if (detectedObstaclesCount > 0)
        {
            // Average the avoidance vectors (or just sum them) and scale
            avoidanceSteeringForce = (avoidanceSteeringForce / detectedObstaclesCount).normalized * obstacleAvoidanceForceMagnitude;
            Debug.DrawRay(transform.position, avoidanceSteeringForce, Color.blue); // Show combined avoidance force
            return avoidanceSteeringForce;
        }

        return Vector3.zero; // No obstacles detected, no avoidance force
    }

    void OnDrawGizmosSelected()
    {
        // Food Detection Radius
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.25f); // Yellow, semi-transparent
        Gizmos.DrawSphere(transform.position, foodDetectionRadius);

        // Pursuit Detection Radius
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f); // Cyan, semi-transparent
        Gizmos.DrawSphere(transform.position, pursuitDetectionRadius);

        // Show current wander target if wandering
        if (currentState == FishState.Wandering && Application.isPlaying) // Only if playing and in wander state
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(wanderTargetPosition, 0.3f);
            Gizmos.DrawLine(transform.position, wanderTargetPosition);
        }

        // Obstacle avoidance whiskers are drawn using Debug.DrawRay in CalculateObstacleAvoidance
        // but you could add Gizmos for them here too if preferred (they'd show even when not playing).
    }
}