export interface RecurringAssignmentTarget {
  storeId: string;
  storeName: string;
}

export interface RecurringAssignmentDto {
  id: string;
  name: string;
  checklistId: string;
  checklistName: string;
  targetStores: RecurringAssignmentTarget[];
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
  targetStoreIds: string[];
  cronExpression: string;
  startsAt: string;
  endsAt?: string;
  assignedToUserId?: string;
}
