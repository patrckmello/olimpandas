using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;

    float elapsedTime;
    bool isRunning;

    public float ElapsedTime => elapsedTime;

    // 00:12,4
    public static string Format(float seconds) =>
        $"{(int)(seconds / 60f):00}:{(seconds % 60f).ToString("00.0", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',')}";

    public void StartTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    void Update()
    {
        if (!isRunning) return;

        if (timerText == null)
        {
            Debug.LogError("Timer Text está vazio neste objeto!", this);
            isRunning = false;
            return;
        }

        elapsedTime += Time.deltaTime;
        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}