#!/usr/bin/env node
// Renderiza o vídeo de lançamento do Olimpandas.
//
//   node render.mjs                    -> out/olimpandas-lancamento.mp4 (1080p60 + áudio)
//   node render.mjs --stills 1.2,6,12  -> PNGs desses instantes em out/stills/
//   node render.mjs --preview          -> versão rápida 960x540 a 30 fps
//   node render.mjs --audio            -> só a trilha (out/audio.wav)
//
// Fluxo: um servidor HTTP local serve o repositório (a cena usa a arte real de
// Assets/Art), o Chromium do Playwright abre scene/index.html, cada frame é
// posicionado com window.__seek(t) e capturado; o ffmpeg codifica e junta o áudio,
// que é sintetizado com Web Audio (OfflineAudioContext) na mesma página.

import { chromium } from 'playwright';
import http from 'node:http';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '..');
const OUT = path.join(HERE, 'out');
const FINAL = path.join(OUT, 'olimpandas-lancamento.mp4');

const args = process.argv.slice(2);
const flag = (name) => args.includes(`--${name}`);
const opt = (name, def) => {
  const i = args.indexOf(`--${name}`);
  return i >= 0 && args[i + 1] && !args[i + 1].startsWith('--') ? args[i + 1] : def;
};

const PREVIEW = flag('preview');
const FPS = Number(opt('fps', PREVIEW ? 30 : 60));
const SCALE = Number(opt('scale', PREVIEW ? 0.5 : 1));
const WORKERS = Number(opt('workers', Math.max(1, Math.min(4, os.cpus().length))));

const MIME = {
  '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.mjs': 'text/javascript; charset=utf-8',
  '.css': 'text/css', '.png': 'image/png', '.jpg': 'image/jpeg', '.woff2': 'font/woff2', '.ttf': 'font/ttf', '.TTF': 'font/ttf',
  '.svg': 'image/svg+xml', '.json': 'application/json',
};

function serve(root) {
  const server = http.createServer((req, res) => {
    const url = new URL(req.url, 'http://x');
    const file = path.normalize(path.join(root, decodeURIComponent(url.pathname)));
    if (!file.startsWith(root)) {
      res.writeHead(403).end();
      return;
    }
    fs.readFile(file, (err, data) => {
      if (err) {
        res.writeHead(404).end();
        return;
      }
      res.writeHead(200, { 'Content-Type': MIME[path.extname(file)] || 'application/octet-stream' });
      res.end(data);
    });
  });
  return new Promise((resolve) => server.listen(0, '127.0.0.1', () => resolve(server)));
}

function run(cmd, argv, { input } = {}) {
  return new Promise((resolve, reject) => {
    const p = spawn(cmd, argv, { stdio: [input ? 'pipe' : 'ignore', 'inherit', 'inherit'] });
    p.on('error', reject);
    p.on('close', (code) => (code === 0 ? resolve() : reject(new Error(`${cmd} saiu com código ${code}`))));
  });
}

async function openScene(browser, base) {
  const page = await browser.newPage({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: SCALE });
  page.on('pageerror', (e) => console.error('[página]', e.message));
  page.on('console', (m) => m.type() === 'error' && console.error('[console]', m.text()));
  await page.goto(`${base}/launch-video/scene/index.html`);
  await page.waitForFunction(() => window.__ready === true || window.__error, null, { timeout: 120000 });
  const err = await page.evaluate(() => window.__error);
  if (err) throw new Error(err);
  const cdp = await page.context().newCDPSession(page);
  return { page, cdp };
}

async function capture({ page, cdp }, t) {
  await page.evaluate((tt) => window.__seek(tt), t);
  const { data } = await cdp.send('Page.captureScreenshot', { format: 'png', optimizeForSpeed: true, captureBeyondViewport: false });
  return Buffer.from(data, 'base64');
}

async function renderAudio(browser, base) {
  const { page } = await openScene(browser, base);
  const t0 = Date.now();
  const b64 = await page.evaluate(() => window.__renderAudio());
  const file = path.join(OUT, 'audio.wav');
  fs.writeFileSync(file, Buffer.from(b64, 'base64'));
  console.log(`♪ áudio sintetizado em ${((Date.now() - t0) / 1000).toFixed(1)} s → ${path.relative(HERE, file)}`);
  await page.close();
  return file;
}

async function renderStills(browser, base, times) {
  const dir = path.join(OUT, 'stills');
  fs.mkdirSync(dir, { recursive: true });
  const sc = await openScene(browser, base);
  for (const t of times) {
    const buf = await capture(sc, t);
    const file = path.join(dir, `t${t.toFixed(2).padStart(6, '0')}.png`);
    fs.writeFileSync(file, buf);
    console.log(`▣ ${path.relative(HERE, file)}`);
  }
}

async function renderSegment(browser, base, i0, i1, file, onFrame) {
  const sc = await openScene(browser, base);
  const ff = spawn('ffmpeg', [
    '-y', '-loglevel', 'error', '-f', 'image2pipe', '-framerate', String(FPS), '-c:v', 'png', '-i', '-',
    '-vf', 'scale=in_range=pc:out_range=tv:out_color_matrix=bt709,format=yuv420p',
    '-c:v', 'libx264', '-preset', 'veryfast', '-crf', '10', '-g', String(FPS),
    '-colorspace', 'bt709', '-color_primaries', 'bt709', '-color_trc', 'bt709', file,
  ], { stdio: ['pipe', 'inherit', 'inherit'] });
  const closed = once(ff, 'close');
  for (let i = i0; i < i1; i++) {
    const buf = await capture(sc, i / FPS);
    if (!ff.stdin.write(buf)) await once(ff.stdin, 'drain');
    onFrame();
  }
  ff.stdin.end();
  const [code] = await closed;
  if (code !== 0) throw new Error(`ffmpeg falhou no segmento ${file}`);
  await sc.page.close();
}

async function main() {
  fs.mkdirSync(OUT, { recursive: true });
  const server = await serve(ROOT);
  const base = `http://127.0.0.1:${server.address().port}`;
  const browser = await chromium.launch({ args: ['--font-render-hinting=none', '--force-color-profile=srgb', '--disable-lcd-text'] });
  try {
    const { DURATION } = await import(path.join(HERE, 'scene', 'timeline.js'));

    if (opt('stills')) {
      await renderStills(browser, base, opt('stills').split(',').map(Number));
      return;
    }
    const audio = await renderAudio(browser, base);
    if (flag('audio')) return;

    const total = Math.round(DURATION * FPS);
    const per = Math.ceil(total / WORKERS);
    const segs = [];
    let done = 0;
    const t0 = Date.now();
    const tick = () => {
      done++;
      if (done % 30 === 0 || done === total) {
        const el = (Date.now() - t0) / 1000;
        process.stdout.write(`\r▶ ${done}/${total} frames · ${(done / el).toFixed(1)} fps · ${el.toFixed(0)} s   `);
      }
    };
    const jobs = [];
    for (let w = 0; w < WORKERS; w++) {
      const i0 = w * per;
      const i1 = Math.min(total, i0 + per);
      if (i0 >= i1) break;
      const file = path.join(OUT, `seg${w}.mkv`);
      segs.push(file);
      jobs.push(renderSegment(browser, base, i0, i1, file, tick));
    }
    await Promise.all(jobs);
    process.stdout.write('\n');

    const list = path.join(OUT, 'segments.txt');
    fs.writeFileSync(list, segs.map((s) => `file '${s.replace(/'/g, "'\\''")}'`).join('\n'));
    const target = PREVIEW ? path.join(OUT, 'preview.mp4') : FINAL;
    await run('ffmpeg', [
      '-y', '-loglevel', 'error', '-f', 'concat', '-safe', '0', '-i', list, '-i', audio,
      '-map', '0:v', '-map', '1:a', '-c:v', 'libx264', '-preset', 'slow', '-crf', PREVIEW ? '23' : '17', '-tune', 'animation',
      '-pix_fmt', 'yuv420p', '-colorspace', 'bt709', '-color_primaries', 'bt709', '-color_trc', 'bt709',
      '-c:a', 'aac', '-b:a', '192k', '-shortest', '-movflags', '+faststart', target,
    ]);
    for (const s of segs) fs.rmSync(s, { force: true });
    fs.rmSync(list, { force: true });
    console.log(`✔ ${path.relative(HERE, target)} (${(fs.statSync(target).size / 1e6).toFixed(1)} MB) em ${((Date.now() - t0) / 1000).toFixed(0)} s`);
  } finally {
    await browser.close();
    server.close();
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
