# Integração da seleção de personagens — Olimpandas

> Atualizado em 26/11/2026 para a **v1.0.0**. Aceite humano ponta a ponta concluído.

## Fluxo validado

`MainMenu → CharacterSelect → CircuitDraw → modalidade sorteada → Podium → próxima modalidade`

A seleção suporta os quatro personagens da entrega:

- Panda-gigante;
- Urso-polar;
- Grizzly;
- Urso-de-óculos.

P1 e P2 escolhem independentemente, podem repetir o mesmo personagem, confirmam separadamente e só iniciam quando ambos estão prontos. As escolhas permanecem travadas durante o Circuito Olímpico.

## Comportamentos validados

- seleção e confirmação independentes de P1/P2;
- quatro personagens disponíveis para os dois jogadores;
- aplicação correta do personagem nas quatro modalidades;
- persistência da escolha durante o circuito;
- placar e medalhas persistentes entre provas;
- revanche mantendo os personagens e reiniciando o circuito;
- troca de personagens voltando à seleção com estado resetado;
- retorno ao menu sem estado residual;
- navegação por teclado e mouse;
- animações Idle, Run, Jump, Fall, Dash e Victory;
- câmera, respawn, controles e resultado sem regressões no fluxo da entrega.

## Modalidades validadas no circuito

- 100m com barreiras;
- Salto a distância;
- Canoagem;
- Pescaria.

Cada modalidade comunica somente o vencedor ao `GameManager`, que registra a medalha geral. O primeiro jogador a alcançar duas medalhas vence o Circuito Olímpico.

## Responsabilidades principais

| Sistema | Responsabilidade |
| --- | --- |
| `CharacterSelectMenu` | Navegação, prévia, confirmação e início do circuito |
| `CharacterSelectionManager` | Estado persistente das escolhas |
| `PlayerCharacterVisual` | Aplicação da arte/Animator do personagem |
| `GameManager` | Medalhas, histórico e estado do circuito |
| `MinigameCatalog` / `MinigameRotation` | Catálogo e sorteio das modalidades |
| `CircuitDrawScreen` | Apresentação da modalidade sorteada |
| `FinishLine.DeclareWinner` / regras equivalentes | Comunicação do vencedor da modalidade |
| `PodiumScreen` | Resultado, medalhas, campeão e navegação pós-prova |

## Aceite da v1.0.0

- [x] P1/P2 selecionam e confirmam personagens separadamente.
- [x] Os quatro personagens são aplicados corretamente no gameplay.
- [x] O circuito mantém as escolhas entre modalidades.
- [x] O placar persiste corretamente entre provas.
- [x] REVANCHE inicia novo circuito com os mesmos personagens.
- [x] TROCAR PERSONAGENS retorna à seleção e permite nova combinação.
- [x] Retorno ao menu limpa o estado da partida.
- [x] As quatro modalidades funcionam no fluxo completo.
- [x] HUDs, pausa, áudio e telas de resultado funcionam no fluxo final.
- [x] Playtest humano ponta a ponta concluído em 26/11/2026.

## Observação sobre testes automatizados

O projeto mantém smoke tests para vários fluxos. Uma execução histórica do `CharacterSelectionFlowSmokeTest` chegou a concluir as asserções do fluxo, mas o Editor retornou erro por uma exceção interna do indexador `UnityEditor.Search`. Isso não substituiu o aceite humano.

Para a **v1.0.0**, o critério final de aceite foi o playtest manual confirmado pelo time em Unity, conforme registrado no [backlog](backlog.md).

## Escopo pós-v1.0.0

Novos personagens, novas modalidades, multiplayer online, backend, contas, matchmaking e ranking online ficam fora da entrega v1.0.0 e permanecem como roadmap.
