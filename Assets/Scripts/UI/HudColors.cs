using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Cores e formatos comuns aos HUDs das provas.
public static class HudColors
{
    public static readonly Color Gold = new Color(1f, 0.79f, 0.24f);
    public static readonly Color Bad = new Color(1f, 0.42f, 0.36f);
    public static readonly Color Good = new Color(0.55f, 0.95f, 0.45f);
    public static readonly Color P1 = new Color(0.66f, 0.82f, 1f);
    public static readonly Color P2 = new Color(1f, 0.73f, 0.7f);

    public static string Meters(float mark) =>
        mark.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',') + " m";
}
