import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type {
  CreateTemplateRequest, TemplateDetailDto, TemplateFilter,
  TemplateListResult, UpdateTemplateRequest,
} from './template.models.js';
import type { ImportTemplateRequest, ImportTemplatesResult } from './import.models.js';

@Injectable({ providedIn: 'root' })
export class TemplateService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api';

  getTemplates(filter: TemplateFilter = {}): Observable<TemplateListResult> {
    let params = new HttpParams();
    if (filter.scope) params = params.set('scope', filter.scope);
    if (filter.category) params = params.set('category', filter.category);
    if (filter.isActive !== undefined) params = params.set('isActive', filter.isActive);
    if (filter.search) params = params.set('search', filter.search);
    if (filter.page) params = params.set('page', filter.page);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);
    return this.http.get<TemplateListResult>(`${this.base}/templates`, { params });
  }

  getTemplate(id: string): Observable<TemplateDetailDto> {
    return this.http.get<TemplateDetailDto>(`${this.base}/templates/${id}`);
  }

  createTemplate(body: CreateTemplateRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/templates`, body);
  }

  updateTemplate(id: string, body: UpdateTemplateRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/templates/${id}`, body);
  }

  deactivateTemplate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/templates/${id}/deactivate`, null);
  }

  activateTemplate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/templates/${id}/activate`, null);
  }

  importTemplates(req: ImportTemplateRequest): Observable<ImportTemplatesResult> {
    return this.http.post<ImportTemplatesResult>(`${this.base}/templates/import`, req);
  }
}
