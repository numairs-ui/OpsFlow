import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type { Region, Store, StoreAssignment, StoreEmployee, User } from './org.models.js';

@Injectable({ providedIn: 'root' })
export class OrgService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  // ── Regions ──────────────────────────────────────────────────────────────────

  getRegions(activeOnly = true): Observable<Region[]> {
    return this.http.get<Region[]>(`${this.base}/regions`, {
      params: new HttpParams().set('activeOnly', activeOnly),
    });
  }

  createRegion(body: { name: string; description?: string }): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/regions`, body);
  }

  updateRegion(id: string, body: { name: string; description?: string }): Observable<void> {
    return this.http.put<void>(`${this.base}/regions/${id}`, body);
  }

  deactivateRegion(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/regions/${id}/deactivate`, null);
  }

  // ── Stores ───────────────────────────────────────────────────────────────────

  getStores(regionId?: string, activeOnly = true): Observable<Store[]> {
    let params = new HttpParams().set('activeOnly', activeOnly);
    if (regionId) params = params.set('regionId', regionId);
    return this.http.get<Store[]>(`${this.base}/stores`, { params });
  }

  createStore(body: { name: string; address?: string; regionId: string }): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/stores`, body);
  }

  updateStore(id: string, body: { name: string; address?: string; regionId: string }): Observable<void> {
    return this.http.put<void>(`${this.base}/stores/${id}`, body);
  }

  deactivateStore(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/stores/${id}/deactivate`, null);
  }

  // ── Users ────────────────────────────────────────────────────────────────────

  getUsers(filters?: { role?: string; storeId?: string; activeOnly?: boolean }): Observable<User[]> {
    let params = new HttpParams().set('activeOnly', filters?.activeOnly ?? true);
    if (filters?.role) params = params.set('role', filters.role);
    if (filters?.storeId) params = params.set('storeId', filters.storeId);
    return this.http.get<User[]>(`${this.base}/users`, { params });
  }

  getUser(userId: string): Observable<User> {
    return this.http.get<User>(`${this.base}/users/${userId}`);
  }

  createUser(body: {
    email: string; password: string; displayName: string;
    role: string; storeId?: string; regionId?: string;
  }): Observable<{ userId: string }> {
    return this.http.post<{ userId: string }>(`${this.base}/users`, body);
  }

  updateUser(userId: string, body: {
    displayName: string; role: string; storeId?: string; regionId?: string;
  }): Observable<void> {
    return this.http.put<void>(`${this.base}/users/${userId}`, body);
  }

  deactivateUser(userId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/users/${userId}/deactivate`, null);
  }

  reactivateUser(userId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/users/${userId}/reactivate`, null);
  }

  getStoreAssignments(userId: string): Observable<StoreAssignment[]> {
    return this.http.get<StoreAssignment[]>(`${this.base}/users/${userId}/store-assignments`);
  }

  addStoreAssignment(userId: string, storeId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/users/${userId}/store-assignments`, { storeId });
  }

  removeStoreAssignment(userId: string, storeId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/users/${userId}/store-assignments/${storeId}`);
  }

  // ── Store Roster ──────────────────────────────────────────────────────────────

  getStoreEmployees(storeId: string): Observable<StoreEmployee[]> {
    return this.http.get<StoreEmployee[]>(`${this.base}/stores/${storeId}/employees`);
  }
}
