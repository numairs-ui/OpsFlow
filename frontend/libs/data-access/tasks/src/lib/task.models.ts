export type TaskStatus =
  | 'Pending'
  | 'InProgress'
  | 'Completed'
  | 'Overdue'
  | 'Verified'
  | 'Cancelled'
  | 'Deferred'
  | 'CorrectiveActionRaised';

export interface TaskBoardItemDto {
  id: string;
  dueAt: string;
  status: TaskStatus;
  assignedToUserId?: string;
  isAdHoc: boolean;
  recurringAssignmentName?: string;
  createdAt: string;
}

export interface TaskGroupDto {
  checklistId: string | null;
  checklistName: string;
  totalCount: number;
  completedCount: number;
  tasks: TaskBoardItemDto[];
}

export interface TodayTasksDto {
  date: string;
  storeId: string;
  storeName: string;
  totalCount: number;
  completedCount: number;
  taskGroups: TaskGroupDto[];
}

export type ScoringType = 'PassFail' | 'Scale1To5';

export interface TaskTemplateItemDto {
  templateId: string;
  templateName: string;
  order: number;
  fieldsJson: string;
  scoringType?: ScoringType | null;
  photoRequired?: boolean;
  failScoreThreshold?: number | null;
}

export interface TaskDetailDto {
  id: string;
  recurringAssignmentId?: string;
  recurringAssignmentName?: string;
  checklistId: string | null;
  checklistName: string;
  checklistDescription?: string;
  storeId: string;
  storeName: string;
  dueAt: string;
  status: TaskStatus;
  assignedToUserId?: string;
  notes?: string;
  isAdHoc: boolean;
  templates: TaskTemplateItemDto[];
  createdAt: string;
  isMdog: boolean;
  previousValues: Record<string, number>;
}

export interface TaskInstanceDto {
  id: string;
  recurringAssignmentId?: string;
  recurringAssignmentName?: string;
  checklistId: string | null;
  checklistName: string;
  storeId: string;
  storeName: string;
  dueAt: string;
  status: TaskStatus;
  assignedToUserId?: string;
  completedByUserId?: string;
  completedAt?: string;
  notes?: string;
  isAdHoc: boolean;
  createdAt: string;
}

export interface CreateTaskRequest {
  storeId: string;
  dueAt: string;
  notes?: string;
  /** Checklist-backed task. Mutually exclusive with taskTemplateId. */
  checklistId?: string;
  /** Standalone task against a single template's fields. Mutually exclusive with checklistId. */
  taskTemplateId?: string;
  /** Optional specific assignee. */
  assignedToUserId?: string;
}

export interface FieldSubmission {
  templateId: string;
  fieldId: string;
  value: string;
}

export interface ItemScoreSubmission {
  templateId: string;
  score: number;
  photoUrl?: string;
}

export interface CompleteTaskRequest {
  completedByVolunteerName?: string;
  fieldValues: FieldSubmission[];
  itemScores?: ItemScoreSubmission[];
}

export interface CorrectiveActionDto {
  fieldLabel: string;
  text: string;
}

export interface TaskCompletionResultDto {
  id: string;
  taskInstanceId: string;
  completedByUserId?: string;
  completedByVolunteerName?: string;
  completedAt: string;
}

export interface CompleteTaskResponse {
  completion: TaskCompletionResultDto;
  triggeredCorrectiveActions: CorrectiveActionDto[];
  compositeScorePercent?: number | null;
  spawnedCorrectiveTaskIds?: string[] | null;
}

export interface TaskStatsDto {
  openToday: number;
  upcomingCount: number;
  overdueCount: number;
  correctiveActionCount: number;
  completedToday: number;
  completionRateToday: number;
}

export interface CancelTaskRequest {
  reason: string;
}

export interface DeferTaskRequest {
  reason: string;
  deferredTo: string;
}
