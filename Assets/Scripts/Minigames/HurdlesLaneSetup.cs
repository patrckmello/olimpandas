using UnityEngine;

public class HurdlesLaneSetup : MonoBehaviour
{
    [SerializeField] private Collider2D player1;
    [SerializeField] private Collider2D player2;
    [SerializeField] private Collider2D[] upperLane;
    [SerializeField] private Collider2D[] lowerLane;

    private void Awake()
    {
        Configure();
    }

    public void Configure()
    {
        if (player1 == null || player2 == null)
        {
            Debug.LogError("HurdlesLaneSetup needs both player colliders.", this);
            return;
        }

        Physics2D.IgnoreCollision(player1, player2, true);
        foreach (Collider2D obstacle in lowerLane)
            if (obstacle != null)
                Physics2D.IgnoreCollision(player1, obstacle, true);
        foreach (Collider2D obstacle in upperLane)
            if (obstacle != null)
                Physics2D.IgnoreCollision(player2, obstacle, true);
    }
}
