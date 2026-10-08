# Vídeo de lançamento do Olimpandas

Vídeo de ~38 s no estilo "anúncio de produto", gerado só com **JavaScript, Playwright e ffmpeg**.
Toda a arte vem do próprio jogo (`Assets/Art`): Panda-gigante, estádio, barreiras, poderes e setas P1/P2.

Resultado: [`out/olimpandas-lancamento.mp4`](out/olimpandas-lancamento.mp4) (1920×1080, 60 fps, AAC estéreo).

## Roteiro

| Tempo | Cena |
| --- | --- |
| 0–5 s | Abertura calma ("Passamos meses treinando. Comemos muito bambu.") até um panda espiar a tela |
| 5–9 s | O panda passa de Dash e revela o título — "Nosso panda mais capaz *até agora*." |
| 9–15 s | 100m com barreiras: countdown 3, 2, 1, VAI!, saltos e o P2 derrubando uma barreira |
| 15–21 s | Poderes: Stun congela o P1, Slow acorrenta o P2 — "Porque amizade tem limite." |
| 21–27 s | Benchmarks: fofura, barreiras puladas e o bambu comido durante a prova (ops.) |
| 27–31 s | Roadmap com os minigames do `docs/backlog.md` e os novos ursos |
| 31–34 s | "Um teclado. Dois pandas." — controles reais (WASD/setas + Shift) e um BONK! |
| 34–38,5 s | Cartão final com confete e o panda comemorando |

## Como gerar

Requisitos: Node 18+, ffmpeg com libx264 e um Chromium do Playwright.

```bash
cd launch-video
npm install                # instala o playwright (use PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1 se já tiver o Chromium)
npx playwright install chromium   # se ainda não tiver o navegador

node render.mjs                    # vídeo final em out/olimpandas-lancamento.mp4
node render.mjs --preview          # versão rápida 960x540 a 30 fps em out/preview.mp4
node render.mjs --stills 6,12.9    # PNGs de instantes específicos em out/stills/
node render.mjs --audio            # só a trilha em out/audio.wav
```

Opções: `--fps N`, `--scale 0.5`, `--workers N`.

## Como funciona

- `scene/index.html` + `scene/scene.js`: as cenas em HTML/CSS/Canvas. Cada frame é uma **função pura do tempo**
  (`window.__seek(t)`), sem estado entre frames — por isso o render roda em vários workers em paralelo.
- `scene/timeline.js`: duração, BPM e todos os *cues*. Imagem e som leem os mesmos números.
- `scene/race.js`: coreografia determinística da corrida (velocidade de 6 un./s, barreiras a cada 6 un.,
  Slow de 60% por 1 s ao bater na barreira — os mesmos valores do jogo).
- `scene/audio.js`: trilha e efeitos 100% sintetizados com Web Audio (`OfflineAudioContext`) dentro do Chromium:
  piano da abertura, groove de 120 BPM, bipes do countdown, gelo, corrente, torcida, trombone triste e fanfarra.
- `render.mjs`: sobe um servidor HTTP local no repositório, captura os frames via Playwright/CDP, codifica
  segmentos com ffmpeg e junta tudo com o áudio.

## Fontes

Newsreader, Inter e JetBrains Mono (Google Fonts, licença OFL — ver `scene/fonts/OFL-*.txt`) e Bangers,
que já vem com o TextMesh Pro do projeto (`Assets/TextMesh Pro/Examples & Extras/Fonts`).
