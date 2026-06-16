import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type {
  ChecklistDetailDto, ChecklistDto, ChecklistItemInput, CreateChecklistRequest,
} from './checklist.models.js';

@Injectable({ providedIn: 'root' })
export class ChecklistService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  getChecklists(scope?: string, isActive?: boolean, search?: string): Observable<ChecklistDto[]> {
    let params = new HttpParams();
    if (scope) params = params.set('scope', scope);
    if (isActive !== undefined) params = params.set('isActive', isActive);
    if (search) params = params.set('search', search);
    return this.http.get<ChecklistDto[]>(`${this.base}/checklists`, { params });
  }

  getChecklist(id: string): Observable<ChecklistDetailDto> {
    return this.http.get<ChecklistDetailDto>(`${this.base}/checklists/${id}`);
  }

  createChecklist(body: CreateChecklistRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/checklists`, body);
  }

  updateItems(id: string, items: ChecklistItemInput[]): Observable<void> {
    return this.http.put<void>(`${this.base}/checklists/${id}/items`, items);
  }

  deactivateChecklist(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/checklists/${id}/deactivate`, null);
  }

  activateChecklist(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/checklists/${id}/activate`, null);
  }
}
