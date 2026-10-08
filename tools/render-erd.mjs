// Renders the Mermaid diagram in ERD.md to ERD.svg, so it shows in any Markdown preview.
// Usage: node --experimental-websocket tools/render-erd.mjs   (needs Chrome and internet for the Mermaid CDN)
import { spawn } from 'node:child_process';
import { readFileSync, writeFileSync, mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const source = readFileSync(join(root, 'ERD.md'), 'utf8').match(/```mermaid\n([\s\S]*?)```/)?.[1];
if (!source) { console.error('No ```mermaid block found in ERD.md'); process.exit(1); }

const work = mkdtempSync(join(tmpdir(), 'share-erd-'));
const escape = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
writeFileSync(join(work, 'erd.html'), `<!doctype html><html><body>
<pre class="mermaid">${escape(source)}</pre>
<script type="module">
import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
mermaid.initialize({ startOnLoad: false, theme: 'neutral', htmlLabels: false, er: { useMaxWidth: false } });
try { await mermaid.run(); window.rendered = 'ok'; } catch (e) { window.rendered = 'error: ' + e.message; }
</script></body></html>`);

const chrome = spawn('/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
  ['--headless=new', '--remote-debugging-port=9337', `--user-data-dir=${join(work, 'profile')}`, '--disable-gpu',
   '--allow-file-access-from-files', 'about:blank'], { stdio: 'ignore' });
const sleep = ms => new Promise(r => setTimeout(r, ms));
let target;
for (let i = 0; i < 50 && !target; i++) { await sleep(200); try { target = (await (await fetch('http://127.0.0.1:9337/json/list')).json()).find(t => t.type === 'page'); } catch {} }
const ws = new WebSocket(target.webSocketDebuggerUrl);
await new Promise(r => ws.onopen = r);
let id = 0; const waiting = new Map();
ws.onmessage = m => { const d = JSON.parse(m.data); if (d.id && waiting.has(d.id)) { waiting.get(d.id)(d); waiting.delete(d.id); } };
const send = (method, params = {}) => new Promise(r => { const i = ++id; waiting.set(i, r); ws.send(JSON.stringify({ id: i, method, params })); });
const evaluate = async expr => (await send('Runtime.evaluate', { expression: expr, returnByValue: true })).result.result.value;

await send('Page.navigate', { url: 'file://' + join(work, 'erd.html') });
let status;
for (let i = 0; i < 100 && !status; i++) { await sleep(200); status = await evaluate('window.rendered'); }
if (status !== 'ok') { console.error('Mermaid did not render:', status ?? 'timed out'); ws.close(); chrome.kill(); process.exit(1); }
const svg = await evaluate(`(() => { const s = document.querySelector('pre.mermaid svg'); s.setAttribute('xmlns', 'http://www.w3.org/2000/svg'); s.style.backgroundColor = '#ffffff'; return s.outerHTML; })()`);
writeFileSync(join(root, 'ERD.svg'), '<?xml version="1.0" encoding="UTF-8"?>\n' + svg);
ws.close(); chrome.kill();
console.log(`ERD.svg written (${Math.round(svg.length / 1024)} KB)`);
