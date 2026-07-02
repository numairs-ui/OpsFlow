import { HttpClient } from '@angular/common/http';
import { Injectable, Signal, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import type { CurrentUser, LoginRequest, LoginResponse } from './auth.models.js';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _accessToken = signal<string | null>(null);

  readonly accessToken: Signal<string | null> = this._accessToken.asReadonly();

  readonly currentUser: Signal<CurrentUser | null> = computed(() => {
    const token = this._accessToken();
    return token ? this.decodeJwt(token) : null;
  });

  readonly isAuthenticated: Signal<boolean> = computed(
    () => this._accessToken() !== null
  );

  async login(request: LoginRequest): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<LoginResponse>('/api/auth/login', request, {
        withCredentials: true,
      })
    );
    this._accessToken.set(response.accessToken);
  }

  async refresh(): Promise<boolean> {
    try {
      const response = await firstValueFrom(
        this.http.post<LoginResponse>('/api/auth/refresh', null, {
          withCredentials: true,
        })
      );
      this._accessToken.set(response.accessToken);
      return true;
    } catch {
      this._accessToken.set(null);
      return false;
    }
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(
        this.http.post('/api/auth/logout', null, { withCredentials: true })
      );
    } finally {
      this._accessToken.set(null);
      await this.router.navigate(['/login']);
    }
  }

  private decodeJwt(token: string): CurrentUser | null {
    try {
      const payload = token.split('.')[1];
      const decoded = JSON.parse(atob(payload));
      // The JWT carries one "regionId" claim per region: a string when single, an array when many.
      const rawRegion = decoded.regionId;
      const regionIds: string[] = Array.isArray(rawRegion)
        ? rawRegion
        : rawRegion
          ? [rawRegion]
          : [];
      return {
        sub: decoded.sub,
        tenantId: decoded.tenantId,
        role: decoded.role,
        storeId: decoded.storeId,
        regionId: regionIds[0],
        regionIds,
      };
    } catch {
      return null;
    }
  }
}
