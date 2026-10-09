# Integração do menu de seleção de personagens — Olimpandas

> Estado de referência: 09/10/2026, branch `main` no commit `90a1763` + testes informados no Unity. Este documento descreve a integração; a tela de escolha ainda **não está implementada**.

## Objetivo

Permitir a escolha **independente** do personagem de P1 e P2 antes de uma MD3 local. Os dois podem escolher o mesmo urso. A escolha permanece fixa durante as rodadas, e após a vitória final há duas ações: **REVANCHE** (mesmos personagens) e **TROCAR PERSONAGENS** (liberar seleção).

## O que já funciona

- `Assets/Prefabs/Characters/PlayerBase.prefab`: base compartilhada de física, movimento, poderes e VFX.
- `PandaGigante.prefab` e `UrsoPolar.prefab`: variantes com filho `Visual`, sprites e Animator próprios.
- `CharacterType.cs`: enum com `PandaGigante` e `UrsoPolar`.
- `CharacterSelectionManager.cs`: singleton persistente com a escolha de cada jogador e trava lógica.
- `PlayerCharacterVisual.cs`: no `Start()`, lê `PlayerId`, pega a escolha e copia `SpriteRenderer.sprite`, Animator Controller, posição e escala do `Visual` do prefab para o jogador existente na cena. **Não instancia um novo PlayerBase.**
- `MatchCharacterInitializer.cs`: no `Start()` da corrida, confirma a seleção se não estiver travada.
- `GameManager.cs`: placar persistente e vitória ao atingir duas medalhas.
- `NextRoundButton.cs`: `NextRound()`, `Rematch()`, `ChangeCharacters()`.
- `FinishLine.cs`: tela de resultado, vitória e botões; na MD3 final apresenta REVANCHE e TROCAR PERSONAGENS.
- As quatro combinações Panda/Panda, Panda/Polar, Polar/Panda e Polar/Polar foram **testadas e confirmadas** no Unity.

## Responsabilidade de cada sistema

| Responsável | Faz | Não deve fazer |
| --- | --- | --- |
| Menu/seleção (pendente) | Exibe opções, recebe escolhas, inicia corrida | Alterar física/Animator dos jogadores |
| `CharacterSelectionManager` | Guarda e trava P1/P2 entre cenas | Criar jogador ou atualizar UI |
| `PlayerCharacterVisual` | Aplica o visual escolhido ao objeto de jogador existente | Alterar `PlayerId`, inputs, Rigidbody ou placar |
| `MatchCharacterInitializer` | Trava a seleção ao entrar na corrida | Escolher urso por conta própria |
| `GameManager` | Mantém medalhas e estado da MD3 | Escolher ou renderizar personagem |
| `FinishLine` / `NextRoundButton` | Resultado, próxima prova/revanche/troca | Implementar a tela de seleção |

## API que o menu deverá usar

O `CharacterSelectionManager` deve existir **antes de carregar a corrida**, preferencialmente criado uma vez no fluxo inicial; ele utiliza `DontDestroyOnLoad` e descarta duplicatas. A cena `HurdlesRace` contém uma instância para testes diretos no Editor.

```csharp
var selection = CharacterSelectionManager.Instance;

// Antes de iniciar uma nova MD3, com seleção desbloqueada:
selection.SelectCharacter(1, CharacterType.PandaGigante);
selection.SelectCharacter(2, CharacterType.UrsoPolar);

// Opcional: confirmar explicitamente no menu.
selection.ConfirmSelection();

// Carregar apenas depois de garantir as duas escolhas.
UnityEngine.SceneManagement.SceneManager.LoadScene("HurdlesRace");
```

`SelectCharacter` **não modifica a escolha quando `IsSelectionLocked` é `true`**. `GetCharacter(1)` e `GetCharacter(2)` recuperam as escolhas. `ConfirmSelection()` trava; `UnlockSelection()` destrava.

**Importante:** a lógica atual permite alterar uma escolha até a confirmação; o menu deve coletar a confirmação dos dois jogadores antes de carregar a prova. O fluxo de confirmação individual, navegação e UI ainda é tarefa pendente.

### Novo jogo versus revanche

- **Próxima prova:** `NextRoundButton.NextRound()` recarrega a cena sem zerar o placar durante a MD3.
- **Revanche:** `Rematch()` (ou `NextRound()` após o término) zera o placar e recarrega a cena, **mantendo o bloqueio e os personagens**.
- **Trocar personagens:** `ChangeCharacters()` zera o placar e chama `UnlockSelection()`, mas **ainda não navega** para a tela de seleção. O menu precisa integrar a navegação; não assumir que ela já funciona.
- Evitar criar nova instância de `CharacterSelectionManager` ao navegar. A instância persistente é a fonte de verdade.

## Passos pendentes para o colega da interface

1. Criar a tela de seleção a partir do fluxo de `MainMenu`, sem substituir as cenas já funcionais.
2. Expor duas escolhas independentes (P1 e P2), inclusive personagens iguais, e estados visuais de confirmação.
3. Garantir que a instância persistente de `CharacterSelectionManager` já exista; chamar `UnlockSelection()` somente ao entrar em um fluxo de **nova** escolha (não nas rodadas).
4. Aplicar `SelectCharacter(1, ...)` e `SelectCharacter(2, ...)`, validar ambas as confirmações, chamar `ConfirmSelection()` e carregar `HurdlesRace`.
5. Conectar `ChangeCharacters()` à navegação de volta à nova tela. Hoje esse método **somente** zera medalhas, desbloqueia e registra log.
6. Manter a entrada direta de `HurdlesRace` no Editor para teste e evitar regressões de `GameManager`, HUD, câmera e poderes.

## Atenções técnicas

- O `PlayerController` localiza `Visual` e `DashTrail` pelo nome no `Awake()`; manter esses nomes. O visual é aplicado posteriormente no `Start()`.
- O tamanho visual depende de `Pixels Per Unit` e escala. Os sprites do polar foram ajustados para a configuração compatível com o panda; não corrigir diferenças apenas alterando a física.
- Os `.anim` e o `Polar_Animator.controller` devem apontar para sprites próprios. Victory é acionado pelo Trigger `Victory` e seu clip não deve fazer loop.
- `MatchCharacterInitializer.Start()` é intencional: evita acessar o singleton antes do `Awake()` dele. Se o menu confirmar antes da carga, não haverá nova confirmação.
- O `MainMenu.Jogar()` atual apenas carrega a cena configurada em `nomeCenaJogo`; sua integração à nova tela é **pendente**.
- Quando houver nova modalidade, não assumir que a mesma inicialização/posicionamento de corrida será adequada sem teste.

## Critérios de aceite para o menu futuro

- P1 e P2 selecionam livremente Panda-gigante/Urso-polar, inclusive o mesmo personagem.
- As escolhas aparecem corretamente na `HurdlesRace` com controles, animações, poderes e câmera intactos.
- Uma MD3 completa preserva os dois personagens e o placar entre rodadas.
- REVANCHE inicia nova MD3 com mesmos personagens e medalhas zeradas.
- TROCAR PERSONAGENS abre a seleção, permite novas escolhas, reinicia o placar e lança uma nova MD3.
- Entrada direta pelo Editor continua possível.
- Sem erros novos no Console e sem reescrever `PlayerController`.

## Commits de referência

- `4cabdae` — add playable polar bear character
- `d6a5fff` — prepare character selection system
- `90a1763` — integrate character selection with match flow
