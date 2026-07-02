// Runtime config, loaded before the app bundle. Deployed builds overwrite this file
// (or Vercel serves an environment-specific copy) to point at the live API — no rebuild needed.
// Leave apiOrigin empty for local dev; the dev-server proxy (proxy.conf.json) handles /api locally.
window.__OPSFLOW_ENV__ = {
  apiOrigin: '',
};
