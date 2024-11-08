using UnityEngine;

public class AgentRewardSystem : MonoBehaviour
{
    [Header("Reward Settings")]
    public float recompensaPlacaRapida = 1.2f;
    public float recompensaPlacaMedia = 0.9f;
    public float recompensaPlacaLenta = 0.8f;
    public float recompensaExploracao = 0.5f;
    public float penalizacaoQueda = -0.5f;
    public float penalizacaoParede = -0.08f;
    public float penalizacaoChao = -0.05f;
    public float penalizacaoPorTempo = -0.0002f;
    public float recompensaMovimentoSaida = 1f;

    // New variables for jumping
    public float recompensaPuloBemSucedido = 0.7f;
    public float penalizacaoPuloFalho = -0.5f;

    private NavigationAgentController agentController;
    private float episodeStartTime;
    public LayerMask wallLayer;
    public LayerMask groundLayer;
    public LayerMask Obstacle; // New layer for obstacles

    private Vector3 lastPosition;
    private float inactivityTimer = 0f;
    private const float inactivityThreshold = 5f; // Time in seconds

    public void InitializeRewards(NavigationAgentController controller)
    {
        agentController = controller;
        lastPosition = transform.position;
    }

    public void Update()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        if (distanceMoved < 0.1f)
        {
            inactivityTimer += Time.deltaTime;
            if (inactivityTimer >= inactivityThreshold)
            {
                agentController.AddReward(-0.1f); // Penalizes for inactivity
                inactivityTimer = 0f; // Resets the timer
            }
        }
        else
        {
            inactivityTimer = 0f; // Resets if the agent moves
        }

        lastPosition = transform.position;
    }

    public void ResetRewards()
    {
        episodeStartTime = Time.time;
    }

    public void ProcessRewards(AgentObjectiveSystem.ObjectiveState objectiveState, AgentMovement.MovementData movementData)
    {
        float timeInEpisode = Time.time - episodeStartTime;

        // Rewards based on the objective
        if (objectiveState.reachedDoor && !objectiveState.previouslyReachedDoor)
        {
            if (timeInEpisode < 5f) agentController.AddReward(recompensaPlacaRapida);
            else if (timeInEpisode < 10f) agentController.AddReward(recompensaPlacaMedia);
            else agentController.AddReward(recompensaPlacaLenta);
        }

        // Rewards for exploration (Curriculum Learning)
        if (objectiveState.explorationAllowed && !objectiveState.reachedDoor)
        {
            agentController.AddReward(recompensaExploracao);
        }

        // Movement rewards
        if (!objectiveState.reachedDoor)
        {
            agentController.AddReward(-objectiveState.distanceToDoor * penalizacaoPorTempo);

            if (Vector3.Dot(movementData.velocity, objectiveState.directionToDoor) > 0)
            {
                agentController.AddReward(recompensaMovimentoSaida);
            }
        }
        else
        {
            agentController.AddReward(-objectiveState.distanceToRoom2 * penalizacaoPorTempo);
        }

        // Penalties
        if (movementData.position.y < -1f)
        {
            agentController.AddReward(penalizacaoQueda);
            agentController.EndEpisode();
        }

        agentController.AddReward(penalizacaoPorTempo);

        // Reward for jumping over an obstacle
        if (movementData.isJumpingOverObstacle)
        {
            agentController.AddReward(recompensaPuloBemSucedido);
        }

        // Penalty for colliding with an obstacle without jumping
        if (movementData.collidedWithObstacle)
        {
            agentController.AddReward(penalizacaoPuloFalho);
        }
    }

    public void ProcessCollision(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            agentController.AddReward(penalizacaoParede);
        }
        else if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            agentController.AddReward(penalizacaoChao);
        }
        else if (((1 << collision.gameObject.layer) & Obstacle) != 0)
        {
            // Penalty for colliding with an obstacle
            agentController.AddReward(penalizacaoPuloFalho);
        }
    }
}
