import { Injectable } from '@angular/core';

/**
 * Holds the org's display locale + currency, populated once at app-init from tenant settings.
 * The LOCALE_ID / DEFAULT_CURRENCY_CODE factories read from here, so the value must be set before
 * the first date/currency pipe renders (i.e. during provideAppInitializer). Because LOCALE_ID is
 * fixed for the lifetime of a bootstrapped app, changing the org locale only takes effect on a full
 * page load — hence the post-login hard navigation in the login component.
 */
@Injectable({ providedIn: 'root' })
export class LocaleConfigService {
  private locale: string | null = null;
  private currency: string | null = null;

  get localeCode(): string {
    return this.locale ?? 'en-US';
  }

  get currencyCode(): string {
    return this.currency ?? 'USD';
  }

  set(locale: string | null, currency: string | null): void {
    this.locale = locale;
    this.currency = currency;
  }
}
