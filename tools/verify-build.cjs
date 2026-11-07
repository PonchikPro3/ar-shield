const fs = require('node:fs');
const path = require('node:path');
const https = require('node:https');
const zlib = require('node:zlib');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const project = path.resolve(__dirname, '..');
const build = path.join(project,'WebGL');
const base = process.argv[2] || 'https://localhost:8443/';
const isLocal = new URL(base).hostname === 'localhost';
const ca = isLocal ? new crypto.X509Certificate(fs.readFileSync(path.join(project,'.certs/rootCA.cer'))).toString() : undefined;
function request(url, method='GET') {
  return new Promise((resolve,reject)=>https.get(new URL(url.replace(/^\//,''),base),{ca,method},res=>{
    const chunks=[];res.on('data',c=>chunks.push(c));
    res.on('end',()=>resolve({status:res.statusCode,headers:res.headers,body:Buffer.concat(chunks)}));
  }).on('error',reject));
}
(async()=>{
  const settings=fs.readFileSync(path.join(project,'UnityProject/ProjectSettings/ProjectSettings.asset'),'utf8');
  assert.match(settings,/webGLThreadsSupport: 0/);
  assert.match(settings,/webGLDecompressionFallback: 0/);
  assert.match(settings,/webGLCompressionFormat: 2/);
  const evidence=JSON.parse(fs.readFileSync(path.join(project,'reports/unity-build.json')));
  assert.equal(evidence.result,'Succeeded');
  const scene=fs.readFileSync(path.join(project,'UnityProject/Assets/Scenes/ViewerSpace.unity'),'utf8');
  for (const name of ['WebXRManager','WebXRCameraSet','WebXRController LEFT','WebXRController RIGHT','CameraARL','CameraARR']) assert.ok(scene.includes(name),name);
  const home=await request('/');assert.equal(home.status,200);
  const html=home.body.toString();assert.ok(!html.includes('{{{'));assert.ok(!html.includes('mindar'));
  const files=[];
  for (const name of fs.readdirSync(path.join(build,'Build'))) {
    assert.ok(!name.endsWith('.gz') && !name.endsWith('.br'), 'Pages build must not require Content-Encoding');
    const disk=fs.readFileSync(path.join(build,'Build',name));
    const served=await request('/Build/'+name);
    assert.equal(served.status,200);assert.deepEqual(served.body,disk);
    if(name.endsWith('.wasm')) {
      assert.equal(served.headers['content-type'],'application/wasm');
      assert.equal(disk.subarray(0,4).toString('hex'),'0061736d');
    }
    files.push({name,bytes:disk.length,sha256:crypto.createHash('sha256').update(disk).digest('hex')});
  }
  assert.equal((await request('/missing-file')).status,404);
  if (isLocal) {
    assert.equal((await request('/%2e%2e/.certs/server-key.pem')).status,404);
    assert.equal((await request('/ca.cer')).status,200);
  }
  const conversion=JSON.parse(fs.readFileSync(path.join(project,'UnityProject/Assets/Models/conversion.json')));
  assert.equal(conversion.source,'assets/sunny_kitty.glb');
  assert.equal(conversion.sha256,crypto.createHash('sha256').update(fs.readFileSync(path.join(project,conversion.source))).digest('hex'));
  const report={date:new Date().toISOString(),url:base,unity:evidence,model:conversion,files,buildBytes:files.reduce((n,f)=>n+f.bytes,0),
    checks:['Unity build succeeded','WebXR camera rig and two controllers saved in scene','Compression disabled for Pages','Compression fallback off','Threads off','TLS verified','WASM MIME and magic verified','Served bytes match build','Sunny Kitty GLB source hash verified'],
    mobile:fs.existsSync(path.join(project,'reports/mobile-test.json')) ? JSON.parse(fs.readFileSync(path.join(project,'reports/mobile-test.json'))) :
      {status:'Pending physical Android Chrome test',model:null,browser:null}};
  fs.mkdirSync(path.join(project,'reports'),{recursive:true});
  fs.writeFileSync(path.join(project,isLocal?'reports/verification.json':'reports/pages-verification.json'),JSON.stringify(report,null,2));
  console.log(JSON.stringify(report,null,2));
})().catch(e=>{console.error(e);process.exitCode=1;});
