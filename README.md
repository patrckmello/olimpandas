# Olimpandas

Olimpandas é um party game 2D competitivo desenvolvido em Unity e C#. Dois jogadores disputam modalidades curtas e conquistam medalhas. O recorte jogável atual é uma MD3 de corridas; o produto planejado é o Circuito Olímpico local 1v1.

> **Projeto acadêmico:** este repositório foi criado com foco em aprendizado e prática de desenvolvimento de jogos, programação em C#, Unity e colaboração com Git/GitHub. O jogo ainda está em desenvolvimento e não representa uma versão comercial ou finalizada.

## Visão geral

A proposta do Olimpandas é reunir provas rápidas e fáceis de entender em uma experiência PvP local. O primeiro recorte jogável valida o loop principal: iniciar a prova, competir, identificar o vencedor, entregar uma medalha e avançar para a rodada seguinte.

A seleção de personagens está integrada ao menu e à primeira prova oficial, **100m com barreiras**. O foco atual é validar a experiência visual e preparar o próximo marco de rotação de modalidades. A cena jogável é `HurdlesRace`; `PrototypeRace` permanece como protótipo técnico.

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
- tela `CharacterSelect` com escolhas independentes de P1/P2, personagens repetidos e confirmação dos dois antes de iniciar;
- escolhas preservadas e travadas durante a MD3;
- REVANCHE com os mesmos personagens e placar zerado; TROCAR PERSONAGENS zera o placar, desbloqueia e abre `CharacterSelect`;
- retorno da seleção ao `MainMenu`, sem conservar confirmações ou pontuação antiga;
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
4. Abra `Assets/Scenes/MainMenu.unity` para testar o fluxo completo. `CharacterSelect` e `HurdlesRace` também permitem entrada direta no Editor; `PrototypeRace` é o protótipo técnico.
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

Desenvolver o **`MinigameRotationManager`**, após o aceite manual da integração. O sorteio de modalidades não foi implementado nesta entrega. Veja [o fluxo e os testes da seleção](docs/character-selection-integration.md).

Decisões preservadas: Circuito Olímpico local 1v1, sorteio surpresa de uma modalidade por vez, sem repetição enquanto houver outras disponíveis, e vitória geral ao conquistar duas medalhas de modalidades. Cada modalidade terá regras internas próprias (inclusive MD3 quando fizer sentido), desempate por disputa extra e definição dos poderes permitidos. Personagens terão jogabilidade equivalente no MVP. A meta do circuito é **5–8 minutos**. O placar atual ainda concede uma medalha por corrida; a separação entre rodada, modalidade e circuito pertence ao próximo marco.

Em paralelo, continuar o polimento e a validação da primeira prova 100m com barreiras. Não expandir para online, seis jogadores ou vários minigames antes do vertical slice local.

## Limitações atuais

- o projeto continua em desenvolvimento;
- o modo principal atual é PvP local para dois jogadores;
- ainda não existe uma coleção finalizada de múltiplas provas;
- integração implementada; inspeção visual e playtest humano do fluxo completo ainda pendentes (ver relatório de testes na documentação);
- arte, áudio e balanceamento ainda podem mudar;
- não há sistema de contas, backend, matchmaking ou ranking online;
- multiplayer online e expansão WebGL não são prioridade antes da estabilização do jogo local.

---

## English summary

Olimpandas is a competitive 2D party game built with Unity and C#. Two local players compete in short events and earn medals. The current slice is a best-of-three race; the planned product is a local Olympic Circuit.

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
- independent P1/P2 character selection, including matching characters and confirmation before starting;
- locked character choices throughout a best-of-three match;
- rematch with the same characters; change-characters resets the score and returns to selection;
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
4. Open `Assets/Scenes/MainMenu.unity` for the full flow (direct entry into `CharacterSelect` or `HurdlesRace` is also supported).
5. Press **Play** in the Unity Editor.

Selection-screen integration is implemented; visual review and human gameplay checks remain pending. See the [integration and test report](docs/character-selection-integration.md). The next milestone is event rotation; no minigame randomizer was added here.

No standalone build is currently distributed.
