using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class Minigame
{
    public string DisplayName { get; }
    public string SceneName { get; }

    public Minigame(string displayName, string sceneName)
    {
        DisplayName = displayName;
        SceneName = sceneName;
    }
}

// Modalidades que entram no sorteio do Circuito Olímpico.
// Prova nova: adicionar aqui e incluir a cena no Build Settings.
public static class MinigameCatalog
{
    public static readonly IReadOnlyList<Minigame> All = new[]
    {
        new Minigame("100m com barreiras", "HurdlesRace"),
        new Minigame("Salto a distância", "LongJump"),
    };

    // Só as que podem ser carregadas (cena presente e habilitada no Build Settings).
    public static List<Minigame> Available() =>
        All.Where(m => Application.CanStreamedLevelBeLoaded(m.SceneName)).ToList();
}
