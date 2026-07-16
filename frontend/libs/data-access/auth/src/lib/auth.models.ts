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
  /** User's email, surfaced for identity display. Absent on legacy tokens minted before the claim was added. */
  email?: string;
  /** First assigned region (back-compat for single-region roles like supervisor). */
  regionId?: string;
  /** Full region set — one entry for supervisor, several for a region-scoped admin. */
  regionIds: string[];
}
