export interface DepositLogDto {
  id: string;
  storeId: string;
  amount: number;
  submittedByManagerId: string;
  submittedAt: string;
}

export interface RecordDepositRequest {
  amount: number;
}

export interface GetDepositLogResponse {
  items: DepositLogDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}
