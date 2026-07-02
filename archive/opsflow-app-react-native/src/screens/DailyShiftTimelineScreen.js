import React, { useState } from 'react';
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
import {
  DataRow,
  ScreenHeader,
  SectionAccordion,
  StickyActionBar,
  TaskRow,
} from '../components/OperationalPrimitives';

const SHIFT_SECTIONS = [
  {
    id: 'opening',
    time: '9:00 AM',
    title: 'Opening Manager',
    deadline: '11:00 AM',
    status: 'completed',
    assignee: 'Maria',
    tasks: [
      { id: 'arrival', title: 'Arrival time logged', meta: 'Timestamp captured', status: 'completed', assignee: 'Maria' },
      { id: 'security', title: 'Security walk-through', meta: 'Doors, safe, back entrance', status: 'completed', assignee: 'Maria' },
      { id: 'makeline', title: 'Set up make line', meta: 'Labels, MDOG, thermometers', status: 'completed', assignee: 'Sam' },
    ],
  },
  {
    id: 'product',
    time: '11:00 AM',
    title: 'Product Management',
    deadline: '11:00 AM',
    status: 'in_progress',
    assignee: 'Maya',
    tasks: [
      { id: 'dough', title: 'Complete 3-day dough and cheese plan', meta: 'Inventory action state required', status: 'in_progress', assignee: 'Maya' },
      { id: 'temp-walkin', title: 'Walk-in temperature reading', meta: 'Target 34-40 F', status: 'completed', assignee: 'Sam' },
      { id: 'tills', title: 'Count tills and store cash', meta: 'Expected amount loaded', status: 'overdue', assignee: 'Unassigned' },
    ],
  },
  {
    id: 'rush',
    time: '3:30 PM',
    title: 'Pre-Rush Walk Through',
    deadline: '3:30 PM',
    status: 'due_soon',
    assignee: 'Sam',
    tasks: [
      { id: 'stocked', title: 'Make line stocked', meta: 'Cheese, sauce, dough, portion cups', status: 'pending', assignee: 'Sam' },
      { id: 'restrooms', title: 'Restrooms checked', meta: 'Photo optional if issue found', status: 'pending', assignee: 'Leo' },
      { id: 'lobby', title: 'Lobby and dining room sweep', meta: 'Guest-ready check', status: 'pending', assignee: 'Leo' },
    ],
  },
  {
    id: 'close',
    time: 'Close',
    title: 'Closing Checklist',
    deadline: 'End of shift',
    status: 'pending',
    assignee: 'Manager',
    tasks: [
      { id: 'close-cash', title: 'Final cash reconciliation', meta: 'Initials required', status: 'pending', assignee: 'Manager' },
      { id: 'deep-clean', title: 'Deep clean make line', meta: 'Manager inspection required', status: 'pending', assignee: 'Unassigned' },
      { id: 'signoff', title: 'Manager closeout sign-off', meta: 'Blocked until exceptions resolved', status: 'pending', assignee: 'Manager' },
    ],
  },
];

const statusLabel = (status) => status.replace('_', ' ');

export const DailyShiftTimelineScreen = ({ onNavigate }) => {
  const [expanded, setExpanded] = useState(['product', 'rush']);
  const { width } = useWindowDimensions();
  const isWide = width >= 900;

  const toggleSection = (id) => {
    setExpanded((current) => (
      current.includes(id)
        ? current.filter((sectionId) => sectionId !== id)
        : [...current, id]
    ));
  };

  const completionCount = SHIFT_SECTIONS.reduce((count, section) => (
    count + section.tasks.filter((task) => task.status === 'completed').length
  ), 0);
  const totalCount = SHIFT_SECTIONS.reduce((count, section) => count + section.tasks.length, 0);

  return (
    <View style={styles.container}>
      <ScreenHeader
        eyebrow="Daily timeline"
        title="Pizza Restaurant Daily"
        subtitle={`${completionCount} of ${totalCount} tasks complete | Current window: Pre-Rush`}
      />

      <ScrollView
        style={styles.scroll}
        contentContainerStyle={[styles.content, isWide && styles.contentWide]}
        showsVerticalScrollIndicator={false}
      >
        <View style={[styles.summary, isWide && styles.summaryWide]}>
          <View style={styles.summaryCard}>
            <Text style={styles.summaryValue}>{Math.round((completionCount / totalCount) * 100)}%</Text>
            <Text style={styles.summaryLabel}>Checklist progress</Text>
          </View>
          <View style={styles.summaryCard}>
            <Text style={[styles.summaryValue, styles.dangerText]}>1</Text>
            <Text style={styles.summaryLabel}>Overdue blocker</Text>
          </View>
          <View style={styles.summaryCard}>
            <Text style={[styles.summaryValue, styles.warningText]}>3</Text>
            <Text style={styles.summaryLabel}>Due before rush</Text>
          </View>
        </View>

        <View style={[styles.layout, isWide && styles.layoutWide]}>
          <View style={styles.timelineColumn}>
            {SHIFT_SECTIONS.map((section) => {
              const done = section.tasks.filter((task) => task.status === 'completed').length;
              const isExpanded = expanded.includes(section.id);
              return (
                <SectionAccordion
                  key={section.id}
                  title={`${section.time} - ${section.title}`}
                  meta={`Deadline: ${section.deadline} | ${done}/${section.tasks.length} tasks | Owner: ${section.assignee}`}
                  status={section.status}
                  expanded={isExpanded}
                  onPress={() => toggleSection(section.id)}
                >
                  {section.tasks.map((task) => (
                    <TaskRow
                      key={task.id}
                      title={task.title}
                      meta={task.meta}
                      assignee={task.assignee}
                      status={task.status}
                      onPress={() => onNavigate?.('taskdetail')}
                    />
                  ))}
                </SectionAccordion>
              );
            })}
          </View>

          {isWide && (
            <View style={styles.detailPanel}>
              <Text style={styles.panelTitle}>Selected Window</Text>
              <StatusBadge status="due_soon" label="Due soon" />
              <Text style={styles.panelHeading}>Pre-Rush Walk Through</Text>
              <Text style={styles.panelCopy}>
                This window protects dinner rush readiness. Keep assigned tasks visible until
                each station is stocked, clean, and manager-reviewed.
              </Text>
              <View style={styles.dataBlock}>
                <DataRow label="Deadline" value="3:30 PM" />
                <DataRow label="Owner" value="Sam" />
                <DataRow label="Required data" value="1 temperature" />
                <DataRow label="Open exceptions" value="0" />
              </View>
          <Button title="Open Task Detail" variant="secondary" onPress={() => onNavigate?.('taskdetail')} />
            </View>
          )}
        </View>

        <View style={styles.bottomPadding} />
      </ScrollView>

      {!isWide && (
        <StickyActionBar>
          <Button title="Assign Open Work" onPress={() => onNavigate?.('assign')} />
          <TouchableOpacity style={styles.secondaryAction} onPress={() => onNavigate?.('alerts')}>
            <Text style={styles.secondaryActionText}>View overdue alert</Text>
          </TouchableOpacity>
        </StickyActionBar>
      )}
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
  },
  summary: {
    gap: 12,
  },
  summaryWide: {
    flexDirection: 'row',
  },
  summaryCard: {
    flex: 1,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 16,
    padding: 16,
  },
  summaryValue: {
    fontSize: 28,
    fontWeight: '700',
    color: colors.text,
  },
  summaryLabel: {
    fontSize: 13,
    color: colors.textSecondary,
    marginTop: 4,
  },
  layout: {
    gap: 16,
  },
  layoutWide: {
    flexDirection: 'row',
    alignItems: 'flex-start',
  },
  timelineColumn: {
    flex: 1.6,
    gap: 12,
  },
  detailPanel: {
    flex: 1,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 16,
    padding: 16,
    gap: 12,
  },
  panelTitle: {
    fontSize: 12,
    fontWeight: '700',
    color: colors.textMuted,
    textTransform: 'uppercase',
  },
  panelHeading: {
    fontSize: 20,
    fontWeight: '700',
    color: colors.text,
  },
  panelCopy: {
    fontSize: 14,
    lineHeight: 20,
    color: colors.textSecondary,
  },
  dataBlock: {
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: 8,
    gap: 2,
  },
  dangerText: {
    color: colors.danger,
  },
  warningText: {
    color: colors.warning,
  },
  secondaryAction: {
    minHeight: 44,
    alignItems: 'center',
    justifyContent: 'center',
  },
  secondaryActionText: {
    fontSize: 15,
    fontWeight: '600',
    color: colors.accent,
  },
  bottomPadding: {
    height: 90,
  },
});

export default DailyShiftTimelineScreen;
