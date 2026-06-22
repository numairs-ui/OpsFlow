import { provideAppInitializer, inject } from '@angular/core';
import { AuthService } from './auth.service.js';

export function provideAuthInitializer() {
  return provideAppInitializer(() => {
    const auth = inject(AuthService);
    return auth.refresh();
  });
}
