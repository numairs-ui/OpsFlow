import type { TemplateScope } from './template.models.js';

export type ScoringType = 'PassFail' | 'Scale1To5';

export interface ChecklistItemInput {
  templateId: string;
  order: number;
  scoringType?: ScoringType | null;
  weight?: number;
  photoRequired?: boolean;
  failCorrectiveActionText?: string | null;
  failScoreThreshold?: number | null;
}

export interface ChecklistDto {
  id: string;
  name: string;
  description?: string;
  scope: TemplateScope;
  regionId?: string;
  regionName?: string;
  storeId?: string;
  storeName?: string;
  itemCount: number;
  firstThreeTemplateNames: string[];
  isActive: boolean;
  createdAt: string;
}

export interface ChecklistItemDto {
  templateId: string;
  templateName: string;
  order: number;
  fieldsJson?: string;
  scoringType?: ScoringType | null;
  weight?: number;
  photoRequired?: boolean;
  failCorrectiveActionText?: string | null;
  failScoreThreshold?: number | null;
}

export interface ChecklistDetailDto extends ChecklistDto {
  items: ChecklistItemDto[];
}

export interface CreateChecklistRequest {
  name: string;
  description?: string;
  scope: TemplateScope;
  regionId?: string;
  storeId?: string;
  items: ChecklistItemInput[];
}

export interface UpdateChecklistRequest {
  name: string;
  description?: string;
  scope: TemplateScope;
  regionId?: string;
  storeId?: string;
}

export interface DailyScoreDto {
  date: string;
  averageScorePercent: number;
  completionCount: number;
}

export interface RegionChecklistScoreDto {
  regionId: string;
  regionName: string;
  storeCount: number;
  averageScorePercent: number;
  failingCount: number;
  completionCount: number;
}

export interface StoreChecklistScoreDto {
  storeId: string;
  storeName: string;
  averageScorePercent: number;
  completionCount: number;
  failingCount: number;
  lastCompletedAt?: string;
}

export interface RecentCompletionDto {
  taskInstanceId: string;
  storeId: string;
  storeName: string;
  checklistName: string;
  scorePercent?: number | null;
  completedAt: string;
  completedByUserId?: string;
}

export interface ChecklistPerformanceDto {
  averageScorePercent: number;
  scoredCompletionCount: number;
  totalCompletionCount: number;
  failingCompletionCount: number;
  scoreTrend: DailyScoreDto[];
  regionBreakdown: RegionChecklistScoreDto[];
  storeBreakdown: StoreChecklistScoreDto[];
  recentCompletions: RecentCompletionDto[];
}
