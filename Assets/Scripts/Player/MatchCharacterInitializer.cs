
using UnityEngine;

public class MatchCharacterInitializer : MonoBehaviour
{
    private void Start()
    {
        CharacterSelectionManager selection =
            CharacterSelectionManager.Instance;

        if (selection == null)
        {
            Debug.LogError(
                "CharacterSelectionManager não encontrado."
            );
            return;
        }

        if (!selection.IsSelectionLocked)
        {
            selection.ConfirmSelection();
        }
    }
}

