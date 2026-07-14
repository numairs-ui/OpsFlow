export interface RecurringAssignmentDto {
  id: string;
  name: string;
  checklistId: string;
  checklistName: string;
  storeId: string;
  storeName: string;
  cronExpression: string;
  startsAt: string;
  endsAt?: string;
  isPaused: boolean;
  taskInstanceCount: number;
  createdAt: string;
  assignedToUserId?: string;
  assignedToUserName?: string;
}

export interface CreateRecurringAssignmentRequest {
  name: string;
  checklistId: string;
  storeId: string;
  cronExpression: string;
  startsAt: string;
  endsAt?: string;
  assignedToUserId?: string;
}
