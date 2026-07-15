import {
  ApplicationConfig,
  DEFAULT_CURRENCY_CODE,
  LOCALE_ID,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeGb from '@angular/common/locales/en-GB';
import localeCa from '@angular/common/locales/en-CA';
import localeAu from '@angular/common/locales/en-AU';
import localeIe from '@angular/common/locales/en-IE';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService, authInterceptor } from '@org/data-access-auth';
import { TenantService } from '@org/data-access-org';
import { apiOriginInterceptor } from '@org/util-http';
import { appRoutes } from './app.routes.js';
import { LocaleConfigService } from './core/locale-config.service.js';

// Locale data for the curated supported set (en-US is built in).
registerLocaleData(localeGb);
registerLocaleData(localeCa);
registerLocaleData(localeAu);
registerLocaleData(localeIe);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(appRoutes),
    provideHttpClient(withInterceptors([apiOriginInterceptor, authInterceptor])),
    // Restore the session, then (if authenticated) load the org's locale/currency before the first
    // pipe renders. Replaces the standalone auth initializer.
    provideAppInitializer(() => {
      const auth = inject(AuthService);
      const tenant = inject(TenantService);
      const locale = inject(LocaleConfigService);
      return auth.refresh().then(async (ok) => {
        if (!ok) return;
        try {
          const s = await firstValueFrom(tenant.getSettings());
          locale.set(s.localeCode, s.currencyCode);
        } catch {
          // No tenant settings / not permitted — keep app defaults (en-US / USD).
        }
      });
    }),
    { provide: LOCALE_ID, useFactory: (l: LocaleConfigService) => l.localeCode, deps: [LocaleConfigService] },
    { provide: DEFAULT_CURRENCY_CODE, useFactory: (l: LocaleConfigService) => l.currencyCode, deps: [LocaleConfigService] },
  ],
};
