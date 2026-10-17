const test = require('node:test');
const assert = require('node:assert/strict');
const {installSurfacePlacement} = require('../UnityProject/Assets/WebGLTemplates/WebXRTemplate/surface-session.js');

test('AR requires stable local tracking and hit-test; screen select places once; end removes listener', async () => {
  const session = new EventTarget();
  let calls = 0, options, selects = 0;
  const xr = {requestSession(mode, init) {calls++; options=init; return Promise.resolve(session);}};
  installSurfacePlacement(xr, {select(){selects++;}, failure(){assert.fail('Unexpected failure');}});
  const pending = xr.requestSession('immersive-ar', {requiredFeatures:['local'], optionalFeatures:['hit-test','hand-tracking']});
  assert.equal(calls,1,'native request happens synchronously, retaining user activation');
  assert.deepEqual(options.requiredFeatures,['local','hit-test']);
  assert.deepEqual(options.optionalFeatures,['hand-tracking']);
  assert.equal(await pending,session);
  session.dispatchEvent(new Event('select'));
  assert.equal(selects,1);
  session.dispatchEvent(new Event('end'));
  session.dispatchEvent(new Event('select'));
  assert.equal(selects,1,'a closed session cannot place again');
});

test('unsupported hit-test or denied camera reports failure and preserves rejection', async () => {
  const error = new Error('Not supported'); error.name='NotSupportedError';
  let reported;
  const xr={requestSession:()=>Promise.reject(error)};
  installSurfacePlacement(xr,{select(){assert.fail('No active session');},failure(e){reported=e;}});
  await assert.rejects(xr.requestSession('immersive-ar'),e=>e===error);
  assert.equal(reported,error);
});

test('VR is forwarded unchanged', async () => {
  const init={requiredFeatures:['local-floor']}; let forwarded;
  const session={};
  const xr={requestSession(mode,options){forwarded={mode,options};return Promise.resolve(session);}};
  installSurfacePlacement(xr,{});
  assert.equal(await xr.requestSession('immersive-vr',init),session);
  assert.deepEqual(forwarded,{mode:'immersive-vr',options:init});
});

test('desktop without WebXR is supported',()=>assert.doesNotThrow(()=>installSurfacePlacement(undefined,{})));
