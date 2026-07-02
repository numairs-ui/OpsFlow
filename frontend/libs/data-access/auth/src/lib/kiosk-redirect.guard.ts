import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service.js';

/**
 * Confines the shared store_kiosk station account to the kiosk board. A kiosk login has no
 * personal identity and must not reach individual views (task detail, personal submissions,
 * quick-template). Individual roles pass through untouched.
 */
export const kioskRedirectGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.currentUser()?.role === 'store_kiosk') return router.createUrlTree(['/kiosk']);
  return true;
};
