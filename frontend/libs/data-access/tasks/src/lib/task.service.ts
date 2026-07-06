import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type {
  CancelTaskRequest, CompleteTaskRequest, CompleteTaskResponse,
  CreateTaskRequest, DeferTaskRequest,
  TaskDetailDto, TaskInstanceDto, TaskStatus, TodayTasksDto,
} from './task.models.js';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  getTodayTasks(storeId: string): Observable<TodayTasksDto> {
    return this.http.get<TodayTasksDto>(`${this.base}/stores/${storeId}/tasks/today`);
  }

  getTask(taskId: string): Observable<TaskDetailDto> {
    return this.http.get<TaskDetailDto>(`${this.base}/tasks/${taskId}`);
  }

  getTasks(
    storeId?: string,
    status?: TaskStatus,
    from?: string,
    to?: string,
    statuses?: TaskStatus[]
  ): Observable<TaskInstanceDto[]> {
    let params = new HttpParams();
    if (storeId) params = params.set('storeId', storeId);
    if (status) params = params.set('status', status);
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    if (statuses?.length) params = params.set('statuses', statuses.join(','));
    return this.http.get<TaskInstanceDto[]>(`${this.base}/tasks`, { params });
  }

  createTask(body: CreateTaskRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/tasks`, body);
  }

  claimTask(taskId: string, volunteerName?: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/${taskId}/claim`, { volunteerName });
  }

  startTask(taskId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/${taskId}/start`, null);
  }

  completeTask(taskId: string, body: CompleteTaskRequest): Observable<CompleteTaskResponse> {
    return this.http.post<CompleteTaskResponse>(`${this.base}/tasks/${taskId}/complete`, body);
  }

  verifyTask(taskId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/${taskId}/verify`, null);
  }

  cancelTask(taskId: string, body: CancelTaskRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/${taskId}/cancel`, body);
  }

  deferTask(taskId: string, body: DeferTaskRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/tasks/${taskId}/defer`, body);
  }

  assignTask(taskId: string, assignedToUserId: string | null): Observable<void> {
    return this.http.patch<void>(`${this.base}/tasks/${taskId}/assign`, { assignedToUserId });
  }
}
