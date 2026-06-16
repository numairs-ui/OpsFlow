export interface ImportTemplateItem {
  type: 'Task' | 'Checklist';
  name: string;
  description?: string;
  category: string;
  scope: 'System' | 'Regional' | 'Store';
  regionId?: string;
  storeId?: string;
  fieldsJson?: string;
}

export interface ImportTemplateRequest {
  templates: ImportTemplateItem[];
}

export interface ImportFailure {
  index: number;
  errors: string[];
}

export interface ImportTemplatesResult {
  created: number;
  failed: ImportFailure[];
}
