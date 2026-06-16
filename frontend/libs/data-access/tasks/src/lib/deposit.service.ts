import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import type { DepositLogDto, GetDepositLogResponse, RecordDepositRequest } from './deposit.models';

@Injectable({ providedIn: 'root' })
export class DepositService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/stores';

  recordDeposit(storeId: string, req: RecordDepositRequest): Observable<DepositLogDto> {
    return this.http.post<DepositLogDto>(`${this.base}/${storeId}/deposit-log`, req);
  }

  getDepositLog(storeId: string, options?: { from?: string; to?: string; page?: number; pageSize?: number }): Observable<GetDepositLogResponse> {
    let params = new HttpParams();
    if (options?.from) params = params.set('from', options.from);
    if (options?.to) params = params.set('to', options.to);
    if (options?.page) params = params.set('page', options.page);
    if (options?.pageSize) params = params.set('pageSize', options.pageSize);
    return this.http.get<GetDepositLogResponse>(`${this.base}/${storeId}/deposit-log`, { params });
  }

  getDepositByDate(storeId: string, date: string): Observable<DepositLogDto | null> {
    return this.http.get<DepositLogDto | null>(`${this.base}/${storeId}/deposit-log/${date}`);
  }
}
