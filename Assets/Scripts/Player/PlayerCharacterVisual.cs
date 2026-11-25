
using UnityEngine;

public class PlayerCharacterVisual : MonoBehaviour
{
    [Header("Prefabs dos personagens")]
    [SerializeField] private GameObject pandaGigantePrefab;
    [SerializeField] private GameObject ursoPolarPrefab;
    [SerializeField] private GameObject grizzlyPrefab;
    [SerializeField] private GameObject ursoDeOculosPrefab;

    private PlayerController playerController;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError("PlayerCharacterVisual requer PlayerController no mesmo objeto.", this);
            return;
        }

        if (CharacterSelectionManager.Instance == null)
        {
            Debug.LogWarning(
                "CharacterSelectionManager não encontrado."
            );
            return;
        }

        CharacterType selectedCharacter =
            CharacterSelectionManager.Instance.GetCharacter(
                playerController.PlayerId
            );

        ApplyCharacter(selectedCharacter);
    }

    private void ApplyCharacter(CharacterType character)
    {
        GameObject selectedPrefab = character switch
        {
            CharacterType.UrsoPolar => ursoPolarPrefab,
            CharacterType.Grizzly => grizzlyPrefab,
            CharacterType.UrsoDeOculos => ursoDeOculosPrefab,
            _ => pandaGigantePrefab
        };

        if (selectedPrefab == null)
        {
            Debug.LogError(
                $"Prefab não configurado para {character}"
            );
            return;
        }

        Transform prefabVisual =
            selectedPrefab.transform.Find("Visual");

        Transform currentVisual =
            transform.Find("Visual");

        if (prefabVisual == null || currentVisual == null)
        {
            Debug.LogError(
                "Visual não encontrado no prefab ou no jogador."
            );
            return;
        }

        SpriteRenderer sourceRenderer =
            prefabVisual.GetComponent<SpriteRenderer>();

        Animator sourceAnimator =
            prefabVisual.GetComponent<Animator>();

        SpriteRenderer targetRenderer =
            currentVisual.GetComponent<SpriteRenderer>();

        Animator targetAnimator =
            currentVisual.GetComponent<Animator>();

        if (sourceRenderer == null ||
            sourceAnimator == null ||
            targetRenderer == null ||
            targetAnimator == null)
        {
            Debug.LogError("Componentes visuais não encontrados.");
            return;
        }

        targetRenderer.sprite = sourceRenderer.sprite;
        targetAnimator.runtimeAnimatorController =
            sourceAnimator.runtimeAnimatorController;

        currentVisual.localScale = prefabVisual.localScale;
        currentVisual.localPosition = prefabVisual.localPosition;
    }
}

