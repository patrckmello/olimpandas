using System;
using System.Linq;
using TMPro;
using UnityEngine;

// Painel de controles do menu: preenche cada cartão com os textos do MinigameCatalog,
// os mesmos que aparecem no sorteio.
public class ControlsPanel : MonoBehaviour
{
    [Serializable]
    public class Row
    {
        public string sceneName;
        public TMP_Text nameText;
        public TMP_Text goalText;
        public TMP_Text controlsP1Text;
        public TMP_Text controlsP2Text;
    }

    [SerializeField] private Row[] rows;

    private void Awake()
    {
        foreach (Row row in rows)
        {
            Minigame minigame = MinigameCatalog.All.FirstOrDefault(m => m.SceneName == row.sceneName);
            if (minigame == null)
                continue;

            row.nameText.text = minigame.DisplayName.ToUpperInvariant();
            row.goalText.text = minigame.Goal;
            row.controlsP1Text.text = minigame.ControlsP1;
            row.controlsP2Text.text = minigame.ControlsP2;
        }
    }
}
