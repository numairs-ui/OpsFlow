export interface ImportChecklistItem {
  name: string;
  description?: string;
  category?: string;
  fieldsJson?: string;
  order?: number;
  scoringType?: 'PassFail' | 'Scale1To5' | null;
  weight?: number;
  photoRequired?: boolean;
  failCorrectiveActionText?: string | null;
  failScoreThreshold?: number | null;
}

export interface ImportTemplateItem {
  type: 'Task' | 'Checklist';
  name: string;
  description?: string;
  category: string;
  scope: 'System' | 'Regional' | 'Store';
  regionId?: string;
  storeId?: string;
  fieldsJson?: string;
  /** For type === 'Checklist': sub-items that become scored ChecklistTemplateItems. */
  items?: ImportChecklistItem[];
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
