import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DoughNeedTargetDto {
  day2Need: number;
  day3Need: number;
}

export interface TenantSettingsDto {
  id: string;
  name: string;
  logoUrl: string | null;
  primaryContactEmail: string | null;
  isActive: boolean;
  // Org-wide defaults new stores inherit (null → server falls back to a code literal).
  defaultTimezoneId: string | null;
  defaultOverdueGraceMinutes: number | null;
  defaultDepositDeadlineLocalTime: string | null;
  defaultTillABase: number | null;
  defaultTillBBase: number | null;
  defaultDoughNeedTargets: Record<string, DoughNeedTargetDto> | null;
  // Org display conventions honored app-wide (null → app default en-US / USD).
  localeCode: string | null;
  currencyCode: string | null;
}

export interface UpdateTenantSettingsRequest {
  name: string;
  logoUrl?: string | null;
  primaryContactEmail?: string | null;
  defaultTimezoneId?: string | null;
  defaultOverdueGraceMinutes?: number | null;
  defaultDepositDeadlineLocalTime?: string | null;
  defaultTillABase?: number | null;
  defaultTillBBase?: number | null;
  defaultDoughNeedTargets?: Record<string, DoughNeedTargetDto> | null;
  localeCode?: string | null;
  currencyCode?: string | null;
}

@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/tenant/settings';

  getSettings(): Observable<TenantSettingsDto> {
    return this.http.get<TenantSettingsDto>(this.base);
  }

  updateSettings(req: UpdateTenantSettingsRequest): Observable<void> {
    return this.http.put<void>(this.base, req);
  }
}
