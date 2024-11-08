using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;

public class AgentObservationSystem : MonoBehaviour
{
    private NavigationAgentController agentController;
    private AgentMovement movementSystem;
    private AgentObjectiveSystem objectiveSystem;

    [Header("Raycast Settings")]
    public int numLowRaycasts = 7;
    public int numMediumRaycasts = 17;
    public int numHighRaycasts = 17;
    public float raycastFOV = 70f;
    public float rayLength = 10f;
    public LayerMask detectableLayers;

    [Header("Observation Settings")]
    public int stackedObservations = 6;

    private Queue<ObservationData> observationHistory;

    public void InitializeObservations(NavigationAgentController controller)
    {
        agentController = controller;
        movementSystem = controller.movementSystem;
        objectiveSystem = controller.objectiveSystem;

        observationHistory = new Queue<ObservationData>();
    }

    public void ResetObservations()
    {
        observationHistory.Clear();
        for (int i = 0; i < stackedObservations; i++)
        {
            observationHistory.Enqueue(new ObservationData
            {
                position = Vector3.zero,
                velocity = Vector3.zero,
                wasGrounded = false,
                distanceToDoor = 0f,
                distanceToRoom2 = 0f
            });
        }
    }

    public void UpdateObservations()
    {
        if (observationHistory.Count >= stackedObservations)
        {
            observationHistory.Dequeue();
        }

        var movementData = movementSystem.GetMovementData();
        var objectiveState = objectiveSystem.GetCurrentState();

        ObservationData obsData = new ObservationData
        {
            position = transform.position,
            velocity = movementData.velocity,
            wasGrounded = movementData.isGrounded,
            distanceToDoor = objectiveState.distanceToDoor,
            distanceToRoom2 = objectiveState.distanceToRoom2
        };

        observationHistory.Enqueue(obsData);
    }

    public void CollectObservations(VectorSensor sensor)
    {
        // Add the agent's position
        sensor.AddObservation(transform.position);

        // Add observation history
        foreach (var obs in observationHistory)
        {
            sensor.AddObservation(obs.position);
            sensor.AddObservation(obs.velocity);
            sensor.AddObservation(obs.wasGrounded ? 1.0f : 0.0f);
            sensor.AddObservation(obs.distanceToDoor);
            sensor.AddObservation(obs.distanceToRoom2);
        }

        // Perform raycasts
        CastRaycasts(sensor);
    }

    private void CastRaycasts(VectorSensor sensor)
    {
        // Low raycasts (pointing downward)
        CastRaysAtAngle(-15f, numLowRaycasts, sensor);

        // Medium raycasts (horizontal)
        CastRaysAtAngle(0f, numMediumRaycasts, sensor);

        // High raycasts (pointing upward)
        CastRaysAtAngle(15f, numHighRaycasts, sensor);
    }

    private void CastRaysAtAngle(float pitchAngle, int numRays, VectorSensor sensor)
    {
        Vector3 rayStart = transform.position; // Raycast origin is the center of the agent
        float angleStep = raycastFOV / (numRays - 1);
        float startAngle = -raycastFOV / 2;

        for (int i = 0; i < numRays; i++)
        {
            float yawAngle = startAngle + i * angleStep;

            // Calculate the raycast direction with yaw and pitch
            Quaternion rotation = Quaternion.Euler(pitchAngle, yawAngle + transform.eulerAngles.y, 0);
            Vector3 direction = rotation * Vector3.forward;

            RaycastHit hit;
            bool hasHit = Physics.Raycast(rayStart, direction, out hit, rayLength, detectableLayers);

            // Presence of object
            sensor.AddObservation(hasHit ? 1.0f : 0.0f);

            // Normalized distance
            sensor.AddObservation(hasHit ? hit.distance / rayLength : 1.0f);

            // Object type (one-hot encoding)
            float[] objectType = new float[4]; // Assuming 4 types of detectable objects
            if (hasHit)
            {
                int layerIndex = hit.collider.gameObject.layer;
                if (layerIndex == LayerMask.NameToLayer("Ground"))
                    objectType[0] = 1.0f;
                else if (layerIndex == LayerMask.NameToLayer("Wall"))
                    objectType[1] = 1.0f;
                else if (layerIndex == LayerMask.NameToLayer("Obstacle"))
                    objectType[2] = 1.0f;
                else if (layerIndex == LayerMask.NameToLayer("Platform"))
                    objectType[3] = 1.0f;
            }
            // Add the object type
            foreach (var val in objectType)
            {
                sensor.AddObservation(val);
            }
        }
    }

    // Updated method to draw gizmos with the new angle
    private void OnDrawGizmos()
    {
        // Low raycasts (pointing downward)
        DrawRaycastsGizmos(-15f, numLowRaycasts, Color.green);

        // Medium raycasts (horizontal)
        DrawRaycastsGizmos(0f, numMediumRaycasts, Color.yellow);

        // High raycasts (pointing upward)
        DrawRaycastsGizmos(15f, numHighRaycasts, Color.red);
    }

    private void DrawRaycastsGizmos(float pitchAngle, int numRays, Color color)
    {
        Gizmos.color = color;
        Vector3 rayStart = transform.position; // Raycast origin is the center of the agent
        float angleStep = raycastFOV / (numRays - 1);
        float startAngle = -raycastFOV / 2;

        for (int i = 0; i < numRays; i++)
        {
            float yawAngle = startAngle + i * angleStep;

            // Calculate the raycast direction with yaw and pitch
            Quaternion rotation = Quaternion.Euler(pitchAngle, yawAngle + transform.eulerAngles.y, 0);
            Vector3 direction = rotation * Vector3.forward;

            Gizmos.DrawLine(rayStart, rayStart + direction * rayLength);
        }
    }

    public struct ObservationData
    {
        public Vector3 position;
        public Vector3 velocity;
        public bool wasGrounded;
        public float distanceToDoor;
        public float distanceToRoom2;
    }
}
