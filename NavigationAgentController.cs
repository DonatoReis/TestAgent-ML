using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(AgentMovement))]
[RequireComponent(typeof(AgentRewardSystem))]
[RequireComponent(typeof(AgentObservationSystem))]
[RequireComponent(typeof(AgentObjectiveSystem))]
public class NavigationAgentController : Agent
{
    [Header("Required Components")]
    [SerializeField] public AgentMovement movementSystem;
    [SerializeField] private AgentRewardSystem rewardSystem;
    [SerializeField] public AgentObservationSystem observationSystem;
    [SerializeField] public AgentObjectiveSystem objectiveSystem;
    [SerializeField] private SpawnAreaManager spawnManager;

    public int lessonNumber;

    protected override void Awake() // Added 'override' here
    {
        base.Awake(); // Calls the base class's Awake
        ValidateComponents();
    }

    private void ValidateComponents()
    {
        if (movementSystem == null)
            movementSystem = GetComponent<AgentMovement>();
        if (rewardSystem == null)
            rewardSystem = GetComponent<AgentRewardSystem>();
        if (observationSystem == null)
            observationSystem = GetComponent<AgentObservationSystem>();
        if (objectiveSystem == null)
            objectiveSystem = GetComponent<AgentObjectiveSystem>();
        if (spawnManager == null)
            spawnManager = GetComponentInParent<SpawnAreaManager>();


        // Checks and warns about missing components
        if (spawnManager == null)
            Debug.LogError("SpawnAreaManager not found in the scene! Please add a SpawnAreaManager.");

        // Passes the reference to the ObjectiveSystem
        if (objectiveSystem != null && spawnManager != null)
            objectiveSystem.spawnManager = spawnManager;
    }

    public override void Initialize()
    {
        ValidateComponents();
        
        movementSystem.InitializeMovement(this);
        rewardSystem.InitializeRewards(this);
        observationSystem.InitializeObservations(this);
        objectiveSystem.InitializeObjectives(this);
    }

    public override void OnEpisodeBegin()
    {
        // Get curriculum parameters
        float maxJumpHeight = Academy.Instance.EnvironmentParameters.GetWithDefault("maxJumpHeight", 1.0f);
        bool allowMovement = Academy.Instance.EnvironmentParameters.GetWithDefault("allowMovement", 1.0f) > 0.5f;
        bool allowJump = Academy.Instance.EnvironmentParameters.GetWithDefault("allowJump", 1.0f) > 0.5f;
        bool exploreRoom = Academy.Instance.EnvironmentParameters.GetWithDefault("exploreRoom", 1.0f) > 0.5f;
        bool hasObjective = Academy.Instance.EnvironmentParameters.GetWithDefault("hasObjective", 1.0f) > 0.5f;

        // Configure the agent and environment based on these parameters
        movementSystem.SetMovementAllowed(allowMovement);
        movementSystem.SetJumpAllowed(allowJump);
        movementSystem.SetMaxJumpHeight(maxJumpHeight);

        objectiveSystem.SetExplorationAllowed(exploreRoom);
        objectiveSystem.SetObjectiveActive(hasObjective);

        // Reset the systems
        movementSystem.ResetMovement();
        rewardSystem.ResetRewards();
        observationSystem.ResetObservations();
        objectiveSystem.ResetObjectives();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        observationSystem.CollectObservations(sensor);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        movementSystem.ProcessActions(actions);
        objectiveSystem.CheckObjectives();
        rewardSystem.ProcessRewards(
            objectiveSystem.GetCurrentState(),
            movementSystem.GetMovementData()
        );
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        movementSystem.ProcessHeuristic(in actionsOut);
    }

    private void FixedUpdate()
    {
        movementSystem.UpdateMovement();
        observationSystem.UpdateObservations();
    }

    private void OnCollisionEnter(Collision collision)
    {
        rewardSystem.ProcessCollision(collision);
    }
}
