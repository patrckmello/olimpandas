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

## Regras de produto — Circuito Olímpico (decisões de 09/10/2026)

### Definido para o MVP

- **Modo principal:** Circuito Olímpico local 1v1. A disputa geral termina quando um jogador conquista **duas medalhas de modalidades** (MD3 de modalidades).
- **Sorteio surpresa:** a modalidade é sorteada **uma de cada vez**, antes da respectiva prova; o restante da sequência não é revelado antecipadamente.
- **Disponibilidade:** sortear apenas minigames realmente jogáveis/habilitados. Não tratar as nove modalidades planejadas como prontas.
- **Repetição:** evitar repetir modalidade dentro do mesmo circuito enquanto houver modalidades habilitadas ainda não utilizadas. Se todas já saíram, permitir novo ciclo de sorteio; com apenas uma disponível, repetir é inevitável.
- **Pontuação hierárquica:** disputas/tentativas/pontos internos determinam o vencedor da modalidade. **Somente a vitória da modalidade concede uma medalha geral**, e não cada confronto interno.
- **Formato interno adaptável à modalidade:** modalidades de confrontos curtos, como Corrida e Sumô, **podem usar MD3 interna**; outras (Tênis, Pescaria, Salto a distância, Lançamento de dardo etc.) devem usar regras próprias de pontos, tempo ou tentativas. Não obrigar todos os minigames a seguirem MD3 idêntica.
- **Personagens no MVP:** diferença **somente visual**; mesmos parâmetros de gameplay e acesso aos poderes para Panda-gigante, Urso-polar e personagens futuros.
- **Escolha de personagem:** P1/P2 escolhem antes do Circuito, mantêm os ursos durante toda a MD3 geral; revanche preserva escolha e nova seleção permite trocar.
- **Empates:** se uma disputa terminar empatada, haverá **disputa extra até existir um vencedor**. O formato e o limite operacional desse desempate serão definidos por modalidade; empate não entrega medalha geral.
- **Poderes por modalidade:** cada minigame define explicitamente quais poderes estão disponíveis (incluindo a possibilidade de nenhum). Não presumir Stun/Slow em todos os esportes.
- **Duração-alvo do circuito:** **5–8 minutos** por Circuito Olímpico completo, como objetivo de design a validar com playtests, não como temporizador global já implementado.

> **Mudança futura, ainda não implementada:** o código atual `GameManager.AddMedal` concede medalha diretamente à vitória da corrida, e `NextRoundButton` recarrega a cena atual. Isso é o comportamento validado no protótipo, **não** a regra final de medalhas por modalidade. Será necessário distinguir vencedor da disputa interna, vencedor da modalidade e campeão geral antes de integrar múltiplas provas.

### Hipóteses de design a validar por playtest (não são regras fechadas)

- A meta **definida** é **5–8 minutos por Circuito Olímpico**; medir por playtest e ajustar tempos internos para aproximar o jogo dessa meta.
- Corrida e Sumô são candidatos a MD3 interna; Tênis pode funcionar por pontos, Pescaria por pontuação com cronômetro, e provas de marca por tentativas. Definir regras exatas separadamente antes da implementação de cada uma.
- Uma tela de sorteio animada deve mostrar o próximo esporte; ritmo, animação e confirmação de controles dependem de testes de usabilidade.
- **Prova Livre** para escolher minigames diretamente é uma possibilidade posterior, não parte do MVP.

### Regras de negócio pendentes de decisão

| Tema | Pergunta que precisa de resposta | Impacto |
| --- | --- | --- |
| Tempo por modalidade | Considerando o circuito de 5–8 minutos, quanto dura cada confronto, tentativa e transição? Qual o teto de tempo quando ninguém termina? | Preservar a meta de duração |
| Desempate por esporte | A disputa extra repete a prova inteira, usa morte súbita ou variante curta? O que fazer se os empates se repetirem muitas vezes? | Garantir um vencedor sem partidas intermináveis |
| Formato de cada esporte | Corrida e Sumô terão MD3 obrigatória? Quantos pontos, tentativas ou segundos nas demais provas? | Condição de vitória e UI específicas |
| Reinício de disputa interna | Entre tentativas/rounds, o que reseta: posição, poderes, cooldown, obstáculos, cenário? | Consistência competitiva |
| Lista de poderes por modalidade | Quais poderes específicos são habilitados em Corrida, Sumô, Tênis etc.? Há limite por disputa e o inventário reseta entre rounds? | Balanceamento e justiça |
| Sorteio e repetição | Deve haver pesos por modalidade, duração ou categoria? Alguma prova pode ficar fora de determinados circuitos? | Justiça e variedade |
| Desistência / pausa | Como pausar, reiniciar, desistir, voltar ao menu ou lidar com jogador ausente? | UX local e recuperação de estado |
| Controles | Teclado compartilhado é suficiente no MVP? Suporte a controles, remapeamento e tela de instruções? | Acessibilidade e entrada |
| Vantagens de pista/lado | Haverá alternância de raias/lados ou compensações para garantir igualdade? | Competitividade |
| Resultado do circuito | Exibir placar por disputa interna, medalhas gerais, histórico de modalidades e campeão? | Clareza do fluxo |
| Nova partida | REVANCHE faz novo sorteio desde o começo? TROCAR PERSONAGENS volta para qual cena/painel? | Navegação e estado persistente |
| Falhas de carregamento | O que fazer se a cena sorteada estiver indisponível ou sem configuração válida? | Robustez |

### Registro das decisões fechadas em 09/10/2026

| Decisão | Regra aprovada | Status técnico |
| --- | --- | --- |
| Empates | Disputa extra até surgir vencedor | **REGRA DEFINIDA; NÃO IMPLEMENTADA** no fluxo genérico |
| Poderes | Cada modalidade determina os poderes permitidos | **REGRA DEFINIDA; CONFIGURAÇÃO POR MODALIDADE PENDENTE** |
| Duração | Circuito Olímpico com alvo de 5–8 minutos | **META DEFINIDA; A VALIDAR POR PLAYTEST** |

### Próximas decisões prioritárias

1. **Formato da MD3 interna da corrida:** definir se cada corrida vencida vale 1 ponto interno e duas vitórias dão a medalha geral, ou se a corrida usa outra regra.
2. **Tempo máximo e desempate concreto por modalidade:** como impedir que disputas extras ultrapassem repetidamente a meta de 5–8 minutos.
3. **Inventário e poderes:** decidir quais são permitidos na Corrida, quando pickups surgem e se os poderes acumulados reiniciam entre disputas.
4. **Critérios de vitória dos demais esportes:** pontos/tentativas, validade e desempate de cada modalidade antes de desenvolvê-la.
5. **Duração e ritmo:** definir orçamento de tempo para sorteio, introdução, gameplay e tela de resultado; testar com pessoas.
6. **Revanche e navegação:** novo sorteio no início da revanche? Endereço final da tela de seleção e comportamento de sair/pausar.

### Backlog de implementação desse fluxo

- [ ] Definir contrato entre `GameManager` (medalhas do circuito) e placar/regras internas de cada minigame, sem destruir a corrida atual.
- [ ] Adicionar catálogo de poderes permitidos por modalidade e aplicar as restrições sem afetar o sistema de poderes existente.
- [ ] Implementar mecanismo de disputa extra para empate, com regras concretas por esporte.
- [ ] Medir duração do circuito completo e ajustar para meta de 5–8 minutos.
- [ ] Criar catálogo de modalidades habilitadas e `MinigameRotationManager` simples.
- [ ] Sortear uma modalidade por vez, com histórico da MD3 geral e política de repetição.
- [ ] Criar apresentação de sorteio e transição para a cena sorteada.
- [ ] Fazer o minigame comunicar **somente o vencedor da modalidade** ao sistema geral de medalhas.
- [ ] Ajustar os botões de próxima prova, revanche e troca de personagens para o fluxo de Circuito Olímpico.
- [ ] Validar com ao menos duas modalidades reais; idealmente três para testar ausência de repetição em um circuito completo.
- [ ] Testar pontuação, persistência dos personagens e regresso ao menu no fluxo ponta a ponta.

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
| Melhor de 3 / medalhas (fluxo atual da corrida) | **VALIDADO** | Primeiro a 2 medalhas vence a partida no protótipo; separar MD3 interna da modalidade e MD3 geral ainda é **PLANEJADO**. |
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
- No protótipo atual, o vencedor da corrida recebe a medalha diretamente. No Circuito Olímpico definitivo, a medalha geral só deve ser entregue ao vencedor da modalidade (após a MD3 interna, caso adotada). Preservar a animação de Victory.

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
