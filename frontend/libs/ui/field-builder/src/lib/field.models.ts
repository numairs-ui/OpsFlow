export type FieldType = 'Numeric' | 'Boolean' | 'Text' | 'Photo' | 'Checklist';

export interface ChecklistSubItem {
  id: string;
  label: string;
  required: boolean;
}

export interface TemplateField {
  id: string;
  type: FieldType;
  label: string;
  required: boolean;
  // Numeric
  rangeMin?: number;
  rangeMax?: number;
  correctiveActionText?: string;
  // Checklist
  subItems?: ChecklistSubItem[];
}

export function createField(type: FieldType): TemplateField {
  return {
    id: crypto.randomUUID(),
    type,
    label: '',
    required: false,
  };
}

export function createSubItem(): ChecklistSubItem {
  return { id: crypto.randomUUID(), label: '', required: false };
}
