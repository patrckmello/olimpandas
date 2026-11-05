# Product Backlog — Olimpandas

> Atualizado em 05/11/2026.
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

O fluxo `MainMenu → CharacterSelect → HurdlesRace → resultado da MD3` está implementado, incluindo revanche e retorno à seleção. O relatório de verificações está em [integração de personagens](character-selection-integration.md).

Com aproximadamente **3 semanas/aulas restantes para a entrega acadêmica**, o escopo foi reduzido para **4 personagens e 4 minigames**. Esse corte vale para a entrega atual; o restante permanece como roadmap pós-entrega.

Objetivos imediatos:

1. fechar e validar **100m com barreiras** como primeira modalidade completa;
2. implementar **Salto a distância**, **Canoagem** e **Pescaria** em versões MVP curtas, competitivas e completas;
3. entregar quatro personagens visualmente distintos: **Panda-gigante, Urso-polar, Grizzly e Urso-de-óculos**, todos reutilizando a mesma base de gameplay;
4. integrar as quatro modalidades ao fluxo do Circuito Olímpico sem reescrever sistemas já estáveis;
5. reservar a parte final da terceira semana para integração, build, playtest e correção de bugs — sem abrir novas features.

## Escopo da entrega acadêmica — 3 semanas

### Conteúdo obrigatório

**Personagens da entrega:**
- Panda-gigante — **VALIDADO**;
- Urso-polar — **VALIDADO**;
- Grizzly — **PLANEJADO PARA A ENTREGA**;
- Urso-de-óculos — **PLANEJADO PARA A ENTREGA**.

**Minigames da entrega:**
- 100m com barreiras — **VALIDADO**;
- Salto a distância — **EM DESENVOLVIMENTO** (MVP jogável em `LongJump.unity`; arte provisória);
- Canoagem — **PLANEJADO PARA A ENTREGA**;
- Pescaria — **PLANEJADO PARA A ENTREGA**.

**Pós-entrega:** Lançamento de dardo, Levantamento de peso, Sumô, Natação e Tênis. Urso-do-sol e Panda-vermelho também ficam fora da entrega acadêmica atual.

### Regra de corte

Uma modalidade só conta como entregue quando existir o fluxo completo **entrada → instrução/contagem → gameplay 1v1 → condição de vitória → resultado → retorno ao circuito**. Polimento extra, efeitos, variedade de mapas e regras secundárias só entram depois que esse fluxo estiver funcional nas quatro modalidades.

### Divisão do time

| Responsável | Foco principal | Tarefas da entrega |
| --- | --- | --- |
| **Patrick** | Mecânicas, sprites e integração de gameplay | Fechar 100m; implementar lógica de Salto, Canoagem e Pescaria; produzir/integrar Grizzly e Urso-de-óculos; manter base compartilhada e contratos dos minigames. |
| **Gabriel** | UI principal e fluxo de navegação | Menu, seleção de personagens, telas de sorteio/transição, resultado geral, medalhas, campeão, revanche/troca de personagens e consistência visual entre as telas. |
| **Cadu (EoGarlos)** | Microfeatures de UI/gamefeel com implementação própria | Criar componentes fechados de HUD e feedback, com script + prefab próprios: cronômetro, contador de tentativas, placar da Pescaria, indicador de progresso da Canoagem, avisos simples de falta/resultado e painel de controles/créditos. Não altera lógica interna dos minigames de Patrick; consome dados/eventos expostos por eles. |

### Plano por semana

#### Semana 1 — consolidar a base
- **Patrick:** finalizar/polir 100m com barreiras e deixar Salto a distância jogável do início ao fim; iniciar sprites do Grizzly.
- **Gabriel:** fechar resultado do 100m, preparar o visual reutilizável de instruções/transições e adaptar a seleção para quatro personagens sem quebrar os dois já existentes.
- **Cadu:** evoluir o cronômetro que já criou para um HUD reutilizável de tempo, exibir o tempo final da corrida e criar um contador visual de tentativas para o Salto a distância. Cada item deve ser um componente isolado, com script/prefab próprio, sem alterar a regra da corrida ou do salto.
- **Gate da semana:** 100m validado + Salto a distância jogável + terceiro personagem pelo menos integrado em teste.

#### Semana 2 — variedade de gameplay
- **Patrick:** implementar Canoagem MVP e Pescaria MVP; concluir Grizzly e iniciar/concluir Urso-de-óculos conforme pipeline já validado.
- **Gabriel:** tela curta de instruções de cada modalidade, apresentação visual das provas e telas de resultado/transição.
- **Cadu:** criar o indicador de progresso da Canoagem e o placar em tempo real da Pescaria (P1, P2 e tempo restante), além de feedbacks simples como “FALTA”, “PEIXE!”, “+PONTOS” ou equivalentes. A lógica de quando esses eventos acontecem continua pertencendo aos scripts de gameplay de Patrick.
- **Gate da semana:** quatro minigames executáveis isoladamente e quatro personagens selecionáveis, mesmo que ainda sem polimento final.

#### Semana 3 — integração e entrega
- **Patrick:** integrar as quatro modalidades ao Circuito Olímpico, corrigir bugs de gameplay e evitar novas features após a integração.
- **Gabriel:** fechar sorteio/transições, placar geral, campeão, revanche/troca de personagens e consistência visual.
- **Cadu:** criar/fechar o painel de controles e créditos da entrega, polir os componentes de HUD que produziu e corrigir bugs especificamente nessas features. Também participa do playtest final, mas QA não é sua responsabilidade principal.
- **Gate final:** circuito ponta a ponta jogável com quatro modalidades e quatro personagens, sem erro bloqueante no Console.

### Contrato de trabalho entre Patrick e Cadu

Para evitar conflito em cenas/scripts, Patrick continua dono da **regra do minigame** e expõe somente os dados necessários para a interface, preferencialmente por propriedades, métodos públicos pequenos ou eventos. Cadu fica dono do componente visual que consome esses dados.

Exemplos:
- Patrick informa tempo/estado da corrida; Cadu controla apresentação do cronômetro.
- Patrick informa tentativa atual, falta e distância do Salto; Cadu apresenta contador/feedback.
- Patrick informa progresso/posição da Canoagem; Cadu apresenta indicador visual.
- Patrick informa pontos, captura e tempo da Pescaria; Cadu apresenta placar e feedback.

Cadu não deve editar `PlayerController`, regras de vitória, física, scripts centrais dos minigames ou cenas em desenvolvimento de Patrick sem combinar antes. O objetivo é permitir trabalho paralelo com features concretas e baixa chance de conflito no Git.

### MVP mínimo de cada nova modalidade

- **Salto a distância:** corrida de aproximação, linha de falta, detecção do primeiro pouso, distância válida, até 3 tentativas e maior marca vence.
- **Canoagem:** remadas esquerda/direita geram impulso; alternância eficiente mantém velocidade; excesso de um lado causa desvio; cima/baixo permite evitar obstáculos; colisão reduz velocidade; primeiro a chegar vence. Evitar física naval complexa.
- **Pescaria:** rodada cronometrada em rio compartilhado, peixes com movimento simples, ação de arremesso/fisgada e etapa curta de recolhimento/timing; peixes dão pontos e maior pontuação vence. Empate usa disputa extra curta.

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
6. **Revanche e navegação:** novo sorteio no início da revanche? A seleção atual é `CharacterSelect`; definir pausa/desistência no circuito futuro.

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
- [ ] Validar o fluxo com as quatro modalidades da entrega: 100m com barreiras, Salto a distância, Canoagem e Pescaria.
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
| Botões REVANCHE e TROCAR PERSONAGENS | **IMPLEMENTADO** | Revanche preserva personagens; troca zera placar, desbloqueia e carrega `CharacterSelect`. Aceite humano da integração pendente. |
| Tela de seleção / navegação | **IMPLEMENTADO** | `CharacterSelectMenu` recebe confirmações independentes e permite voltar ao `MainMenu`. Ver relatório de testes. |
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
| Cronômetro de corrida | **IMPLEMENTADO** | Ajuste de Carlos integrado via PR #4; `Timer` existe e está configurado no `PrototypeRace`. Aplicação por modalidade deve ser validada individualmente. |

## Minigames oficiais planejados

As ideias antigas de **Arco e Flecha, Curling e Snowboard** não fazem mais parte do roadmap ativo.

| Minigame | Status | Mecânica atual |
| --- | --- | --- |
| **100m com barreiras** | **VALIDADO — ENTREGA** | Corrida 1v1, pista de atletismo, barreiras e chegada. Primeiro a cruzar vence. |
| **Salto a distância** | **EM DESENVOLVIMENTO — ENTREGA** | Simultâneo em duas raias; alternar esquerda/direita gera velocidade, saltar antes da linha (tábua perfeita dá bônus) e medir a distância válida; 3 tentativas, maior marca vence, empate vai a tentativa extra. Bandeira da melhor marca, areia no pouso e câmera lenta quando todos estão no ar. Dash desativado. |
| **Lançamento de dardo** | **ADIADO — PÓS-ENTREGA** | Barra de força, ângulo e vento; maior distância válida vence. |
| **Levantamento de peso** | **ADIADO — PÓS-ENTREGA** | Escolha da carga, timing/força e equilíbrio; maior peso válido vence. |
| **Sumô** | **ADIADO — PÓS-ENTREGA** | Arena curta com movimentação, investida e disputa por posição; objetivo é tirar o adversário do ringue. |
| **Canoagem** | **PLANEJADO — ENTREGA** | Câmera diagonal/aérea; rio com obstáculos; remadas esquerda/direita controlam impulso e desvio, enquanto cima/baixo permitem evitar obstáculos. |
| **Natação** | **ADIADO — PÓS-ENTREGA** | Braçadas alternadas, ritmo e stamina para impedir spam; virada na borda com timing. |
| **Tênis** | **ADIADO — PÓS-ENTREGA** | Movimento lateral e rebatida baseada em timing, com variações como lob e smash. |
| **Pescaria** | **PLANEJADO — ENTREGA** | Rio compartilhado, peixes em movimento, rodada cronometrada e disputa por pontos com mecânica simples de fisgada/recolhimento. |

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

**Planejados para esta entrega:** Grizzly e Urso-de-óculos.

**Pós-entrega:** Urso-do-sol e Panda-vermelho.

Os dois jogáveis reutilizam `PlayerBase`, câmera, poderes, física e fluxo de vitória. A seleção visual é configurada independentemente para P1 e P2. A UI `CharacterSelect` está implementada com prévias, navegação independente, confirmação/cancelamento e início condicionado às duas confirmações.

Fluxo, métodos e roteiro de validação: [docs/character-selection-integration.md](character-selection-integration.md).

Para a entrega acadêmica, o elenco fecha em quatro personagens. Grizzly e Urso-de-óculos devem reutilizar integralmente `PlayerBase`, câmera, poderes, física e fluxo de vitória; nenhuma mecânica exclusiva de personagem entra neste prazo.

## Escopo adiado

Continuam fora do foco atual:

- multiplayer online;
- backend;
- contas de usuário;
- matchmaking;
- ranking online;
- seis jogadores simultâneos;
- expansão WebGL como prioridade;
- produção dos seis personagens antes de validar múltiplos minigames;
- Lançamento de dardo, Levantamento de peso, Sumô, Natação e Tênis até depois da entrega acadêmica;
- Urso-do-sol e Panda-vermelho até depois da entrega acadêmica.

O objetivo continua sendo um **vertical slice local, reutilizável e jogável** antes de expandir escopo.

## Entrega da integração de personagens — implementação e aceite

- [ ] Menu permite selecionar e confirmar personagens de P1 e P2 separadamente.
- [ ] As escolhas são aplicadas ao carregar a cena `HurdlesRace`.
- [ ] Uma MD3 inteira mantém os personagens escolhidos e o placar entre rodadas.
- [ ] REVANCHE inicia uma nova MD3 sem alterar os personagens.
- [ ] TROCAR PERSONAGENS abre a tela de seleção e permite nova combinação.
- [ ] O fluxo completo funciona sem regressões em poderes, câmera, controles e vitória.

Os comportamentos acima estão implementados; as caixas representam o **aceite humano ponta a ponta**, ainda pendente. Os registros anteriores de VALIDADO referem-se aos testes históricos informados no projeto, não a uma nova validação visual nesta revisão. As verificações automatizadas desta entrega e seus limites estão no relatório de integração.
