using UnityEngine;

// Corrente do rio: desloca um sprite ladrilhado e volta a cada ladrilho.
public class ScrollingTile : MonoBehaviour
{
    [SerializeField] private Vector2 velocity = new Vector2(0f, -1.2f);
    [SerializeField] private float tileSize = 4f;

    private Vector3 origin;
    private Vector2 offset;

    private void Awake()
    {
        origin = transform.position;
    }

    private void Update()
    {
        offset += velocity * Time.deltaTime;
        offset.x %= tileSize;
        offset.y %= tileSize;
        transform.position = origin + (Vector3)offset;
    }
}
