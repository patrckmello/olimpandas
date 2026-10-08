// Timeline compartilhada entre imagem (scene.js) e som (audio.js).
// Todo instante do vídeo é função pura do tempo t (em segundos).

export const W = 1920;
export const H = 1080;
export const FPS = 60;
export const DURATION = 38.5;

// A trilha entra no grid de 120 BPM a partir do "drop" em 5s.
export const BPM = 120;
export const BEAT = 60 / BPM;
export const BAR = BEAT * 4;
export const GRID0 = 5.0;

export const SCENES = {
  intro: [0, 5],
  title: [5, 9],
  race: [9, 15],
  powers: [15, 21],
  bench: [21, 27],
  roadmap: [27, 31],
  keys: [31, 34],
  end: [34, DURATION],
};

export const CUE = {
  // 1. Abertura calma
  l1: 0.35,
  l2: 1.75,
  l3: 3.05,
  peekUp: 3.8,
  peekDown: 4.5,
  // 2. Título
  dash: 5.0,
  dashEnd: 5.55,
  eyebrow: 5.5,
  mark: 5.75,
  sub: 6.0,
  pills: [7.0, 7.25, 7.5],
  titleOut: 8.5,
  // 3. 100m com barreiras
  raceIn: 9.0,
  count: [9.5, 10.0, 10.5],
  go: 11.0,
  raceOut: 14.5,
  // 4. Poderes
  powersIn: 15.0,
  iceThrow: 16.0,
  iceHit: 16.25,
  chainThrow: 17.5,
  chainHit: 17.75,
  punch: 18.5,
  powersOut: 20.5,
  // 5. Benchmarks
  benchIn: 21.0,
  groups: [21.75, 22.75, 23.75],
  overshoot: 24.5,
  ops: 25.25,
  foot: 25.5,
  benchOut: 26.5,
  // 6. Roadmap
  roadIn: 27.0,
  chips: [27.5, 27.75, 28.0, 28.25, 28.5, 28.75, 29.0, 29.25, 29.5],
  bears: 29.75,
  roadOut: 30.5,
  // 7. Um teclado, dois pandas
  keysIn: 31.0,
  kW: 31.5,
  kUp: 32.0,
  kShiftL: 32.5,
  kShiftR: 32.75,
  bonk: 33.0,
  keysOut: 33.6,
  // 8. Cartão final
  wipe: 34.0,
  lockup: 34.25,
  tagline: 34.75,
  party: 35.0,
  final: 37.0,
};
