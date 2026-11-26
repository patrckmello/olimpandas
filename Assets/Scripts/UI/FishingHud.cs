using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pescaria: placar dos dois, tempo restante e avisos de peixe, bota e erro.
public class FishingHud : MonoBehaviour
{
    [SerializeField] private FishingManager manager;
    [SerializeField] private TMP_Text scoreP1Text;
    [SerializeField] private TMP_Text scoreP2Text;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Image timeFill;
    [SerializeField] private HudPopup popup;
    [SerializeField] private Vector2 popupP1 = new Vector2(-600f, 160f);
    [SerializeField] private Vector2 popupP2 = new Vector2(600f, 160f);

    private void OnEnable()
    {
        manager.Caught += OnCaught;
        manager.Missed += OnMissed;
    }

    private void OnDisable()
    {
        manager.Caught -= OnCaught;
        manager.Missed -= OnMissed;
    }

    private void Update()
    {
        scoreP1Text.text = manager.GetScore(1).ToString();
        scoreP2Text.text = manager.GetScore(2).ToString();
        timeText.text = manager.IsSuddenDeath ? "!" : Mathf.CeilToInt(manager.RemainingTime).ToString();
        if (timeFill != null)
            timeFill.fillAmount = manager.IsSuddenDeath ? 1f : manager.RemainingTime / manager.RoundTime;
    }

    private void OnCaught(int playerId, FishKind kind)
    {
        string text = kind.IsJunk ? $"BOTA! {kind.points}" : kind.points >= 5 ? $"DOURADO! +{kind.points}" : $"PEIXE! +{kind.points}";
        popup.Show(text, kind.IsJunk ? HudColors.Bad : kind.points >= 5 ? HudColors.Gold : HudColors.Good, Side(playerId));
    }

    private void OnMissed(int playerId) => popup.Show("ERROU!", HudColors.Bad, Side(playerId) + Vector2.down * 60f);

    private Vector2 Side(int playerId) => playerId == 1 ? popupP1 : popupP2;
}
