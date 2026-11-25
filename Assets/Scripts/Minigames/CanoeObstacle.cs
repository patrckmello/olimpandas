using UnityEngine;

// Obstáculo do rio (pedra, tronco): não bloqueia a canoa, só derruba a velocidade
// e empurra para o lado. O collider precisa ser trigger.
[RequireComponent(typeof(Collider2D))]
public class CanoeObstacle : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float speedMultiplier = 0.35f;

    public float SpeedMultiplier => speedMultiplier;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }
}
