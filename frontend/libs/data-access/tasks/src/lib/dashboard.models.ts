export interface OverdueTaskSummary {
  id: string;
  name: string;
  dueAt: string;
  status: string;
  elapsedMinutes: number;
}

export interface StoreDashboardDto {
  completionRate: number;
  openCount: number;
  overdueCount: number;
  activeCorrectiveActionCount: number;
  depositLoggedToday: boolean;
  depositAmount?: number;
  overdueTasks: OverdueTaskSummary[];
}

export interface StoreScoreDto {
  storeId: string;
  name: string;
  completionRate: number;
  openCount: number;
  overdueCount: number;
  correctiveActionCount: number;
  depositLoggedToday: boolean;
  compositeScore: number;
}

export interface RegionDashboardDto {
  stores: StoreScoreDto[];
}

export interface MissedDepositStore {
  storeId: string;
  storeName: string;
}

export interface RegionalSummaryDto {
  regionId: string;
  regionName: string;
  storeCount: number;
  averageCompletionRate: number;
  criticalAlertCount: number;
}

export interface SystemDashboardDto {
  systemCompletionRate: number;
  totalOpenCount: number;
  totalOverdueCount: number;
  storesWithMissedDeposits: MissedDepositStore[];
  regionalSummary: RegionalSummaryDto[];
}

export interface MyCompletionDto {
  taskId: string;
  taskName: string;
  status: string;
  completedAt: string;
}
