import { HttpInterceptorFn } from '@angular/common/http';

declare global {
  interface Window {
    __OPSFLOW_ENV__?: { apiOrigin?: string };
  }
}

/**
 * Rewrites relative `/api/...` requests to the deployed API's origin. In dev, apiOrigin is empty
 * and the request passes through unchanged — the dev-server proxy (proxy.conf.json) strips the
 * `/api` prefix and forwards to the local API. In production there's no proxy, so this interceptor
 * does the same rewrite: strip `/api`, prepend apiOrigin. The origin is read from
 * window.__OPSFLOW_ENV__, set by each app's public/env.js, so the same build artifact can be
 * pointed at a different backend without rebuilding.
 */
export const apiOriginInterceptor: HttpInterceptorFn = (req, next) => {
  const apiOrigin = window.__OPSFLOW_ENV__?.apiOrigin;

  if (!apiOrigin || !req.url.startsWith('/api')) {
    return next(req);
  }

  return next(req.clone({ url: `${apiOrigin}${req.url.slice('/api'.length)}` }));
};
