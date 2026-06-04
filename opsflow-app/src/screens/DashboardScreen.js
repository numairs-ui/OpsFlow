import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  useWindowDimensions,
} from 'react-native';
import { colors } from '../theme/colors';
import { Button } from '../components/Button';
import { StatusBadge } from '../components/StatusBadge';
import { getAccountCapabilities, getRoleLabel } from '../data/accounts';
import {
  DataRow,
  MetricTile,
  ScreenHeader,
  SectionAccordion,
  TaskRow,
} from '../components/OperationalPrimitives';

const CURRENT_WINDOW = {
  title: 'Pre-Rush Walk Through',
  meta: 'Due by 3:30 PM | 7 of 10 complete',
  status: 'due_soon',
};

const PRIORITY_TASKS = [
  {
    id: 'cash',
    title: 'Cash reconciliation',
    meta: '45 min overdue | Till B',
    assignee: 'Unassigned',
    status: 'overdue',
  },
  {
    id: 'temp',
    title: 'Make line temperature reading',
    meta: 'Requires numeric reading and notes if out of range',
    assignee: 'Sam',
    status: 'due_soon',
  },
  {
    id: 'inventory',
    title: '3-day dough and cheese plan',
    meta: 'Action state required for each item',
    assignee: 'Maya',
    status: 'in_progress',
  },
];

const TEAM = [
  { name: 'Sam', role: 'Shift Lead', tasks: '5 assigned', status: 'in_progress' },
  { name: 'Maya', role: 'Prep', tasks: '4 assigned', status: 'completed' },
  { name: 'Leo', role: 'Cash', tasks: '2 assigned', status: 'pending' },
];

export const DashboardScreen = ({ onNavigate, activeAccount }) => {
  const { width } = useWindowDimensions();
  const isWide = width >= 900;
  const capabilities = getAccountCapabilities(activeAccount);
  const isPreviewOnly = activeAccount?.previewOnly;

  return (
    <View style={styles.container}>
      <ScreenHeader
        eyebrow="Manager cockpit"
        title="Today at Store 1382"
        subtitle="Friday, May 2 | Opening through closeout"
        action={
          isWide ? (
            <Button title="Open Timeline" size="small" onPress={() => onNavigate('timeline')} />
          ) : null
        }
      />

      <ScrollView
        style={styles.scroll}
        contentContainerStyle={[styles.content, isWide && styles.contentWide]}
        showsVerticalScrollIndicator={false}
      >
        <View style={[styles.metricsGrid, isWide && styles.metricsGridWide]}>
          <MetricTile label="Completed" value="21/28" status="completed" helper="75% of today's checklist" />
          <MetricTile label="Overdue" value="1" status="overdue" helper="Cash reconciliation" />
          <MetricTile label="Due soon" value="3" status="due_soon" helper="Next 45 minutes" />
          <MetricTile label="Closeout" value="72%" status="in_progress" helper="Ready after blockers clear" />
        </View>

        <View style={[styles.columns, isWide && styles.columnsWide]}>
          <View style={styles.primaryColumn}>
            <View style={styles.accountPanel}>
              <View style={styles.accountPanelText}>
                <Text style={styles.accountKicker}>Active role/profile</Text>
                <Text style={styles.accountTitle}>{activeAccount?.displayName}</Text>
                <Text style={styles.accountCopy}>
                  {getRoleLabel(activeAccount?.role)}
                  {activeAccount?.role === 'store'
                    ? ' | Shared device completion requires employee name and initials.'
                    : capabilities.canCreateStoreWork
                      ? ' | Can create store work and assign to the Store Account or employees.'
                      : ' | Can complete assigned operational work.'}
                </Text>
              </View>
              {isPreviewOnly && <StatusBadge status="due_soon" label="Preview stub" />}
            </View>

            <SectionAccordion
              title={CURRENT_WINDOW.title}
              meta={CURRENT_WINDOW.meta}
              status={CURRENT_WINDOW.status}
            >
              {PRIORITY_TASKS.map((task) => (
                <TaskRow
                  key={task.id}
                  title={task.title}
                  meta={task.meta}
                  assignee={task.assignee}
                  status={task.status}
                  onPress={() => onNavigate(task.id === 'cash' ? 'cash' : task.id === 'temp' ? 'temp' : 'inventory')}
                />
              ))}
              <Button title="Continue Timeline" variant="outline" onPress={() => onNavigate('timeline')} />
            </SectionAccordion>

            <View style={styles.quickGrid}>
              <TouchableOpacity style={styles.quickCard} onPress={() => onNavigate('assign')}>
                <Text style={styles.quickTitle}>Assign work</Text>
                <Text style={styles.quickCopy}>3 tasks need manager attention before rush.</Text>
              </TouchableOpacity>
              <TouchableOpacity style={styles.quickCard} onPress={() => onNavigate('alerts')}>
                <Text style={styles.quickTitle}>Resolve alerts</Text>
                <Text style={styles.quickCopy}>Overdue and variance items stay visible until handled.</Text>
              </TouchableOpacity>
              <TouchableOpacity style={styles.quickCard} onPress={() => onNavigate('signoff')}>
                <Text style={styles.quickTitle}>Closeout readiness</Text>
                <Text style={styles.quickCopy}>Review blockers before manager sign-off.</Text>
              </TouchableOpacity>
              <TouchableOpacity style={styles.quickCard} onPress={() => onNavigate('builder')}>
                <Text style={styles.quickTitle}>{capabilities.canCreateStoreWork ? 'Create work' : 'Template preview'}</Text>
                <Text style={styles.quickCopy}>
                  {capabilities.canCreateStoreWork
                    ? 'Add store-level tasks or checklists and assign to store or people.'
                    : 'View the active F0890 operating structure and role limits.'}
                </Text>
              </TouchableOpacity>
            </View>
          </View>

          <View style={styles.secondaryColumn}>
            <View style={styles.panel}>
              <Text style={styles.panelTitle}>Team Snapshot</Text>
              {TEAM.map((member) => (
                <View key={member.name} style={styles.teamRow}>
                  <View>
                    <Text style={styles.teamName}>{member.name}</Text>
                    <Text style={styles.teamMeta}>{member.role} | {member.tasks}</Text>
                  </View>
                  <Text style={[styles.teamState, member.status === 'completed' && styles.teamStateDone]}>
                    {member.status === 'completed' ? 'On track' : member.status === 'pending' ? 'Needs work' : 'Active'}
                  </Text>
                </View>
              ))}
            </View>

            <View style={styles.panel}>
              <Text style={styles.panelTitle}>Closeout Readiness</Text>
              <DataRow label="Sections signed" value="3 of 5" />
              <DataRow label="Open exceptions" value="2" valueStyle={styles.dangerText} />
              <DataRow label="Cash variance" value="Not resolved" valueStyle={styles.dangerText} />
              <DataRow label="Temperature logs" value="1 pending" valueStyle={styles.warningText} />
              <Button title="Review Sign-off" variant="secondary" onPress={() => onNavigate('signoff')} />
            </View>
          </View>
        </View>

        <View style={styles.bottomPadding} />
      </ScrollView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  scroll: {
    flex: 1,
  },
  content: {
    padding: 16,
    gap: 16,
  },
  contentWide: {
    maxWidth: 1180,
    width: '100%',
    alignSelf: 'center',
    paddingHorizontal: 24,
    paddingBottom: 24,
  },
  metricsGrid: {
    gap: 12,
  },
  metricsGridWide: {
    flexDirection: 'row',
  },
  columns: {
    gap: 16,
  },
  columnsWide: {
    flexDirection: 'row',
    alignItems: 'flex-start',
  },
  primaryColumn: {
    flex: 1.6,
    gap: 16,
  },
  secondaryColumn: {
    flex: 1,
    gap: 16,
  },
  accountPanel: {
    minHeight: 96,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 16,
    padding: 16,
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    gap: 12,
  },
  accountPanelText: {
    flex: 1,
  },
  accountKicker: {
    fontSize: 11,
    fontWeight: '800',
    color: colors.accent,
    textTransform: 'uppercase',
    letterSpacing: 0.7,
  },
  accountTitle: {
    fontSize: 18,
    fontWeight: '800',
    color: colors.text,
    marginTop: 6,
  },
  accountCopy: {
    fontSize: 13,
    lineHeight: 18,
    color: colors.textSecondary,
    marginTop: 6,
  },
  quickGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12,
  },
  quickCard: {
    flexGrow: 1,
    flexBasis: 220,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 16,
    padding: 16,
    minHeight: 112,
  },
  quickTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: colors.text,
  },
  quickCopy: {
    fontSize: 13,
    lineHeight: 18,
    color: colors.textSecondary,
    marginTop: 8,
  },
  panel: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 16,
    padding: 16,
    gap: 12,
  },
  panelTitle: {
    fontSize: 17,
    fontWeight: '700',
    color: colors.text,
  },
  teamRow: {
    minHeight: 54,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 12,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: 12,
  },
  teamName: {
    fontSize: 15,
    fontWeight: '700',
    color: colors.text,
  },
  teamMeta: {
    fontSize: 13,
    color: colors.textSecondary,
    marginTop: 2,
  },
  teamState: {
    fontSize: 12,
    fontWeight: '700',
    color: colors.warning,
  },
  teamStateDone: {
    color: colors.success,
  },
  dangerText: {
    color: colors.danger,
  },
  warningText: {
    color: colors.warning,
  },
  bottomPadding: {
    height: 90,
  },
});

export default DashboardScreen;
