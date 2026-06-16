import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type {
  InventoryHistoryDto, InventorySnapshotDto,
  StoreSettingsDto, UpdateStoreSettingsRequest,
} from './inventory.models.js';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  getLatestInventory(storeId: string): Observable<InventorySnapshotDto[]> {
    return this.http.get<InventorySnapshotDto[]>(`${this.base}/stores/${storeId}/inventory/latest`);
  }

  getInventoryHistory(storeId: string, days = 7): Observable<InventoryHistoryDto[]> {
    const params = new HttpParams().set('days', days);
    return this.http.get<InventoryHistoryDto[]>(`${this.base}/stores/${storeId}/inventory/history`, { params });
  }

  getStoreSettings(storeId: string): Observable<StoreSettingsDto> {
    return this.http.get<StoreSettingsDto>(`${this.base}/stores/${storeId}/settings`);
  }

  updateStoreSettings(storeId: string, body: UpdateStoreSettingsRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/stores/${storeId}/settings`, body);
  }
}
