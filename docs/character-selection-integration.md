# Integração da seleção de personagens — Olimpandas

> Revisão de 22/10/2026, após o PR #2. Integração implementada; o aceite visual e o playtest humano continuam pendentes. Resultados automatizados e limites estão registrados abaixo.

## Fluxo atual

`MainMenu → CharacterSelect → HurdlesRace → resultado da rodada/MD3`

- P1 e P2 escolhem Panda-gigante ou Urso-polar independentemente, inclusive o mesmo personagem.
- Cada painel permite avançar/voltar e CONFIRMAR/CANCELAR. Confirmar bloqueia apenas as setas daquele painel.
- INICIAR fica desabilitado até os dois confirmarem. O método também verifica as confirmações antes de carregar a corrida.
- Ao iniciar, as duas escolhas são gravadas e travadas no manager persistente.
- Cada corrida vencida entrega uma medalha no **recorte atual**. A primeira pessoa a obter duas vence a MD3.
- PRÓXIMA PROVA recarrega `HurdlesRace`, mantendo escolhas e placar.
- REVANCHE zera o placar, recarrega a corrida e mantém personagens/trava.
- TROCAR PERSONAGENS zera o placar, desbloqueia e carrega `CharacterSelect`.
- VOLTAR na seleção abre `MainMenu`, com pontuação zerada e seleção desbloqueada. As escolhas permanecem como prévias; as confirmações são refeitas ao voltar.

## Métodos e responsabilidades

| Arquivo / método | Responsabilidade |
| --- | --- |
| `MainMenu.Jogar()` | Carrega `nomeCenaJogo`, configurado como `CharacterSelect` na cena e agora também no valor padrão do script. |
| `CharacterSelectMenu.Start()` | Valida os controles obrigatórios e IDs dos painéis; cria o manager se necessário, zera placar existente, desbloqueia e conecta os eventos em runtime. |
| `SetupPanel`, `Cycle`, `ToggleReady`, `RefreshAll` | Recuperam a escolha anterior, alteram cada painel independentemente e controlam a disponibilidade de INICIAR. |
| `StartMatch()` | Exige as duas confirmações, impede navegação duplicada, grava P1/P2, chama `ConfirmSelection()` e abre `HurdlesRace`. |
| `BackToMainMenu()` | Zera o placar existente, libera a seleção e volta ao menu principal. |
| `CharacterSelectionManager` | `SelectCharacter`, `GetCharacter`, `ConfirmSelection`, `UnlockSelection` e `IsSelectionLocked` mantêm o contrato existente. `DontDestroyOnLoad` preserva escolhas e `Awake` descarta duplicatas. |
| `PlayerCharacterVisual.Start()` | Usa `PlayerId` e aplica sprite, Animator Controller, posição e escala ao filho `Visual`. Não instancia outro jogador nem altera física/inputs. Agora diagnostica ausência de `PlayerController`. |
| `MatchCharacterInitializer.Start()` | Confirma a seleção na entrada direta da corrida, após os `Awake` dos managers. Permanece sem mudanças. |
| `GameManager` | Mantém medalhas, `ResetMatch()` e vitória a duas medalhas. `DontDestroyOnLoad` e descarte de duplicatas preservados. |
| `NextRoundButton.NextRound()` | Preserva o placar entre rodadas; se a MD3 terminou, zera antes de recarregar (REVANCHE). |
| `NextRoundButton.Rematch()` | API explícita de revanche, preservando personagens. O botão da cena utiliza `NextRound()`. |
| `NextRoundButton.ChangeCharacters()` | Zera placar, desbloqueia e carrega `characterSelectSceneName` (padrão `CharacterSelect`). |
| `FinishLine` | Entrega medalha uma vez por corrida, congela jogadores, foca o vencedor e exibe resultado. Exibe TROCAR PERSONAGENS somente ao terminar a MD3. Permanece sem mudanças. |

## Cenas, referências e ciclo de vida

- `MainMenu`, `CharacterSelect` e `HurdlesRace` estão habilitadas em `ProjectSettings/EditorBuildSettings.asset`. `PrototypeRace` permanece disponível como protótipo.
- `MainMenu` tem referências para painel de opções, volume e tela cheia; eventos serializados apontam para `Jogar`, `AbrirOpcoes`, `FecharOpcoes`, `DefinirVolume`, `DefinirTelaCheia` e `Sair`.
- `CharacterSelect` tem painéis com IDs 1/2, setas, confirmações, imagens, nomes/status, duas prévias e referências INICIAR/VOLTAR. Os listeners são adicionados por `CharacterSelectMenu.Start()`; listas persistentes vazias nessa cena são intencionais.
- `HurdlesRace` contém `GameManager` com `medalsToWin = 2`, `CharacterSelectionManager` com padrão Panda/Polar e `MatchCharacterInitializer`. Isso permite iniciar a corrida diretamente no Editor.
- Os dois jogadores têm `PlayerCharacterVisual` com os dois prefabs atribuídos. Os prefabs reutilizam `PlayerBase`; a troca mantém o mesmo `Visual`, preservando as referências obtidas por `PlayerController.Awake()`.
- Os botões de resultado já estavam ligados a `NextRound()` e `ChangeCharacters()`. O defeito estava no método de navegação; não foi necessário regravar as cenas.
- `OnDestroy()` limpa a referência estática de cada manager **somente se aquela instância for a proprietária**. Destruir uma duplicata não apaga o singleton persistente.
- Entrar na seleção representa uma nova MD3. Não carregar essa cena como um overlay durante uma rodada em andamento.

## Correções desta revisão

1. `ChangeCharacters()` passou a carregar a seleção após zerar/desbloquear.
2. Entrar na seleção ou voltar ao menu elimina placar residual; confirmações individuais sempre reiniciam.
3. INICIAR é desabilitado no `Awake`, antes de conectar eventos; navegação repetida é bloqueada.
4. Referências obrigatórias/IDs dos painéis são verificados antes de usar botões; nomes opcionais toleram array nulo. `PlayerCharacterVisual` verifica a presença do controller.
5. Os dois singletons limpam suas referências ao serem destruídos. As ações do resultado toleram ausência de manager sem `NullReferenceException`.
6. O destino padrão de `MainMenu` foi alinhado à cena já serializada.

Nenhuma alteração em `PlayerBase`, `PlayerController`, animações, Stun/Slow, câmera, checkpoints, respawn, countdown ou regras de medalhas/vitória. Não foram criados controllers por personagem.

## Verificações desta revisão

### Verificações estáticas executadas

- Referências locais por `fileID` nas três cenas: nenhuma referência pendente encontrada.
- Callbacks persistentes: alvos e métodos existentes; seleção usa listeners em runtime.
- Build Settings, nomes de cenas, IDs dos painéis e referências dos prefabs revisados.
- `git -c core.whitespace=cr-at-eol diff --check` executado ao finalizar, respeitando os arquivos CRLF já existentes.

### Teste automatizado no Unity

O script `Assets/Editor/CharacterSelectionFlowSmokeTest.cs` executa o fluxo em Play Mode via batchmode, em cópia isolada do projeto. Não salva cenas. O comando deve ser executado **sem `-quit`**, pois o runner encerra o Editor ao concluir:

```bash
"<Unity 6000.5.9f1>/Editor/Unity" -batchmode -nographics \
  -projectPath "<cópia isolada do projeto>" \
  -executeMethod CharacterSelectionFlowSmokeTest.Run \
  -logFile /tmp/olimpandas-selection-flow.log
```

O runner invoca os eventos reais dos botões, aguarda o countdown, simula a entrada na chegada chamando `OnTriggerEnter2D` e verifica managers, placar, escolhas, controllers visuais, resultado e transições. Isso **não valida uma corrida jogada por teclado nem a renderização**. A segunda MD3 de cada combinação é abreviada com uma medalha atribuída diretamente para testar o retorno à seleção.

Executado no Unity **6000.5.9f1**, em duas execuções na cópia temporária:

| Verificação | Resultado observado |
| --- | --- |
| Compilação C# | Sem erros; avisos de APIs de busca obsoletas nos scripts existentes. |
| Panda × Panda, Panda × Polar, Polar × Panda, Polar × Polar | Todas concluíram as asserções de seleção, aplicação do Animator/transform, MD3 2–1, revanche e troca nas duas execuções. |
| Confirmação/cancelamento e bloqueio de INICIAR | Asserções concluídas. |
| Managers únicos e persistentes; placar entre rodadas | Asserções concluídas. |
| Chegada repetida; visibilidade do resultado/troca | Asserções concluídas. |
| Voltar ao menu, limpar estado e abrir seleção novamente | Asserções concluídas. |
| Destruição dos singletons e entrada direta na corrida | Asserções concluídas. |
| Resultado global do runner | **FALHOU (exit code 1)** nas duas execuções, devido a `ArgumentOutOfRangeException` em `UnityEditor.Search.SearchDatabase.EnumerateAll` / `SearchInit.IndexationOnStartup`. Nenhuma asserção do fluxo falhou, mas a suíte não é considerada totalmente aprovada. |

A exceção tem stack do indexador interno do Editor e também ocorreu após a importação inicial. Ela não foi suprimida pelo teste. Logs locais: `/tmp/olimpandas-selection-flow.log` e `/tmp/olimpandas-selection-flow-rerun.log`. É necessário repetir a suíte em um Editor sem essa falha de infraestrutura para obter execução global limpa.

Também foi executado `HurdlesRaceSceneSmokeTest.Run` com `-batchmode -nographics -quit`: **aprovado, exit code 0**. A suíte existente aprovou `HurdleSmokeTest` (queda e Slow), `HurdlesLaneSetupSmokeTest` (isolamento das raias) e as verificações da cena (barreiras, cenário, limites da câmera, countdown, referências de UI e Build Settings). Log local: `/tmp/olimpandas-hurdles-review.log`. Esses testes não substituem inspeção visual ou gameplay humano.

### Aceite manual pendente

Repetir em Play Mode com interface gráfica e numa build local:

- [ ] Panda × Panda, Panda × Polar, Polar × Panda e Polar × Polar: prévias, nomes, legibilidade e confirmação/cancelamento de cada painel.
- [ ] Verificar todos os clipes (Idle, Run, Jump, Fall, Dash, Victory), escala/posição e orientação visual com ambos os jogadores em movimento.
- [ ] Controles P1/P2, pulo, dash/Air Dash, Stun/Slow, pickups, câmera, checkpoints, queda/respawn e countdown.
- [ ] Jogar MD3 de 2–0 e 2–1, alternando campeão; confirmar placar, vitória, próxima rodada, revanche e troca com cliques físicos.
- [ ] Repetir ciclos de troca → voltar → jogar; inspecionar Console, confirmar ausência de managers duplicados e seleção destravada.
- [ ] Abrir diretamente cada cena no Editor, inclusive repetir Play Mode com Domain Reload desabilitado; inspecionar UI e Console.

## Limite do marco e decisões de produto

A MD3 atual de corridas permanece estável em escopo. Ela ainda **não implementa o Circuito Olímpico final**: `GameManager.AddMedal` pontua cada corrida, enquanto o circuito deverá pontuar somente a vitória da modalidade.

Decisões preservadas: Circuito local 1v1; sorteio surpresa de uma modalidade por vez; evitar repetição enquanto outras estiverem disponíveis; primeiro a duas medalhas de modalidades vence; regras internas próprias por modalidade (MD3 quando fizer sentido); empates resolvidos por disputa extra; poderes permitidos definidos por modalidade; personagens equivalentes em gameplay no MVP; duração-alvo de 5–8 minutos.

`MinigameRotationManager`, sorteio, separação da pontuação interna/geral e regras genéricas de desempate permanecem para o próximo marco. Não foram implementados nesta tarefa.
