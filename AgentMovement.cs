// AgentMovement.cs
using UnityEngine;
using Unity.MLAgents.Actuators;

public class AgentMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float rotateSpeed = 80f;

    [Header("Jump Settings")]
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public LayerMask platformLayer;
    public LayerMask obstacleLayer;
    public float maxJumpCooldown = 5f;
    public float maxJumpHeight = 8f;
    public float minJumpHeightThreshold = 0.5f; // New: minimum threshold to consider as a jump

    private Rigidbody rb;
    private Vector3 startPosition;
    private bool isGrounded;
    private float lastJumpTime;
    private NavigationAgentController agentController;
    private RaycastHit groundHit;

    // Variables for Curriculum Learning
    private bool movementAllowed = true;
    private bool jumpAllowed = true;

    // Variables to monitor the jump
    private bool isJumpingOverObstacle = false;
    private bool collidedWithObstacle = false;
    private bool wasGrounded = true;
    private float maxHeightReached = 0f;
    private float jumpStartHeight = 0f;

    public struct MovementData
    {
        public Vector3 position;
        public Vector3 velocity;
        public bool isGrounded;
        public Vector3 forward;
        public bool isJumpingOverObstacle;
        public bool collidedWithObstacle;
        public float currentJumpHeight;
        public bool isJumping;
    }

    public void InitializeMovement(NavigationAgentController controller)
    {
        agentController = controller;
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        lastJumpTime = -maxJumpCooldown;

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.mass = 1f;
        rb.linearDamping = 0f;
    }

    public void ResetMovement()
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        lastJumpTime = -maxJumpCooldown;

        // Reset jump flags
        isJumpingOverObstacle = false;
        collidedWithObstacle = false;
        wasGrounded = true;
        maxHeightReached = 0f;
        jumpStartHeight = 0f;
    }

    public void UpdateMovement()
    {
        isGrounded = Physics.SphereCast(
            transform.position + Vector3.up * groundCheckRadius,
            groundCheckRadius,
            Vector3.down,
            out groundHit,
            groundCheckRadius * 2,
            groundLayer | platformLayer
        );

        // Improved jump detection logic
        if (!isGrounded && wasGrounded)
        {
            // Started a jump
            jumpStartHeight = transform.position.y;
            maxHeightReached = jumpStartHeight;
        }
        else if (!isGrounded)
        {
            // During the jump
            maxHeightReached = Mathf.Max(maxHeightReached, transform.position.y);
        }
        else if (!wasGrounded && isGrounded)
        {
            // Ended a jump
            float jumpHeight = maxHeightReached - jumpStartHeight;
            if (jumpHeight > minJumpHeightThreshold)
            {
                isJumpingOverObstacle = true;
            }
        }

        wasGrounded = isGrounded;
    }

    private Vector3 acceleration;

    public Vector3 GetAcceleration()
    {
        return acceleration;
    }

    public void ProcessActions(ActionBuffers actions)
    {
        // Reset jump flags each action
        isJumpingOverObstacle = false;
        collidedWithObstacle = false;

        float moveForward = movementAllowed ? actions.ContinuousActions[0] : 0f;
        float rotate = movementAllowed ? actions.ContinuousActions[1] : 0f;
        bool jump = actions.DiscreteActions[0] == 1 && jumpAllowed;

        // Movement
        Vector3 movement = transform.forward * moveForward * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

        // Rotation
        transform.Rotate(0, rotate * rotateSpeed * Time.fixedDeltaTime, 0);

        // Jump
        if (jump && isGrounded && Time.time > lastJumpTime + maxJumpCooldown)
        {
            float gravity = Physics.gravity.y;
            float jumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * maxJumpHeight);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);
            lastJumpTime = Time.time;
            jumpStartHeight = transform.position.y;
        }
    }

    public void ProcessHeuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;

        continuousActionsOut[0] = Input.GetKey(KeyCode.W) ? 1.0f : Input.GetKey(KeyCode.S) ? -1.0f : 0.0f;
        continuousActionsOut[1] = Input.GetKey(KeyCode.A) ? -1.0f : Input.GetKey(KeyCode.D) ? 1.0f : 0.0f;
        discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    public MovementData GetMovementData()
    {
        return new MovementData
        {
            position = transform.position,
            velocity = rb.linearVelocity,
            isGrounded = isGrounded,
            forward = transform.forward,
            isJumpingOverObstacle = isJumpingOverObstacle,
            collidedWithObstacle = collidedWithObstacle,
            currentJumpHeight = maxHeightReached - jumpStartHeight,
            isJumping = !isGrounded && rb.linearVelocity.y > 0
        };
    }

    public void SetMovementAllowed(bool allowed)
    {
        movementAllowed = allowed;
        if (!allowed)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    public void SetJumpAllowed(bool allowed)
    {
        jumpAllowed = allowed;
    }

    public void SetMaxJumpHeight(float height)
    {
        maxJumpHeight = height;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            if (!isGrounded && rb.linearVelocity.y > 0)
            {
                isJumpingOverObstacle = true;
            }
            else
            {
                collidedWithObstacle = true;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            collidedWithObstacle = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * groundCheckRadius, groundCheckRadius);
        }
    }
}
