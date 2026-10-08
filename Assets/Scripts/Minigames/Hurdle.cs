using System.Collections;
using UnityEngine;

public class Hurdle : MonoBehaviour
{
    [Header("Optional falling sprites, in order")]
    [SerializeField] private Sprite[] fallFrames;
    [SerializeField] private float frameDuration = 0.08f;
    [SerializeField] private float fallbackFallDuration = 0.25f;

    private Collider2D obstacleCollider;
    private Transform visual;
    private SpriteRenderer visualRenderer;

    public bool IsFallen { get; private set; }

    private void Awake()
    {
        FindParts();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<PlayerController>() == null)
            return;

        float direction = collision.transform.position.x < transform.position.x ? -1f : 1f;
        Drop(direction);
    }

    public void Hit()
    {
        Drop(-1f);
    }

    private void Drop(float direction)
    {
        if (IsFallen)
            return;

        FindParts();
        IsFallen = true;
        if (obstacleCollider != null)
            obstacleCollider.enabled = false;

        if (Application.isPlaying)
            StartCoroutine(PlayFall(direction));
        else if (visual != null)
            visual.localRotation = Quaternion.Euler(0f, 0f, direction * 90f);
    }

    private IEnumerator PlayFall(float direction)
    {
        if (visualRenderer != null && fallFrames != null && fallFrames.Length > 1)
        {
            foreach (Sprite frame in fallFrames)
            {
                if (frame != null)
                    visualRenderer.sprite = frame;
                yield return new WaitForSeconds(frameDuration);
            }
            yield break;
        }

        if (visual == null)
            yield break;

        Quaternion start = visual.localRotation;
        Quaternion end = Quaternion.Euler(0f, 0f, direction * 90f);
        float elapsed = 0f;
        while (elapsed < fallbackFallDuration)
        {
            elapsed += Time.deltaTime;
            visual.localRotation = Quaternion.Slerp(start, end,
                Mathf.Clamp01(elapsed / fallbackFallDuration));
            yield return null;
        }
        visual.localRotation = end;
    }

    private void FindParts()
    {
        if (obstacleCollider == null)
            obstacleCollider = GetComponent<Collider2D>();
        if (visual == null)
            visual = transform.Find("Visual");
        if (visualRenderer == null && visual != null)
            visualRenderer = visual.GetComponentInChildren<SpriteRenderer>();
    }
}
