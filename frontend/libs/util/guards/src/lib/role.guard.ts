import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '@org/data-access-auth';

export const roleGuard = (...allowedRoles: string[]): CanActivateFn =>
  () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    const user = auth.currentUser();
    if (!user) return router.parseUrl('/login');
    if (!allowedRoles.includes(user.role)) return router.parseUrl('/unauthorized');
    return true;
  };
