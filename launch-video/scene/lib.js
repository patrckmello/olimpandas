// Utilitários de animação determinística: easing, molas, RNG com seed,
// carregamento de sprites e revelação de texto palavra por palavra.

export const clamp = (x, a = 0, b = 1) => (x < a ? a : x > b ? b : x);
export const lerp = (a, b, k) => a + (b - a) * k;
export const seg = (t, t0, dur) => clamp((t - t0) / dur);
export const TAU = Math.PI * 2;

export const E = {
  linear: (k) => k,
  inQuad: (k) => k * k,
  outQuad: (k) => 1 - (1 - k) * (1 - k),
  inCubic: (k) => k * k * k,
  outCubic: (k) => 1 - Math.pow(1 - k, 3),
  inOutCubic: (k) => (k < 0.5 ? 4 * k * k * k : 1 - Math.pow(-2 * k + 2, 3) / 2),
  outQuart: (k) => 1 - Math.pow(1 - k, 4),
  outQuint: (k) => 1 - Math.pow(1 - k, 5),
  inOutQuint: (k) => (k < 0.5 ? 16 * k ** 5 : 1 - Math.pow(-2 * k + 2, 5) / 2),
  outExpo: (k) => (k >= 1 ? 1 : 1 - Math.pow(2, -10 * k)),
  inExpo: (k) => (k <= 0 ? 0 : Math.pow(2, 10 * k - 10)),
  inOutExpo: (k) =>
    k <= 0 ? 0 : k >= 1 ? 1 : k < 0.5 ? Math.pow(2, 20 * k - 10) / 2 : (2 - Math.pow(2, -20 * k + 10)) / 2,
  outBack: (k, s = 1.70158) => 1 + (s + 1) * Math.pow(k - 1, 3) + s * Math.pow(k - 1, 2),
  inBack: (k, s = 1.70158) => (s + 1) * k * k * k - s * k * k,
};

// Resposta ao degrau de uma mola subamortecida (x em segundos desde o início).
export function spring(x, freq = 2.4, damp = 0.32) {
  if (x <= 0) return 0;
  const w = TAU * freq;
  const wd = w * Math.sqrt(1 - damp * damp);
  const env = Math.exp(-damp * w * x);
  return 1 - env * (Math.cos(wd * x) + ((damp * w) / wd) * Math.sin(wd * x));
}

// mulberry32: RNG determinístico (cada worker de render gera exatamente os mesmos valores).
export function rng(seed) {
  let s = seed >>> 0;
  return () => {
    s = (s + 0x6d2b79f5) | 0;
    let t = Math.imul(s ^ (s >>> 15), 1 | s);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

export function loadImage(src) {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => img.decode().then(() => resolve(img), () => resolve(img));
    img.onerror = () => reject(new Error(`Falha ao carregar ${src}`));
    img.src = src;
  });
}

export function makeCanvas(w, h) {
  const c = document.createElement('canvas');
  c.width = w;
  c.height = h;
  return c;
}

function rgbToHsv(r, g, b) {
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const d = max - min;
  let h = 0;
  if (d) {
    if (max === r) h = ((g - b) / d) % 6;
    else if (max === g) h = (b - r) / d + 2;
    else h = (r - g) / d + 4;
    h *= 60;
    if (h < 0) h += 360;
  }
  return [h, max ? d / max : 0, max / 255];
}

function hsvToRgb(h, s, v) {
  const c = v * s;
  const x = c * (1 - Math.abs(((h / 60) % 2) - 1));
  const m = v - c;
  let r = 0, g = 0, b = 0;
  if (h < 60) [r, g, b] = [c, x, 0];
  else if (h < 120) [r, g, b] = [x, c, 0];
  else if (h < 180) [r, g, b] = [0, c, x];
  else if (h < 240) [r, g, b] = [0, x, c];
  else if (h < 300) [r, g, b] = [x, 0, c];
  else [r, g, b] = [c, 0, x];
  return [(r + m) * 255, (g + m) * 255, (b + m) * 255];
}

// Troca só os pixels azuis do uniforme por vermelho: vira o "P2".
export function blueToRed(r, g, b) {
  const [h, s, v] = rgbToHsv(r, g, b);
  if (s < 0.14 || h < 188 || h > 262) return null;
  const nh = (((h - 225 + 357) % 360) + 360) % 360;
  return hsvToRgb(nh, Math.min(1, s * 1.04), Math.min(1, v * 1.08));
}

export function recolor(img, fn) {
  const c = makeCanvas(img.width, img.height);
  const x = c.getContext('2d', { willReadFrequently: true });
  x.drawImage(img, 0, 0);
  const data = x.getImageData(0, 0, c.width, c.height);
  const a = data.data;
  for (let i = 0; i < a.length; i += 4) {
    if (a[i + 3] === 0) continue;
    const out = fn(a[i], a[i + 1], a[i + 2]);
    if (!out) continue;
    a[i] = out[0];
    a[i + 1] = out[1];
    a[i + 2] = out[2];
  }
  x.putImageData(data, 0, 0);
  return c;
}

// Cópia da imagem com uma cor aplicada por cima, respeitando o alpha (gelo, silhueta...).
export function tinted(img, color, alpha) {
  const c = makeCanvas(img.width, img.height);
  const x = c.getContext('2d');
  x.drawImage(img, 0, 0);
  x.globalCompositeOperation = 'source-atop';
  x.globalAlpha = alpha;
  x.fillStyle = color;
  x.fillRect(0, 0, c.width, c.height);
  return c;
}

// ---------- Texto ----------

// Envolve cada palavra (inclusive dentro de <em>, <b>...) num <span class="w">.
export function splitWords(el) {
  const spans = [];
  const walk = (node) => {
    for (const child of [...node.childNodes]) {
      if (child.nodeType === Node.TEXT_NODE) {
        const parts = child.textContent.split(/(\s+)/);
        const frag = document.createDocumentFragment();
        for (const p of parts) {
          if (!p) continue;
          if (/^\s+$/.test(p)) {
            frag.appendChild(document.createTextNode(' '));
            continue;
          }
          const s = document.createElement('span');
          s.className = 'w';
          s.textContent = p;
          frag.appendChild(s);
          spans.push(s);
        }
        child.replaceWith(frag);
      } else if (child.nodeType === Node.ELEMENT_NODE) {
        walk(child);
      }
    }
  };
  walk(el);
  return spans;
}

export function style(el, { o = 1, x = 0, y = 0, s = 1, sx, sy, r = 0, blur = 0 } = {}) {
  const st = el.style;
  st.opacity = o <= 0.001 ? '0' : o >= 0.999 ? '1' : o.toFixed(3);
  const scx = sx ?? s;
  const scy = sy ?? s;
  const tf =
    x || y || scx !== 1 || scy !== 1 || r
      ? `translate(${x.toFixed(2)}px, ${y.toFixed(2)}px) rotate(${r.toFixed(3)}deg) scale(${scx.toFixed(4)}, ${scy.toFixed(4)})`
      : 'none';
  st.transform = tf;
  st.filter = blur > 0.05 ? `blur(${blur.toFixed(2)}px)` : 'none';
}

// Revelação "estilo anúncio": cada palavra sobe, perde o blur e aparece.
export function revealWords(spans, t, t0, { stagger = 0.075, dur = 0.7, y = 26, blur = 12 } = {}) {
  spans.forEach((sp, i) => {
    const k = E.outCubic(seg(t, t0 + i * stagger, dur));
    style(sp, { o: k, y: (1 - k) * y, blur: (1 - k) * blur });
  });
}

// Saída suave de um bloco inteiro (sobe e desfoca).
export function exitFx(t, t0, dur = 0.45, dy = -30, blur = 14) {
  const k = E.inCubic(seg(t, t0, dur));
  return { o: 1 - k, y: k * dy, blur: k * blur };
}

export function fmtNum(v, decimals = 1) {
  return v.toFixed(decimals).replace('.', ',');
}
