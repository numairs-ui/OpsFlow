import {
  HttpContextToken,
  HttpErrorResponse,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service.js';

const RETRIED_AFTER_REFRESH = new HttpContextToken(() => false);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const isAuthEndpoint = req.url.includes('/auth/');
  const alreadyRetried = req.context.get(RETRIED_AFTER_REFRESH);
  const token = auth.accessToken();

  const withToken =
    token && !isAuthEndpoint ? addBearerToken(req, token) : req;

  return next(withToken).pipe(
    catchError((err) => {
      const shouldRefresh =
        err instanceof HttpErrorResponse &&
        err.status === 401 &&
        !isAuthEndpoint &&
        !alreadyRetried;

      if (!shouldRefresh) {
        return throwError(() => err);
      }

      return from(auth.refresh()).pipe(
        switchMap((ok) => {
          if (!ok) {
            router.navigate(['/login']);
            return throwError(() => err);
          }
          const retried = addBearerToken(req, auth.accessToken()!).clone({
            context: req.context.set(RETRIED_AFTER_REFRESH, true),
          });
          return next(retried);
        })
      );
    })
  );
};

function addBearerToken(req: HttpRequest<unknown>, token: string) {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
