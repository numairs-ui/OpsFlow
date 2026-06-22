import { FormatStatusPipe } from './format-status.pipe';

describe('FormatStatusPipe', () => {
  const pipe = new FormatStatusPipe();

  it('formats CorrectiveActionRaised', () => {
    expect(pipe.transform('CorrectiveActionRaised')).toBe('Corrective Action Raised');
  });

  it('formats InProgress', () => {
    expect(pipe.transform('InProgress')).toBe('In Progress');
  });

  it('formats PendingApproval', () => {
    expect(pipe.transform('PendingApproval')).toBe('Pending Approval');
  });

  it('leaves single-word statuses unchanged', () => {
    expect(pipe.transform('Pending')).toBe('Pending');
    expect(pipe.transform('Completed')).toBe('Completed');
    expect(pipe.transform('Overdue')).toBe('Overdue');
    expect(pipe.transform('Draft')).toBe('Draft');
  });

  it('returns empty string for null', () => {
    expect(pipe.transform(null)).toBe('');
  });

  it('returns empty string for undefined', () => {
    expect(pipe.transform(undefined)).toBe('');
  });

  it('returns empty string for empty string', () => {
    expect(pipe.transform('')).toBe('');
  });
});
