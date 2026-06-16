export interface Region {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
}

export interface Store {
  id: string;
  name: string;
  address?: string;
  regionId: string;
  regionName: string;
  isActive: boolean;
  createdAt: string;
}

export interface User {
  userId: string;
  email: string;
  displayName: string;
  role: string;
  storeId?: string;
  storeName?: string;
  regionId?: string;
  regionName?: string;
  isActive: boolean;
  mustChangePassword: boolean;
  createdAt: string;
}

export interface StoreAssignment {
  storeId: string;
  storeName: string;
  regionName?: string;
  assignedAt: string;
}

export interface StoreEmployee {
  userId: string;
  email: string;
  displayName: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export type UserRole = 'store_employee' | 'store_manager' | 'supervisor' | 'admin';
