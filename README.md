# Olimpandas

**Olimpandas** é um party game 2D competitivo local para **2 jogadores**, desenvolvido em **Unity + C#**. No Circuito Olímpico, os jogadores escolhem seus personagens, recebem uma modalidade sorteada a cada rodada e disputam medalhas. O primeiro a conquistar **2 medalhas** vence o circuito.

> **v1.0.0 — entrega acadêmica validada em gameplay (26/11/2026).**

## Conteúdo da v1.0.0

A versão 1.0.0 fecha o primeiro vertical slice completo do projeto com:

- PvP local 1v1;
- Circuito Olímpico com sorteio de modalidades;
- melhor de 3 por medalhas;
- seleção independente de personagens para P1 e P2;
- 4 personagens jogáveis: **Panda-gigante, Urso-polar, Grizzly e Urso-de-óculos**;
- 4 modalidades: **100m com barreiras, Salto a distância, Canoagem e Pescaria**;
- pódio, revanche e troca de personagens;
- HUDs específicos por prova;
- pausa, opções de áudio, tela cheia, controles e créditos;
- música e efeitos sonoros;
- sistema compartilhado de câmera, vitória, respawn, status e poderes;
- suporte ao fluxo completo em **Windows e Linux**.

## Como jogar

O fluxo principal é:

`MainMenu → CharacterSelect → CircuitDraw → modalidade → Podium → próxima modalidade`

Cada vitória em uma modalidade concede **1 medalha**. O primeiro jogador a chegar a **2 medalhas** é o campeão do Circuito Olímpico.

### Controles gerais

| Ação | Jogador 1 | Jogador 2 |
| --- | --- | --- |
| Esquerda | `A` | `←` |
| Direita | `D` | `→` |
| Pular | `W` | `↑` |
| Dash | `Shift esquerdo` | `Shift direito` |
| Pausar | `Esc` | `Esc` |

### 100m com barreiras

Corra, pule as barreiras e seja o primeiro a cruzar a chegada.

- **P1:** `A/D` para correr, `W` para pular, `Shift esquerdo` para Dash.
- **P2:** `←/→` para correr, `↑` para pular, `Shift direito` para Dash.

### Salto a distância

Cada jogador tem **3 tentativas**. Alterne rapidamente esquerda/direita para ganhar velocidade e pule antes da linha. A maior marca válida vence.

- **P1:** alterne `A/D`; `W` salta.
- **P2:** alterne `←/→`; `↑` salta.

### Canoagem

Alterne os lados da remada para ganhar velocidade e use o eixo vertical para desviar dos obstáculos.

- **P1:** `A/D` para remar; `W/S` para desviar.
- **P2:** `←/→` para remar; `↑/↓` para desviar.

Bater em obstáculos reduz a velocidade. A primeira canoa a chegar vence; o limite da prova também permite decidir pela maior progressão.

### Pescaria

Os peixes mostram sequências de direção. A primeira entrada escolhe o alvo; complete a sequência antes do rival para pontuar.

- **P1:** `W/A/S/D`.
- **P2:** setas direcionais.

Os peixes valem pontuações diferentes, o peixe dourado vale mais e a bota desconta pontos. A rodada dura 60 segundos; empate vai para morte súbita.

## Executar a versão compilada

A v1.0.0 foi preparada para distribuição em **Windows x64** e **Linux x64**.

### Windows

1. Baixe o pacote Windows da release `v1.0.0`.
2. Extraia o arquivo ZIP inteiro para uma pasta.
3. Execute `Olimpandas.exe`.
4. Mantenha a pasta `Olimpandas_Data` junto do executável.

O Windows pode exibir um aviso do SmartScreen porque o executável acadêmico não possui assinatura digital comercial.

### Linux

1. Baixe o pacote Linux da release `v1.0.0`.
2. Extraia o arquivo.
3. No terminal, entre na pasta extraída.
4. Dê permissão de execução e abra o jogo:

```bash
chmod +x Olimpandas.x86_64
./Olimpandas.x86_64
```

Se necessário, mantenha a pasta `Olimpandas_Data` e os demais arquivos do build no mesmo diretório do executável.

## Executar pelo Unity Editor

Para desenvolvimento ou teste pelo código-fonte:

### Requisitos

- Git;
- Unity Hub;
- **Unity Editor 6000.5.9f1**.

Clone o projeto:

```bash
git clone https://github.com/patrckmello/olimpandas.git
cd olimpandas
```

Depois:

1. Abra o Unity Hub.
2. Use **Add → Add project from disk**.
3. Selecione a pasta clonada.
4. Abra com **Unity 6000.5.9f1**.
5. Abra `Assets/Scenes/MainMenu.unity`.
6. Pressione **Play**.

## Gerar builds

No Unity, abra **File → Build Profiles** e use as cenas habilitadas em `ProjectSettings/EditorBuildSettings.asset`.

### Windows x64

Selecione **Windows → x86_64**, gere o build e distribua o executável junto da pasta `Olimpandas_Data` e demais arquivos criados pela Unity.

### Linux x64

Selecione **Linux → x86_64**, gere o build e distribua o executável `Olimpandas.x86_64` junto dos arquivos gerados pela Unity.

## Cenas principais

- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/CharacterSelect.unity`
- `Assets/Scenes/CircuitDraw.unity`
- `Assets/Scenes/HurdlesRace.unity`
- `Assets/Scenes/LongJump.unity`
- `Assets/Scenes/Canoeing.unity`
- `Assets/Scenes/Fishing.unity`
- `Assets/Scenes/Podium.unity`

`PrototypeRace.unity` permanece como cena técnica/protótipo.

## Estrutura do projeto

```text
Assets/
  Art/              Personagens, mapas, interface e VFX
  Audio/            Músicas e efeitos sonoros
  Prefabs/          Objetos reutilizáveis
  Scenes/           Cenas do jogo
  Scripts/
    Core/           Circuito e sistemas compartilhados
    Minigames/      Regras específicas das modalidades
    Player/         Movimento, física, respawn e status
    Powers/         Poderes, inventário e pickups
    UI/             Menus, HUDs e fluxo visual
docs/               Backlog e documentação técnica
Packages/
ProjectSettings/
```

## Status

A **v1.0.0** representa o fechamento do escopo acadêmico planejado e foi validada em playtest local com o circuito completo, quatro modalidades e quatro personagens.

Funcionalidades pós-v1.0.0 permanecem no roadmap, incluindo outras modalidades, novos personagens e possíveis expansões futuras. Multiplayer online, backend, contas, matchmaking e ranking online não fazem parte desta versão.

Consulte o [Product Backlog](docs/backlog.md) para o histórico e roadmap.

## Créditos

Projeto desenvolvido colaborativamente como trabalho acadêmico.

- **Patrick Mello** — gameplay, arquitetura, minigames, personagens e integração;
- **Gabriel** — UI principal e fluxo visual;
- **Carlos Eduardo (Cadu / EoGarlos)** — HUDs, cronômetro e microfeatures de UI/gamefeel;
- **Mateus** — contribuições de áudio.

---

## English

**Olimpandas v1.0.0** is a local two-player competitive 2D party game built with Unity and C#. Players choose a character, compete in randomly selected Olympic-style minigames and earn medals. The first player to win two events becomes the Olympic Circuit champion.

The v1.0.0 academic release includes four playable characters and four events: 100m hurdles, long jump, canoeing and fishing. The project uses Unity **6000.5.9f1** and supports standalone Windows x64 and Linux x64 builds.
