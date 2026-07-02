import type { AbstractControl, ValidationErrors } from '@angular/forms';

export function noWhitespace(control: AbstractControl): ValidationErrors | null {
  return control.value?.trim() ? null : { whitespace: true };
}

const ROLE_LABELS: Record<string, string> = {
  super_admin: 'Super Admin',
  admin: 'Administrator',
  supervisor: 'Supervisor',
  store_manager: 'Store Manager',
  store_employee: 'Store Employee',
  store_kiosk: 'Store Kiosk',
};

export function roleLabel(role: string | null | undefined): string {
  return ROLE_LABELS[role ?? ''] ?? role ?? '';
}

const DOW_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
const DOW_LABELS = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'];

export function cronLabel(expr: string | null | undefined): string {
  if (!expr) return '';
  const parts = expr.trim().split(/\s+/);
  if (parts.length !== 6) return expr;
  const [, min, hr, dom, , dow] = parts;
  const hh = String(hr).padStart(2, '0');
  const mm = String(min).padStart(2, '0');
  if (dow !== '?' && dow !== '*') {
    const idx = DOW_LABELS.indexOf(dow.toUpperCase());
    const dayName = idx >= 0 ? DOW_NAMES[idx] : dow;
    return `Every ${dayName} at ${hh}:${mm}`;
  }
  if (dom !== '?' && dom !== '*') return `On day ${dom} of every month at ${hh}:${mm}`;
  if (hr === '*') return `Every hour at minute :${mm}`;
  return `Every day at ${hh}:${mm}`;
}
