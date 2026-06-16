import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TenantSettingsDto {
  id: string;
  name: string;
  logoUrl: string | null;
  primaryContactEmail: string | null;
  isActive: boolean;
}

export interface UpdateTenantSettingsRequest {
  name: string;
  logoUrl?: string | null;
  primaryContactEmail?: string | null;
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
