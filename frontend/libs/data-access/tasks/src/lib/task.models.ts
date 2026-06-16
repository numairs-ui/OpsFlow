export type TaskStatus = 'Pending' | 'InProgress' | 'Completed' | 'Overdue' | 'Cancelled';

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
  checklistId: string;
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

export interface TaskTemplateItemDto {
  templateId: string;
  templateName: string;
  order: number;
  fieldsJson: string;
}

export interface TaskDetailDto {
  id: string;
  recurringAssignmentId?: string;
  recurringAssignmentName?: string;
  checklistId: string;
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
  checklistId: string;
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
  checklistId: string;
  storeId: string;
  dueAt: string;
  notes?: string;
}

export interface FieldSubmission {
  templateId: string;
  fieldId: string;
  value: string;
}

export interface CompleteTaskRequest {
  completedByVolunteerName?: string;
  fieldValues: FieldSubmission[];
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
}

export interface CancelTaskRequest {
  reason: string;
}

export interface DeferTaskRequest {
  reason: string;
  deferredTo: string;
}
