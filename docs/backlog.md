# Product Backlog — Olimpandas

> Atualizado em 09/10/2026.
>
> Este backlog deve refletir o estado real do projeto. Em caso de divergência, a prioridade é:
> 1. teste confirmado no Unity;
> 2. estado atual da branch `main`;
> 3. este documento;
> 4. planos e ideias antigas.

## Legenda

| Status | Significado |
| --- | --- |
| **VALIDADO** | Implementado e confirmado funcionando em gameplay. |
| **IMPLEMENTADO** | Presente no repositório, mas sem confirmação recente de teste nesta documentação. |
| **EM DESENVOLVIMENTO** | Foco atual ou trabalho iniciado. |
| **PLANEJADO** | Mecânica aprovada para desenvolvimento posterior. |
| **ADIADO** | Fora do escopo atual de desenvolvimento. |

## Prioridade atual

A cena `HurdlesRace` já existe e o fluxo de corrida local/MD3 foi testado. As prioridades seguintes são conectar o menu de seleção de personagens ao fluxo já implementado, validar a experiência ponta a ponta e consolidar a primeira prova oficial:

**100m com barreiras**

Objetivos imediatos:

1. integrar a futura tela de seleção ao `CharacterSelectionManager` persistente;
2. conectar `ChangeCharacters()` à navegação para a tela de seleção (hoje apenas desbloqueia e zera o placar);
3. validar o ciclo menu → escolha P1/P2 → `HurdlesRace` → MD3 → revanche ou nova escolha;
4. validar apresentação e colisão das barreiras, enquadramento de duas raias, câmera, poderes e resultado sem regressões;
5. continuar testando a prova completa antes de expandir o conteúdo.

## Estado dos sistemas compartilhados

| Sistema | Status | Observação |
| --- | --- | --- |
| PvP local para 2 jogadores | **VALIDADO** | Base atual do projeto. |
| Movimento, pulo e direção | **VALIDADO** | Compartilhado pelo `PlayerBase`. |
| Dash e Air Dash | **VALIDADO** | Um Air Dash por ciclo no ar. |
| Dash Trail | **VALIDADO** | VFX separado da spritesheet. |
| Panda-gigante | **VALIDADO** | Idle, Run, Jump, Fall, Dash e Victory. |
| Urso-polar | **VALIDADO** | Segundo personagem com sprites/Animator próprios, testado em corrida 1v1. |
| Prefab Variants de personagens | **IMPLEMENTADO** | `PandaGigante.prefab` e `UrsoPolar.prefab` reutilizam `PlayerBase`. |
| Seleção dinâmica P1/P2 | **VALIDADO** | Panda/Panda, Panda/Polar, Polar/Panda e Polar/Polar testados em Unity. |
| Escolha persistente/travada na MD3 | **VALIDADO** | `CharacterSelectionManager`, `MatchCharacterInitializer` e aplicação por `PlayerCharacterVisual`. |
| Botões REVANCHE e TROCAR PERSONAGENS | **VALIDADO** | Resultado e ação de desbloqueio testados; navegação de troca ainda pendente. |
| Tela de seleção / navegação | **PLANEJADO** | Menu ainda não coleta escolhas nem recebe retorno de `ChangeCharacters()`. |
| Câmera multiplayer dinâmica | **VALIDADO** | Enquadra os dois jogadores e possui foco no vencedor. |
| FinishLine e Victory | **VALIDADO** | Congela a prova, entrega medalha e exibe resultado. |
| Melhor de 3 / medalhas | **VALIDADO** | Primeiro a 2 medalhas vence a partida. |
| Checkpoint / respawn / DeathZone | **VALIDADO** | Base reutilizável. |
| PlayerStatusEffects | **VALIDADO** | Base de Stun e Slow confirmada em gameplay. |
| Poder Stun | **IMPLEMENTADO** | Controller, projétil, pickup, impacto e feedback existem no repo. |
| Poder Slow | **IMPLEMENTADO** | Controller, projétil, pickup, impacto e feedback existem no repo. |
| Inventário/HUD de poderes | **IMPLEMENTADO** | Presente no repo. |
| Spawn aleatório de pickups | **IMPLEMENTADO** | `PowerSpawnManager` presente no repo. |
| Countdown de largada | **IMPLEMENTADO** | Fluxo 3 → 2 → 1 → VAI! presente no repo. |
| Setas de direção dos jogadores | **IMPLEMENTADO** | Alteração de Carlos integrada via PR #1. |

## Minigames oficiais planejados

As ideias antigas de **Arco e Flecha, Curling e Snowboard** não fazem mais parte do roadmap ativo.

| Minigame | Status | Mecânica atual |
| --- | --- | --- |
| **100m com barreiras** | **EM DESENVOLVIMENTO** | Corrida 1v1, pista de atletismo, barreiras e chegada. Primeiro a cruzar vence. |
| **Salto a distância** | **PLANEJADO** | Corrida de aproximação, salto antes da linha e medição da distância válida. |
| **Lançamento de dardo** | **PLANEJADO** | Barra de força, ângulo e vento; maior distância válida vence. |
| **Levantamento de peso** | **PLANEJADO** | Escolha da carga, timing/força e equilíbrio; maior peso válido vence. |
| **Sumô** | **PLANEJADO** | Arena curta com movimentação, investida e disputa por posição; objetivo é tirar o adversário do ringue. |
| **Canoagem** | **PLANEJADO** | Câmera diagonal aérea; rio horizontal com obstáculos; remadas esquerda/direita controlam impulso e rotação, enquanto cima/baixo permitem desviar. |
| **Natação** | **PLANEJADO** | Braçadas alternadas, ritmo e stamina para impedir spam; virada na borda com timing. |
| **Tênis** | **PLANEJADO** | Movimento lateral e rebatida baseada em timing, com variações como lob e smash. |
| **Pescaria** | **PLANEJADO** | Câmera mais aérea; P1 e P2 em margens opostas, rio compartilhado ao centro e peixes em movimento. |

## User Stories

### US01 — 100m com barreiras

**Como** jogador,  
**quero** competir em uma corrida de 100m com barreiras contra outro jogador,  
**para** vencer a prova chegando primeiro ao final da pista.

**Critérios de aceitação:**
- P1 e P2 iniciam a prova de forma justa após o countdown.
- Os dois jogadores conseguem percorrer a pista sem sobreposição visual que prejudique a leitura.
- As barreiras são obstáculos separados do background e podem ser configuradas na cena.
- A FinishLine identifica corretamente o primeiro jogador a cruzá-la.
- O vencedor recebe a medalha e o fluxo de Victory existente é preservado.

### US02 — Salto a distância

**Como** jogador,  
**quero** correr e executar um salto antes da linha limite,  
**para** tentar alcançar a maior distância possível.

**Critérios de aceitação:**
- Existe uma área de corrida de aproximação.
- O salto precisa ocorrer antes da linha de falta.
- O ponto de aterrissagem é medido.
- Tentativas inválidas não contam como melhor marca.
- Vence o jogador com a maior distância válida.

### US03 — Lançamento de dardo

**Como** jogador,  
**quero** controlar força e ângulo do lançamento,  
**para** lançar o dardo o mais longe possível.

**Critérios de aceitação:**
- O jogador controla uma barra de força.
- O jogador influencia o ângulo de lançamento.
- O vento influencia a trajetória.
- Lançamentos fora da área válida não contam.
- Vence a maior distância válida.

### US04 — Levantamento de peso

**Como** jogador,  
**quero** escolher uma carga e executar corretamente o levantamento,  
**para** registrar o maior peso válido.

**Critérios de aceitação:**
- O jogador escolhe a carga antes da tentativa.
- Existe uma etapa de força/timing para iniciar o levantamento.
- Existe uma etapa de equilíbrio para estabilizar a barra.
- Cada jogador possui até 3 tentativas.
- Vence o maior peso levantado com sucesso.

### US05 — Sumô

**Como** jogador,  
**quero** disputar espaço físico com meu adversário em uma arena,  
**para** empurrá-lo para fora antes que ele faça o mesmo comigo.

**Critérios de aceitação:**
- Os dois jogadores iniciam dentro do ringue.
- Existe movimentação e uma ação de investida/empurrão.
- Sair da área válida determina a derrota do round.
- A disputa é curta e adequada ao ritmo de party game.
- A prova possui condição clara de vitória.

### US06 — Canoagem

**Como** jogador,  
**quero** controlar a canoa enquanto remo e desvio de obstáculos,  
**para** completar o percurso antes do adversário.

**Critérios de aceitação:**
- A câmera utiliza visão diagonal/aérea adequada ao percurso.
- Remar para esquerda e direita influencia o avanço e a rotação da canoa.
- Remar apenas de um lado faz a canoa virar.
- O jogador pode deslocar a canoa para cima/baixo para desviar de obstáculos.
- Vence quem cruza primeiro a chegada.

### US07 — Natação

**Como** jogador,  
**quero** alternar braçadas e administrar stamina,  
**para** manter um ritmo eficiente até o final da prova.

**Critérios de aceitação:**
- Braçadas alternadas geram avanço.
- Existe uma barra ou valor de stamina.
- Spam de comandos possui custo suficiente para não ser a melhor estratégia.
- A borda possui uma mecânica de virada baseada em timing.
- Vence quem toca primeiro a borda final.

### US08 — Tênis

**Como** jogador,  
**quero** me posicionar e rebater a bola no timing correto,  
**para** marcar pontos contra meu adversário.

**Critérios de aceitação:**
- Cada jogador se movimenta dentro do seu lado da quadra.
- A rebatida depende do timing do contato.
- A bola pode mudar força/direção conforme a execução.
- O jogo reconhece quando um ponto termina.
- A partida termina ao atingir a pontuação definida para o minigame.

### US09 — Pescaria

**Como** jogador,  
**quero** disputar peixes em um rio compartilhado,  
**para** terminar a rodada com a melhor pontuação.

**Critérios de aceitação:**
- A prova utiliza câmera aérea ou diagonal com boa leitura dos dois jogadores.
- P1 e P2 ficam em margens opostas do rio.
- Os peixes se movimentam pela área compartilhada.
- Os dois jogadores conseguem interagir com a pescaria simultaneamente sem sobreposição confusa.
- A condição de pontuação e vitória deve ser definida antes da implementação do MVP.

## Personagens

**Jogáveis, validados:** Panda-gigante (personagem mestre) e Urso-polar.

**Planejados:** Grizzly, Urso-do-sol, Urso-de-óculos e Panda-vermelho.

Os dois jogáveis reutilizam `PlayerBase`, câmera, poderes, física e fluxo de vitória. A seleção visual é configurada independentemente para P1 e P2. A UI de seleção ainda não foi implementada.

Contrato técnico para o colega responsável pelo menu: [docs/character-selection-integration.md](character-selection-integration.md).

A produção do restante do elenco deve esperar a estabilidade do recorte local em mais provas.

## Escopo adiado

Continuam fora do foco atual:

- multiplayer online;
- backend;
- contas de usuário;
- matchmaking;
- ranking online;
- seis jogadores simultâneos;
- expansão WebGL como prioridade;
- produção dos seis personagens antes de validar múltiplos minigames.

O objetivo continua sendo um **vertical slice local, reutilizável e jogável** antes de expandir escopo.

## Entrega da integração de personagens — critérios de aceite pendentes

- [ ] Menu permite selecionar e confirmar personagens de P1 e P2 separadamente.
- [ ] As escolhas são aplicadas ao carregar a cena `HurdlesRace`.
- [ ] Uma MD3 inteira mantém os personagens escolhidos e o placar entre rodadas.
- [ ] REVANCHE inicia uma nova MD3 sem alterar os personagens.
- [ ] TROCAR PERSONAGENS abre a tela de seleção e permite nova combinação.
- [ ] O fluxo completo funciona sem regressões em poderes, câmera, controles e vitória.

Os testes de combinações e a lógica de travamento/revanche já foram validados isoladamente. Os itens acima são para a **integração ponta a ponta do menu**, ainda pendente.
