// Trilha e efeitos sonoros 100% sintetizados com Web Audio (OfflineAudioContext).
// Usa os mesmos cues de timeline.js e a mesma coreografia de race.js, então cada
// "boing" cai exatamente no frame certo.

import { DURATION, CUE, GRID0, BEAT, BAR } from './timeline.js';
import { race } from './race.js';
import { rng } from './lib.js';

const SR = 48000;
const mtof = (m) => 440 * Math.pow(2, (m - 69) / 12);
const at = (bar, beat = 0) => GRID0 + bar * BAR + beat * BEAT;

// ---------------------------------------------------------------- motor

function engine(ctx) {
  const S = { ctx };
  const master = ctx.createGain();
  master.gain.setValueAtTime(0.6, 0);
  master.gain.setValueAtTime(0.6, DURATION - 0.9);
  master.gain.linearRampToValueAtTime(0, DURATION);
  const hp = ctx.createBiquadFilter();
  hp.type = 'highpass';
  hp.frequency.value = 32;
  hp.Q.value = 0.6;
  const comp = ctx.createDynamicsCompressor();
  comp.threshold.value = -14;
  comp.knee.value = 10;
  comp.ratio.value = 3.5;
  comp.attack.value = 0.004;
  comp.release.value = 0.2;
  const clip = ctx.createWaveShaper();
  const curve = new Float32Array(2048);
  for (let i = 0; i < curve.length; i++) {
    const x = (i / (curve.length - 1)) * 2 - 1;
    curve[i] = Math.tanh(1.15 * x) / Math.tanh(1.15);
  }
  clip.curve = curve;
  clip.oversample = '2x';
  master.connect(hp).connect(comp).connect(clip).connect(ctx.destination);

  const bus = (v) => {
    const g = ctx.createGain();
    g.gain.value = v;
    g.connect(master);
    return g;
  };
  S.music = bus(0.6);
  S.sfx = bus(0.85);
  S.amb = bus(0.5);

  const verb = ctx.createConvolver();
  const len = Math.floor(SR * 2.6);
  const ir = ctx.createBuffer(2, len, SR);
  for (let ch = 0; ch < 2; ch++) {
    const d = ir.getChannelData(ch);
    const r = rng(100 + ch);
    for (let i = 0; i < len; i++) d[i] = (r() * 2 - 1) * Math.pow(1 - i / len, 3.2) * (i < SR * 0.012 ? 0 : 1);
  }
  verb.buffer = ir;
  const damp = ctx.createBiquadFilter();
  damp.type = 'lowpass';
  damp.frequency.value = 5200;
  const verbOut = ctx.createGain();
  verbOut.gain.value = 0.42;
  verb.connect(damp).connect(verbOut).connect(master);
  S.verb = verb;

  const nb = ctx.createBuffer(1, SR * 4, SR);
  const nd = nb.getChannelData(0);
  const r = rng(9);
  for (let i = 0; i < nd.length; i++) nd[i] = r() * 2 - 1;
  S.noiseBuf = nb;
  return S;
}

const gainNode = (S, v = 0) => {
  const g = S.ctx.createGain();
  g.gain.value = v;
  return g;
};
const filt = (S, type, f, q = 0.7) => {
  const b = S.ctx.createBiquadFilter();
  b.type = type;
  b.frequency.value = f;
  b.Q.value = q;
  return b;
};
const osc = (S, type, f) => {
  const o = S.ctx.createOscillator();
  o.type = type;
  o.frequency.value = f;
  return o;
};
const pan = (S, p) => {
  const n = S.ctx.createStereoPanner();
  n.pan.value = p;
  return n;
};
function noise(S, t, dur) {
  const s = S.ctx.createBufferSource();
  s.buffer = S.noiseBuf;
  s.loop = true;
  s.start(t, (t * 7.31) % 3.2);
  s.stop(t + dur + 0.05);
  return s;
}
function send(S, node, amount) {
  const g = gainNode(S, amount);
  node.connect(g).connect(S.verb);
}
// Envelope percussivo: sobe em `a`, cai exponencialmente até `t + d`.
function perc(param, t, peak, a, d) {
  param.setValueAtTime(0.0001, t);
  param.linearRampToValueAtTime(peak, t + a);
  param.exponentialRampToValueAtTime(0.0001, t + a + d);
}

// ---------------------------------------------------------------- bateria

function kick(S, t, v = 1) {
  const o = osc(S, 'sine', 150);
  o.frequency.setValueAtTime(165, t);
  o.frequency.exponentialRampToValueAtTime(58, t + 0.08);
  o.frequency.exponentialRampToValueAtTime(48, t + 0.3);
  const g = gainNode(S);
  perc(g.gain, t, 0.78 * v, 0.003, 0.32);
  o.connect(g).connect(S.music);
  o.start(t);
  o.stop(t + 0.45);
  const n = noise(S, t, 0.03);
  const hp = filt(S, 'highpass', 2500);
  const ng = gainNode(S);
  perc(ng.gain, t, 0.16 * v, 0.001, 0.018);
  n.connect(hp).connect(ng).connect(S.music);
}

function snare(S, t, v = 0.5, dest = S.music) {
  const n = noise(S, t, 0.25);
  const bp = filt(S, 'bandpass', 2100, 0.6);
  const g = gainNode(S);
  perc(g.gain, t, v, 0.002, 0.16);
  n.connect(bp).connect(g).connect(dest);
  send(S, g, 0.12);
  const o = osc(S, 'triangle', 190);
  o.frequency.exponentialRampToValueAtTime(150, t + 0.08);
  const og = gainNode(S);
  perc(og.gain, t, v * 0.55, 0.002, 0.09);
  o.connect(og).connect(dest);
  o.start(t);
  o.stop(t + 0.15);
}

function clap(S, t, v = 0.5) {
  const n = noise(S, t, 0.3);
  const bp = filt(S, 'bandpass', 1250, 0.9);
  const g = gainNode(S);
  for (let i = 0; i < 3; i++) {
    const ti = t + i * 0.011;
    g.gain.setValueAtTime(0.0001, ti);
    g.gain.linearRampToValueAtTime(0.75 * v, ti + 0.001);
    g.gain.exponentialRampToValueAtTime(0.05 * v, ti + 0.0095);
  }
  g.gain.setValueAtTime(0.55 * v, t + 0.034);
  g.gain.exponentialRampToValueAtTime(0.0001, t + 0.23);
  n.connect(bp).connect(g).connect(S.music);
  send(S, g, 0.18);
}

function hat(S, t, v = 0.2, open = false, p = 0.2) {
  const n = noise(S, t, open ? 0.35 : 0.08);
  const hp = filt(S, 'highpass', 7600);
  const g = gainNode(S);
  perc(g.gain, t, v, 0.001, open ? 0.24 : 0.045);
  n.connect(hp).connect(g).connect(pan(S, p)).connect(S.music);
}

function crash(S, t, v = 0.4) {
  const n = noise(S, t, 2.2);
  const hp = filt(S, 'highpass', 4200);
  const g = gainNode(S);
  perc(g.gain, t, v, 0.002, 1.9);
  n.connect(hp).connect(g).connect(S.music);
  send(S, g, 0.25);
}

function tom(S, t, f, v = 0.5) {
  const o = osc(S, 'sine', f);
  o.frequency.setValueAtTime(f * 1.5, t);
  o.frequency.exponentialRampToValueAtTime(f, t + 0.05);
  const g = gainNode(S);
  perc(g.gain, t, v, 0.002, 0.28);
  o.connect(g).connect(S.music);
  send(S, g, 0.15);
  o.start(t);
  o.stop(t + 0.35);
}

// ---------------------------------------------------------------- instrumentos

function bass(S, t, midi, dur, v = 0.5) {
  const f = mtof(midi);
  const o1 = osc(S, 'sawtooth', f);
  const o2 = osc(S, 'sine', f);
  const lp = filt(S, 'lowpass', 1800, 4.5);
  lp.frequency.setValueAtTime(2200, t);
  lp.frequency.exponentialRampToValueAtTime(240, t + 0.17);
  const g = gainNode(S);
  g.gain.setValueAtTime(0.0001, t);
  g.gain.linearRampToValueAtTime(v, t + 0.006);
  g.gain.exponentialRampToValueAtTime(v * 0.45, t + dur * 0.8);
  g.gain.linearRampToValueAtTime(0.0001, t + dur);
  const g2 = gainNode(S, 0.7);
  o1.connect(lp).connect(g);
  o2.connect(g2).connect(g);
  g.connect(S.music);
  for (const o of [o1, o2]) {
    o.start(t);
    o.stop(t + dur + 0.02);
  }
}

function marimba(S, t, midi, v = 0.25, p = 0) {
  const f = mtof(midi);
  const out = pan(S, p);
  out.connect(S.music);
  send(S, out, 0.22);
  for (const [ratio, amp, dec] of [[1, 1, 0.55], [3.98, 0.3, 0.16], [9.1, 0.07, 0.05]]) {
    const o = osc(S, 'sine', f * ratio);
    const g = gainNode(S);
    perc(g.gain, t, v * amp, 0.002, dec);
    o.connect(g).connect(out);
    o.start(t);
    o.stop(t + dec + 0.05);
  }
}

function pluck(S, t, midi, v = 0.25, p = 0) {
  const f = mtof(midi);
  const o = osc(S, 'triangle', f);
  const o2 = osc(S, 'square', f * 2);
  const lp = filt(S, 'lowpass', 4000, 2);
  lp.frequency.setValueAtTime(5200, t);
  lp.frequency.exponentialRampToValueAtTime(700, t + 0.2);
  const g = gainNode(S);
  perc(g.gain, t, v, 0.002, 0.32);
  const g2 = gainNode(S, 0.18);
  o.connect(lp);
  o2.connect(g2).connect(lp);
  lp.connect(g).connect(pan(S, p)).connect(S.sfx);
  send(S, g, 0.25);
  for (const x of [o, o2]) {
    x.start(t);
    x.stop(t + 0.4);
  }
}

function brass(S, t, midis, dur, v = 0.16) {
  const out = gainNode(S, 1);
  out.connect(S.music);
  send(S, out, 0.28);
  for (const m of midis) {
    for (const det of [-9, 9]) {
      const o = osc(S, 'sawtooth', mtof(m));
      o.detune.value = det;
      const lp = filt(S, 'lowpass', 600, 1.2);
      lp.frequency.setValueAtTime(500, t);
      lp.frequency.exponentialRampToValueAtTime(3400, t + 0.05);
      lp.frequency.exponentialRampToValueAtTime(1500, t + 0.3);
      const g = gainNode(S);
      g.gain.setValueAtTime(0.0001, t);
      g.gain.linearRampToValueAtTime(v, t + 0.025);
      g.gain.linearRampToValueAtTime(v * 0.75, t + 0.2);
      g.gain.setValueAtTime(v * 0.75, t + dur);
      g.gain.exponentialRampToValueAtTime(0.0001, t + dur + 0.25);
      o.connect(lp).connect(g).connect(out);
      o.start(t);
      o.stop(t + dur + 0.3);
    }
  }
}

function pad(S, t, midis, dur, v = 0.05) {
  const lp = filt(S, 'lowpass', 1100, 0.5);
  const g = gainNode(S);
  g.gain.setValueAtTime(0.0001, t);
  g.gain.linearRampToValueAtTime(v, t + Math.min(0.6, dur * 0.4));
  g.gain.setValueAtTime(v, t + dur - 0.1);
  g.gain.linearRampToValueAtTime(0.0001, t + dur + 0.5);
  lp.connect(g).connect(S.music);
  send(S, g, 0.4);
  for (const m of midis)
    for (const [type, det] of [['triangle', -7], ['sawtooth', 7]]) {
      const o = osc(S, type, mtof(m));
      o.detune.value = det;
      const og = gainNode(S, type === 'sawtooth' ? 0.35 : 1);
      o.connect(og).connect(lp);
      o.start(t);
      o.stop(t + dur + 0.6);
    }
}

// Piano suave da abertura (soma de parciais com leve inarmonicidade).
function piano(S, t, midi, v = 0.3, dur = 3.4, p = 0) {
  const f = mtof(midi);
  const lp = filt(S, 'lowpass', 3600, 0.5);
  const out = pan(S, p);
  lp.connect(out).connect(S.music);
  send(S, out, 0.55);
  const parts = [[1, 1, 1], [2, 0.4, 0.55], [3, 0.2, 0.4], [4, 0.1, 0.3], [5, 0.06, 0.22], [6, 0.035, 0.16]];
  for (const [n, amp, dk] of parts) {
    const o = osc(S, 'sine', f * n * (1 + 0.00035 * n * n));
    const g = gainNode(S);
    perc(g.gain, t, v * amp, 0.004, dur * dk);
    o.connect(g).connect(lp);
    o.start(t);
    o.stop(t + dur * dk + 0.05);
  }
}

// ---------------------------------------------------------------- efeitos

function whoosh(S, t, dur, v = 0.4, f0 = 300, f1 = 3500, p0 = -0.7, p1 = 0.7) {
  const n = noise(S, t, dur);
  const bp = filt(S, 'bandpass', f0, 1.3);
  bp.frequency.setValueAtTime(f0, t);
  bp.frequency.exponentialRampToValueAtTime(f1, t + dur);
  const g = gainNode(S);
  g.gain.setValueAtTime(0.0001, t);
  g.gain.exponentialRampToValueAtTime(v, t + dur * 0.62);
  g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  const pn = pan(S, p0);
  pn.pan.setValueAtTime(p0, t);
  pn.pan.linearRampToValueAtTime(p1, t + dur);
  n.connect(bp).connect(g).connect(pn).connect(S.sfx);
  send(S, g, 0.15);
}

function riser(S, t, dur, v = 0.25) {
  const n = noise(S, t, dur);
  const hp = filt(S, 'highpass', 300, 2);
  hp.frequency.setValueAtTime(300, t);
  hp.frequency.exponentialRampToValueAtTime(7000, t + dur);
  const g = gainNode(S);
  g.gain.setValueAtTime(0.0001, t);
  g.gain.exponentialRampToValueAtTime(v, t + dur);
  g.gain.linearRampToValueAtTime(0.0001, t + dur + 0.03);
  n.connect(hp).connect(g).connect(S.sfx);
  const o = osc(S, 'sawtooth', 110);
  o.frequency.setValueAtTime(110, t);
  o.frequency.exponentialRampToValueAtTime(880, t + dur);
  const lp = filt(S, 'lowpass', 1800);
  const og = gainNode(S);
  og.gain.setValueAtTime(0.0001, t);
  og.gain.exponentialRampToValueAtTime(v * 0.25, t + dur);
  og.gain.linearRampToValueAtTime(0.0001, t + dur + 0.03);
  o.connect(lp).connect(og).connect(S.sfx);
  o.start(t);
  o.stop(t + dur + 0.05);
}

function boing(S, t, v = 0.3, f0 = 170, f1 = 330, dur = 0.5, p = 0) {
  const o = osc(S, 'triangle', f0);
  o.frequency.setValueAtTime(f0, t);
  o.frequency.exponentialRampToValueAtTime(f1, t + dur);
  const lfo = osc(S, 'sine', 15);
  const lg = gainNode(S);
  lg.gain.setValueAtTime(f0 * 0.32, t);
  lg.gain.exponentialRampToValueAtTime(1, t + dur);
  lfo.connect(lg).connect(o.frequency);
  const g = gainNode(S);
  perc(g.gain, t, v, 0.005, dur);
  o.connect(g).connect(pan(S, p)).connect(S.sfx);
  for (const x of [o, lfo]) {
    x.start(t);
    x.stop(t + dur + 0.05);
  }
}

function pop(S, t, v = 0.3, f0 = 900, f1 = 260) {
  const o = osc(S, 'sine', f0);
  o.frequency.setValueAtTime(f0, t);
  o.frequency.exponentialRampToValueAtTime(f1, t + 0.07);
  const g = gainNode(S);
  perc(g.gain, t, v, 0.002, 0.09);
  o.connect(g).connect(S.sfx);
  o.start(t);
  o.stop(t + 0.12);
}

function slideWhistle(S, t, dur, f0, f1, v = 0.2) {
  const o = osc(S, 'sine', f0);
  o.frequency.setValueAtTime(f0, t);
  o.frequency.exponentialRampToValueAtTime(f1, t + dur);
  const lfo = osc(S, 'sine', 6);
  const lg = gainNode(S, f0 * 0.025);
  lfo.connect(lg).connect(o.frequency);
  const g = gainNode(S);
  g.gain.setValueAtTime(0.0001, t);
  g.gain.linearRampToValueAtTime(v, t + 0.04);
  g.gain.setValueAtTime(v, t + dur - 0.05);
  g.gain.linearRampToValueAtTime(0.0001, t + dur + 0.06);
  o.connect(g).connect(S.sfx);
  send(S, g, 0.2);
  for (const x of [o, lfo]) {
    x.start(t);
    x.stop(t + dur + 0.1);
  }
}

function clank(S, t, v = 0.4) {
  const out = gainNode(S, 1);
  out.connect(pan(S, 0.2)).connect(S.sfx);
  send(S, out, 0.2);
  for (const [f, a, d] of [[430, 0.5, 0.5], [1150, 0.35, 0.35], [1770, 0.3, 0.3], [2480, 0.22, 0.22], [3350, 0.15, 0.15]]) {
    const o = osc(S, 'sine', f);
    const g = gainNode(S);
    perc(g.gain, t, v * a, 0.001, d);
    o.connect(g).connect(out);
    o.start(t);
    o.stop(t + d + 0.05);
  }
  const n = noise(S, t, 0.06);
  const bp = filt(S, 'bandpass', 2600, 0.8);
  const ng = gainNode(S);
  perc(ng.gain, t, v * 0.8, 0.001, 0.05);
  n.connect(bp).connect(ng).connect(out);
}

function thud(S, t, v = 0.5) {
  const o = osc(S, 'sine', 120);
  o.frequency.setValueAtTime(130, t);
  o.frequency.exponentialRampToValueAtTime(45, t + 0.13);
  const g = gainNode(S);
  perc(g.gain, t, v, 0.002, 0.24);
  o.connect(g).connect(S.sfx);
  o.start(t);
  o.stop(t + 0.3);
  const n = noise(S, t, 0.08);
  const lp = filt(S, 'lowpass', 500);
  const ng = gainNode(S);
  perc(ng.gain, t, v * 0.7, 0.002, 0.07);
  n.connect(lp).connect(ng).connect(S.sfx);
}

function iceHit(S, t, v = 0.4) {
  const n = noise(S, t, 0.12);
  const hp = filt(S, 'highpass', 3000);
  const ng = gainNode(S);
  perc(ng.gain, t, v, 0.001, 0.09);
  n.connect(hp).connect(ng).connect(S.sfx);
  const r = rng(77);
  for (let i = 0; i < 14; i++) {
    const ti = t + 0.01 + r() * 0.42;
    const o = osc(S, 'sine', 1800 + r() * 3600);
    const g = gainNode(S);
    const d = 0.25 + r() * 0.5;
    perc(g.gain, ti, v * (0.08 + r() * 0.08), 0.001, d);
    const p = pan(S, r() * 1.4 - 0.7);
    o.connect(g).connect(p).connect(S.sfx);
    send(S, g, 0.6);
    o.start(ti);
    o.stop(ti + d + 0.05);
  }
  // "Congelamento": chiado agudo que sobe.
  const n2 = noise(S, t, 0.6);
  const bp = filt(S, 'bandpass', 4000, 3);
  bp.frequency.setValueAtTime(2500, t);
  bp.frequency.exponentialRampToValueAtTime(9000, t + 0.5);
  const g2 = gainNode(S);
  g2.gain.setValueAtTime(0.0001, t);
  g2.gain.exponentialRampToValueAtTime(v * 0.25, t + 0.15);
  g2.gain.exponentialRampToValueAtTime(0.0001, t + 0.6);
  n2.connect(bp).connect(g2).connect(S.sfx);
  thud(S, t, v * 0.6);
}

function chainHit(S, t, v = 0.4) {
  const r = rng(55);
  let ti = t;
  for (let i = 0; i < 8; i++) {
    const out = pan(S, r() * 1.2 - 0.6);
    out.connect(S.sfx);
    for (const base of [2300, 3150, 4700]) {
      const o = osc(S, 'sine', base * (0.88 + r() * 0.24));
      const g = gainNode(S);
      const d = 0.06 + r() * 0.1;
      perc(g.gain, ti, v * (0.12 + r() * 0.1) * (1 - i * 0.08), 0.001, d);
      o.connect(g).connect(out);
      o.start(ti);
      o.stop(ti + d + 0.03);
    }
    ti += 0.025 + r() * 0.05;
  }
  thud(S, t, v * 0.9);
}

function slowMo(S, t, v = 0.22) {
  const o = osc(S, 'sawtooth', 420);
  o.frequency.setValueAtTime(420, t);
  o.frequency.exponentialRampToValueAtTime(65, t + 0.95);
  const lfo = osc(S, 'sine', 5);
  const lg = gainNode(S, 12);
  lfo.connect(lg).connect(o.frequency);
  const lp = filt(S, 'lowpass', 900, 3);
  const g = gainNode(S);
  g.gain.setValueAtTime(0.0001, t);
  g.gain.linearRampToValueAtTime(v, t + 0.03);
  g.gain.setValueAtTime(v, t + 0.75);
  g.gain.linearRampToValueAtTime(0.0001, t + 1.0);
  o.connect(lp).connect(g).connect(S.sfx);
  for (const x of [o, lfo]) {
    x.start(t);
    x.stop(t + 1.05);
  }
}

// "Brrr": batida de dentes (ruído modulado a ~26 Hz).
function chatter(S, t, dur, v = 0.18) {
  const n = noise(S, t, dur);
  const bp = filt(S, 'bandpass', 1700, 2.5);
  const am = gainNode(S, 0.5);
  const lfo = osc(S, 'square', 26);
  const lg = gainNode(S, 0.5);
  lfo.connect(lg).connect(am.gain);
  const g = gainNode(S);
  g.gain.setValueAtTime(0.0001, t);
  g.gain.linearRampToValueAtTime(v, t + 0.05);
  g.gain.setValueAtTime(v, t + dur - 0.1);
  g.gain.linearRampToValueAtTime(0.0001, t + dur);
  n.connect(bp).connect(am).connect(g).connect(pan(S, -0.35)).connect(S.sfx);
  lfo.start(t);
  lfo.stop(t + dur + 0.05);
}

function keyClick(S, t, v = 0.3, p = 0) {
  const n = noise(S, t, 0.03);
  const bp = filt(S, 'bandpass', 3800, 1.8);
  const g = gainNode(S);
  perc(g.gain, t, v, 0.001, 0.02);
  const out = pan(S, p);
  out.connect(S.sfx);
  n.connect(bp).connect(g).connect(out);
  const o = osc(S, 'sine', 210);
  const og = gainNode(S);
  perc(og.gain, t + 0.004, v * 0.5, 0.002, 0.05);
  o.connect(og).connect(out);
  o.start(t);
  o.stop(t + 0.08);
}

function sparkle(S, t, v = 0.12, notes = [84, 88, 91, 96]) {
  notes.forEach((m, i) => marimba(S, t + i * 0.05, m, v, -0.4 + i * 0.25));
}

function bonk(S, t, v = 0.55) {
  const o = osc(S, 'sine', 620);
  o.frequency.setValueAtTime(700, t);
  o.frequency.exponentialRampToValueAtTime(330, t + 0.07);
  const g = gainNode(S);
  perc(g.gain, t, v, 0.001, 0.2);
  o.connect(g).connect(S.sfx);
  send(S, g, 0.2);
  o.start(t);
  o.stop(t + 0.25);
  const n = noise(S, t, 0.05);
  const bp = filt(S, 'bandpass', 950, 3);
  const ng = gainNode(S);
  perc(ng.gain, t, v * 0.9, 0.001, 0.04);
  n.connect(bp).connect(ng).connect(S.sfx);
  thud(S, t, v * 0.8);
  boing(S, t + 0.06, v * 0.6, 240, 150, 0.55);
}

function popper(S, t, v = 0.45, p = 0) {
  const n = noise(S, t, 0.1);
  const bp = filt(S, 'bandpass', 1900, 0.7);
  const g = gainNode(S);
  perc(g.gain, t, v, 0.001, 0.08);
  n.connect(bp).connect(g).connect(pan(S, p)).connect(S.sfx);
  send(S, g, 0.3);
  const o = osc(S, 'sine', 150);
  o.frequency.exponentialRampToValueAtTime(55, t + 0.1);
  const og = gainNode(S);
  perc(og.gain, t, v * 0.8, 0.002, 0.12);
  o.connect(og).connect(pan(S, p)).connect(S.sfx);
  o.start(t);
  o.stop(t + 0.16);
}

function bloop(S, t, f0, f1, v = 0.08, dur = 0.5, p = 0) {
  const o = osc(S, 'sine', f0);
  o.frequency.setValueAtTime(f0, t);
  o.frequency.exponentialRampToValueAtTime(f1, t + dur);
  const g = gainNode(S);
  g.gain.setValueAtTime(0.0001, t);
  g.gain.linearRampToValueAtTime(v, t + 0.03);
  g.gain.exponentialRampToValueAtTime(0.0001, t + dur + 0.05);
  o.connect(g).connect(pan(S, p)).connect(S.sfx);
  o.start(t);
  o.stop(t + dur + 0.1);
}

// Trombone triste "uó uó uó uóóó" (dente de serra filtrado com vibrato no fim).
function sadTrombone(S, t, v = 0.17) {
  const notes = [[62, 0.26], [61, 0.26], [60, 0.26], [59, 0.85]];
  let ti = t;
  notes.forEach(([m, d], i) => {
    const o = osc(S, 'sawtooth', mtof(m));
    o.frequency.setValueAtTime(mtof(m) * 1.02, ti);
    o.frequency.exponentialRampToValueAtTime(mtof(m), ti + 0.06);
    if (i === 3) {
      const lfo = osc(S, 'sine', 5.5);
      const lg = gainNode(S, 0);
      lg.gain.setValueAtTime(0, ti);
      lg.gain.linearRampToValueAtTime(7, ti + 0.3);
      lfo.connect(lg).connect(o.frequency);
      lfo.start(ti);
      lfo.stop(ti + d + 0.1);
    }
    const lp = filt(S, 'lowpass', 900, 2);
    lp.frequency.setValueAtTime(500, ti);
    lp.frequency.linearRampToValueAtTime(1400, ti + 0.08);
    lp.frequency.linearRampToValueAtTime(700, ti + d);
    const g = gainNode(S);
    g.gain.setValueAtTime(0.0001, ti);
    g.gain.linearRampToValueAtTime(v, ti + 0.04);
    g.gain.setValueAtTime(v, ti + d - 0.06);
    g.gain.linearRampToValueAtTime(0.0001, ti + d);
    o.connect(lp).connect(g).connect(S.sfx);
    send(S, g, 0.2);
    o.start(ti);
    o.stop(ti + d + 0.05);
    ti += d + 0.02;
  });
}

// Torcida: ruído filtrado com respiração lenta e aleatória.
function crowd(S, t0, t1, level, seed = 1, cheerAt = []) {
  const dur = t1 - t0;
  const r = rng(seed);
  const pts = 64;
  const curve = new Float32Array(pts);
  let w = 0.7;
  for (let i = 0; i < pts; i++) {
    w += (r() - 0.5) * 0.25;
    w = Math.min(1, Math.max(0.45, w));
    const tt = t0 + (i / (pts - 1)) * dur;
    let c = w;
    for (const [ct, amt] of cheerAt) {
      const d = tt - ct;
      if (d > -0.15 && d < 1.6) c += amt * (d < 0 ? (d + 0.15) / 0.15 : Math.exp(-d * 1.8));
    }
    const fade = Math.min(1, (tt - t0) / 0.6, (t1 - tt) / 0.5);
    curve[i] = Math.max(0.0001, level * c * Math.max(0, fade));
  }
  for (const [f, q, p, g0] of [[700, 0.6, -0.5, 1], [1900, 0.8, 0.5, 0.7], [3200, 1, 0, 0.3]]) {
    const n = noise(S, t0, dur);
    const bp = filt(S, 'bandpass', f, q);
    const g = gainNode(S);
    g.gain.setValueCurveAtTime(curve.map((x) => x * g0), t0, dur);
    n.connect(bp).connect(g).connect(pan(S, p)).connect(S.amb);
    send(S, g, 0.3);
  }
}

// "Ôôôôh" da arquibancada: várias vozes de dente de serra num formante de "o".
function crowdOoh(S, t, v = 0.05) {
  const r = rng(31);
  const f1 = filt(S, 'bandpass', 480, 2.5);
  const f2 = filt(S, 'bandpass', 850, 3);
  const mix = gainNode(S, 1);
  f1.connect(mix);
  f2.connect(mix);
  const g = gainNode(S);
  g.gain.setValueAtTime(0.0001, t);
  g.gain.linearRampToValueAtTime(v, t + 0.22);
  g.gain.exponentialRampToValueAtTime(0.0001, t + 1.15);
  mix.connect(g).connect(S.amb);
  send(S, g, 0.4);
  for (let i = 0; i < 16; i++) {
    const f = 150 + r() * 170;
    const o = osc(S, 'sawtooth', f);
    o.frequency.setValueAtTime(f * 1.04, t);
    o.frequency.exponentialRampToValueAtTime(f * 0.86, t + 1.1);
    o.connect(r() < 0.5 ? f1 : f2);
    o.start(t + r() * 0.06);
    o.stop(t + 1.2);
  }
}

// ---------------------------------------------------------------- partitura

const CH = {
  C: { root: 36, triad: [60, 64, 67] },
  G: { root: 43, triad: [59, 62, 67] },
  G7: { root: 43, triad: [59, 62, 65, 67] },
  Am: { root: 45, triad: [60, 64, 69] },
  F: { root: 41, triad: [60, 65, 69] },
};
const MEL = {
  C: [[0, 79], [2, 76], [3, 79], [5, 81], [6, 79], [7, 76]],
  G: [[0, 74], [2, 71], [3, 74], [5, 79], [6, 77], [7, 74]],
  Am: [[0, 76], [2, 72], [3, 76], [5, 81], [6, 79], [7, 76]],
  F: [[0, 77], [2, 81], [3, 84], [5, 81], [6, 79], [7, 77]],
};
// Um compasso (2 s) por entrada, a partir do drop em 5 s.
const BARS = [
  { c: 'C', mode: 'full', mel: true }, // 5  título
  { c: 'G', mode: 'full', mel: true }, // 7
  { c: 'G7', mode: 'tension' }, // 9  countdown
  { c: 'C', mode: 'full', mel: true, hats16: true }, // 11 corrida
  { c: 'G', mode: 'full', mel: true, hats16: true }, // 13
  { c: 'Am', mode: 'full' }, // 15 poderes
  { c: 'F', mode: 'full', fill: true }, // 17
  { c: 'G', mode: 'full', mel: true }, // 19
  { c: 'C', mode: 'light' }, // 21 benchmarks
  { c: 'G', mode: 'light' }, // 23
  { c: 'F', mode: 'light' }, // 25
  { c: 'C', mode: 'full', mel: true }, // 27 roadmap
  { c: 'G', mode: 'full', mel: true }, // 29
  { c: 'F', mode: 'full' }, // 31 teclado
  { c: 'G', mode: 'break' }, // 33 BONK + virada
  { c: 'C', mode: 'full', mel: true, big: true }, // 35 final
];

function groove(S) {
  BARS.forEach((B, b) => {
    const ch = CH[B.c];
    const t0 = at(b);
    if (B.mode === 'tension') {
      // Countdown: só um "coração" batendo, tique-taque e um riser até o VAI!.
      pad(S, t0, ch.triad, BAR, 0.025);
      for (let i = 0; i < 4; i++) {
        kick(S, t0 + i * BEAT, 0.32 + i * 0.06);
        kick(S, t0 + i * BEAT + 0.16, 0.18 + i * 0.04);
      }
      for (let i = 0; i < 8; i++) hat(S, t0 + i * BEAT * 0.5 + BEAT * 0.25, 0.035 + i * 0.008, false, 0.3);
      riser(S, t0 + BEAT * 2, BEAT * 2, 0.12);
      return;
    }
    if (B.mode === 'break') {
      // BONK no tempo 1, silêncio cômico, e virada de caixa até o final.
      const s = at(b, 1);
      for (let i = 0; i < 8; i++) snare(S, s + i * BEAT * 0.25, 0.12 + i * 0.05);
      tom(S, at(b, 2.5), 140, 0.4);
      tom(S, at(b, 2.75), 100, 0.45);
      return;
    }
    const light = B.mode === 'light';
    pad(S, t0, ch.triad, BAR, light ? 0.06 : 0.045);
    for (let beat = 0; beat < 4; beat++) {
      const tb = at(b, beat);
      if (!light) kick(S, tb, beat === 0 ? 1 : 0.85);
      if (beat % 2 === 1) light ? snare(S, tb, 0.16) : clap(S, tb, 0.42);
      hat(S, tb + BEAT / 2, light ? 0.07 : 0.11, beat === 3 && !light, 0.25);
      if (B.hats16) {
        hat(S, tb + BEAT / 4, 0.045, false, -0.25);
        hat(S, tb + (3 * BEAT) / 4, 0.045, false, -0.25);
      }
    }
    // Baixo "disco" em oitavas.
    const pat = [0, 12, 0, 12, 0, 12, 7, 12];
    pat.forEach((iv, i) => bass(S, t0 + i * BEAT * 0.5, ch.root + iv, BEAT * 0.42, (i % 2 ? 0.22 : 0.3) * (light ? 0.8 : 1)));
    if (B.mel) for (const [e, m] of MEL[B.c === 'G7' ? 'G' : B.c]) marimba(S, t0 + e * BEAT * 0.5, m, 0.22, 0.15);
    if (light) {
      // Arpejo "pensativo" para o gráfico.
      ch.triad.concat(ch.triad.map((m) => m + 12)).forEach((m, i) => pluck(S, t0 + i * BEAT * 0.5 + BEAT * 0.25, m + 12, 0.045, i % 2 ? 0.4 : -0.4));
    }
    if (B.fill) {
      // "Ba-dum-tss" que cai em cima da piada (o "tss" é o crash do compasso seguinte).
      tom(S, CUE.punch, 180, 0.42);
      tom(S, CUE.punch + 0.25, 120, 0.46);
    }
  });
}

// Abaixa a música por um instante para o efeito sonoro "falar" (ducking).
function ducks(S, list) {
  const g = S.music.gain;
  const base = g.value;
  g.setValueAtTime(base, 0);
  for (const [t, depth, hold] of [...list].sort((a, b) => a[0] - b[0])) {
    g.setValueAtTime(base, t - 0.03);
    g.linearRampToValueAtTime(base * depth, t + 0.02);
    g.setValueAtTime(base * depth, t + hold);
    g.linearRampToValueAtTime(base, t + hold + 0.25);
  }
}

function score(S) {
  const R = race();
  ducks(S, [
    [R.p2.crash.t, 0.45, 0.35],
    [CUE.iceHit, 0.45, 0.4],
    [CUE.chainHit, 0.45, 0.45],
    [CUE.overshoot, 0.55, 0.5],
    [CUE.ops, 0.35, 1.25],
  ]);
  // 1. Abertura: piano + pad, no clima de anúncio.
  const pn = (t, m, v, d, p) => piano(S, t, m, v * 1.5, d, p);
  pad(S, 0.1, [60, 64, 67, 71], 4.6, 0.045);
  pn(CUE.l1, 48, 0.2, 3.8, -0.2);
  pn(CUE.l1, 55, 0.16, 3.6, -0.1);
  pn(CUE.l1 + 0.02, 76, 0.24, 3.2, 0.2);
  pn(CUE.l1 + 0.7, 74, 0.18, 2.6, 0.25);
  pn(CUE.l2, 45, 0.2, 3.6, -0.2);
  pn(CUE.l2, 52, 0.15, 3.4, -0.1);
  pn(CUE.l2 + 0.02, 72, 0.23, 3.0, 0.2);
  pn(CUE.l2 + 0.7, 71, 0.17, 2.4, 0.25);
  pn(CUE.l3, 41, 0.2, 3.0, -0.2);
  pn(CUE.l3, 48, 0.15, 3.0, -0.1);
  pn(CUE.l3 + 0.02, 69, 0.22, 2.6, 0.2);
  [0, 1, 2].forEach((i) => pn(CUE.l3 + 0.42 + i * 0.13, 79 + [0, 2, 4][i], 0.07, 1.2, 0.3));
  // O panda espia.
  pop(S, CUE.peekUp, 0.32, 700, 1300);
  boing(S, CUE.peekUp + 0.02, 0.2, 200, 420, 0.45, 0.3);
  marimba(S, CUE.peekUp + 0.42, 91, 0.12, 0.3);
  marimba(S, CUE.peekUp + 0.5, 96, 0.1, 0.3);
  whoosh(S, CUE.peekDown, 0.3, 0.12, 2000, 400, 0.3, 0.1);
  riser(S, 4.25, 0.75, 0.2);
  for (let i = 0; i < 12; i++) snare(S, 4.4 + i * 0.05, 0.04 + i * 0.022);

  // 2. Drop + título.
  groove(S);
  crash(S, CUE.dash, 0.42);
  brass(S, CUE.dash, [60, 64, 67, 72], 0.32, 0.12);
  whoosh(S, CUE.dash - 0.08, 0.7, 0.42, 350, 6000, -0.9, 0.9);
  sparkle(S, CUE.mark, 0.11);
  CUE.pills.forEach((tp, i) => pluck(S, tp, [84, 88, 91][i], 0.14, -0.3 + i * 0.3));
  whoosh(S, CUE.titleOut, 0.45, 0.14, 2500, 500, 0.4, -0.4);

  // 3. Corrida.
  whoosh(S, CUE.raceIn - 0.1, 0.5, 0.14, 500, 2200, 0, 0);
  crowd(S, CUE.raceIn, CUE.raceOut + 0.6, 0.055, 3, [[CUE.go, 0.9]]);
  CUE.count.forEach((tc) => {
    const o = osc(S, 'square', 440);
    const lp = filt(S, 'lowpass', 2600);
    const g = gainNode(S);
    g.gain.setValueAtTime(0.0001, tc);
    g.gain.linearRampToValueAtTime(0.16, tc + 0.004);
    g.gain.setValueAtTime(0.16, tc + 0.15);
    g.gain.linearRampToValueAtTime(0.0001, tc + 0.17);
    o.connect(lp).connect(g).connect(S.sfx);
    o.start(tc);
    o.stop(tc + 0.2);
  });
  {
    const tc = CUE.go;
    for (const f of [880, 1760]) {
      const o = osc(S, 'square', f);
      const lp = filt(S, 'lowpass', 3500);
      const g = gainNode(S);
      g.gain.setValueAtTime(0.0001, tc);
      g.gain.linearRampToValueAtTime(f > 1000 ? 0.04 : 0.16, tc + 0.004);
      g.gain.setValueAtTime(f > 1000 ? 0.04 : 0.16, tc + 0.5);
      g.gain.linearRampToValueAtTime(0.0001, tc + 0.56);
      o.connect(lp).connect(g).connect(S.sfx);
      o.start(tc);
      o.stop(tc + 0.6);
    }
  }
  crash(S, CUE.go, 0.4);
  brass(S, CUE.go, [60, 64, 67, 72], 0.3, 0.11);
  for (const [p, pn] of [[R.p1, -0.3], [R.p2, 0.3]]) {
    for (const j of p.jumps) {
      if (j.t0 > CUE.raceOut + 0.3) continue;
      whoosh(S, j.t0, 0.28, 0.06, 600, 2400, pn, pn);
      thud(S, j.t1, 0.09);
    }
  }
  const crashT = R.p2.crash.t;
  clank(S, crashT, 0.36);
  thud(S, crashT, 0.4);
  boing(S, crashT + 0.06, 0.14, 300, 120, 0.4, 0.3);
  crowdOoh(S, crashT + 0.08, 0.045);
  whoosh(S, CUE.raceOut, 0.45, 0.14, 2500, 500, 0.4, -0.4);

  // 4. Poderes.
  whoosh(S, CUE.powersIn - 0.1, 0.5, 0.12, 500, 2200, 0, 0);
  whoosh(S, CUE.iceThrow, CUE.iceHit - CUE.iceThrow + 0.05, 0.3, 1500, 7000, -0.9, -0.3);
  iceHit(S, CUE.iceHit, 0.42);
  chatter(S, CUE.iceHit + 0.4, 0.8, 0.12);
  whoosh(S, CUE.chainThrow, CUE.chainHit - CUE.chainThrow + 0.05, 0.3, 300, 1800, -0.1, 0.4);
  chainHit(S, CUE.chainHit, 0.45);
  slowMo(S, CUE.chainHit + 0.05, 0.16);
  whoosh(S, CUE.powersOut, 0.45, 0.14, 2500, 500, 0.4, -0.4);

  // 5. Benchmarks.
  whoosh(S, CUE.benchIn - 0.1, 0.5, 0.12, 500, 2200, 0, 0);
  CUE.groups.forEach((tg, gi) => {
    [0, 1, 2].forEach((bi) => {
      const top = gi === 2 ? [1, 0.11, 0.02][bi] : [[1, 0.81, 0.12], [1, 0.71, 0.14]][gi][bi];
      bloop(S, tg + bi * 0.1, 320, 320 + 900 * top, 0.06, 0.55, -0.3 + bi * 0.3);
    });
  });
  bass(S, CUE.overshoot - 0.32, 31, 0.32, 0.2);
  slideWhistle(S, CUE.overshoot, 0.5, 480, 1900, 0.17);
  boing(S, CUE.overshoot + 0.48, 0.16, 260, 520, 0.5, 0.35);
  sadTrombone(S, CUE.ops, 0.15);
  whoosh(S, CUE.benchOut, 0.45, 0.14, 2500, 500, 0.4, -0.4);

  // 6. Roadmap.
  whoosh(S, CUE.roadIn - 0.1, 0.5, 0.12, 500, 2200, 0, 0);
  const penta = [72, 74, 76, 79, 81, 84, 86, 88, 91];
  CUE.chips.forEach((tc, i) => {
    pluck(S, tc, penta[i], 0.15, -0.6 + (i % 3) * 0.6);
    pop(S, tc, 0.08, 500 + i * 60, 900 + i * 80);
  });
  sparkle(S, CUE.bears, 0.07, [79, 84, 88]);
  whoosh(S, CUE.roadOut, 0.45, 0.14, 2500, 500, 0.4, -0.4);

  // 7. Teclado.
  whoosh(S, CUE.keysIn - 0.1, 0.5, 0.12, 500, 2200, 0, 0);
  keyClick(S, CUE.kW, 0.3, -0.5);
  boing(S, CUE.kW + 0.02, 0.12, 220, 440, 0.4, -0.4);
  keyClick(S, CUE.kUp, 0.3, 0.5);
  boing(S, CUE.kUp + 0.02, 0.12, 260, 520, 0.4, 0.4);
  keyClick(S, CUE.kShiftL, 0.3, -0.5);
  keyClick(S, CUE.kShiftL + 0.02, 0.24, -0.5);
  whoosh(S, CUE.kShiftL, 0.32, 0.16, 500, 3000, -0.6, 0);
  keyClick(S, CUE.kShiftR, 0.3, 0.5);
  keyClick(S, CUE.kShiftR + 0.02, 0.24, 0.5);
  whoosh(S, CUE.kShiftR, 0.28, 0.16, 500, 3000, 0.6, 0);
  bonk(S, CUE.bonk, 0.5);

  // 8. Final: fanfarra, confete e torcida.
  whoosh(S, CUE.wipe - 0.05, 0.6, 0.2, 300, 3000, 0, 0);
  crash(S, CUE.wipe, 0.3);
  [[67, 0], [72, 0.25], [76, 0.5], [79, 0.75]].forEach(([m, dt]) => brass(S, CUE.wipe + dt, [m, m - 12], 0.2, 0.1));
  sparkle(S, CUE.lockup + 0.1, 0.09, [84, 88, 91, 96, 100]);
  popper(S, CUE.party, 0.42, -0.7);
  popper(S, CUE.party + 0.03, 0.42, 0.7);
  crash(S, CUE.party, 0.45);
  brass(S, CUE.party, [48, 60, 64, 67, 72], 0.9, 0.1);
  crowd(S, CUE.party - 0.1, DURATION, 0.07, 8, [[CUE.party, 1.4], [CUE.final, 0.9]]);
  popper(S, CUE.final, 0.36, -0.7);
  popper(S, CUE.final + 0.03, 0.36, 0.7);
  kick(S, CUE.final, 1);
  crash(S, CUE.final, 0.45);
  brass(S, CUE.final, [48, 55, 60, 64, 67, 72], 1.2, 0.11);
  bass(S, CUE.final, 36, 1.3, 0.4);
  piano(S, CUE.final, 84, 0.12, 2.4, 0.2);
  piano(S, CUE.final, 88, 0.1, 2.4, -0.2);
}

// ---------------------------------------------------------------- saída WAV

function wavBase64(buf) {
  const n = buf.length;
  const ch = buf.numberOfChannels;
  const data = new DataView(new ArrayBuffer(44 + n * ch * 2));
  const str = (o, s) => [...s].forEach((c, i) => data.setUint8(o + i, c.charCodeAt(0)));
  str(0, 'RIFF');
  data.setUint32(4, 36 + n * ch * 2, true);
  str(8, 'WAVE');
  str(12, 'fmt ');
  data.setUint32(16, 16, true);
  data.setUint16(20, 1, true);
  data.setUint16(22, ch, true);
  data.setUint32(24, SR, true);
  data.setUint32(28, SR * ch * 2, true);
  data.setUint16(32, ch * 2, true);
  data.setUint16(34, 16, true);
  str(36, 'data');
  data.setUint32(40, n * ch * 2, true);
  const chans = [...Array(ch)].map((_, c) => buf.getChannelData(c));
  let o = 44;
  for (let i = 0; i < n; i++)
    for (let c = 0; c < ch; c++) {
      const v = Math.max(-1, Math.min(1, chans[c][i]));
      data.setInt16(o, v < 0 ? v * 0x8000 : v * 0x7fff, true);
      o += 2;
    }
  const bytes = new Uint8Array(data.buffer);
  let s = '';
  for (let i = 0; i < bytes.length; i += 0x8000) s += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));
  return btoa(s);
}

export async function renderAudioWavBase64() {
  const ctx = new OfflineAudioContext(2, Math.ceil(DURATION * SR), SR);
  const S = engine(ctx);
  score(S);
  const buf = await ctx.startRendering();
  return wavBase64(buf);
}
