using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Salto: tentativa atual, melhor marca de cada um e avisos de falta/perfeito/marca.
public class LongJumpHud : MonoBehaviour
{
    [SerializeField] private LongJumpManager manager;
    [SerializeField] private TMP_Text attemptText;
    [SerializeField] private TMP_Text bestP1Text;
    [SerializeField] private TMP_Text bestP2Text;
    [SerializeField] private HudPopup popup;
    [SerializeField] private Vector2 popupP1 = new Vector2(-560f, 120f);
    [SerializeField] private Vector2 popupP2 = new Vector2(560f, 120f);

    private void OnEnable()
    {
        manager.AttemptStarted += OnAttempt;
        manager.AttemptResolved += OnResolved;
        manager.PerfectTakeoff += OnPerfect;
    }

    private void OnDisable()
    {
        manager.AttemptStarted -= OnAttempt;
        manager.AttemptResolved -= OnResolved;
        manager.PerfectTakeoff -= OnPerfect;
    }

    private void Start()
    {
        attemptText.text = $"TENTATIVA 1/{manager.MaxAttempts}";
        bestP1Text.text = bestP2Text.text = "MELHOR: —";
    }

    private void OnAttempt(int attempt) =>
        attemptText.text = manager.IsTiebreak ? "DESEMPATE" : $"TENTATIVA {attempt}/{manager.MaxAttempts}";

    private void OnPerfect(int playerId) =>
        popup.Show("PERFEITO!", HudColors.Gold, Side(playerId) + Vector2.up * 70f);

    private void OnResolved(int playerId, bool valid, float mark)
    {
        popup.Show(valid ? HudColors.Meters(mark) : "FALTA!", valid ? Color.white : HudColors.Bad, Side(playerId));
        float best = manager.GetBestMark(playerId);
        (playerId == 1 ? bestP1Text : bestP2Text).text =
            best == LongJumpManager.NoMark ? "MELHOR: —" : "MELHOR: " + HudColors.Meters(best);
    }

    private Vector2 Side(int playerId) => playerId == 1 ? popupP1 : popupP2;
}
