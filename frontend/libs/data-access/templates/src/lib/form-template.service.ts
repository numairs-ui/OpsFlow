import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type {
  CreateFormTemplateRequest, FormTemplateDetailDto, FormTemplateFilter,
  FormTemplateListResult, UpdateFormTemplateRequest,
} from './form-template.models.js';

@Injectable({ providedIn: 'root' })
export class FormTemplateService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  getFormTemplates(filter: FormTemplateFilter = {}): Observable<FormTemplateListResult> {
    let params = new HttpParams();
    if (filter.scope) params = params.set('scope', filter.scope);
    if (filter.propagationType) params = params.set('propagationType', filter.propagationType);
    if (filter.isActive !== undefined) params = params.set('isActive', filter.isActive);
    if (filter.search) params = params.set('search', filter.search);
    if (filter.page) params = params.set('page', filter.page);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);
    return this.http.get<FormTemplateListResult>(`${this.base}/form-templates`, { params });
  }

  getFormTemplate(id: string): Observable<FormTemplateDetailDto> {
    return this.http.get<FormTemplateDetailDto>(`${this.base}/form-templates/${id}`);
  }

  createFormTemplate(body: CreateFormTemplateRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/form-templates`, body);
  }

  updateFormTemplate(id: string, body: UpdateFormTemplateRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/form-templates/${id}`, body);
  }

  deactivateFormTemplate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/form-templates/${id}/deactivate`, null);
  }

  activateFormTemplate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/form-templates/${id}/activate`, null);
  }
}
