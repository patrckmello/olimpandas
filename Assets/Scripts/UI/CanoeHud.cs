using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Canoagem: barra de progresso com uma canoa por jogador e aviso de batida.
public class CanoeHud : MonoBehaviour
{
    [SerializeField] private CanoeRaceManager manager;
    [SerializeField] private RectTransform track;
    [SerializeField] private RectTransform markerP1;
    [SerializeField] private RectTransform markerP2;
    [SerializeField] private HudPopup popup;
    [SerializeField] private Vector2 popupP1 = new Vector2(-520f, 250f);
    [SerializeField] private Vector2 popupP2 = new Vector2(-520f, -170f);

    private void OnEnable() => manager.Collided += OnHit;
    private void OnDisable() => manager.Collided -= OnHit;

    private void Update()
    {
        float half = track.rect.width / 2f;
        Place(markerP1, manager.GetProgress(1), half);
        Place(markerP2, manager.GetProgress(2), half);
    }

    private static void Place(RectTransform marker, float progress, float half) =>
        marker.anchoredPosition = new Vector2(Mathf.Lerp(-half, half, progress), marker.anchoredPosition.y);

    private void OnHit(int playerId) =>
        popup.Show("BATEU!", HudColors.Bad, playerId == 1 ? popupP1 : popupP2);
}
