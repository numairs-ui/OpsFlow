import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '@org/data-access-auth';

export const roleGuard = (requiredRole: string): CanActivateFn =>
  () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    const user = auth.currentUser();
    if (!user) return router.parseUrl('/login');
    if (user.role !== requiredRole) return router.parseUrl('/unauthorized');
    return true;
  };
