import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import type {
  MyCompletionDto,
  RegionDashboardDto,
  StoreDashboardDto,
  SystemDashboardDto,
} from './dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  getStoreDashboard(storeId: string): Observable<StoreDashboardDto> {
    return this.http.get<StoreDashboardDto>(`/api/dashboard/store/${storeId}`);
  }

  getRegionDashboard(regionId: string): Observable<RegionDashboardDto> {
    return this.http.get<RegionDashboardDto>(`/api/dashboard/region/${regionId}`);
  }

  getSystemDashboard(): Observable<SystemDashboardDto> {
    return this.http.get<SystemDashboardDto>('/api/dashboard/system');
  }

  getMyCompletions(days = 7): Observable<MyCompletionDto[]> {
    return this.http.get<MyCompletionDto[]>('/api/users/me/completions', {
      params: new HttpParams().set('days', days),
    });
  }
}
