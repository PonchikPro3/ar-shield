const https = require('node:https');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../WebGL');
const certs = path.resolve(__dirname, '../.certs');
const port = Number(process.env.PORT || 8443);
const types = {'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.json':'application/json','.png':'image/png','.css':'text/css','.cer':'application/pkix-cert'};
function serve(req,res) {
  if (!['GET','HEAD'].includes(req.method)) {res.writeHead(405);res.end();return;}
  let name;
  try {name=decodeURIComponent(new URL(req.url,'https://localhost').pathname);} catch {res.writeHead(400);res.end();return;}
  const file = name==='/ca.cer' ? path.join(certs,'rootCA.cer') : path.resolve(root, '.'+(name==='/'?'/index.html':name));
  if (name!=='/ca.cer' && !file.startsWith(root+path.sep)) {res.writeHead(403);res.end();return;}
  fs.stat(file,(err,stat)=>{
    if(err || !stat.isFile()) {res.writeHead(404);res.end('Not found');return;}
    const gzip=file.endsWith('.gz');
    const extension=path.extname(gzip?file.slice(0,-3):file);
    res.setHeader('Content-Type', types[extension]||'application/octet-stream');
    res.setHeader('Content-Length', stat.size);
    res.setHeader('X-Content-Type-Options','nosniff');
    res.setHeader('Permissions-Policy','xr-spatial-tracking=(self), camera=(self)');
    res.setHeader('Cache-Control','no-store');
    if (gzip) res.setHeader('Content-Encoding','gzip');
    if(req.method==='HEAD') {res.end();return;}
    const stream=fs.createReadStream(file);stream.on('error',()=>res.destroy());stream.pipe(res);
  });
}
https.createServer({key:fs.readFileSync(path.join(certs,'server-key.pem')),cert:fs.readFileSync(path.join(certs,'server.pem'))}, serve)
  .listen(port,'0.0.0.0',()=>console.log(`AR Shield: https://localhost:${port}/ (LAN HTTPS enabled)`));
// Loopback is a secure context too; useful for desktop preview without CA setup.
require('node:http').createServer(serve).listen(8080,'127.0.0.1',()=>console.log('Desktop preview: http://localhost:8080/'));
