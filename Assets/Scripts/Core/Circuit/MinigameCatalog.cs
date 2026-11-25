using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class Minigame
{
    public string DisplayName { get; }
    public string SceneName { get; }
    public string Goal { get; }
    public string ControlsP1 { get; }
    public string ControlsP2 { get; }

    public Minigame(string displayName, string sceneName,
        string goal = "", string controlsP1 = "", string controlsP2 = "")
    {
        DisplayName = displayName;
        SceneName = sceneName;
        Goal = goal;
        ControlsP1 = controlsP1;
        ControlsP2 = controlsP2;
    }
}

// Modalidades que entram no sorteio do Circuito Olímpico.
// Prova nova: adicionar aqui e incluir a cena no Build Settings.
public static class MinigameCatalog
{
    public static readonly IReadOnlyList<Minigame> All = new[]
    {
        new Minigame("100m com barreiras", "HurdlesRace",
            "Pule as barreiras e cruze a chegada primeiro!",
            "A / D  correr\nW  pular   SHIFT  dash",
            "Setas  correr\nSeta cima  pular   SHIFT dir.  dash"),
        new Minigame("Salto a distância", "LongJump",
            "Três tentativas: ganhe velocidade e pule antes da linha!",
            "Alterne A e D  correr\nW  pular",
            "Alterne setas esq./dir.  correr\nSeta cima  pular"),
        new Minigame("Canoagem", "Canoeing",
            "Reme até a chegada e desvie das pedras!",
            "Alterne A e D  remar\nW / S  desviar",
            "Alterne setas esq./dir.  remar\nSetas cima / baixo  desviar"),
        new Minigame("Pescaria", "Fishing",
            "Digite as setas do peixe antes do rival. Cuidado com a bota!",
            "W A S D\nA 1ª seta escolhe o peixe",
            "Setas\nA 1ª seta escolhe o peixe"),
    };

    // Só as que podem ser carregadas (cena presente e habilitada no Build Settings).
    public static List<Minigame> Available() =>
        All.Where(m => Application.CanStreamedLevelBeLoaded(m.SceneName)).ToList();
}
