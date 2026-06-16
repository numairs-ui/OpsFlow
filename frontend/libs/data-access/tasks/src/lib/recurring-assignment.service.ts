import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type { CreateRecurringAssignmentRequest, RecurringAssignmentDto } from './recurring-assignment.models.js';

@Injectable({ providedIn: 'root' })
export class RecurringAssignmentService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  getRecurringAssignments(storeId?: string, isPaused?: boolean): Observable<RecurringAssignmentDto[]> {
    let params = new HttpParams();
    if (storeId) params = params.set('storeId', storeId);
    if (isPaused !== undefined) params = params.set('isPaused', isPaused);
    return this.http.get<RecurringAssignmentDto[]>(`${this.base}/recurring-assignments`, { params });
  }

  createRecurringAssignment(body: CreateRecurringAssignmentRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/recurring-assignments`, body);
  }

  pause(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/recurring-assignments/${id}/pause`, null);
  }

  resume(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/recurring-assignments/${id}/resume`, null);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/recurring-assignments/${id}`);
  }
}
