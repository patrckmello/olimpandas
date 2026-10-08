# Arte da prova de barreiras

Cena de trabalho: `Assets/Scenes/HurdlesRace.unity`. Ela contém um único estádio provisório, uma pista compartilhada, duas raias, cinco barreiras por raia e uma chegada. `PrototypeRace.unity` permanece como sandbox.

## Background

Importar a arte em `Assets/Art/Maps/` como Sprite (2D and UI). Usar 256 Pixels Per Unit como ponto de partida. A cena já possui os objetos `Single stadium backdrop`, `Single grandstand`, `Shared athletics track` e `Lane divider`, que são formas provisórias sem colisão. Substituir apenas a aparência desses objetos ou adicionar camadas de arte atrás dos jogadores. O chão físico está nos objetos `Ground_P1` e `Ground_P2`, com SpriteRenderer desligado; a imagem do mapa não deve carregar a colisão.

O cenário deve mostrar um estádio e uma pista com duas raias no mesmo enquadramento. As raias atuais estão a 2,6 unidades de distância vertical para manter os dois pandas legíveis. Ao ajustar a arte, conferir no Game View se os pés dos jogadores coincidem com suas raias.

## Spritesheet da barreira

A arte fornecida está em `Assets/Art/Maps/HurdleFall5.png` (2172 × 724 px, fundo transparente). Ela já foi importada como Sprite (2D and UI), Sprite Mode Multiple, 256 Pixels Per Unit, tamanho máximo 4096 e cinco recortes, da barreira em pé até caída. Os quadros têm larguras diferentes; seus pivots foram alinhados ao mesmo apoio da base para a queda não deslocar a barreira na pista.

O prefab `Assets/Prefabs/Gameplay/Hurdle.prefab` já usa o primeiro quadro no filho `Visual/HurdleArt`, com Draw Mode Simple, cor branca, posição local `(0, 0, 0)` e escala `(0,3, 0,3, 0,3)`. Os cinco quadros estão ligados a `fallFrames` na ordem da queda. O collider provisório mede 0,24 × 0,45 unidade; conferir o encaixe com o panda no Game View. A colisão o desliga quando o panda bate e aplica Slow com velocidade a 60% por 1 segundo; intensidade e duração ficam configuráveis no componente. A animação acontece no objeto `Visual`.

## Integração e teste

Abrir `HurdlesRace.unity` em Unity 6000.5.9f1. Conferir P1/P2 simultâneos, salto e dash em cada raia, reação das barreiras, câmera, countdown, chegada, medalhas e repetição da rodada. Os poderes ainda não possuem pickups nessa cena; a interação entre jogadores em raias separadas precisa ser definida antes de ativá-los. O botão de resultado reutilizado do protótipo recarrega a cena atual.
