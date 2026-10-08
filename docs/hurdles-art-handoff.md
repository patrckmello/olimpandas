# Arte da prova de barreiras

Cena de trabalho: `Assets/Scenes/HurdlesRace.unity`. Ela contém um único estádio, uma pista compartilhada, duas raias, sete barreiras por raia e uma chegada. `PrototypeRace.unity` permanece como sandbox.

## Background

O arquivo `Assets/Art/Maps/StadiumTwoLanes.png` (2508 × 627 px) já está integrado no objeto `Single stadium backdrop` com 256 Pixels Per Unit. A imagem principal ocupa 56 × 14 unidades. Para a câmera poder abrir quando os pandas se afastam, o cenário tem continuações espelhadas à esquerda e à direita e extensões de céu e gramado feitas com recortes da mesma arte. O objeto `Stadium overflow fill` delimita a cobertura completa da câmera e fica atrás dessas camadas. Esses elementos são apenas visuais, sem colisão ou novas raias jogáveis. As formas provisórias `Single grandstand`, `Shared athletics track` e `Lane divider` estão com os renderizadores desligados. O chão físico permanece nos objetos `Ground_P1` e `Ground_P2`, com SpriteRenderer desligado; a imagem do mapa não carrega a colisão.

O cenário mostra um estádio e uma pista com duas raias no mesmo enquadramento. As raias físicas estão a 2,6 unidades de distância vertical. Conferir no Game View se os pés dos jogadores coincidem com as faixas pintadas na imagem. As sete barreiras de cada raia ocupam os mesmos X: -7, -1, 5, 11, 17, 23 e 29.

## Spritesheet da barreira

A arte fornecida está em `Assets/Art/Maps/HurdleFall5.png` (2172 × 724 px, fundo transparente). Ela já foi importada como Sprite (2D and UI), Sprite Mode Multiple, 256 Pixels Per Unit, tamanho máximo 4096 e cinco recortes, da barreira em pé até caída. Os quadros têm larguras diferentes; seus pivots foram alinhados ao mesmo apoio da base para a queda não deslocar a barreira na pista.

O prefab `Assets/Prefabs/Gameplay/Hurdle.prefab` já usa o primeiro quadro no filho `Visual/HurdleArt`, com Draw Mode Simple, cor branca, posição local `(0, 0, 0)` e escala `(0,8, 0,8, 0,8)`. Os cinco quadros estão ligados a `fallFrames` na ordem da queda. O collider mede 0,8 × 1 unidade e fica ligeiramente abaixo do topo desenhado para o salto ser tolerante; conferir o encaixe com o panda no Game View. A colisão o desliga quando o panda bate e aplica Slow com velocidade a 60% por 1 segundo, sem efeito visual no panda; intensidade e duração ficam configuráveis no componente. A animação acontece no objeto `Visual`.

## Integração e teste

Abrir `HurdlesRace.unity` em Unity 6000.5.9f1. Conferir P1/P2 simultâneos, salto e dash em cada raia, reação das barreiras, câmera, countdown, chegada, medalhas e repetição da rodada. Os poderes ainda não possuem pickups nessa cena; a interação entre jogadores em raias separadas precisa ser definida antes de ativá-los. O botão de resultado reutilizado do protótipo recarrega a cena atual.
