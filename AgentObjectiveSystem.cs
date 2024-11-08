using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class AgentObjectiveSystem : MonoBehaviour
{
    [Header("Objectives")]
    public Transform doorTarget;
    public Transform room2Target;

    [Header("Settings")]
    public float doorReachDistance = 1.5f;
    public float room2ReachDistance = 1.5f;

    [Header("Timer Settings")]
    public float maxTimeInRoom = 30f;
    private float currentRoomTime;

    // Add serialized fields for adjustment in the Inspector
    [Header("Rewards")]
    [SerializeField] private float checkpointReward = 0.2f;
    [SerializeField] private float doorReward = 1.0f;
    [SerializeField] private float finalReward = 2.0f;
    [SerializeField] private float progressMultiplier = 0.1f;
    [SerializeField] private float timeExpiredPenalty = -0.5f;

    [Header("UI Elements")]
    public TMP_Text timerText;
    public TMP_Text generationText;
    public TMP_Text phaseText;
    private int currentGeneration = 0;

    [Header("Spawn Settings")]
    public SpawnAreaManager spawnManager;

    // State Variables
    private bool reachedDoor = false;
    private NavigationAgentController agentController;
    private Vector3 lastPosition;
    private const float inactivityThreshold = 5f;
    private Vector3 previousVelocity;
    private float previousDistanceToTarget;

    // Variables for Curriculum Learning
    private bool explorationAllowed = true;
    private bool objectiveActive = true;

    // Intermediate Checkpoint System
    [Header("Checkpoints")]
    [SerializeField] private List<GameObject> checkpoints;
    private bool[] checkpointsReached;

    public enum ObjectivePhase
    {
        ReachDoor,
        ReachRoom2
    }
    private ObjectivePhase currentPhase = ObjectivePhase.ReachDoor;

    public struct ObjectiveState
    {
        public float distanceToDoor;
        public float distanceToRoom2;
        public bool reachedDoor;
        public bool previouslyReachedDoor;
        public Vector3 directionToDoor;
        public Vector3 directionToRoom2;
        public float timeInRoom;
        public bool explorationAllowed;
        public int currentPhase;
        public Vector3 velocity;
        public Vector3 acceleration;
        public float angleToTarget;
    }

    private void Awake()
    {
        lastPosition = transform.position;
        previousVelocity = Vector3.zero;
    }

    public void InitializeObjectives(NavigationAgentController controller)
    {
        agentController = controller;
        ResetTimer();
        UpdateUI();

        // Initialize the previous distance variable
        previousDistanceToTarget = GetCurrentDistanceToTarget();

        // Initialize the checkpoints
        InitializeCheckpoints();
    }

    private void InitializeCheckpoints()
    {
        if (checkpoints != null && checkpoints.Count > 0)
        {
            checkpointsReached = new bool[checkpoints.Count];
        }
        else
        {
            checkpointsReached = new bool[0];
            Debug.LogWarning("No checkpoints defined. Please configure the checkpoints in the Inspector.");
        }
    }

    private void ResetTimer()
    {
        currentRoomTime = 0f;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timerText != null)
        {
            int remainingTime = Mathf.CeilToInt(maxTimeInRoom - currentRoomTime);
            timerText.text = remainingTime.ToString("00");
        }

        if (generationText != null)
        {
            generationText.text = currentGeneration.ToString("00");
        }

        if (phaseText != null)
        {
            phaseText.text = currentPhase.ToString();
        }
    }

    public void ResetObjectives()
    {
        ResetToInitialState();
    }

    private void ResetToInitialState()
    {
        currentPhase = ObjectivePhase.ReachDoor;
        reachedDoor = false;
        if (spawnManager != null)
        {
            spawnManager.RespawnAgent(gameObject);
        }
        currentGeneration++;
        ResetTimer();
        UpdateUI();

        // Reset the previous distance
        previousDistanceToTarget = GetCurrentDistanceToTarget();

        // Reset the checkpoints
        if (checkpointsReached != null)
        {
            for (int i = 0; i < checkpointsReached.Length; i++)
            {
                checkpointsReached[i] = false;
            }
        }
    }

    public void CheckObjectives()
    {
        var state = GetCurrentState();

        switch (currentPhase)
        {
            case ObjectivePhase.ReachDoor:
                HandleDoorPhase(state);
                break;

            case ObjectivePhase.ReachRoom2:
                HandleRoom2Phase(state);
                break;
        }

        // Check the checkpoints
        CheckCheckpoints();
    }

    private void HandleDoorPhase(ObjectiveState state)
    {
        currentRoomTime += Time.deltaTime;
        UpdateUI();

        // Gradual Penalty for Time
        if (currentRoomTime >= maxTimeInRoom)
        {
            float timeRatio = currentRoomTime / maxTimeInRoom;
            float penalty = Mathf.Lerp(timeExpiredPenalty * 0.2f, timeExpiredPenalty, timeRatio);
            agentController.AddReward(penalty);
            ResetToInitialState();
            return;
        }

        // Reward for Progress
        float progressReward = previousDistanceToTarget - state.distanceToDoor;
        if (progressReward > 0)
        {
            agentController.AddReward(progressReward * progressMultiplier);
        }
        previousDistanceToTarget = state.distanceToDoor;

        // Reached the door
        if (state.distanceToDoor < doorReachDistance)
        {
            reachedDoor = true;
            currentPhase = ObjectivePhase.ReachRoom2;
            agentController.AddReward(doorReward);
            ResetTimer();

            // Update the previous distance for the new phase
            previousDistanceToTarget = state.distanceToRoom2;
        }
    }

    private void HandleRoom2Phase(ObjectiveState state)
    {
        // Reward for Progress towards the final objective
        float progressReward = previousDistanceToTarget - state.distanceToRoom2;
        if (progressReward > 0)
        {
            agentController.AddReward(progressReward * progressMultiplier);
        }
        previousDistanceToTarget = state.distanceToRoom2;

        // Reached the final objective
        if (state.distanceToRoom2 < room2ReachDistance)
        {
            agentController.AddReward(finalReward);
            agentController.EndEpisode();
        }
    }

    private void CheckCheckpoints()
    {
        if (checkpoints == null || checkpointsReached == null) return;

        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (!checkpointsReached[i] && checkpoints[i] != null)
            {
                float distanceToCheckpoint = Vector3.Distance(transform.position, checkpoints[i].transform.position);
                if (distanceToCheckpoint < 1.0f)
                {
                    checkpointsReached[i] = true;
                    agentController.AddReward(checkpointReward);
                }
            }
        }
    }

    public ObjectiveState GetCurrentState()
    {
        Vector3 currentPosition = transform.position;
        Transform currentTarget = currentPhase == ObjectivePhase.ReachDoor ? doorTarget : room2Target;
        Vector3 targetPosition = currentTarget.position;
        Vector3 directionToTarget = (targetPosition - currentPosition).normalized;

        // Movement Information
        Vector3 velocity = agentController.movementSystem.GetMovementData().velocity;
        Vector3 acceleration = agentController.movementSystem.GetAcceleration();
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

        return new ObjectiveState
        {
            distanceToDoor = Vector3.Distance(currentPosition, doorTarget.position),
            distanceToRoom2 = Vector3.Distance(currentPosition, room2Target.position),
            reachedDoor = reachedDoor,
            previouslyReachedDoor = reachedDoor, // Updated here
            directionToDoor = (doorTarget.position - currentPosition).normalized,
            directionToRoom2 = (room2Target.position - currentPosition).normalized,
            timeInRoom = currentRoomTime,
            explorationAllowed = explorationAllowed,
            currentPhase = (int)currentPhase,
            velocity = velocity,
            acceleration = acceleration,
            angleToTarget = angleToTarget
        };
    }

    private float GetCurrentDistanceToTarget()
    {
        return currentPhase == ObjectivePhase.ReachDoor ?
            Vector3.Distance(transform.position, doorTarget.position) :
            Vector3.Distance(transform.position, room2Target.position);
    }

    // Methods for Curriculum Learning
    public void SetExplorationAllowed(bool allowed)
    {
        explorationAllowed = allowed;
    }

    public void SetObjectiveActive(bool active)
    {
        objectiveActive = active;
    }

    public void ResetGenerationCount()
    {
        currentGeneration = 0;
        UpdateUI();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Visualize door reach area
        if (doorTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(doorTarget.position, doorReachDistance);
        }

        // Visualize final objective reach area
        if (room2Target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(room2Target.position, room2ReachDistance);
        }

        // Draw direction lines
        var state = GetCurrentState();

        // Line to the door
        if (doorTarget != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, state.directionToDoor * 2f);
        }

        // Line to the final objective
        if (room2Target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, state.directionToRoom2 * 2f);
        }

        // Show remaining time when in the first phase
        if (!reachedDoor && timerText != null)
        {
            float remainingTime = maxTimeInRoom - currentRoomTime;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"Phase: {currentPhase}\n" +
                $"Time: {remainingTime:F1}s\n" +
                $"Distance: {state.distanceToDoor:F2}m\n" +
                $"Angle: {state.angleToTarget:F1}°");
        }

        // Draw the checkpoints
        if (checkpoints != null)
        {
            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (checkpoints[i] != null)
                {
                    Gizmos.color = checkpointsReached != null && checkpointsReached.Length > i && checkpointsReached[i] ? Color.green : Color.red;
                    Gizmos.DrawSphere(checkpoints[i].transform.position, 0.5f);
                }
            }
        }
    }

    private void OnValidate()
    {
        maxTimeInRoom = Mathf.Max(1f, maxTimeInRoom);
        doorReachDistance = Mathf.Max(0.1f, doorReachDistance);
        room2ReachDistance = Mathf.Max(0.1f, room2ReachDistance);

        doorReward = Mathf.Max(0f, doorReward);
        finalReward = Mathf.Max(0f, finalReward);
        progressMultiplier = Mathf.Clamp(progressMultiplier, 0f, 1f);
    }
}
