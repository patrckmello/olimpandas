# Olimpandas

Olimpandas é um party game 2D competitivo desenvolvido em Unity e C#. Dois jogadores disputam minigames curtos, conquistam medalhas e tentam vencer uma série melhor de três.

> **Projeto acadêmico:** este repositório foi criado com foco em aprendizado e prática de desenvolvimento de jogos, programação em C#, Unity e colaboração com Git/GitHub. O jogo ainda está em desenvolvimento e não representa uma versão comercial ou finalizada.

## Visão geral

A proposta do Olimpandas é reunir provas rápidas e fáceis de entender em uma experiência PvP local. O primeiro recorte jogável valida o loop principal: iniciar a prova, competir, identificar o vencedor, entregar uma medalha e avançar para a rodada seguinte.

O foco atual é consolidar a primeira prova oficial, **100m com barreiras**, e preparar a integração da seleção de personagens ao menu. A cena jogável é `HurdlesRace`; `PrototypeRace` permanece como protótipo técnico.

**Documentação:** [Product Backlog](docs/backlog.md) · [Integração do menu e seleção de personagens](docs/character-selection-integration.md).

O backlog completo e atualizado está em `docs/backlog.md`.

## Estado atual

O projeto já possui:

- PvP local para dois jogadores;
- movimentação, pulo e dash independentes;
- Air Dash limitado a um uso por ciclo no ar;
- câmera compartilhada com enquadramento dinâmico;
- foco/zoom no vencedor;
- checkpoints, zonas de queda e respawn individual;
- detecção da linha de chegada e do vencedor;
- medalhas persistentes entre rodadas;
- partida melhor de três;
- interface de resultado e opção de próxima prova ou nova partida;
- countdown de largada;
- Panda-gigante e Urso-polar com animações independentes de Idle, Run, Jump, Fall, Dash e Victory;
- prefabs variantes de `PlayerBase` para ambos os personagens;
- seleção dinâmica de personagem para P1 e P2 (as quatro combinações testadas no Unity);
- escolhas preservadas e travadas durante a MD3;
- opções REVANCHE e TROCAR PERSONAGENS no resultado final; a navegação para o menu de seleção ainda não foi implementada;
- Dash Trail separado da spritesheet;
- base reutilizável de Stun e Slow;
- projéteis, pickups e efeitos visuais de Stun e Slow;
- inventário/HUD de poderes;
- spawn aleatório e respawn de pickups;
- setas de direção dos jogadores.

## Controles

| Ação                  | Jogador 1        | Jogador 2              |
| --------------------- | ---------------- | ---------------------- |
| Mover para a esquerda | `A`              | `Seta para a esquerda` |
| Mover para a direita  | `D`              | `Seta para a direita`  |
| Pular                 | `W`              | `Seta para cima`       |
| Dash                  | `Shift esquerdo` | `Shift direito`        |

As teclas de poderes são configuradas por jogador no Inspector e podem mudar durante o desenvolvimento.

## Minigames planejados

O roadmap ativo de provas é:

- **100m com barreiras** — em desenvolvimento;
- **Salto a distância**;
- **Lançamento de dardo**;
- **Levantamento de peso**;
- **Sumô**;
- **Canoagem**;
- **Natação**;
- **Tênis**;
- **Pescaria**.

Ideias antigas como Arco e Flecha, Curling e Snowboard não fazem mais parte do roadmap ativo neste momento.

## Tecnologias

- Unity `6000.5.9f1`;
- C#;
- física e ferramentas 2D da Unity;
- TextMesh Pro e Unity UI;
- Git e GitHub.

## Como executar

### Requisitos

- Unity Hub;
- Unity Editor `6000.5.9f1` ou uma versão compatível da mesma linha;
- Git.

### Passos

```bash
git clone https://github.com/patrckmello/olimpandas.git
cd olimpandas
```

1. No Unity Hub, selecione **Add > Add project from disk**.
2. Escolha a pasta clonada do projeto.
3. Abra o projeto com a versão indicada do Unity.
4. Abra a cena `Assets/Scenes/HurdlesRace.unity` para testar a prova atual (`PrototypeRace.unity` é o protótipo técnico).
5. Pressione **Play** no Unity Editor.

Não há uma build distribuída neste repositório no momento.

## Estrutura do projeto

```text
Assets/
  Art/              Arte, personagens, mapas, interface e efeitos
  Audio/            Música e efeitos sonoros
  Prefabs/          Objetos reutilizáveis do jogo
  Scenes/           Cenas jogáveis e protótipos
  Scripts/
    Core/           Sistemas compartilhados
    Minigames/      Regras e objetos específicos das provas
    Player/         Movimento, respawn e status do jogador
    Powers/         Inventário, pickups, projéteis e poderes
    UI/             Interface e fluxo de telas
docs/               Documentação e backlog
Packages/           Dependências do projeto Unity
ProjectSettings/    Configurações versionadas do Unity
```

## Próximo marco

Integrar o **menu de seleção de personagens** ao fluxo já testado de Panda-gigante/Urso-polar e validar entrada no `HurdlesRace`, MD3, revanche e troca de personagens de ponta a ponta. O botão TROCAR PERSONAGENS atualmente libera a escolha e reinicia o placar, mas ainda não navega até uma tela de seleção. Veja [o contrato de integração](docs/character-selection-integration.md).

Em paralelo, continuar o polimento e a validação da primeira prova 100m com barreiras. Não expandir para online, seis jogadores ou vários minigames antes do vertical slice local.

## Limitações atuais

- o projeto continua em desenvolvimento;
- o modo principal atual é PvP local para dois jogadores;
- ainda não existe uma coleção finalizada de múltiplas provas;
- a tela de seleção de personagens e o retorno a ela ainda dependem de integração com o menu;
- arte, áudio e balanceamento ainda podem mudar;
- não há sistema de contas, backend, matchmaking ou ranking online;
- multiplayer online e expansão WebGL não são prioridade antes da estabilização do jogo local.

---

## English summary

Olimpandas is a competitive 2D party game built with Unity and C#. Two local players compete in short minigames, earn medals, and try to win a best-of-three match.

> **Academic project:** this repository focuses on learning and practicing game development, C#, Unity, and Git/GitHub collaboration. The game is still under development and is not a commercial or finished release.

### Current focus

The current milestone is the first official event: **100m hurdles**.

The up-to-date product backlog is available at `docs/backlog.md`.

### Current features

- two-player local PvP;
- independent movement, jumping, dashing and Air Dash;
- dynamic shared camera and winner focus;
- checkpoints and individual respawning;
- finish-line and winner detection;
- persistent medals and best-of-three match flow;
- countdown before race start;
- Giant Panda and Polar Bear animation sets: Idle, Run, Jump, Fall, Dash and Victory;
- dynamic P1/P2 character appearance selection, all four combinations tested;
- locked character choices throughout a best-of-three match;
- rematch/change-characters result actions; the selection screen navigation is not yet implemented;
- reusable Stun and Slow systems;
- Stun/Slow projectiles, pickups and visual feedback;
- power inventory HUD and randomized pickup spawning;
- player direction arrows.

### Planned minigames

- 100m hurdles;
- long jump;
- javelin throw;
- weightlifting;
- sumo;
- canoeing;
- swimming;
- tennis;
- fishing.

### Run the project

1. Clone this repository.
2. Add the cloned folder to Unity Hub.
3. Open it with Unity `6000.5.9f1` or a compatible version.
4. Open `Assets/Scenes/HurdlesRace.unity` (the current event; `PrototypeRace` is a technical prototype).
5. Press **Play** in the Unity Editor.

Selection-screen integration is still pending. See [character selection integration guide](docs/character-selection-integration.md).

No standalone build is currently distributed.