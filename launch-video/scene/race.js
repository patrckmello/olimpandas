// Coreografia determinística dos 100m com barreiras.
// Integra as posições a 1 ms e devolve funções de amostragem por tempo,
// para que a imagem e o som concordem exatamente sobre saltos e quedas.

import { CUE } from './timeline.js';
import { lerp } from './lib.js';

export const RACE = {
  V: 860, // px/s em velocidade de cruzeiro (6 unidades/s do jogo)
  ramp: 0.34, // segundos até a velocidade máxima
  dt: 1 / 1000,
  end: 15.4,
  hurdles: [700, 1560, 2420, 3280, 4140, 5000], // x mundial das barreiras (mesmo X nas duas raias)
  barMid: 34, // centro da travessa em relação ao poste
  jumpDur: 0.5,
  jumpH: 160,
  start: [-55, 55], // P1 e P2 levemente desencontrados para não se esconderem na câmera
  p2Head: 0.08, // o P2 larga um pouquinho antes (e se empolga demais)
  crashIdx: 1, // barreira que o P2 derruba
  crashGap: 44,
  slow: 0.6, // como no jogo: 60% da velocidade por 1 segundo
  slowDur: 1.0,
  recover: 0.35,
  tumble: 0.55,
  fallDur: 0.3,
};

function integrate(x0, head, crashAt) {
  const { V, ramp, dt, end } = RACE;
  const go = CUE.go;
  const n = Math.ceil((end - go) / dt) + 1;
  const xs = new Float64Array(n);
  let x = x0;
  let crashT = null;
  let crashX = null;
  for (let i = 0; i < n; i++) {
    const tau = i * dt + head;
    let v = V * Math.min(1, Math.max(0, tau) / ramp);
    if (crashT !== null) {
      const since = i * dt - crashT;
      if (since < RACE.slowDur) v *= RACE.slow;
      else if (since < RACE.slowDur + RACE.recover)
        v *= lerp(RACE.slow, 1, (since - RACE.slowDur) / RACE.recover);
    }
    if (i) x += v * dt;
    xs[i] = x;
    if (crashAt != null && crashT === null && x >= crashAt) {
      crashT = i * dt;
      crashX = x;
    }
  }
  return { xs, crash: crashT === null ? null : { t: go + crashT, x: crashX } };
}

function sampler(xs) {
  const { dt } = RACE;
  const go = CUE.go;
  return (t) => {
    if (t <= go) return xs[0];
    const f = (t - go) / dt;
    const i = Math.floor(f);
    if (i >= xs.length - 1) return xs[xs.length - 1] + (f - (xs.length - 1)) * dt * RACE.V;
    return lerp(xs[i], xs[i + 1], f - i);
  };
}

function planJumps(xs, skip) {
  const { dt, jumpDur, hurdles, barMid } = RACE;
  const go = CUE.go;
  const jumps = [];
  hurdles.forEach((hx, k) => {
    if (k === skip) return;
    const target = hx + barMid;
    let i = 0;
    while (i < xs.length && xs[i] < target) i++;
    if (i >= xs.length) return;
    const tMid = go + i * dt;
    jumps.push({ k, t0: tMid - jumpDur / 2, t1: tMid + jumpDur / 2 });
  });
  return jumps;
}

let cached = null;

export function race() {
  if (cached) return cached;
  const a = integrate(RACE.start[0], 0, null);
  const b = integrate(RACE.start[1], RACE.p2Head, RACE.hurdles[RACE.crashIdx] - RACE.crashGap);
  const p1 = { x: sampler(a.xs), jumps: planJumps(a.xs, -1), crash: null };
  const p2 = { x: sampler(b.xs), jumps: planJumps(b.xs, RACE.crashIdx), crash: b.crash };
  for (const p of [p1, p2]) {
    p.speed = (t) => (p.x(t + 0.004) - p.x(t - 0.004)) / 0.008;
    p.jumpAt = (t) => p.jumps.find((j) => t >= j.t0 && t < j.t1) || null;
  }
  cached = { p1, p2 };
  return cached;
}
