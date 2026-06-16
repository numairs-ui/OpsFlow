export type FormSubmissionStatus =
  | 'Draft' | 'Submitted' | 'PendingApproval' | 'Returned' | 'Rejected' | 'Approved' | 'Recorded';

export interface MySubmissionDto {
  id: string;
  formTemplateId?: string;
  formTemplateName?: string;
  storeId: string;
  status: FormSubmissionStatus;
  currentStepOrder?: number;
  createdAt: string;
  submittedAt?: string;
  resolvedAt?: string;
}

export interface PendingReviewDto {
  id: string;
  formTemplateId?: string;
  formTemplateName?: string;
  storeId: string;
  storeName?: string;
  submittedByUserId: string;
  status: FormSubmissionStatus;
  stepOrder: number;
  submittedAt?: string;
}

export interface ApprovalStepRecordDto {
  stepOrder: number;
  role: string;
  actionByUserId?: string;
  action: string;
  comments?: string;
  actionAt?: string;
}

export interface FormSubmissionDetailDto {
  id: string;
  formTemplateId?: string;
  formTemplateName?: string;
  formTemplateFieldsJson?: string;
  storeId: string;
  storeName?: string;
  submittedByUserId: string;
  status: FormSubmissionStatus;
  currentStepOrder?: number;
  fieldValuesJson: string;
  createdAt: string;
  submittedAt?: string;
  resolvedAt?: string;
  approvalSteps: ApprovalStepRecordDto[];
}

export interface FormSubmissionSummaryDto {
  id: string;
  formTemplateId?: string;
  formTemplateName?: string;
  storeId: string;
  storeName?: string;
  submittedByUserId: string;
  status: FormSubmissionStatus;
  createdAt: string;
  submittedAt?: string;
  resolvedAt?: string;
}

export interface CreateFormSubmissionRequest {
  formTemplateId?: string;
  storeId: string;
  fieldValues: Record<string, string>;
}

export interface SubmitFormSubmissionRequest {
  fieldValues?: Record<string, string>;
}
