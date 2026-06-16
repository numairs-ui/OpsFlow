export type TemplateScope = 'System' | 'Regional' | 'Store';

export interface TemplateDto {
  id: string;
  name: string;
  description?: string;
  category: string;
  scope: TemplateScope;
  regionId?: string;
  regionName?: string;
  storeId?: string;
  storeName?: string;
  fieldCount: number;
  isActive: boolean;
  createdAt: string;
}

export interface TemplateDetailDto extends TemplateDto {
  fieldsJson: string;
}

export interface TemplateListResult {
  items: TemplateDto[];
  totalCount: number;
}

export interface CreateTemplateRequest {
  name: string;
  description?: string;
  category: string;
  scope: TemplateScope;
  regionId?: string;
  storeId?: string;
  fieldsJson: string;
}

export interface UpdateTemplateRequest {
  name: string;
  description?: string;
  category: string;
  fieldsJson?: string;
}

export interface TemplateFilter {
  scope?: TemplateScope;
  category?: string;
  isActive?: boolean;
  search?: string;
  page?: number;
  pageSize?: number;
}
