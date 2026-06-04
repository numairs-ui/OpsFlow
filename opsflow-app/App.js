import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  SafeAreaView,
  StatusBar,
  useWindowDimensions,
} from 'react-native';
import { colors } from './src/theme/colors';
import { ACCOUNTS, getAccountById, getRoleLabel } from './src/data/accounts';

import { DashboardScreen } from './src/screens/DashboardScreen';
import { DailyShiftTimelineScreen } from './src/screens/DailyShiftTimelineScreen';
import { InventoryManagementScreen } from './src/screens/InventoryManagementScreen';
import { StaffAssignmentScreen } from './src/screens/StaffAssignmentScreen';
import { ManagerSignOffScreen } from './src/screens/ManagerSignOffScreen';
import { TaskDetailScreen } from './src/screens/TaskDetailScreen';
import { CashManagementScreen } from './src/screens/CashManagementScreen';
import { OverdueAlertScreen } from './src/screens/OverdueAlertScreen';
import { CommunicationViewerScreen } from './src/screens/CommunicationViewerScreen';
import { DailyReviewReportScreen } from './src/screens/DailyReviewReportScreen';
import { TemperatureLoggerScreen } from './src/screens/TemperatureLoggerScreen';
import { EmployeeTaskViewScreen } from './src/screens/EmployeeTaskViewScreen';
import { VarianceManagementScreen } from './src/screens/VarianceManagementScreen';
import { ChecklistBuilderScreen } from './src/screens/ChecklistBuilderScreen';

const PRIMARY_NAV = [
  { id: 'today', name: 'Today', screen: 'dashboard', short: 'Today' },
  { id: 'tasks', name: 'Tasks', screen: 'tasks', short: 'Tasks' },
  { id: 'operations', name: 'Operations', screen: 'timeline', short: 'Ops' },
  { id: 'closeout', name: 'Closeout', screen: 'signoff', short: 'Close' },
  { id: 'templates', name: 'Templates', screen: 'builder', short: 'More' },
];

const SECTION_LINKS = [
  { section: 'today', id: 'dashboard', name: 'Dashboard', description: 'Shift status and priorities' },
  { section: 'today', id: 'timeline', name: 'Daily Timeline', description: 'Time-window checklist execution' },
  { section: 'today', id: 'alerts', name: 'Alerts', description: 'Overdue and exception center' },
  { section: 'today', id: 'report', name: 'Daily Review', description: 'Completion and exception summary' },
  { section: 'tasks', id: 'tasks', name: 'My Tasks', description: 'Employee task queue' },
  { section: 'tasks', id: 'taskdetail', name: 'Task Detail', description: 'Instructions and audit trail' },
  { section: 'operations', id: 'timeline', name: 'Timeline', description: 'Operational checklist' },
  { section: 'operations', id: 'assign', name: 'Staff Assignment', description: 'Deploy work to staff' },
  { section: 'operations', id: 'inventory', name: 'Inventory', description: '3-day dough and cheese planning' },
  { section: 'operations', id: 'cash', name: 'Cash/Till', description: 'Till count and variance' },
  { section: 'operations', id: 'temp', name: 'Temperature', description: 'Temperature readings' },
  { section: 'operations', id: 'variance', name: 'Variance', description: 'Resolve discrepancies' },
  { section: 'closeout', id: 'signoff', name: 'Manager Sign-off', description: 'Closeout readiness' },
  { section: 'closeout', id: 'report', name: 'Daily Review Report', description: 'End-of-day summary' },
  { section: 'templates', id: 'builder', name: 'Template Preview', description: 'Checklist structure preview' },
  { section: 'templates', id: 'comm', name: 'Communications', description: 'Manager messages' },
];

export default function App() {
  const [activeScreen, setActiveScreen] = useState('dashboard');
  const [activeAccountId, setActiveAccountId] = useState('manager-maria');
  const { width } = useWindowDimensions();
  const isWide = width >= 900;
  const activeAccount = getAccountById(activeAccountId);

  const activeSection = SECTION_LINKS.find((item) => item.id === activeScreen)?.section || 'today';
  const sectionLinks = SECTION_LINKS.filter((item) => item.section === activeSection);

  const handleNavigate = (screenId) => {
    setActiveScreen(screenId);
  };

  const renderScreen = () => {
    switch (activeScreen) {
      case 'dashboard':
        return <DashboardScreen onNavigate={handleNavigate} activeAccount={activeAccount} />;
      case 'timeline':
        return <DailyShiftTimelineScreen onNavigate={handleNavigate} />;
      case 'inventory':
        return <InventoryManagementScreen onNavigate={handleNavigate} />;
      case 'assign':
        return <StaffAssignmentScreen onNavigate={handleNavigate} activeAccount={activeAccount} />;
      case 'signoff':
        return <ManagerSignOffScreen onNavigate={handleNavigate} />;
      case 'taskdetail':
        return <TaskDetailScreen activeAccount={activeAccount} />;
      case 'cash':
        return <CashManagementScreen />;
      case 'alerts':
        return <OverdueAlertScreen />;
      case 'comm':
        return <CommunicationViewerScreen />;
      case 'report':
        return <DailyReviewReportScreen onNavigate={handleNavigate} />;
      case 'temp':
        return <TemperatureLoggerScreen />;
      case 'tasks':
        return <EmployeeTaskViewScreen activeAccount={activeAccount} />;
      case 'variance':
        return <VarianceManagementScreen />;
      case 'builder':
        return <ChecklistBuilderScreen onNavigate={handleNavigate} activeAccount={activeAccount} />;
      default:
        return <DashboardScreen onNavigate={handleNavigate} activeAccount={activeAccount} />;
    }
  };

  const renderAccountSwitcher = (mode) => (
    <View style={mode === 'rail' ? styles.accountSwitcherRail : styles.accountSwitcherMobile}>
      <Text style={styles.accountSwitcherLabel}>Role/Profile</Text>
      <View style={mode === 'rail' ? styles.accountListRail : styles.accountListMobile}>
        {ACCOUNTS.map((account) => {
          const isActive = account.id === activeAccountId;
          return (
            <TouchableOpacity
              key={account.id}
              style={[
                styles.accountChip,
                isActive && styles.accountChipActive,
                account.previewOnly && styles.accountChipPreview,
              ]}
              onPress={() => setActiveAccountId(account.id)}
            >
              <Text style={[styles.accountChipName, isActive && styles.accountChipNameActive]}>
                {account.shortName}
              </Text>
              {mode === 'rail' && (
                <Text style={[styles.accountChipRole, isActive && styles.accountChipRoleActive]}>
                  {getRoleLabel(account.role)}
                </Text>
              )}
            </TouchableOpacity>
          );
        })}
      </View>
    </View>
  );

  const renderPrimaryNav = (mode) => (
    <View style={mode === 'rail' ? styles.navRailItems : styles.tabItems}>
      {PRIMARY_NAV.map((item) => {
        const isActive = activeSection === item.id;
        return (
          <TouchableOpacity
            key={item.id}
            style={[
              mode === 'rail' ? styles.railItem : styles.tabItem,
              isActive && (mode === 'rail' ? styles.railItemActive : styles.tabItemActive),
            ]}
            onPress={() => handleNavigate(item.screen)}
          >
            <Text
              style={[
                mode === 'rail' ? styles.railItemText : styles.tabLabel,
                isActive && (mode === 'rail' ? styles.railItemTextActive : styles.tabLabelActive),
              ]}
            >
              {mode === 'rail' ? item.name : item.short}
            </Text>
          </TouchableOpacity>
        );
      })}
    </View>
  );

  const renderSectionLinks = () => (
    <View style={styles.sectionLinks}>
      <Text style={styles.sectionTitle}>
        {PRIMARY_NAV.find((item) => item.id === activeSection)?.name}
      </Text>
      {sectionLinks.map((item) => {
        const isActive = activeScreen === item.id;
        return (
          <TouchableOpacity
            key={`${item.section}-${item.id}`}
            style={[styles.sectionLink, isActive && styles.sectionLinkActive]}
            onPress={() => handleNavigate(item.id)}
          >
            <Text style={[styles.sectionLinkText, isActive && styles.sectionLinkTextActive]}>
              {item.name}
            </Text>
            {isWide && (
              <Text style={styles.sectionLinkDescription}>
                {item.description}
              </Text>
            )}
          </TouchableOpacity>
        );
      })}
    </View>
  );

  const renderRail = () => {
    if (!isWide) return null;

    return (
      <View style={styles.navRail}>
        <View style={styles.brandBlock}>
          <Text style={styles.brandName}>OpsFlow</Text>
          <Text style={styles.brandMeta}>{activeAccount.storeName} | {getRoleLabel(activeAccount.role)}</Text>
        </View>
        {renderAccountSwitcher('rail')}
        {renderPrimaryNav('rail')}
        {renderSectionLinks()}
      </View>
    );
  };

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar barStyle="dark-content" backgroundColor={colors.background} />
      <View style={styles.shell}>
        {renderRail()}
        <View style={styles.main}>
          {!isWide && (
            <View style={styles.mobileHeader}>
              <View style={styles.mobileBrandBlock}>
                <Text style={styles.brandName}>OpsFlow</Text>
                <Text style={styles.brandMeta}>{activeAccount.storeName} | {getRoleLabel(activeAccount.role)}</Text>
              </View>
              {renderAccountSwitcher('mobile')}
            </View>
          )}
          {!isWide && renderSectionLinks()}
          <View style={styles.content}>
            {renderScreen()}
          </View>
        </View>
      </View>
      {!isWide && (
        <View style={styles.tabBar}>
          {renderPrimaryNav('tabs')}
        </View>
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  shell: {
    flex: 1,
    flexDirection: 'row',
    backgroundColor: colors.background,
  },
  navRail: {
    width: 280,
    backgroundColor: colors.surface,
    borderRightWidth: 1,
    borderRightColor: colors.border,
    paddingHorizontal: 16,
    paddingTop: 20,
  },
  brandBlock: {
    paddingHorizontal: 8,
    paddingBottom: 18,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    marginBottom: 16,
  },
  brandName: {
    fontSize: 22,
    fontWeight: '700',
    color: colors.text,
  },
  brandMeta: {
    fontSize: 13,
    color: colors.textSecondary,
    marginTop: 4,
  },
  accountSwitcherRail: {
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingBottom: 16,
    marginBottom: 16,
    gap: 8,
  },
  accountSwitcherMobile: {
    marginTop: 10,
    gap: 8,
  },
  accountSwitcherLabel: {
    fontSize: 11,
    fontWeight: '800',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.8,
  },
  accountListRail: {
    gap: 6,
  },
  accountListMobile: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 6,
  },
  accountChip: {
    minHeight: 38,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceElevated,
    paddingHorizontal: 10,
    paddingVertical: 8,
    justifyContent: 'center',
  },
  accountChipActive: {
    backgroundColor: colors.accent,
    borderColor: colors.accent,
  },
  accountChipPreview: {
    borderStyle: 'dashed',
  },
  accountChipName: {
    fontSize: 13,
    fontWeight: '800',
    color: colors.text,
  },
  accountChipNameActive: {
    color: colors.textInverse,
  },
  accountChipRole: {
    fontSize: 11,
    color: colors.textSecondary,
    marginTop: 2,
  },
  accountChipRoleActive: {
    color: colors.textInverse,
  },
  navRailItems: {
    gap: 6,
    marginBottom: 20,
  },
  railItem: {
    minHeight: 44,
    borderRadius: 12,
    paddingHorizontal: 12,
    justifyContent: 'center',
  },
  railItemActive: {
    backgroundColor: colors.surfacePressed,
  },
  railItemText: {
    fontSize: 15,
    fontWeight: '600',
    color: colors.textSecondary,
  },
  railItemTextActive: {
    color: colors.accent,
  },
  sectionLinks: {
    gap: 8,
    paddingHorizontal: 16,
    paddingBottom: 12,
  },
  sectionTitle: {
    fontSize: 12,
    fontWeight: '700',
    color: colors.textMuted,
    textTransform: 'uppercase',
    marginBottom: 4,
  },
  sectionLink: {
    borderRadius: 12,
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderWidth: 1,
    borderColor: 'transparent',
  },
  sectionLinkActive: {
    backgroundColor: colors.accent,
    borderColor: colors.accent,
  },
  sectionLinkText: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.text,
  },
  sectionLinkTextActive: {
    color: colors.textInverse,
  },
  sectionLinkDescription: {
    fontSize: 12,
    color: colors.textMuted,
    marginTop: 3,
    lineHeight: 16,
  },
  main: {
    flex: 1,
    minWidth: 0,
  },
  mobileHeader: {
    backgroundColor: colors.background,
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 10,
  },
  mobileBrandBlock: {
    gap: 0,
  },
  content: {
    flex: 1,
  },
  tabBar: {
    backgroundColor: colors.surface,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingBottom: 20,
    paddingTop: 8,
  },
  tabItems: {
    flexDirection: 'row',
    paddingHorizontal: 8,
  },
  tabItem: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 44,
    borderRadius: 12,
    marginHorizontal: 4,
  },
  tabItemActive: {
    backgroundColor: colors.surfacePressed,
  },
  tabLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: colors.textMuted,
  },
  tabLabelActive: {
    color: colors.accent,
  },
});
