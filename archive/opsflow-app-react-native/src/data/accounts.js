export const ROLE_LABELS = {
  employee: 'Employee',
  store: 'Store Account',
  manager: 'Store Manager/GM',
  supervisor: 'Supervisor/Ops Lead',
  auditor: 'Auditor',
  admin: 'System Admin',
};

export const ACCOUNTS = [
  {
    id: 'manager-maria',
    displayName: 'Maria R.',
    shortName: 'Maria',
    role: 'manager',
    accountKind: 'person',
    storeId: 'F0890',
    storeName: 'F0890',
  },
  {
    id: 'store-f0890',
    displayName: 'F0890 Shared Chromebook',
    shortName: 'F0890',
    role: 'store',
    accountKind: 'store',
    storeId: 'F0890',
    storeName: 'F0890',
    sharedDevice: true,
  },
  {
    id: 'employee-sam',
    displayName: 'Sam P.',
    shortName: 'Sam',
    role: 'employee',
    accountKind: 'person',
    storeId: 'F0890',
    storeName: 'F0890',
  },
  {
    id: 'employee-maya',
    displayName: 'Maya K.',
    shortName: 'Maya',
    role: 'employee',
    accountKind: 'person',
    storeId: 'F0890',
    storeName: 'F0890',
  },
  {
    id: 'supervisor-ops',
    displayName: 'Ops Lead',
    shortName: 'Ops',
    role: 'supervisor',
    accountKind: 'person',
    storeId: 'multi-store',
    storeName: '12-store group',
    previewOnly: true,
  },
  {
    id: 'auditor-field',
    displayName: 'Field Auditor',
    shortName: 'Audit',
    role: 'auditor',
    accountKind: 'person',
    storeId: 'company',
    storeName: 'Company',
    previewOnly: true,
  },
  {
    id: 'admin-system',
    displayName: 'System Admin',
    shortName: 'Admin',
    role: 'admin',
    accountKind: 'person',
    storeId: 'company',
    storeName: 'Company',
    previewOnly: true,
  },
];

export const ASSIGNMENT_TARGETS = ACCOUNTS.filter((account) => (
  ['store', 'employee', 'manager', 'supervisor', 'auditor'].includes(account.role)
)).map((account) => ({
  id: account.id,
  label: account.displayName,
  shortName: account.shortName,
  role: account.role,
  roleLabel: ROLE_LABELS[account.role],
  targetType: account.role === 'store' ? 'store' : 'person',
  storeId: account.storeId,
  previewOnly: account.previewOnly,
}));

export const TASK_FAMILIES = [
  'Inventory check',
  'Food prep',
  'Basic cleaning',
  'Cash/till',
  'Temperature',
  'Audit',
  'Incident',
  'Corrective action',
  'General operations',
];

export const ROLE_CAPABILITIES = {
  employee: {
    canAssign: false,
    canCreateStoreWork: false,
    canCreatePersonalWork: false,
    canCompleteStoreWork: true,
    completionRequiresName: false,
  },
  store: {
    canAssign: false,
    canCreateStoreWork: false,
    canCreatePersonalWork: false,
    canCompleteStoreWork: true,
    completionRequiresName: true,
  },
  manager: {
    canAssign: true,
    canCreateStoreWork: true,
    canCreatePersonalWork: false,
    canCompleteStoreWork: true,
    completionRequiresName: false,
  },
  supervisor: {
    canAssign: true,
    canCreateStoreWork: true,
    canCreatePersonalWork: true,
    canCompleteStoreWork: true,
    completionRequiresName: false,
  },
  auditor: {
    canAssign: false,
    canCreateStoreWork: false,
    canCreatePersonalWork: false,
    canCompleteStoreWork: false,
    canCreateCorrectiveActions: true,
    completionRequiresName: false,
  },
  admin: {
    canAssign: true,
    canCreateStoreWork: false,
    canCreatePersonalWork: false,
    canCompleteStoreWork: false,
    completionRequiresName: false,
  },
};

export const getAccountById = (accountId) => (
  ACCOUNTS.find((account) => account.id === accountId) || ACCOUNTS[0]
);

export const getRoleLabel = (role) => ROLE_LABELS[role] || role;

export const getAccountCapabilities = (account) => (
  ROLE_CAPABILITIES[account?.role] || ROLE_CAPABILITIES.employee
);

export const requiresTypedCompletionIdentity = (account) => (
  Boolean(getAccountCapabilities(account).completionRequiresName)
);

export const isStoreAssignableTarget = (target) => (
  target.role === 'store' || target.role === 'employee' || target.role === 'manager'
);
