export interface LoginRequest {
  email: string;
  password: string;
  tenantId: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresIn: number;
}

export interface CurrentUser {
  sub: string;
  tenantId: string;
  role: string;
  storeId?: string;
  regionId?: string;
}
