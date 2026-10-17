// Keep XR input in the native session, including Android's unhanded screen taps.
// No DOM overlay or additional buttons are needed inside immersive AR.
(function (root) {
  function installSurfacePlacement(xr, handlers) {
    if (!xr) return;
    const requestSession = xr.requestSession.bind(xr);
    xr.requestSession = function (mode, init) {
      if (mode !== 'immersive-ar') return requestSession(mode, init);
      const required = [...new Set([...(init?.requiredFeatures || []), 'local', 'hit-test'])];
      const options = {...init, requiredFeatures:required,
        optionalFeatures:(init?.optionalFeatures || []).filter(feature => !required.includes(feature))};
      // Call immediately so requestSession retains the button's user activation.
      return requestSession(mode, options).then(session => {
        const select = () => handlers.select();
        session.addEventListener('select', select);
        session.addEventListener('end', () => session.removeEventListener('select', select), {once:true});
        return session;
      }).catch(error => { handlers.failure(error); throw error; });
    };
  }
  if (typeof module !== 'undefined' && module.exports) module.exports = {installSurfacePlacement};
  else root.installSurfacePlacement = installSurfacePlacement;
})(typeof window !== 'undefined' ? window : globalThis);
