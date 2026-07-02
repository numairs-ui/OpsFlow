import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { authInterceptor, provideAuthInitializer } from '@org/data-access-auth';
import { apiOriginInterceptor } from '@org/util-http';
import { appRoutes } from './app.routes.js';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(appRoutes),
    provideHttpClient(withInterceptors([apiOriginInterceptor, authInterceptor])),
    provideAuthInitializer(),
  ],
};
