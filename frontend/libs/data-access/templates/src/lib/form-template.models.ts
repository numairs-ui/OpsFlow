import type { TemplateScope } from './template.models.js';

export type PropagationType = 'Sequential' | 'Parallel' | 'NotificationOnly';
export type ApprovalRole = 'store_employee' | 'store_manager' | 'supervisor' | 'admin' | 'super_admin';

export interface ApprovalStep {
  role: ApprovalRole;
  order: number;
}

export interface FormTemplateDto {
  id: string;
  name: string;
  description?: string;
  scope: TemplateScope;
  regionId?: string;
  regionName?: string;
  storeId?: string;
  storeName?: string;
  propagationType: PropagationType;
  fieldCount: number;
  isActive: boolean;
  createdAt: string;
}

export interface FormTemplateDetailDto {
  id: string;
  name: string;
  description?: string;
  scope: TemplateScope;
  regionId?: string;
  regionName?: string;
  storeId?: string;
  storeName?: string;
  propagationType: PropagationType;
  approvalStepsJson: string;
  fieldsJson: string;
  isActive: boolean;
  createdAt: string;
}

export interface FormTemplateFilter {
  scope?: TemplateScope;
  propagationType?: PropagationType;
  isActive?: boolean;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface FormTemplateListResult {
  items: FormTemplateDto[];
  totalCount: number;
}

export interface CreateFormTemplateRequest {
  name: string;
  description?: string;
  scope: TemplateScope;
  regionId?: string;
  storeId?: string;
  propagationType: PropagationType;
  approvalSteps: ApprovalStep[];
  fieldsJson: string;
}

export interface UpdateFormTemplateRequest {
  name: string;
  description?: string;
  propagationType: PropagationType;
  approvalSteps: ApprovalStep[];
  fieldsJson?: string;
}
