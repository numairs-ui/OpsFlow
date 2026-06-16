import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type {
  CreateFormSubmissionRequest, FormSubmissionDetailDto, FormSubmissionSummaryDto,
  MySubmissionDto, PendingReviewDto, SubmitFormSubmissionRequest,
} from './form-submission.models.js';

@Injectable({ providedIn: 'root' })
export class FormSubmissionService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/form-submissions';

  createSubmission(body: CreateFormSubmissionRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, body);
  }

  submitSubmission(id: string, body?: SubmitFormSubmissionRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/submit`, body ?? {});
  }

  approve(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/approve`, null);
  }

  reject(id: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/reject`, { reason });
  }

  return(id: string, comments: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/return`, { comments });
  }

  getMySubmissions(status?: string): Observable<MySubmissionDto[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<MySubmissionDto[]>(`${this.base}/my-submissions`, { params });
  }

  getPendingReview(): Observable<PendingReviewDto[]> {
    return this.http.get<PendingReviewDto[]>(`${this.base}/pending-review`);
  }

  getSubmission(id: string): Observable<FormSubmissionDetailDto> {
    return this.http.get<FormSubmissionDetailDto>(`${this.base}/${id}`);
  }

  getSubmissions(storeId?: string, regionId?: string, status?: string): Observable<FormSubmissionSummaryDto[]> {
    let params = new HttpParams();
    if (storeId) params = params.set('storeId', storeId);
    if (regionId) params = params.set('regionId', regionId);
    if (status) params = params.set('status', status);
    return this.http.get<FormSubmissionSummaryDto[]>(this.base, { params });
  }
}
