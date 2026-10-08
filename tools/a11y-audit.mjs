// Usage (app running on :5050): node --experimental-websocket tools/a11y-audit.mjs / /Cases /Cases/1 ...
// Drives headless Chrome over the DevTools protocol: axe-core (WCAG 2.2 AA + best practice),
// horizontal overflow at phone width, heading outline, title.
import { spawn } from 'node:child_process';
import { writeFileSync, mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
const base = 'http://127.0.0.1:5050';
const pages = process.argv.slice(2);
const chrome = spawn('/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
  ['--headless=new', '--remote-debugging-port=9333', '--user-data-dir=' + mkdtempSync(join(tmpdir(), 'share-a11y-')), '--disable-gpu', 'about:blank'], { stdio: 'ignore' });
const sleep = ms => new Promise(r => setTimeout(r, ms));
let target;
for (let i = 0; i < 50 && !target; i++) { await sleep(200); try { target = (await (await fetch('http://127.0.0.1:9333/json/list')).json()).find(t => t.type === 'page'); } catch {} }
const ws = new WebSocket(target.webSocketDebuggerUrl);
await new Promise(r => ws.onopen = r);
let id = 0; const waiting = new Map();
ws.onmessage = m => { const d = JSON.parse(m.data); if (d.id && waiting.has(d.id)) { waiting.get(d.id)(d); waiting.delete(d.id); } };
const send = (method, params = {}) => new Promise(r => { const i = ++id; waiting.set(i, r); ws.send(JSON.stringify({ id: i, method, params })); });
const evaluate = async expr => { const r = await send('Runtime.evaluate', { expression: expr, awaitPromise: true, returnByValue: true }); if (r.result.exceptionDetails) throw new Error(JSON.stringify(r.result.exceptionDetails)); return r.result.result.value; };
const axeSrc = await (await fetch('https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.10.2/axe.min.js')).text();

const results = [];
for (const width of [1280, 390]) {
  await send('Emulation.setDeviceMetricsOverride', { width, height: 900, deviceScaleFactor: 1, mobile: width < 500 });
  for (const path of pages) {
    await send('Page.navigate', { url: base + path });
    for (let i = 0; i < 50; i++) { await sleep(100); if (await evaluate('document.readyState') === 'complete') break; }
    await sleep(300);
    await evaluate(axeSrc + ';1');
    const r = await evaluate(`(async () => {
      const a = await axe.run(document, { runOnly: ['wcag2a','wcag2aa','wcag21a','wcag21aa','wcag22aa','best-practice'] });
      return {
        title: document.title,
        overflow: document.documentElement.scrollWidth > window.innerWidth ? document.documentElement.scrollWidth : 0,
        headings: [...document.querySelectorAll('h1,h2,h3,h4')].map(h => h.tagName + ' ' + h.textContent.trim().replace(/\\s+/g,' ').slice(0,60)),
        violations: a.violations.map(v => ({ id: v.id, impact: v.impact, help: v.help, nodes: v.nodes.slice(0,3).map(n => n.target.join(' ')) })),
        incomplete: a.incomplete.filter(v => v.id === 'color-contrast').map(v => v.nodes.length),
      };
    })()`);
    results.push({ width, path, ...r });
  }
}
writeFileSync(join(tmpdir(), 'share-a11y-results.json'), JSON.stringify(results, null, 1));
ws.close(); chrome.kill();
for (const r of results) {
  const v = r.violations.map(v => `${v.impact}:${v.id} [${v.nodes.join(' | ')}]`).join('; ');
  console.log(`${r.width} ${r.path} | ${r.title} | overflow:${r.overflow} | ${v || 'no violations'}`);
}
