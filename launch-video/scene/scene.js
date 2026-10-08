// Vídeo de lançamento do Olimpandas.
// Cada frame é uma função pura de t: window.__seek(t) posiciona tudo e o
// render.mjs tira um screenshot. Nada depende do frame anterior, então vários
// workers podem renderizar trechos diferentes em paralelo.

import { W, H, DURATION, SCENES, CUE } from './timeline.js';
import {
  clamp, lerp, seg, E, spring, rng, TAU, loadImage, makeCanvas, recolor, blueToRed, tinted,
  splitWords, style, revealWords, exitFx, fmtNum,
} from './lib.js';
import { RACE, race } from './race.js';

const $ = (s) => document.querySelector(s);
const $$ = (s) => [...document.querySelectorAll(s)];
const ART = '/Assets/Art';
const A = {};
const C = {
  ink: '#141413', ink2: '#3D3D3A', muted: '#87867F', paper: '#FAF9F5', clay: '#D97757', clayDeep: '#C6613F',
  p1: '#1F5AD6', p2: '#D93A30', gold: '#E9B949', yellow: '#FFD25E', ice: '#BFE6FF',
};

// Spritesheets do Panda-gigante (256x256 por quadro). "feet" = linha do chão dentro do quadro.
const SHEET = {
  idle: { file: 'Idle/Panda_Idle_6x256_v2.png', n: 6, feet: 253 },
  run: { file: 'Run/Panda_Run_8x256.png', n: 8, feet: 243 },
  jump: { file: 'Jump/Panda_Jump_4x256_v2.png', n: 4, feet: 249 },
  fall: { file: 'Fall/Panda_Fall_2x256_v2.png', n: 2, feet: 252 },
  dash: { file: 'Dash/Panda_Dash_4x256.png', n: 4, feet: 241 },
  victory: { file: 'Victory/Panda_Victory_6x256.png', n: 6, feet: 251 },
};

// Recortes da barreira (HurdleFall5.png.meta): x, largura e pivot X; pivot Y comum.
const HURDLE = [
  { x: 0, w: 426, px: 0.23943663 },
  { x: 426, w: 427, px: 0.19906323 },
  { x: 853, w: 432, px: 0.16898148 },
  { x: 1285, w: 425, px: 0.15058823 },
  { x: 1710, w: 462, px: 0.14718615 },
];
const HURDLE_PIVOT_Y = 724 * (1 - 0.2044199);

// Poderes (Olimpandas_Powers_Stun_Slow_5x2.png): linha 0 = gelo (Stun), linha 1 = corrente (Slow).
const POW = { pickup: 0, projectile: 1, impact: 2, status: 3, icon: 4 };

// ---------------------------------------------------------------- desenho

function drawPanda(ctx, set, sheet, frame, x, y, scale, { flip = false, rot = 0, alpha = 1, anchor } = {}) {
  const meta = SHEET[sheet];
  const f = ((Math.floor(frame) % meta.n) + meta.n) % meta.n;
  ctx.save();
  ctx.translate(x, y);
  if (rot) ctx.rotate(rot);
  ctx.scale(flip ? -scale : scale, scale);
  ctx.globalAlpha *= alpha;
  ctx.drawImage(set[sheet], f * 256, 0, 256, 256, -128, -(anchor ?? meta.feet), 256, 256);
  ctx.restore();
}

function drawPower(ctx, row, col, x, y, size, { rot = 0, alpha = 1, sx = 1, sy = 1 } = {}) {
  ctx.save();
  ctx.translate(x, y);
  if (rot) ctx.rotate(rot);
  ctx.scale((sx * size) / 256, (sy * size) / 256);
  ctx.globalAlpha *= alpha;
  ctx.drawImage(A.powers, col * 256, row * 256, 256, 256, -128, -128, 256, 256);
  ctx.restore();
}

function drawHurdle(ctx, frame, x, y, scale) {
  const f = HURDLE[frame];
  const pivX = f.w * f.px;
  ctx.drawImage(A.hurdle, f.x, 0, f.w, 724, x - pivX * scale, y - HURDLE_PIVOT_Y * scale, f.w * scale, 724 * scale);
}

// Estádio espelhado lado a lado (como as "continuações espelhadas" da cena no Unity).
function drawStadium(ctx, camX, s, srcY0, cw, ch) {
  const D = A.stadium2;
  const period = D.width * s;
  const srcH = ch / s;
  for (let k = Math.floor(camX / period); ; k++) {
    const tileX = k * period - camX;
    if (tileX >= cw) break;
    const v0 = Math.max(0, tileX);
    const v1 = Math.min(cw, tileX + period);
    if (v1 <= v0) continue;
    ctx.drawImage(D, (v0 - tileX) / s, srcY0, (v1 - v0) / s, srcH, v0, 0, v1 - v0, ch);
  }
}

function drawArrow(ctx, img, label, color, x, topY, t, phase = 0) {
  const w = 60;
  const h = (w * 167) / 335;
  const bob = Math.sin(t * 5.5 + phase) * 4;
  const y = topY - 18 - h + bob;
  ctx.drawImage(img, 177, 109, 335, 167, x - w / 2, y, w, h);
  ctx.font = '800 26px Inter';
  ctx.textAlign = 'center';
  ctx.textBaseline = 'alphabetic';
  ctx.lineJoin = 'round';
  ctx.lineWidth = 7;
  ctx.strokeStyle = 'rgba(255,255,255,.95)';
  ctx.strokeText(label, x, y - 8);
  ctx.fillStyle = color;
  ctx.fillText(label, x, y - 8);
}

function shadow(ctx, x, y, rx, ry, alpha) {
  ctx.save();
  ctx.fillStyle = `rgba(30,20,10,${alpha})`;
  ctx.beginPath();
  ctx.ellipse(x, y, rx, ry, 0, 0, TAU);
  ctx.fill();
  ctx.restore();
}

function starPath(ctx, x, y, r1, r2, n, rot = 0) {
  ctx.beginPath();
  for (let i = 0; i < n * 2; i++) {
    const r = i % 2 ? r2 : r1;
    const a = rot + (i * Math.PI) / n - Math.PI / 2;
    ctx.lineTo(x + Math.cos(a) * r, y + Math.sin(a) * r);
  }
  ctx.closePath();
}

// Texto "de jogo": Bangers com contorno grosso, como os pop-ups de party game.
function comicText(ctx, text, x, y, size, { fill = '#fff', stroke = C.ink, sw, rot = 0, scale = 1, alpha = 1, shadowCol = 'rgba(20,20,19,.25)' } = {}) {
  if (alpha <= 0.001 || scale <= 0.001) return;
  ctx.save();
  ctx.translate(x, y);
  ctx.rotate(rot);
  ctx.scale(scale, scale);
  ctx.globalAlpha *= alpha;
  ctx.font = `${size}px Bangers`;
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.lineJoin = 'round';
  ctx.lineWidth = sw ?? size * 0.11;
  if (shadowCol) {
    ctx.fillStyle = shadowCol;
    ctx.strokeStyle = shadowCol;
    ctx.strokeText(text, 0, size * 0.06);
  }
  ctx.strokeStyle = stroke;
  ctx.strokeText(text, 0, 0);
  ctx.fillStyle = fill;
  ctx.fillText(text, 0, 0);
  ctx.restore();
}

function burst(ctx, x, y, r, t, { color = C.yellow, n = 12, alpha = 1, width = 7 } = {}) {
  ctx.save();
  ctx.globalAlpha *= alpha;
  ctx.strokeStyle = color;
  ctx.lineWidth = width;
  ctx.lineCap = 'round';
  for (let i = 0; i < n; i++) {
    const a = (i / n) * TAU + 0.2;
    ctx.beginPath();
    ctx.moveTo(x + Math.cos(a) * r * 0.62, y + Math.sin(a) * r * 0.62);
    ctx.lineTo(x + Math.cos(a) * r, y + Math.sin(a) * r);
    ctx.stroke();
  }
  ctx.restore();
}

function dizzyStars(ctx, x, y, t, alpha, rx = 52) {
  if (alpha <= 0) return;
  for (let i = 0; i < 3; i++) {
    const a = t * 7 + (i * TAU) / 3;
    ctx.save();
    ctx.globalAlpha *= alpha;
    ctx.fillStyle = C.yellow;
    ctx.strokeStyle = C.ink;
    ctx.lineWidth = 3;
    starPath(ctx, x + Math.cos(a) * rx, y + Math.sin(a) * rx * 0.3, 14, 6, 5, a);
    ctx.fill();
    ctx.stroke();
    ctx.restore();
  }
}

// Poeira determinística: n bolinhas que sobem e somem.
function dust(ctx, x, y, u, seed, n = 6, spread = 1) {
  if (u < 0 || u > 0.55) return;
  const r = rng(seed);
  for (let i = 0; i < n; i++) {
    const dir = (r() - 0.5) * 2;
    const sp = 60 + r() * 90;
    const k = u / 0.55;
    const px = x + dir * sp * u * 2.2 * spread;
    const py = y - (20 + r() * 30) * E.outCubic(k);
    const rad = (8 + r() * 10) * (0.6 + k);
    ctx.fillStyle = `rgba(236,220,196,${0.55 * (1 - k)})`;
    ctx.beginPath();
    ctx.arc(px, py, rad, 0, TAU);
    ctx.fill();
  }
}

function markSVG(id) {
  return `
  <defs><linearGradient id="${id}g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#F4D06E"/><stop offset="1" stop-color="#DBA437"/></linearGradient></defs>
  <path d="M30 0 H54 L72 64 H48 Z" fill="${C.p1}"/>
  <path d="M90 0 H66 L48 64 H72 Z" fill="${C.p2}"/>
  <circle cx="60" cy="96" r="47" fill="url(#${id}g)" stroke="#B98424" stroke-width="4"/>
  <circle cx="60" cy="96" r="38" fill="none" stroke="#FBE6A6" stroke-width="2" opacity=".85"/>
  <g transform="translate(60 99)">
    <circle cx="-19" cy="-19" r="9.5" fill="${C.ink}"/>
    <circle cx="19" cy="-19" r="9.5" fill="${C.ink}"/>
    <circle r="26" fill="#FFFFFF" stroke="${C.ink}" stroke-width="2.6"/>
    <ellipse cx="-10.5" cy="-2" rx="6.6" ry="8.8" transform="rotate(28 -10.5 -2)" fill="${C.ink}"/>
    <ellipse cx="10.5" cy="-2" rx="6.6" ry="8.8" transform="rotate(-28 10.5 -2)" fill="${C.ink}"/>
    <g class="eyes"><circle cx="-9.5" cy="-3.2" r="2.3" fill="#fff"/><circle cx="9.5" cy="-3.2" r="2.3" fill="#fff"/></g>
    <ellipse cx="0" cy="8" rx="4.2" ry="2.9" fill="${C.ink}"/>
    <path d="M-4.6 12.2 q2.3 3.3 4.6 0 q2.3 3.3 4.6 0" fill="none" stroke="${C.ink}" stroke-width="1.9" stroke-linecap="round"/>
  </g>`;
}

function splitLetters(el) {
  const text = el.textContent;
  el.textContent = '';
  return [...text].map((ch) => {
    const s = document.createElement('span');
    s.className = 'w';
    s.textContent = ch;
    el.appendChild(s);
    return s;
  });
}

function mixHex(a, b, k) {
  const pa = [1, 3, 5].map((i) => parseInt(a.slice(i, i + 2), 16));
  const pb = [1, 3, 5].map((i) => parseInt(b.slice(i, i + 2), 16));
  return `rgb(${pa.map((v, i) => Math.round(lerp(v, pb[i], k))).join(',')})`;
}

// ---------------------------------------------------------------- cenas

const scenes = [];
function scene(id, [t0, t1], render, padAfter = 0) {
  scenes.push({ el: $(id), t0, t1, render, padAfter });
}

// 1. Abertura — o clima calmo de anúncio, até um panda aparecer.
function setupIntro() {
  const S = {
    l1: splitWords($('#l1')),
    l2: splitWords($('#l2')),
    l3: splitWords($('#l3t')),
    dots: $$('#dots .d'),
    cv: addCanvas('#s-intro'),
  };
  const lines = [$('#l1'), $('#l2'), $('#l3')];
  scene('#s-intro', SCENES.intro, (t) => {
    const opt = { stagger: 0.1, dur: 0.85, y: 22, blur: 14 };
    revealWords(S.l1, t, CUE.l1, opt);
    revealWords(S.l2, t, CUE.l2, opt);
    revealWords(S.l3, t, CUE.l3, opt);
    S.dots.forEach((d, i) => {
      const k = E.outBack(seg(t, CUE.l3 + 0.42 + i * 0.13, 0.3));
      style(d, { o: clamp(k * 2), y: (1 - k) * -18 });
    });
    const ex = exitFx(t, CUE.peekDown + 0.02, 0.42, -26, 16);
    lines.forEach((el, i) => {
      const next = [CUE.l2, CUE.l3, 99][i];
      const dim = E.inOutCubic(seg(t, next, 0.7));
      style(el, { o: (1 - 0.7 * dim) * ex.o, y: ex.y, blur: ex.blur });
    });

    // O panda que espia por baixo da tela.
    const ctx = S.cv.getContext('2d');
    ctx.clearRect(0, 0, W, H);
    const up = spring(t - CUE.peekUp, 2.3, 0.42);
    const down = E.inBack(seg(t, CUE.peekDown, 0.26), 2.2);
    const k = up * (1 - down);
    if (k > 0.001 || (t > CUE.peekUp && t < CUE.peekDown + 0.3)) {
      const scale = 1.75;
      const headTop = lerp(1110, 782, k);
      const feet = headTop + (251 - 12) * scale;
      const excited = t > CUE.peekUp + 0.42;
      const wob = Math.sin((t - CUE.peekUp) * 8) * 0.035 * (1 - down);
      drawPanda(ctx, A.blue, 'victory', excited ? 1 : 0, 1290, feet, scale, { rot: wob });
      const ek = spring(t - (CUE.peekUp + 0.16), 3, 0.4) * (1 - down);
      comicText(ctx, '!', 1440, headTop - 40, 130, { fill: C.clay, scale: ek, rot: 0.18, alpha: clamp(ek * 3) });
    }
  });
}

// 2. Título — o panda passa de Dash e "escreve" o nome.
function setupTitle() {
  $('#t-mark').setAttribute('viewBox', '0 0 120 150');
  $('#t-mark').innerHTML = markSVG('tm');
  const S = {
    wrap: $('#s-title .title-wrap'),
    eyebrow: $('#t-eyebrow'),
    word: $('#t-word'),
    mark: $('#t-mark'),
    sub: splitWords($('#t-sub')),
    pills: $$('#t-pills .pill'),
    cv: addCanvas('#s-title'),
  };
  const r = rng(7);
  const lines = Array.from({ length: 14 }, () => ({ dy: (r() - 0.5) * 300, len: 160 + r() * 360, off: 60 + r() * 520, w: 2 + r() * 4 }));
  scene('#s-title', SCENES.title, (t) => {
    const wr = S.word.getBoundingClientRect();
    const dk = seg(t, CUE.dash, CUE.dashEnd - CUE.dash);
    const px = lerp(-420, W + 520, dk);
    const reveal = px - 60 - wr.left;
    const hidden = Math.max(0, wr.width - reveal);
    S.word.style.clipPath = hidden >= wr.width ? 'inset(0 100% 0 0)' : `inset(-40px ${hidden.toFixed(1)}px -60px -40px)`;

    const push = lerp(1, 1.035, E.inOutCubic(seg(t, CUE.dash, 3.5)));
    const ex = exitFx(t, CUE.titleOut, 0.42, -34, 16);
    style(S.wrap, { o: ex.o, y: ex.y, blur: ex.blur, s: push });

    const ek = E.outCubic(seg(t, CUE.eyebrow, 0.6));
    style(S.eyebrow, { o: ek, y: (1 - ek) * 14 });
    const mk = spring(t - CUE.mark, 2.1, 0.36);
    style(S.mark, { s: Math.max(0, mk), r: (1 - mk) * -28, o: clamp(mk * 3) });
    const blink = seg(t, 7.6, 0.08) * (1 - seg(t, 7.68, 0.08));
    S.mark.querySelector('.eyes').setAttribute('transform', `translate(0 ${-3.2 * blink}) scale(1 ${1 - 0.9 * blink})`);
    revealWords(S.sub, t, CUE.sub, { stagger: 0.085, dur: 0.75 });
    S.pills.forEach((p, i) => {
      const k = spring(t - CUE.pills[i], 2.4, 0.45);
      style(p, { o: clamp(k * 2), s: 0.75 + 0.25 * k, y: (1 - k) * 24 });
    });

    // Dash do panda com rastro azul (o "Dash Trail" do jogo).
    const ctx = S.cv.getContext('2d');
    ctx.clearRect(0, 0, W, H);
    if (dk > 0 && dk < 1) {
      const y = wr.top + wr.height * 0.52;
      ctx.save();
      for (const l of lines) {
        const x1 = px - l.off;
        const g = ctx.createLinearGradient(x1 - l.len, 0, x1, 0);
        g.addColorStop(0, 'rgba(31,90,214,0)');
        g.addColorStop(1, 'rgba(31,90,214,.5)');
        ctx.strokeStyle = g;
        ctx.lineWidth = l.w;
        ctx.lineCap = 'round';
        ctx.beginPath();
        ctx.moveTo(x1 - l.len, y + l.dy);
        ctx.lineTo(x1, y + l.dy);
        ctx.stroke();
      }
      const g = ctx.createLinearGradient(px - 760, 0, px - 40, 0);
      g.addColorStop(0, 'rgba(31,90,214,0)');
      g.addColorStop(1, 'rgba(31,90,214,.38)');
      ctx.fillStyle = g;
      ctx.beginPath();
      ctx.moveTo(px - 40, y - 46);
      ctx.quadraticCurveTo(px - 400, y - 20, px - 760, y - 4);
      ctx.lineTo(px - 760, y + 4);
      ctx.quadraticCurveTo(px - 400, y + 30, px - 40, y + 50);
      ctx.closePath();
      ctx.fill();
      ctx.restore();
      for (let i = 4; i >= 1; i--) drawPanda(ctx, A.blue, 'dash', 2, px - i * 95, y, 1.55, { anchor: 128, alpha: 0.16 / i });
      drawPanda(ctx, A.blue, 'dash', 2, px, y, 1.55, { anchor: 128 });
    }
  });
}

// 3. 100m com barreiras — countdown "3, 2, 1, VAI!" e a primeira prova.
// Proporção próxima à do jogo: o panda (Visual 1,8 un.) tem ~1,6x o espaçamento das raias pintadas.
const RC = { w: 1640, h: 760, srcY0: 150 };
RC.s = RC.h / (627 - RC.srcY0);
RC.laneTop = (406 - RC.srcY0) * RC.s;
RC.laneBottom = (545 - RC.srcY0) * RC.s;
RC.feet1 = (440 - RC.srcY0) * RC.s + 8;
RC.feet2 = (509 - RC.srcY0) * RC.s + 8;
RC.scale1 = 0.76;
RC.scale2 = 0.82;
RC.hurdle1 = 0.25;
RC.hurdle2 = 0.27;

function buildCamera() {
  const R = race();
  const dt = 1 / 600;
  const t0 = CUE.raceIn;
  const n = Math.ceil((RACE.end - t0) / dt) + 1;
  const cam = new Float64Array(n);
  const home = -0.3 * RC.w;
  let c = home;
  let v = 0;
  const w = 5.2;
  for (let i = 0; i < n; i++) {
    const t = t0 + i * dt;
    const mid = (R.p1.x(t) + R.p2.x(t)) / 2;
    const lead = ((R.p1.speed(t) + R.p2.speed(t)) / 2) * (2 / w);
    const target = Math.max(home, mid + lead - 0.47 * RC.w);
    const a = w * w * (target - c) - 2 * w * v;
    v += a * dt;
    c += v * dt;
    cam[i] = c;
  }
  return (t) => {
    const f = clamp((t - t0) / dt, 0, n - 1);
    const i = Math.floor(f);
    return i >= n - 1 ? cam[n - 1] : lerp(cam[i], cam[i + 1], f - i);
  };
}

function runnerState(p, t) {
  const x = p.x(t);
  let sheet = 'idle';
  let frame = Math.floor(t * 8);
  let y = 0;
  let rot = 0;
  if (t >= CUE.go) {
    sheet = 'run';
    frame = Math.floor(x / 56);
  }
  const j = p.jumpAt(t);
  if (j) {
    const k = (t - j.t0) / (j.t1 - j.t0);
    const a = clamp((k - 0.06) / 0.94);
    y = -RACE.jumpH * 4 * a * (1 - a);
    if (k < 0.06) [sheet, frame] = ['jump', 0];
    else if (k < 0.26) [sheet, frame] = ['jump', 1];
    else if (k < 0.46) [sheet, frame] = ['jump', 2];
    else if (k < 0.66) [sheet, frame] = ['jump', 3];
    else if (k < 0.86) [sheet, frame] = ['fall', 0];
    else [sheet, frame] = ['fall', 1];
  }
  if (p.crash && t >= p.crash.t && t < p.crash.t + RACE.tumble) {
    const k = (t - p.crash.t) / RACE.tumble;
    y = -64 * Math.sin(Math.PI * k);
    rot = 0.34 * Math.sin(Math.PI * k);
    sheet = 'fall';
    frame = k < 0.6 ? 1 : 0;
  }
  return { x, y, sheet, frame, rot };
}

function setupRace() {
  const cv = $('#race-cv');
  const ctx = cv.getContext('2d');
  const cam = buildCamera();
  const R = race();
  const S = {
    head: $('#r-head'),
    eyebrow: $('#r-head .eyebrow'),
    words: splitWords($('#r-head .headline')),
    chip: $('#r-chip'),
    card: $('#r-card'),
  };
  // Flashes de câmera na arquibancada (posições fixas no mundo).
  const r = rng(42);
  const flashes = [];
  for (let i = 0; i < 110; i++) {
    const ft = CUE.raceIn + 0.3 + r() * (RACE.end - CUE.raceIn - 0.3);
    flashes.push({ t: ft, wx: cam(ft) + r() * RC.w, y: 150 + r() * 150, s: 0.6 + r() * 0.8 });
  }

  scene('#s-race', SCENES.race, (t) => {
    // Entrada e saída do bloco.
    const ck = E.outCubic(seg(t, CUE.raceIn, 0.7));
    const ex = exitFx(t, CUE.raceOut, 0.45, -36, 14);
    style(S.card, { o: ck * ex.o, y: (1 - ck) * 70 + ex.y, s: lerp(0.95, 1, ck), blur: ex.blur });
    const ek = E.outCubic(seg(t, CUE.raceIn + 0.05, 0.6));
    style(S.eyebrow, { o: ek, y: (1 - ek) * 12 });
    revealWords(S.words, t, CUE.raceIn + 0.12, { stagger: 0.08 });
    style(S.head, { o: ex.o, y: ex.y, blur: ex.blur });
    const chk = E.outCubic(seg(t, CUE.raceIn + 0.4, 0.6));
    style(S.chip, { o: chk * ex.o, x: (1 - chk) * 20, y: ex.y });

    const cx = cam(t);
    ctx.clearRect(0, 0, RC.w, RC.h);
    drawStadium(ctx, cx, RC.s, RC.srcY0, RC.w, RC.h);

    for (const f of flashes) {
      const d = Math.abs(t - f.t);
      if (d > 0.13) continue;
      const a = 1 - d / 0.13;
      const sx = f.wx - cx;
      ctx.save();
      ctx.globalAlpha = a;
      const g = ctx.createRadialGradient(sx, f.y, 0, sx, f.y, 22 * f.s);
      g.addColorStop(0, 'rgba(255,255,255,1)');
      g.addColorStop(1, 'rgba(255,255,255,0)');
      ctx.fillStyle = g;
      ctx.fillRect(sx - 30, f.y - 30, 60, 60);
      ctx.fillStyle = '#fff';
      starPath(ctx, sx, f.y, 16 * f.s * a, 2.5, 4);
      ctx.fill();
      ctx.restore();
    }

    // Linha de largada.
    const sl = 150 - cx;
    if (sl > -40 && sl < RC.w + 40) {
      ctx.fillStyle = 'rgba(255,255,255,.92)';
      ctx.beginPath();
      ctx.moveTo(sl - 6, RC.laneTop);
      ctx.lineTo(sl + 6, RC.laneTop);
      ctx.lineTo(sl + 14, RC.laneBottom);
      ctx.lineTo(sl + 2, RC.laneBottom);
      ctx.closePath();
      ctx.fill();
    }

    const crash = R.p2.crash;
    const lane = (feetY, scale, isLane2) => {
      RACE.hurdles.forEach((hx, k) => {
        const sx = hx - cx;
        if (sx < -260 || sx > RC.w + 120) return;
        let frame = 0;
        if (isLane2 && k === RACE.crashIdx && t >= crash.t) frame = Math.min(4, Math.floor(((t - crash.t) / RACE.fallDur) * 5));
        drawHurdle(ctx, frame, sx, feetY, scale);
      });
    };
    const runner = (p, set, feetY, scale, isP2) => {
      const st = runnerState(p, t);
      const sx = st.x - cx;
      const lift = clamp(-st.y / 160);
      shadow(ctx, sx, feetY + 2, 70 * scale * (1 - 0.45 * lift), 13 * scale * (1 - 0.45 * lift), 0.28 * (1 - 0.5 * lift));
      drawPanda(ctx, set, st.sheet, st.frame, sx, feetY + st.y, scale, { rot: st.rot });
      for (const j of p.jumps) dust(ctx, p.x(j.t1) - cx, feetY, t - j.t1, Math.floor(j.t1 * 1000), 5);
      dust(ctx, p.x(CUE.go) - 40 - cx, feetY, t - CUE.go, isP2 ? 11 : 22, 7, 1.4);
      return { sx, top: feetY + st.y - 236 * scale };
    };

    lane(RC.feet1, RC.hurdle1, false);
    const a1 = runner(R.p1, A.blue, RC.feet1, RC.scale1, false);
    lane(RC.feet2, RC.hurdle2, true);
    const a2 = runner(R.p2, A.red, RC.feet2, RC.scale2, true);

    // A trombada do P2 na barreira.
    if (crash) {
      const u = t - crash.t;
      const px = R.p2.x(t) - cx;
      if (u > 0 && u < 0.45) {
        const r2 = rng(5);
        for (let i = 0; i < 7; i++) {
          const a = -Math.PI * (0.15 + r2() * 0.7);
          const d = 40 + 160 * E.outCubic(u / 0.45);
          ctx.save();
          ctx.globalAlpha = 1 - u / 0.45;
          ctx.fillStyle = C.yellow;
          ctx.strokeStyle = C.ink;
          ctx.lineWidth = 2.5;
          starPath(ctx, px + 70 + Math.cos(a) * d, RC.feet2 - 110 + Math.sin(a) * d * 0.8, 13, 5, 5, u * 6);
          ctx.fill();
          ctx.stroke();
          ctx.restore();
        }
      }
      const bk = spring(u - 0.03, 2.6, 0.4);
      const ba = 1 - seg(u, 0.8, 0.2);
      if (u > 0 && ba > 0) {
        const bx = px + 175;
        const by = RC.feet2 - 205;
        ctx.save();
        ctx.translate(bx, by);
        ctx.scale(bk, bk);
        ctx.globalAlpha = ba;
        ctx.fillStyle = C.yellow;
        ctx.strokeStyle = C.ink;
        ctx.lineWidth = 6;
        ctx.lineJoin = 'round';
        starPath(ctx, 0, 0, 104, 68, 11, 0.1);
        ctx.fill();
        ctx.stroke();
        ctx.restore();
        comicText(ctx, 'AI!', bx, by + 2, 92, { scale: bk, alpha: ba, fill: C.p2, rot: -0.12, shadowCol: null });
      }
      dizzyStars(ctx, px, a2.top - 4, t, (1 - seg(u, 1.1, 0.3)) * seg(u, 0.25, 0.15), 46);
    }

    drawArrow(ctx, A.arrowBlue, 'P1', C.p1, a1.sx, a1.top, t, 0);
    drawArrow(ctx, A.arrowRed, 'P2', C.p2, a2.sx, a2.top, t, 1.3);

    // HUD: cronômetro.
    const el = Math.max(0, t - CUE.go);
    const txt = `0:${el.toFixed(2).padStart(5, '0')}`.replace('.', ',');
    ctx.save();
    ctx.fillStyle = 'rgba(20,20,19,.78)';
    ctx.beginPath();
    ctx.roundRect(RC.w - 36 - 236, 30, 236, 62, 31);
    ctx.fill();
    ctx.font = '600 32px "JetBrains Mono"';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillStyle = C.yellow;
    ctx.fillText(txt, RC.w - 36 - 118, 62);
    ctx.restore();

    // Countdown com escurecimento leve do fundo.
    const dim = seg(t, CUE.raceIn + 0.3, 0.3) * (1 - seg(t, CUE.go, 0.25));
    if (dim > 0) {
      ctx.fillStyle = `rgba(20,20,19,${0.22 * dim})`;
      ctx.fillRect(0, 0, RC.w, RC.h);
    }
    const steps = [['3', CUE.count[0]], ['2', CUE.count[1]], ['1', CUE.count[2]], ['VAI!', CUE.go]];
    for (const [s, ct] of steps) {
      const u = t - ct;
      const go = s === 'VAI!';
      const life = go ? 0.75 : 0.48;
      if (u < 0 || u > life) continue;
      const kin = spring(u, 3.1, 0.4);
      const kout = E.inCubic(seg(u, life - 0.16, 0.16));
      const sc = (0.25 + 0.75 * kin) * (1 + 0.3 * kout);
      if (go) burst(ctx, RC.w / 2, 330, 150 + 260 * E.outCubic(seg(u, 0, 0.5)), t, { alpha: 1 - seg(u, 0.25, 0.3), color: C.yellow, n: 16, width: 9 });
      comicText(ctx, s, RC.w / 2, 330, go ? 290 : 250, {
        fill: go ? C.yellow : '#FFFFFF', sw: go ? 30 : 26, scale: sc, alpha: 1 - kout, rot: go ? -0.07 : 0,
      });
    }
  });
}

// 4. Poderes — Stun (gelo) e Slow (corrente).
const PS = { w: 800, h: 600, srcY0: 96 };
PS.s = PS.h / (627 - PS.srcY0);
PS.feet = (509 - PS.srcY0) * PS.s + 6;

function setupPowers() {
  const S = {
    head: $('#p-head'),
    eyebrow: $('#p-head .eyebrow'),
    words: splitWords($('#p-head .headline')),
    punch: splitWords($('#p-punch')),
    punchEl: $('#p-punch'),
    cards: [$('#pc-stun'), $('#pc-slow')],
    caps: $$('#s-powers .pcap'),
  };
  $$('.picon').forEach((el) => (el.style.backgroundImage = `url('${ART}/Effects/Powers/Olimpandas_Powers_Stun_Slow_5x2.png')`));
  const ctxs = S.cards.map((c) => c.querySelector('canvas').getContext('2d'));

  const card = (ctx, t, isStun) => {
    const hit = isStun ? CUE.iceHit : CUE.chainHit;
    const thr = isStun ? CUE.iceThrow : CUE.chainThrow;
    const row = isStun ? 0 : 1;
    const v = 720;
    const t0 = CUE.powersIn;
    let d;
    if (t < hit) d = v * (t - t0);
    else {
      const u = t - hit;
      d = v * (hit - t0) + v * (isStun ? 0.06 * (1 - Math.exp(-u / 0.06)) : 0.22 * u + 0.78 * 0.16 * (1 - Math.exp(-u / 0.16)));
    }
    ctx.clearRect(0, 0, PS.w, PS.h);
    drawStadium(ctx, d + (isStun ? 0 : 1900), PS.s, PS.srcY0, PS.w, PS.h);

    const px = 470;
    const py = PS.feet;
    const after = t >= hit;
    const u = t - hit;
    const set = isStun ? A.blue : A.red;
    const frame = Math.floor(d / 50);

    // Linhas de velocidade enquanto corre.
    const speedK = after ? (isStun ? 0 : 0.22) : 1;
    if (speedK > 0) {
      const r = rng(isStun ? 3 : 4);
      ctx.save();
      ctx.strokeStyle = `rgba(255,255,255,${0.55 * speedK})`;
      ctx.lineCap = 'round';
      for (let i = 0; i < 7; i++) {
        const ly = 120 + r() * 380;
        const len = 80 + r() * 140;
        const lx = ((r() * 1200 - d * 1.6) % 1000 + 1000) % 1000 - 100;
        ctx.lineWidth = 3 + r() * 3;
        ctx.beginPath();
        ctx.moveTo(lx, ly);
        ctx.lineTo(lx + len, ly);
        ctx.stroke();
      }
      ctx.restore();
    }

    shadow(ctx, px, py + 2, 70, 13, 0.28);
    if (isStun) {
      const jit = after ? Math.sin(t * 95) * 2.2 * (0.4 + 0.6 * (1 - seg(u, 0, 2.5))) : 0;
      drawPanda(ctx, set, 'run', frame, px + jit, py, 1);
      if (after) drawPanda(ctx, A.iceRun, 'run', frame, px + jit, py, 1, { alpha: E.outCubic(seg(u, 0, 0.2)) });
    } else {
      if (after) {
        const ga = E.outCubic(seg(u, 0.05, 0.3));
        drawPanda(ctx, set, 'run', frame - 1, px - 46, py, 1, { alpha: 0.16 * ga });
        drawPanda(ctx, set, 'run', frame - 2, px - 92, py, 1, { alpha: 0.08 * ga });
      }
      drawPanda(ctx, set, 'run', frame, px, py, 1);
    }

    // Projétil vindo de trás.
    const pk = seg(t, thr, hit - thr);
    if (t >= thr && t < hit) {
      const x = lerp(-160, px - 70, E.inQuad(pk));
      const y = py - 136;
      for (let i = 3; i >= 1; i--) drawPower(ctx, row, POW.projectile, x - i * 46, y, 220, { alpha: 0.22 / i });
      drawPower(ctx, row, POW.projectile, x, y, 220);
    }
    if (after) {
      // Impacto + flash.
      const ik = seg(u, 0, 0.42);
      if (ik < 1) drawPower(ctx, row, POW.impact, px - 30, py - 136, lerp(140, 380, E.outCubic(ik)), { alpha: 1 - E.inQuad(ik), rot: ik * 0.6 });
      const fl = 1 - seg(u, 0, 0.2);
      if (fl > 0) {
        ctx.fillStyle = `rgba(255,255,255,${0.6 * fl})`;
        ctx.fillRect(0, 0, PS.w, PS.h);
      }
      // Moldura de status (gelo ou corrente) ao redor do panda.
      const sk = E.outBack(seg(u, 0.04, 0.34), 2.2);
      const pulse = 1 + Math.sin(t * 5) * 0.015;
      drawPower(ctx, row, POW.status, px, py - 124, 400 * sk * pulse, { alpha: clamp(sk * 2), rot: Math.sin(t * 2.2) * 0.03 });
      // Estilhaços.
      const r = rng(isStun ? 91 : 92);
      for (let i = 0; i < 12; i++) {
        const a = r() * TAU;
        const sp = 260 + r() * 420;
        const life = 0.7;
        if (u > life) break;
        const k = u / life;
        const x = px - 30 + Math.cos(a) * sp * u;
        const y = py - 136 + Math.sin(a) * sp * u + 700 * u * u;
        ctx.save();
        ctx.globalAlpha = 1 - k;
        ctx.translate(x, y);
        ctx.rotate(a + u * 8);
        ctx.fillStyle = isStun ? (i % 2 ? '#E8F7FF' : '#7CC4FF') : (i % 2 ? '#4A4F5C' : '#9AA0AE');
        ctx.fillRect(-7, -3, 14, 6);
        ctx.restore();
      }
      if (isStun) {
        // Flocos de neve e um "brrr".
        const rs = rng(17);
        for (let i = 0; i < 16; i++) {
          const x0 = px - 220 + rs() * 440;
          const sp = 30 + rs() * 50;
          const yy = ((rs() * 520 + (u * sp)) % 520) + 20;
          const xx = x0 + Math.sin(u * 2 + i) * 16;
          ctx.save();
          ctx.globalAlpha = 0.85 * seg(u, 0.1, 0.4);
          ctx.fillStyle = '#FFFFFF';
          ctx.beginPath();
          ctx.arc(xx, yy, 3 + (i % 3) * 1.5, 0, TAU);
          ctx.fill();
          ctx.restore();
        }
        const bk = spring(u - 0.38, 2.4, 0.45);
        comicText(ctx, 'BRRR', px + 196 + Math.sin(t * 70) * 3, py - 286, 58, { fill: C.ice, scale: bk, alpha: clamp(bk * 2), rot: -0.1 });
      } else {
        const sk2 = spring(u - 0.32, 2, 0.45);
        const bob = Math.sin(t * 2.6) * 8;
        ctx.save();
        ctx.globalAlpha = clamp(sk2 * 2);
        ctx.font = '78px "Noto Color Emoji"';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.translate(px + 190, py - 300 + bob);
        ctx.scale(-sk2, sk2);
        ctx.fillText('🐌', 0, 0);
        ctx.restore();
        comicText(ctx, 'lennnto…', px + 196, py - 220, 44, { fill: '#E7E3FF', scale: sk2, alpha: clamp(sk2 * 2) * (0.6 + 0.4 * Math.sin(t * 3)), rot: -0.06 });
      }
    }
  };

  scene('#s-powers', SCENES.powers, (t) => {
    const ex = exitFx(t, CUE.powersOut, 0.45, -36, 14);
    const ek = E.outCubic(seg(t, CUE.powersIn + 0.05, 0.6));
    style(S.eyebrow, { o: ek, y: (1 - ek) * 12 });
    revealWords(S.words, t, CUE.powersIn + 0.12, { stagger: 0.09 });
    style(S.head, { o: ex.o, y: ex.y, blur: ex.blur });
    revealWords(S.punch, t, CUE.punch, { stagger: 0.09 });
    style(S.punchEl, { o: ex.o, y: ex.y, blur: ex.blur });
    S.cards.forEach((c, i) => {
      const k = E.outCubic(seg(t, CUE.powersIn + 0.15 + i * 0.15, 0.7));
      style(c, { o: k * ex.o, y: (1 - k) * 80 + ex.y, s: lerp(0.95, 1, k), blur: ex.blur });
      const ck = E.outCubic(seg(t, CUE.powersIn + 0.5 + i * 0.15, 0.6));
      style(S.caps[i], { o: ck, y: (1 - ck) * 16 });
      card(ctxs[i], t, i === 0);
    });
  });
}

// 5. Benchmarks — porque todo lançamento sério tem um gráfico de barras.
const BENCH = [
  { title: 'Fofura', unit: '% · maior é melhor', max: 100, vals: [99.9, 81.2, 12.4], fmt: (v) => fmtNum(v, 1) },
  { title: 'Barreiras puladas', unit: 'de 7 · maior é melhor', max: 7, vals: [7, 5, 1], fmt: (v) => `${Math.round(v)}/7` },
  { title: 'Bambu durante a prova', unit: 'kg · menor é melhor', max: 23, vals: [38, 2.1, 0.4], fmt: (v) => `${fmtNum(v, 1)} kg`, over: true },
];
const BAR_COLORS = [C.clay, '#B7B2A6', '#DCD7CB'];
const BASE_H = 400;

function setupBench() {
  const host = $('#b-groups');
  const X = [140, 710, 1280];
  const groups = BENCH.map((g, gi) => {
    const el = document.createElement('div');
    el.className = 'bgroup';
    el.style.left = `${X[gi]}px`;
    el.innerHTML = `<div class="baxis"></div><div class="btitle">${g.title}</div><div class="bunit">${g.unit}</div>`;
    const bars = g.vals.map((v, bi) => {
      const b = document.createElement('div');
      b.className = 'bbar';
      const full = g.over && bi === 0 ? (v / g.max) * BASE_H : Math.min(1, v / g.max) * BASE_H;
      b.style.height = `${full}px`;
      b.style.left = `${46 + bi * 144}px`;
      b.style.background = BAR_COLORS[bi];
      const val = document.createElement('div');
      val.className = `bval${bi === 0 ? ' hi' : ''}`;
      val.style.left = `${46 + bi * 144 - 20}px`;
      el.append(b, val);
      return { b, val, v, full };
    });
    host.appendChild(el);
    return { el, bars, g, axis: el.querySelector('.baxis'), title: el.querySelector('.btitle'), unit: el.querySelector('.bunit') };
  });
  const S = {
    head: $('#b-head'),
    eyebrow: $('#b-head .eyebrow'),
    words: splitWords($('#b-head .headline')),
    legend: $('#b-legend'),
    foot: $('#b-foot'),
    ops: $('#b-ops'),
  };

  scene('#s-bench', SCENES.bench, (t) => {
    const ex = exitFx(t, CUE.benchOut, 0.45, -36, 14);
    const ek = E.outCubic(seg(t, CUE.benchIn + 0.05, 0.6));
    style(S.eyebrow, { o: ek, y: (1 - ek) * 12 });
    revealWords(S.words, t, CUE.benchIn + 0.12, { stagger: 0.09 });
    style(S.head, { o: ex.o, y: ex.y, blur: ex.blur });
    const lk = E.outCubic(seg(t, CUE.benchIn + 0.45, 0.6));
    style(S.legend, { o: lk * ex.o, y: (1 - lk) * 12 + ex.y, blur: ex.blur });

    groups.forEach((G, gi) => {
      const g0 = CUE.groups[gi];
      const ak = E.outCubic(seg(t, g0 - 0.25, 0.5));
      G.axis.style.transformOrigin = '0 50%';
      G.axis.style.transform = `scaleX(${ak})`;
      style(G.title, { o: ak, y: (1 - ak) * 14 });
      style(G.unit, { o: E.outCubic(seg(t, g0 - 0.1, 0.5)), y: 0 });
      let shake = 0;
      G.bars.forEach((B, bi) => {
        const k = E.outCubic(seg(t, g0 + bi * 0.1, 0.7));
        let h;
        let shown;
        if (G.g.over && bi === 0) {
          const capH = BASE_H;
          const tremble = seg(t, CUE.overshoot - 0.32, 0.32) * (1 - seg(t, CUE.overshoot, 0.01));
          const ok = spring(t - CUE.overshoot, 1.5, 0.34);
          h = lerp(capH * k, B.full, ok) + Math.sin(t * 90) * 3 * tremble;
          shown = lerp(G.g.max * k, B.v, E.outCubic(seg(t, CUE.overshoot, 0.55)));
          shake = Math.sin(t * 55) * 9 * seg(t, CUE.overshoot, 0.05) * (1 - seg(t, CUE.overshoot + 0.05, 0.6));
        } else {
          h = B.full * k;
          shown = B.v * k;
        }
        B.b.style.transform = `scaleY(${(h / B.full).toFixed(4)})`;
        B.b.style.opacity = k > 0 ? '1' : '0';
        B.val.textContent = G.g.fmt(shown);
        B.val.style.top = `${800 - h - 46}px`;
        B.val.style.opacity = clamp(k * 3).toFixed(3);
      });
      style(G.el, { o: ex.o, y: ex.y, x: shake, blur: ex.blur });
    });

    const ok = spring(t - CUE.ops, 2.4, 0.4);
    style(S.ops, { o: clamp(ok * 2) * ex.o, s: Math.max(0, ok), r: -9 + (1 - ok) * 20, y: ex.y });
    const fk = E.outCubic(seg(t, CUE.foot, 0.6));
    style(S.foot, { o: fk * ex.o, y: (1 - fk) * 10 + ex.y });
  });
}

// 6. Roadmap — os próximos minigames do backlog.
const GAMES = [
  ['🏃', '100m com barreiras', 'Em desenvolvimento', true],
  ['🦘', 'Salto a distância', 'Em breve'],
  ['🎯', 'Lançamento de dardo', 'Em breve'],
  ['🏋️', 'Levantamento de peso', 'Em breve'],
  ['🤼', 'Sumô', 'Em breve'],
  ['🛶', 'Canoagem', 'Em breve'],
  ['🏊', 'Natação', 'Em breve'],
  ['🎾', 'Tênis', 'Em breve'],
  ['🎣', 'Pescaria', 'Em breve'],
];

function setupRoadmap() {
  const grid = $('#m-grid');
  const chips = GAMES.map(([emo, name, tag, on], i) => {
    const el = document.createElement('div');
    el.className = `gchip${on ? ' on' : ''}`;
    el.style.left = `${140 + (i % 3) * 555}px`;
    el.style.top = `${352 + Math.floor(i / 3) * 156}px`;
    el.innerHTML = `<span class="emo">${emo}</span><div><h4>${name}</h4><small>${tag}</small></div>`;
    grid.appendChild(el);
    return el;
  });
  const S = {
    head: $('#m-head'),
    eyebrow: $('#m-head .eyebrow'),
    words: splitWords($('#m-head .headline')),
    bears: $('#m-bears'),
  };
  scene('#s-roadmap', SCENES.roadmap, (t) => {
    const ex = exitFx(t, CUE.roadOut, 0.45, -36, 14);
    const ek = E.outCubic(seg(t, CUE.roadIn + 0.05, 0.6));
    style(S.eyebrow, { o: ek, y: (1 - ek) * 12 });
    revealWords(S.words, t, CUE.roadIn + 0.1, { stagger: 0.09 });
    style(S.head, { o: ex.o, y: ex.y, blur: ex.blur });
    chips.forEach((el, i) => {
      const k = spring(t - CUE.chips[i], 2.5, 0.5);
      const wiggle = i === 0 ? Math.sin(t * 6) * 0.6 * seg(t, CUE.chips[8], 0.4) : 0;
      style(el, { o: clamp(k * 2.2) * ex.o, s: 0.6 + 0.4 * k, y: (1 - k) * 40 + ex.y, r: (1 - k) * (i % 2 ? 6 : -6) + wiggle, blur: ex.blur });
    });
    const bk = E.outCubic(seg(t, CUE.bears, 0.6));
    style(S.bears, { o: bk * ex.o, y: (1 - bk) * 14 + ex.y, blur: ex.blur });
  });
}

// 7. Um teclado, dois pandas — os controles reais do jogo.
const KEYS = [
  // [rótulo, x, y, jogador, tempos de pressionar, largo?, apagado?]
  ['W', 380, 520, 1, [CUE.kW]],
  ['A', 266, 634, 1, []],
  ['S', 380, 634, 1, [], false, true],
  ['D', 494, 634, 1, [CUE.kShiftL]],
  ['Shift', 323, 748, 1, [CUE.kShiftL], true],
  ['↑', 1540, 520, 2, [CUE.kUp]],
  ['←', 1426, 634, 2, [CUE.kShiftR]],
  ['↓', 1540, 634, 2, [], false, true],
  ['→', 1654, 634, 2, []],
  ['Shift', 1597, 748, 2, [CUE.kShiftR], true],
];

function setupKeys() {
  const host = $('#k-keys');
  const keys = KEYS.map(([label, x, y, pl, presses, wide, dim], i) => {
    const el = document.createElement('div');
    el.className = `key${wide ? ' wide' : ''}${dim ? ' dim' : ''}`;
    el.textContent = label;
    const w = wide ? 222 : 104;
    el.style.left = `${x - w / 2}px`;
    el.style.top = `${y - 52}px`;
    host.appendChild(el);
    return { el, pl, presses, i, dim };
  });
  const labels = [
    [`<img src="${ART}/Effects/Setas/SetaAzul.png" style="clip-path: inset(29% 22% 27% 27%); width:130px; margin:-34px -30px -36px -36px">`, 'P1', C.p1, 380],
    [`<img src="${ART}/Effects/Setas/SetaVermelha.png" style="clip-path: inset(29% 22% 27% 27%); width:130px; margin:-34px -30px -36px -36px">`, 'P2', C.p2, 1540],
  ].map(([img, txt, col, x]) => {
    const el = document.createElement('div');
    el.className = 'plabel';
    el.style.left = `${x - 60}px`;
    el.style.width = '120px';
    el.style.top = '352px';
    el.style.color = col;
    el.innerHTML = `<span>${txt}</span>${img}`;
    host.appendChild(el);
    return el;
  });
  const S = { title: splitWords($('#k-title')), sub: $('#k-sub'), cv: addCanvas('#s-keys') };

  const pandaState = (pl, t) => {
    const P1 = pl === 1;
    const base = P1 ? 800 : 1120;
    const dir = P1 ? 1 : -1;
    const jumpT = P1 ? CUE.kW : CUE.kUp;
    const dashT = P1 ? CUE.kShiftL : CUE.kShiftR;
    const dashDur = CUE.bonk - dashT;
    let x = base;
    let y = 0;
    let rot = 0;
    let sheet = 'idle';
    let frame = Math.floor(t * 8) + (P1 ? 0 : 3);
    const jk = seg(t, jumpT, 0.5);
    if (jk > 0 && jk < 1) {
      y = -120 * 4 * jk * (1 - jk);
      [sheet, frame] = jk < 0.25 ? ['jump', 1] : jk < 0.5 ? ['jump', 2] : jk < 0.75 ? ['jump', 3] : ['fall', 1];
    }
    const dk = seg(t, dashT, dashDur);
    const reach = P1 ? 92 : 92;
    if (t >= dashT && t < CUE.bonk) {
      x = base + dir * reach * E.outCubic(dk);
      sheet = 'dash';
      frame = Math.min(3, Math.floor(dk * 4));
    }
    if (t >= CUE.bonk) {
      const bk = seg(t, CUE.bonk, 0.5);
      x = base + dir * reach - dir * (reach + 70) * E.outCubic(bk);
      y = -80 * Math.sin(Math.PI * bk);
      rot = -dir * 0.45 * Math.sin(Math.PI * bk);
      if (bk < 1) [sheet, frame] = ['fall', 1];
    }
    return { x, y, rot, sheet, frame, flip: !P1 };
  };

  scene('#s-keys', SCENES.keys, (t) => {
    const ex = exitFx(t, CUE.keysOut, 0.4, -30, 12);
    revealWords(S.title, t, CUE.keysIn + 0.05, { stagger: 0.08 });
    $('#k-title').style.opacity = ex.o;
    $('#k-title').style.transform = `translateY(${ex.y}px)`;
    const sk = E.outCubic(seg(t, CUE.keysIn + 0.35, 0.6));
    style(S.sub, { o: sk * ex.o, y: (1 - sk) * 12 + ex.y });
    keys.forEach((K) => {
      const k = spring(t - (CUE.keysIn + 0.2 + K.i * 0.035), 2.6, 0.5);
      let p = 0;
      for (const pt of K.presses) p = Math.max(p, seg(t, pt - 0.03, 0.05) * (1 - seg(t, pt + 0.16, 0.1)));
      const col = K.pl === 1 ? C.p1 : C.p2;
      K.el.style.background = mixHex(C.paper, col, p);
      K.el.style.color = mixHex(C.ink2, '#FFFFFF', p);
      K.el.style.borderColor = mixHex('#D6D0C1', col, p * 0.7);
      K.el.style.borderBottomWidth = `${9 - 6 * p}px`;
      style(K.el, { o: clamp(k * 2) * (K.dim ? 0.38 : 1) * ex.o, s: 0.7 + 0.3 * k, y: 6 * p + ex.y });
    });
    labels.forEach((el, i) => {
      const k = spring(t - (CUE.keysIn + 0.25 + i * 0.1), 2.4, 0.5);
      style(el, { o: clamp(k * 2) * ex.o, y: (1 - k) * 20 + ex.y });
    });

    const ctx = S.cv.getContext('2d');
    ctx.clearRect(0, 0, W, H);
    const pk = spring(t - (CUE.keysIn + 0.15), 2.2, 0.5);
    const outK = E.inCubic(seg(t, CUE.wipe + 0.2, 0.3));
    const floorY = 900 + (1 - pk) * 560 + outK * 300;
    const states = [pandaState(1, t), pandaState(2, t)];
    const u = t - CUE.bonk;
    const bonkOn = u >= 0 && u < 0.62;
    const bk = spring(u, 2.6, 0.4);
    const ba = 1 - seg(u, 0.45, 0.17);
    if (bonkOn) burst(ctx, 960, 640, 150 + 160 * E.outCubic(seg(u, 0, 0.3)), t, { alpha: ba, color: C.clay, n: 14, width: 10 });
    states.forEach((s, i) => {
      const lift = clamp(-s.y / 120);
      shadow(ctx, s.x, floorY + 4, 92 * (1 - 0.4 * lift), 16 * (1 - 0.4 * lift), 0.2 * (1 - 0.5 * lift));
      drawPanda(ctx, i === 0 ? A.blue : A.red, s.sheet, s.frame, s.x, floorY + s.y, 1.3, { flip: s.flip, rot: s.rot });
    });
    if (bonkOn) comicText(ctx, 'BONK!', 960, 640, 120, { fill: C.yellow, scale: bk, alpha: ba, rot: -0.08 });
    const da = seg(u, 0.25, 0.2);
    if (u > 0) {
      dizzyStars(ctx, states[0].x, floorY - 330, t, da, 60);
      dizzyStars(ctx, states[1].x, floorY - 330, t + 0.4, da, 60);
    }
  }, 0.6);
}

// 8. Cartão final — fundo argila, logo, panda campeão e confete.
const CONF_COLORS = ['#1F5AD6', '#B3261E', '#E9B949', '#FAF9F5', '#141413', '#2F9E63', '#F3B9C9'];
function makeConfetti(seed, n, x0, y0, side, t0) {
  const r = rng(seed);
  return Array.from({ length: n }, () => {
    const ang = ((-90 + side * (8 + r() * 42)) * Math.PI) / 180;
    const sp = 1500 + r() * 1900;
    return {
      t0: t0 + r() * 0.1, x0, y0, vx: Math.cos(ang) * sp, vy: Math.sin(ang) * sp, k: 2.3 + r() * 0.9, g: 520 + r() * 260,
      rot: r() * TAU, w: (r() - 0.5) * 16, flip: 5 + r() * 10, size: 14 + r() * 14, color: CONF_COLORS[Math.floor(r() * CONF_COLORS.length)],
      circle: r() < 0.25, sway: r() * TAU,
    };
  });
}
function drawConfetti(ctx, pieces, t) {
  for (const p of pieces) {
    const u = t - p.t0;
    if (u < 0) continue;
    const e = Math.exp(-p.k * u);
    const vt = p.g / p.k;
    const x = p.x0 + (p.vx * (1 - e)) / p.k + Math.sin(u * 3 + p.sway) * 34 * Math.min(1, u);
    const y = p.y0 + vt * u + ((p.vy - vt) * (1 - e)) / p.k;
    if (y > H + 40 || x < -60 || x > W + 60) continue;
    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(p.rot + p.w * u);
    ctx.scale(1, Math.cos(p.flip * u + p.sway));
    ctx.fillStyle = p.color;
    if (p.circle) {
      ctx.beginPath();
      ctx.arc(0, 0, p.size * 0.32, 0, TAU);
      ctx.fill();
    } else ctx.fillRect(-p.size / 2, -p.size / 4, p.size, p.size / 2);
    ctx.restore();
  }
}

function setupEnd() {
  $('#e-mark').setAttribute('viewBox', '0 0 120 150');
  $('#e-mark').innerHTML = markSVG('em');
  const S = {
    wipe: $('#e-wipe'),
    mark: $('#e-mark'),
    letters: splitLetters($('#e-word')),
    tag: splitWords($('#e-tag')),
    fine: [$('#e-fine-l'), $('#e-fine-r')],
    cv: addCanvas('#s-end'),
  };
  const confetti = [
    ...makeConfetti(1, 110, -20, H + 20, 1, CUE.party),
    ...makeConfetti(2, 110, W + 20, H + 20, -1, CUE.party),
    ...makeConfetti(3, 70, -20, H + 20, 1, CUE.final),
    ...makeConfetti(4, 70, W + 20, H + 20, -1, CUE.final),
  ];
  scene('#s-end', SCENES.end, (t) => {
    const wk = E.inOutCubic(seg(t, CUE.wipe, 0.62));
    S.wipe.style.clipPath = `circle(${(wk * 1200).toFixed(1)}px at 50% 52%)`;
    const mk = spring(t - CUE.lockup, 2.1, 0.38);
    const hit = spring(t - CUE.final, 3, 0.3) - (t >= CUE.final ? 1 : 0);
    style(S.mark, { s: Math.max(0, mk) * (1 + 0.06 * Math.max(0, -hit)), r: (1 - mk) * -30, o: clamp(mk * 3) });
    S.letters.forEach((l, i) => {
      const k = E.outCubic(seg(t, CUE.lockup + 0.1 + i * 0.035, 0.6));
      style(l, { o: k, y: (1 - k) * 40, blur: (1 - k) * 10 });
    });
    revealWords(S.tag, t, CUE.tagline, { stagger: 0.09 });
    S.fine.forEach((f, i) => {
      const k = E.outCubic(seg(t, CUE.party + 0.3 + i * 0.1, 0.6));
      style(f, { o: k, y: (1 - k) * 12 });
    });

    const ctx = S.cv.getContext('2d');
    ctx.clearRect(0, 0, W, H);
    const pk = spring(t - CUE.party, 2.2, 0.42);
    if (t >= CUE.party) {
      const feet = lerp(1420, 1016, pk);
      shadow(ctx, 960, 1018, 120 * clamp(pk), 18 * clamp(pk), 0.18);
      drawPanda(ctx, A.blue, 'victory', 1 + Math.floor((t - CUE.party) * 9) % 5, 960, feet, 1.32);
    }
    drawConfetti(ctx, confetti, t);
  });
}

// ---------------------------------------------------------------- infraestrutura

function addCanvas(sectionSel) {
  const c = makeCanvas(W, H);
  c.style.position = 'absolute';
  c.style.inset = '0';
  $(sectionSel).appendChild(c);
  return c;
}

async function loadAssets() {
  A.blue = {};
  const jobs = Object.entries(SHEET).map(async ([k, s]) => {
    A.blue[k] = await loadImage(`${ART}/Characters/PandaGigante/${s.file}`);
  });
  const more = {
    powers: `${ART}/Effects/Powers/Olimpandas_Powers_Stun_Slow_5x2.png`,
    arrowBlue: `${ART}/Effects/Setas/SetaAzul.png`,
    arrowRed: `${ART}/Effects/Setas/SetaVermelha.png`,
    hurdle: `${ART}/Maps/HurdleFall5.png`,
    stadium: `${ART}/Maps/StadiumTwoLanes.png`,
  };
  for (const [k, src] of Object.entries(more)) jobs.push(loadImage(src).then((img) => (A[k] = img)));
  await Promise.all(jobs);

  A.red = {};
  for (const k of Object.keys(SHEET)) A.red[k] = recolor(A.blue[k], blueToRed);
  A.iceRun = { run: tinted(A.blue.run, '#A8DBFF', 0.55) };
  const st = makeCanvas(A.stadium.width * 2, A.stadium.height);
  const sx = st.getContext('2d');
  sx.drawImage(A.stadium, 0, 0);
  sx.translate(A.stadium.width * 2, 0);
  sx.scale(-1, 1);
  sx.drawImage(A.stadium, 0, 0);
  A.stadium2 = st;

  await Promise.all(
    [
      '400 80px Newsreader', '500 80px Newsreader', 'italic 400 80px Newsreader',
      '400 20px Inter', '500 20px Inter', '600 20px Inter', '700 20px Inter', '800 20px Inter',
      '600 20px "JetBrains Mono"', '40px Bangers', '40px "Noto Color Emoji"',
    ].map((f) => document.fonts.load(f, 'Olimpandas 0123 ✓ 🐼')),
  );
  await document.fonts.ready;
}

async function init() {
  await loadAssets();
  setupIntro();
  setupTitle();
  setupRace();
  setupPowers();
  setupBench();
  setupRoadmap();
  setupKeys();
  setupEnd();
  // Garante que os emojis e imagens dos chips já foram decodificados.
  await Promise.all($$('img').map((img) => (img.complete ? img.decode().catch(() => {}) : new Promise((r) => (img.onload = r)))));
  window.__seek(0);
  window.__ready = true;
}

window.__seek = (t) => {
  for (const s of scenes) {
    const on = t >= s.t0 && t < s.t1 + s.padAfter;
    s.el.style.visibility = on ? 'visible' : 'hidden';
    if (on) s.render(t);
  }
};

window.__renderAudio = async () => {
  const { renderAudioWavBase64 } = await import('./audio.js');
  return renderAudioWavBase64();
};

window.__meta = { W, H, DURATION };

init().catch((err) => {
  window.__error = String(err && err.stack ? err.stack : err);
  console.error(err);
});
