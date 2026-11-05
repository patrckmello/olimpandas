using System.Collections.Generic;
using System.Linq;

// Sorteio do Circuito Olímpico: nunca repete enquanto houver modalidade
// inédita no circuito; quando todas já saíram, sorteia de novo evitando
// só repetir a última disputada.
public static class MinigameRotation
{
    public static Minigame Draw(
        IReadOnlyList<Minigame> available,
        IReadOnlyList<string> playedScenes,
        System.Random random)
    {
        if (available.Count == 0)
            return null;

        List<Minigame> options = available
            .Where(m => !playedScenes.Contains(m.SceneName))
            .ToList();

        if (options.Count == 0)
        {
            string last = playedScenes[playedScenes.Count - 1];
            options = available.Where(m => m.SceneName != last).ToList();
            if (options.Count == 0)
                options = available.ToList();
        }

        return options[random.Next(options.Count)];
    }
}
