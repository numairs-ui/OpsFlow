import { HttpInterceptorFn } from '@angular/common/http';

declare global {
  interface Window {
    __OPSFLOW_ENV__?: { apiOrigin?: string };
  }
}

/**
 * Prepends the deployed API's origin to relative `/api/...` requests. In dev, apiOrigin is empty
 * and the request passes through unchanged — the dev-server proxy (proxy.conf.json) handles it.
 * The origin is read from window.__OPSFLOW_ENV__, set by each app's public/env.js, so the same
 * build artifact can be pointed at a different backend without rebuilding.
 */
export const apiOriginInterceptor: HttpInterceptorFn = (req, next) => {
  const apiOrigin = window.__OPSFLOW_ENV__?.apiOrigin;

  if (!apiOrigin || !req.url.startsWith('/api')) {
    return next(req);
  }

  return next(req.clone({ url: `${apiOrigin}${req.url}` }));
};
