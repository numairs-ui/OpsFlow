import { FormControl } from '@angular/forms';
import { cronLabel, noWhitespace, roleLabel } from './validators';

// ── noWhitespace ──────────────────────────────────────────────────────────────

describe('noWhitespace', () => {
  it('returns null for a normal string', () => {
    expect(noWhitespace(new FormControl('Opening Checklist'))).toBeNull();
  });

  it('returns null for a single character', () => {
    expect(noWhitespace(new FormControl('A'))).toBeNull();
  });

  it('returns error for an empty string', () => {
    expect(noWhitespace(new FormControl(''))).toEqual({ whitespace: true });
  });

  it('returns error for whitespace-only string', () => {
    expect(noWhitespace(new FormControl('   '))).toEqual({ whitespace: true });
  });

  it('returns error for tab-only string', () => {
    expect(noWhitespace(new FormControl('\t\t'))).toEqual({ whitespace: true });
  });

  it('returns null for string with leading/trailing spaces around content', () => {
    expect(noWhitespace(new FormControl('  hello  '))).toBeNull();
  });
});

// ── roleLabel ─────────────────────────────────────────────────────────────────

describe('roleLabel', () => {
  it('maps admin to Administrator', () => {
    expect(roleLabel('admin')).toBe('Administrator');
  });

  it('maps supervisor to Supervisor', () => {
    expect(roleLabel('supervisor')).toBe('Supervisor');
  });

  it('maps store_manager to Store Manager', () => {
    expect(roleLabel('store_manager')).toBe('Store Manager');
  });

  it('maps store_employee to Store Employee', () => {
    expect(roleLabel('store_employee')).toBe('Store Employee');
  });

  it('passes through unknown roles unchanged', () => {
    expect(roleLabel('superadmin')).toBe('superadmin');
  });

  it('returns empty string for null', () => {
    expect(roleLabel(null)).toBe('');
  });

  it('returns empty string for undefined', () => {
    expect(roleLabel(undefined)).toBe('');
  });
});

// ── cronLabel ─────────────────────────────────────────────────────────────────

describe('cronLabel', () => {
  it('formats a daily cron expression', () => {
    expect(cronLabel('0 0 9 * * ?')).toBe('Every day at 09:00');
  });

  it('formats a weekly Monday cron expression', () => {
    expect(cronLabel('0 0 8 ? * MON')).toBe('Every Monday at 08:00');
  });

  it('formats a weekly Friday cron expression', () => {
    expect(cronLabel('0 30 17 ? * FRI')).toBe('Every Friday at 17:30');
  });

  it('formats a monthly cron expression', () => {
    expect(cronLabel('0 0 7 1 * ?')).toBe('On day 1 of every month at 07:00');
  });

  it('formats an hourly cron expression', () => {
    expect(cronLabel('0 15 * * * ?')).toBe('Every hour at minute :15');
  });

  it('returns raw expression when format is unrecognised (not 6 parts)', () => {
    const raw = '0 9 * * 1';
    expect(cronLabel(raw)).toBe(raw);
  });

  it('returns empty string for null', () => {
    expect(cronLabel(null)).toBe('');
  });

  it('returns empty string for undefined', () => {
    expect(cronLabel(undefined)).toBe('');
  });
});
