using UnityEngine;

public class MultiplayerCameraFollow : MonoBehaviour
{
    [Header("Jogadores")]
    [SerializeField] private Transform[] targets;

    [Header("Movimento da câmera")]
    [SerializeField] private float moveSmoothTime = 0.2f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("Zoom multiplayer")]
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 12f;
    [SerializeField] private float zoomPadding = 2f;
    [SerializeField] private float zoomSmoothTime = 0.2f;

    [Header("Câmera de vitória")]
    [SerializeField] private float victoryZoom = 3.5f;
    [SerializeField] private float victoryMoveSmoothTime = 0.35f;
    [SerializeField] private float victoryZoomSmoothTime = 0.35f;
    [SerializeField] private Vector3 victoryOffset =
        new Vector3(0f, 0.7f, -10f);

    [Header("Optional scene bounds")]
    [SerializeField] private SpriteRenderer backgroundBounds;

    private Camera cam;

    private Vector3 moveVelocity;
    private float zoomVelocity;

    private bool isFocusingWinner = false;
    private Transform winnerTarget;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (isFocusingWinner)
        {
            FollowWinner();
            ClampToBackground();
            return;
        }

        if (targets == null || targets.Length == 0)
            return;

        MoveCamera();
        ZoomCamera();
        ClampToBackground();
    }

    private void MoveCamera()
    {
        Bounds bounds = GetTargetsBounds();

        Vector3 targetPosition =
            bounds.center + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref moveVelocity,
            moveSmoothTime
        );
    }

    private void ZoomCamera()
    {
        Bounds bounds = GetTargetsBounds();

        float requiredHeight =
            bounds.size.y / 2f + zoomPadding;

        float requiredWidth =
            bounds.size.x / 2f + zoomPadding;

        float heightFromWidth =
            requiredWidth / cam.aspect;

        float targetZoom =
            Mathf.Max(
                requiredHeight,
                heightFromWidth
            );

        targetZoom = Mathf.Clamp(
            targetZoom,
            minZoom,
            maxZoom
        );

        cam.orthographicSize =
            Mathf.SmoothDamp(
                cam.orthographicSize,
                targetZoom,
                ref zoomVelocity,
                zoomSmoothTime
            );
    }

    private void FollowWinner()
    {
        if (winnerTarget == null)
            return;

        Vector3 targetPosition =
            winnerTarget.position + victoryOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref moveVelocity,
            victoryMoveSmoothTime
        );

        cam.orthographicSize =
            Mathf.SmoothDamp(
                cam.orthographicSize,
                victoryZoom,
                ref zoomVelocity,
                victoryZoomSmoothTime
            );
    }

    public void FocusOnWinner(Transform winner)
    {
        winnerTarget = winner;
        isFocusingWinner = true;

        // Zera as velocidades do SmoothDamp
        // para a transição começar limpa.
        moveVelocity = Vector3.zero;
        zoomVelocity = 0f;
    }

    public void StopWinnerFocus()
    {
        winnerTarget = null;
        isFocusingWinner = false;

        moveVelocity = Vector3.zero;
        zoomVelocity = 0f;
    }

    private void ClampToBackground()
    {
        if (backgroundBounds == null)
            return;

        const float margin = 0.05f;
        Bounds bounds = backgroundBounds.bounds;
        float aspect = Mathf.Max(cam.aspect, 0.01f);
        float maxHalfHeight = Mathf.Min(
            bounds.extents.y - margin,
            (bounds.extents.x - margin) / aspect
        );
        if (maxHalfHeight <= 0f)
            return;

        cam.orthographicSize = Mathf.Min(cam.orthographicSize, maxHalfHeight);
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * aspect;
        Vector3 position = transform.position;
        float x = Mathf.Clamp(position.x,
            bounds.min.x + halfWidth + margin,
            bounds.max.x - halfWidth - margin);
        float y = Mathf.Clamp(position.y,
            bounds.min.y + halfHeight + margin,
            bounds.max.y - halfHeight - margin);
        if (!Mathf.Approximately(x, position.x))
            moveVelocity.x = 0f;
        if (!Mathf.Approximately(y, position.y))
            moveVelocity.y = 0f;
        transform.position = new Vector3(x, y, position.z);
    }

    private Bounds GetTargetsBounds()
    {
        bool foundTarget = false;
        Bounds bounds = new Bounds();

        foreach (Transform target in targets)
        {
            if (target == null)
                continue;

            if (!foundTarget)
            {
                bounds = new Bounds(
                    target.position,
                    Vector3.zero
                );

                foundTarget = true;
            }
            else
            {
                bounds.Encapsulate(
                    target.position
                );
            }
        }

        return bounds;
    }
}
