using System.Collections;
using TMPro;
using UnityEngine;

// Aviso rápido que cresce, sobe e some (FALTA!, PEIXE! +2, BATEU!).
// Usa o tempo real: a câmera lenta e a pausa não seguram o aviso na tela.
public class HudPopup : MonoBehaviour
{
    [Tooltip("Texto modelo, desligado.")]
    [SerializeField] private TMP_Text template;
    [SerializeField] private float duration = 1.1f;
    [SerializeField] private float rise = 90f;

    public void Show(string text, Color color, Vector2 position)
    {
        TMP_Text popup = Instantiate(template, template.transform.parent);
        popup.text = text;
        popup.color = color;
        popup.gameObject.SetActive(true);
        StartCoroutine(Animate(popup, position));
    }

    private IEnumerator Animate(TMP_Text popup, Vector2 position)
    {
        var rect = (RectTransform)popup.transform;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            rect.anchoredPosition = position + Vector2.up * (rise * k);
            rect.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.4f, 1.15f, k / 0.15f) : Mathf.Lerp(1.15f, 1f, (k - 0.15f) * 4f));
            popup.alpha = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
            yield return null;
        }
        Destroy(popup.gameObject);
    }
}
