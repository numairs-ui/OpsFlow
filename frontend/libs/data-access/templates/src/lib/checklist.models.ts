import type { TemplateScope } from './template.models.js';

export interface ChecklistItemInput {
  templateId: string;
  order: number;
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
