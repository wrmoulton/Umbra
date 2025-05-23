using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EnemyPatrol : MonoBehaviour
{
    public Transform[] patrolPoints;
    public Transform player;
    public float speed = 2f;
    public float visionLength = 5f;
    public float visionAngle = 40f;
    public LayerMask obstacleLayer;
    public GameObject gameOverScreen;   // Assign this in the Inspector
    public Image detectionFill;         // Assign a UI Image for the detection progress bar
    public float detectionTime = 2f;    // Time (in seconds) to fully detect the player

    private int currentPointIndex = 0;
    public bool isPaused = false;
    private int facingDirection = 1;    // 1 for right, -1 for left
    private float currentDetection = 0f; // Current detection progress
    private ShadowFormController1 playerShadowForm; // Reference to the player's shadow form script

    public float detectionDecreaseRate = 2f;
    private bool isPlayerDetected = false;

    public Transform visionOrigin;

    private Vector3? lastKnownPlayerPosition = null;
    private bool isInvestigating = false;

    void Start()
    {
        // Find the player's ShadowFormController script
        if (player != null)
        {
            playerShadowForm = player.GetComponent<ShadowFormController1>();
        }

        // Initialize detection fill
        if (detectionFill != null)
        {
            detectionFill.fillAmount = 0f;
        }
    }

    void Update()
    {
        if (isInvestigating)
        {
            InvestigateLastKnownPosition();
            return;
        }

        if (!isPaused)
        {
            Patrol();
        }

        CheckVision();

        // For the DetectionIndicator to follow the enemy
        if (detectionFill != null)
        {
            detectionFill.transform.position = transform.position + new Vector3(0, 1, 0);
        }
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0 || isPaused) return;

        transform.position = Vector2.MoveTowards(transform.position, patrolPoints[currentPointIndex].position, speed * Time.deltaTime);

        if (transform.position.x < patrolPoints[currentPointIndex].position.x)
        {
            transform.localScale = new Vector3(1, 1, 1);
            facingDirection = 1; // Enemy facing right
        }
        else
        {
            transform.localScale = new Vector3(-1, 1, 1);
            facingDirection = -1; // Enemy facing left
        }

        if (Vector2.Distance(transform.position, patrolPoints[currentPointIndex].position) < 0.1f)
        {
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        }
    }

    void InvestigateLastKnownPosition()
    {
        if (lastKnownPlayerPosition == null) return;

        Vector2 direction = ((Vector2)lastKnownPlayerPosition.Value - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, lastKnownPlayerPosition.Value);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, obstacleLayer);

        if (hit.collider == null)
        {
            transform.position = Vector2.MoveTowards(transform.position, lastKnownPlayerPosition.Value, speed * Time.deltaTime);
        }
        else
        {
            lastKnownPlayerPosition = null;
            isInvestigating = false;
            isPaused = false;
        }

        if (Vector2.Distance(transform.position, lastKnownPlayerPosition.Value) < 0.1f)
        {
            lastKnownPlayerPosition = null;
            isInvestigating = false;
            isPaused = false;
        }
    }



    void CheckVision()
    {
        if (player == null || playerShadowForm == null)
        {
            return;
        }

        // Skip detection if the player is in shadow form
        if (playerShadowForm.isShadowForm)
        {
            ResetDetection();
            return;
        }

        Vector2 directionToPlayer = (player.position - visionOrigin.position).normalized;
        float distanceToPlayer = Vector2.Distance(visionOrigin.position, player.position);

        // Calculate the enemy's forward direction
        Vector2 enemyForward = facingDirection == 1 ? Vector2.right : Vector2.left;
        float angleToPlayer = Vector2.Angle(enemyForward, directionToPlayer);

        // Check if player is within vision cone
        if (angleToPlayer < visionAngle / 2 && distanceToPlayer <= visionLength)
        {
            // Use OverlapCircle to detect the player
            Collider2D[] hits = Physics2D.OverlapCircleAll(visionOrigin.position, visionLength);

            bool playerDetectedInOverlap = false;
            foreach (Collider2D hit in hits)
            {
                if (hit.transform == player)
                {
                    playerDetectedInOverlap = true;
                    break;
                }
            }

            if (playerDetectedInOverlap)
            {
                // Line-of-sight check for obstacles
                RaycastHit2D hitInfo = Physics2D.Raycast(visionOrigin.position, directionToPlayer, distanceToPlayer, obstacleLayer);

                if (hitInfo.collider == null)
                {
                    // Means Raycast hit nothing, so no obstacle is in the way
                    isPlayerDetected = true;
                    lastKnownPlayerPosition = player.position;
                    isInvestigating = false;

                    currentDetection += Time.deltaTime / detectionTime;
                    if (detectionFill != null)
                        detectionFill.fillAmount = currentDetection;

                    if (currentDetection >= 1f)
                    {
                        TriggerGameOver();
                    }
                }
                else
                {
                    // Raycast hit something, presumably a wall or obstacle
                    isPlayerDetected = false;
                }
            }
            else
            {
                isPlayerDetected = false;
            }
        }
        else
        {
            isPlayerDetected = false;
        }

        // Decrease indicator if the player is not detected
        if (!isPlayerDetected && currentDetection > 0)
        {
            currentDetection -= Time.deltaTime / detectionDecreaseRate;
            currentDetection = Mathf.Clamp(currentDetection, 0, 1);

            if (detectionFill != null)
            {
                detectionFill.fillAmount = currentDetection;
            }
        }

        if (!isPlayerDetected && lastKnownPlayerPosition != null && !isInvestigating)
        {
            isPaused = true;           // Pause patrol
            isInvestigating = true;    // Start investigation behavior
        }
    }

    void ResetDetection()
    {
        // Gradually decrease detection progress
        if (currentDetection > 0)
        {
            currentDetection -= Time.deltaTime / detectionDecreaseRate;
            currentDetection = Mathf.Clamp(currentDetection, 0, 1); // Ensure it doesn't go below 0

            // Update detection fill UI
            if (detectionFill != null)
            {
                detectionFill.fillAmount = currentDetection;
            }

            // Hide the indicator when detection reaches 0
            if (currentDetection <= 0)
            {
                detectionFill.fillAmount = 0f;
            }
        }
        else
        {
            // Reset detection progress
            currentDetection = 0f;

            // Hide the DetectionIndicator
            if (detectionFill != null)
            {
                detectionFill.fillAmount = 0f;
            }
        }
    }

    // **Draw Vision Cone in Scene View**
    void OnDrawGizmos()
    {
        if (visionLength <= 0) return;

        // Set Gizmo color
        Gizmos.color = new Color(1, 1, 0, 0.3f); // Yellow with transparency

        // **Use facing direction to flip the vision cone**
        Vector3 forward = facingDirection * Vector3.right;

        // Calculate left and right boundaries of the vision cone
        Vector3 leftBoundary = Quaternion.Euler(0, 0, -visionAngle / 2) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, visionAngle / 2) * forward;

        // Convert to world space
        Vector3 leftEdge = transform.position + leftBoundary * visionLength;
        Vector3 rightEdge = transform.position + rightBoundary * visionLength;

        // Draw the vision cone as a solid shape
        Gizmos.DrawLine(visionOrigin.position, leftEdge);
        Gizmos.DrawLine(visionOrigin.position, rightEdge);
        Gizmos.DrawWireSphere(visionOrigin.position + forward * visionLength, 0.1f); // Small circle at max range

        // Fill the cone using a triangle
        Gizmos.color = new Color(1, 1, 0, 0.1f); // Even more transparent yellow
        Vector3[] coneVertices = { visionOrigin.position, leftEdge, rightEdge };
        Gizmos.DrawMesh(CreateMesh(coneVertices));
    }

    Mesh CreateMesh(Vector3[] vertices)
    {
        Mesh mesh = new Mesh();
        int[] triangles = { 0, 1, 2 }; // Triangle indices
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        return mesh;
    }

    void TriggerGameOver()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true); // Show Game Over screen
            Time.timeScale = 0f;
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // No need to reset time here anymore
    }


    public void QuitGame()
    {
        Application.Quit();
    }

}