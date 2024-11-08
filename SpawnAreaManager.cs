using UnityEngine;

public class SpawnAreaManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("GameObject that defines the center of the spawn area")]
    public Transform spawnAreaCenter;
    
    [Tooltip("Spawn area size in X")]
    public float spawnAreaWidth = 10f;
    
    [Tooltip("Spawn area size in Z")]
    public float spawnAreaLength = 10f;
    
    [Tooltip("Agent's height when spawning")]
    public float spawnHeight = 0.5f;

    [Header("Visual Settings")]
    [Tooltip("Color of the spawn area in the editor")]
    public Color spawnAreaColor = new Color(1f, 1f, 0f, 0.2f);
    public Color spawnAreaWireColor = Color.yellow;
    public bool showGizmos = true;

    [Header("Layer Settings")]
    [Tooltip("Layer for ground verification")]
    public LayerMask groundCheckLayer;

    private void OnValidate()
    {
        // Ensures minimum values for dimensions
        spawnAreaWidth = Mathf.Max(1f, spawnAreaWidth);
        spawnAreaLength = Mathf.Max(1f, spawnAreaLength);
        spawnHeight = Mathf.Max(0.1f, spawnHeight);

        // Auto-references the center if not set
        if (spawnAreaCenter == null)
            spawnAreaCenter = transform;
    }

    public Vector3 GetRandomSpawnPosition()
    {
        if (spawnAreaCenter == null)
        {
            Debug.LogError("Spawn Area Center not defined!");
            return transform.position;
        }

        // Calculates the bounds based on center and size
        float halfWidth = spawnAreaWidth / 2f;
        float halfLength = spawnAreaLength / 2f;

        // Generates a random position within the defined area
        float randomX = Random.Range(-halfWidth, halfWidth);
        float randomZ = Random.Range(-halfLength, halfLength);

        // Final position relative to the spawn area center
        Vector3 spawnPosition = spawnAreaCenter.position + 
            spawnAreaCenter.right * randomX + 
            spawnAreaCenter.forward * randomZ;

        // Adjusts the height with a raycast
        if (Physics.Raycast(spawnPosition + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 5f, groundCheckLayer))
        {
            spawnPosition.y = hit.point.y + spawnHeight;
        }
        else
        {
            spawnPosition.y = spawnAreaCenter.position.y + spawnHeight;
        }

        return spawnPosition;
    }

    public void RespawnAgent(GameObject agent)
    {
        if (agent == null) return;

        // Sets a new position
        agent.transform.position = GetRandomSpawnPosition();
        
        // Sets a random rotation
        agent.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Resets velocities if Rigidbody is present
        if (agent.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || spawnAreaCenter == null) return;

        // Matrix to rotate the area with the center
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(
            spawnAreaCenter.position, 
            spawnAreaCenter.rotation, 
            Vector3.one
        );
        Gizmos.matrix = rotationMatrix;

        // Draws the solid area
        Gizmos.color = spawnAreaColor;
        Vector3 size = new Vector3(spawnAreaWidth, 0.1f, spawnAreaLength);
        Gizmos.DrawCube(Vector3.zero, size);

        // Draws the outline
        Gizmos.color = spawnAreaWireColor;
        Gizmos.DrawWireCube(Vector3.zero, size);

        // Resets the matrix
        Gizmos.matrix = Matrix4x4.identity;

        // Draws points at the corners of the area
        Vector3[] corners = GetSpawnAreaCorners();
        float pointSize = 0.2f;
        Gizmos.color = Color.red;
        foreach (Vector3 corner in corners)
        {
            Gizmos.DrawSphere(corner, pointSize);
        }
    }

    private Vector3[] GetSpawnAreaCorners()
    {
        if (spawnAreaCenter == null) return new Vector3[0];

        float halfWidth = spawnAreaWidth / 2f;
        float halfLength = spawnAreaLength / 2f;

        Vector3[] corners = new Vector3[4];
        Vector3 center = spawnAreaCenter.position;
        Vector3 right = spawnAreaCenter.right;
        Vector3 forward = spawnAreaCenter.forward;

        corners[0] = center + (right * halfWidth) + (forward * halfLength);    // Front Right
        corners[1] = center + (right * halfWidth) - (forward * halfLength);    // Back Right
        corners[2] = center - (right * halfWidth) - (forward * halfLength);    // Back Left
        corners[3] = center - (right * halfWidth) + (forward * halfLength);    // Front Left

        return corners;
    }
}
