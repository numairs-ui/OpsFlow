export interface InventorySnapshotDto {
  itemKey: string;
  onHandCount: number;
  date: string;
  submittedByUserId?: string;
  updatedAt: string;
}

export interface InventoryHistoryDto {
  date: string;
  itemKey: string;
  onHandCount: number;
  submittedByUserId?: string;
}

export interface DoughNeedTargetDto {
  day2Need: number;
  day3Need: number;
}

export interface StoreSettingsDto {
  storeId: string;
  tillABase?: number;
  tillBBase?: number;
  doughNeedTargets: Record<string, DoughNeedTargetDto>;
  timezoneId: string;
  overdueGraceMinutes: number;
}

export interface UpdateStoreSettingsRequest {
  tillABase?: number;
  tillBBase?: number;
  doughNeedTargets: Record<string, DoughNeedTargetDto>;
  timezoneId: string;
  overdueGraceMinutes: number;
}
