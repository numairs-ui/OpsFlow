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
import { Input } from '../components/Input';
import { StatusBadge } from '../components/StatusBadge';
import {
  ASSIGNMENT_TARGETS,
  TASK_FAMILIES,
  getAccountCapabilities,
  getRoleLabel,
} from '../data/accounts';
import {
  DataRow,
  ScreenHeader,
  SectionAccordion,
  TaskRow,
} from '../components/OperationalPrimitives';

const TASK_TYPES = ['Simple task', 'Temperature', 'Inventory count', 'Cash/till', 'Manager sign-off'];

const ACTIVE_CHECKLIST = [
  {
    id: 'opening',
    title: 'Opening Manager',
    meta: '9:00 AM - 11:00 AM | 8 tasks',
    status: 'completed',
    tasks: [
      { title: 'Security walk-through', meta: 'Maria | complete', status: 'completed' },
      { title: 'Set up make line', meta: 'Marcus | in progress', status: 'in_progress' },
    ],
  },
  {
    id: 'rush',
    title: 'Pre-Rush Walk Through',
    meta: 'Due 3:30 PM | 3 tasks',
    status: 'due_soon',
    tasks: [
      { title: 'Make line stocked', meta: 'Sam | pending', status: 'pending' },
      { title: 'Restrooms checked', meta: 'Leo | pending', status: 'pending' },
    ],
  },
];

export const ChecklistBuilderScreen = ({ onNavigate, activeAccount }) => {
  const [mode, setMode] = useState('task');
  const [taskTitle, setTaskTitle] = useState('');
  const [checklistName, setChecklistName] = useState('');
  const [sectionName, setSectionName] = useState('Pre-Rush Walk Through');
  const [deadline, setDeadline] = useState('3:30 PM');
  const [assigneeId, setAssigneeId] = useState('store-f0890');
  const [taskType, setTaskType] = useState('Simple task');
  const [taskFamily, setTaskFamily] = useState('Basic cleaning');
  const [instructions, setInstructions] = useState('');
  const [createdItems, setCreatedItems] = useState([]);
  const { width } = useWindowDimensions();
  const isWide = width >= 900;
  const capabilities = getAccountCapabilities(activeAccount);
  const canCreateStoreWork = Boolean(capabilities.canCreateStoreWork && !activeAccount?.previewOnly);
  const availableTargets = ASSIGNMENT_TARGETS.filter((target) => (
    activeAccount?.role === 'supervisor'
      ? true
      : target.storeId === activeAccount?.storeId && ['store', 'employee', 'manager'].includes(target.role)
  ));
  const selectedAssignee = ASSIGNMENT_TARGETS.find((target) => target.id === assigneeId);

  const saveTask = () => {
    if (!canCreateStoreWork || !taskTitle.trim()) return;
    setCreatedItems((items) => [
      {
        id: Date.now().toString(),
        kind: 'Task',
        title: taskTitle,
        meta: `${sectionName} | ${deadline} | ${selectedAssignee?.label || 'Unassigned'} | ${taskFamily}`,
      },
      ...items,
    ]);
    setTaskTitle('');
    setInstructions('');
  };

  const saveChecklist = () => {
    if (!canCreateStoreWork || !checklistName.trim()) return;
    setCreatedItems((items) => [
      {
        id: Date.now().toString(),
        kind: 'Checklist',
        title: checklistName,
        meta: `${sectionName || 'New section'} | ${deadline || 'No deadline'} | assigned to ${selectedAssignee?.label || 'Unassigned'}`,
      },
      ...items,
    ]);
    setChecklistName('');
  };

  return (
    <View style={styles.container}>
      <ScreenHeader
        eyebrow="Templates and new work"
        title="Create work for the store"
        subtitle="Store managers create store-level tasks/checklists and assign them to the shared store account or people."
        action={isWide ? <Button title="View Timeline" size="small" variant="secondary" onPress={() => onNavigate?.('timeline')} /> : null}
      />

      <ScrollView
        style={styles.scroll}
        contentContainerStyle={[styles.content, isWide && styles.contentWide]}
        showsVerticalScrollIndicator={false}
      >
        <View style={[styles.layout, isWide && styles.layoutWide]}>
          <View style={styles.formColumn}>
            <View style={styles.permissionPanel}>
              <Text style={styles.permissionTitle}>
                Active role: {activeAccount?.displayName} | {getRoleLabel(activeAccount?.role)}
              </Text>
              <Text style={styles.permissionCopy}>
                {canCreateStoreWork
                  ? 'You can create store-level work. Personal tasks are intentionally out of scope for the store MVP.'
                  : 'This role cannot create store work here. Store Account users can complete assigned work but cannot create personal tasks/checklists.'}
              </Text>
            </View>

            <View style={styles.modeSwitch}>
              <TouchableOpacity
                style={[styles.modeButton, mode === 'task' && styles.modeButtonActive]}
                onPress={() => setMode('task')}
              >
                <Text style={[styles.modeText, mode === 'task' && styles.modeTextActive]}>New Task</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.modeButton, mode === 'checklist' && styles.modeButtonActive]}
                onPress={() => setMode('checklist')}
              >
                <Text style={[styles.modeText, mode === 'checklist' && styles.modeTextActive]}>New Checklist</Text>
              </TouchableOpacity>
            </View>

            <View style={styles.panel}>
              <Text style={styles.panelTitle}>
                {mode === 'task' ? 'Add Task for Employee' : 'Create Store Checklist'}
              </Text>

              {mode === 'task' ? (
                <Input
                  label="Task name"
                  value={taskTitle}
                  onChangeText={setTaskTitle}
                  placeholder="Example: Restock make line before dinner rush"
                />
              ) : (
                <Input
                  label="Checklist name"
                  value={checklistName}
                  onChangeText={setChecklistName}
                  placeholder="Example: Friday Dinner Rush Checklist"
                />
              )}

              <Input
                label="Section / time window"
                value={sectionName}
                onChangeText={setSectionName}
                placeholder="Example: Pre-Rush Walk Through"
              />

              <Input
                label="Deadline"
                value={deadline}
                onChangeText={setDeadline}
                placeholder="Example: 3:30 PM"
              />

              <Text style={styles.fieldLabel}>Assign to</Text>
              <View style={styles.chipRow}>
                {availableTargets.map((target) => (
                  <TouchableOpacity
                    key={target.id}
                    style={[styles.chip, assigneeId === target.id && styles.chipActive]}
                    onPress={() => setAssigneeId(target.id)}
                    disabled={!canCreateStoreWork}
                  >
                    <Text style={[styles.chipText, assigneeId === target.id && styles.chipTextActive]}>
                      {target.shortName}
                    </Text>
                    <Text style={[styles.chipSubText, assigneeId === target.id && styles.chipTextActive]}>
                      {target.targetType === 'store' ? 'Store' : target.roleLabel}
                    </Text>
                  </TouchableOpacity>
                ))}
              </View>

              <Text style={styles.fieldLabel}>Task type</Text>
              <View style={styles.chipRow}>
                {TASK_TYPES.map((type) => (
                  <TouchableOpacity
                    key={type}
                    style={[styles.chip, taskType === type && styles.chipActive]}
                    onPress={() => setTaskType(type)}
                  >
                    <Text style={[styles.chipText, taskType === type && styles.chipTextActive]}>{type}</Text>
                  </TouchableOpacity>
                ))}
              </View>

              <Text style={styles.fieldLabel}>Task family</Text>
              <View style={styles.chipRow}>
                {TASK_FAMILIES.map((family) => (
                  <TouchableOpacity
                    key={family}
                    style={[styles.chip, taskFamily === family && styles.chipActive]}
                    onPress={() => setTaskFamily(family)}
                    disabled={!canCreateStoreWork}
                  >
                    <Text style={[styles.chipText, taskFamily === family && styles.chipTextActive]}>{family}</Text>
                  </TouchableOpacity>
                ))}
              </View>

              <Input
                label="Instructions"
                value={instructions}
                onChangeText={setInstructions}
                placeholder="What does the employee need to know?"
                multiline
                numberOfLines={4}
              />

              <Button
                title={mode === 'task' ? 'Add Task to Today' : 'Create Checklist'}
                onPress={mode === 'task' ? saveTask : saveChecklist}
                disabled={!canCreateStoreWork || (mode === 'task' ? !taskTitle.trim() : !checklistName.trim())}
              />
            </View>

            {createdItems.length > 0 && (
              <View style={styles.panel}>
                <Text style={styles.panelTitle}>Just Created</Text>
                {createdItems.map((item) => (
                  <View key={item.id} style={styles.createdRow}>
                    <View style={styles.createdText}>
                      <StatusBadge status="in_progress" label={item.kind} />
                      <Text style={styles.createdTitle}>{item.title}</Text>
                      <Text style={styles.createdMeta}>{item.meta}</Text>
                    </View>
                    <Button title="Assign" size="small" variant="secondary" onPress={() => onNavigate?.('assign')} />
                  </View>
                ))}
              </View>
            )}
          </View>

          <View style={styles.previewColumn}>
            <View style={styles.panel}>
              <Text style={styles.panelTitle}>Active Checklist Preview</Text>
              <DataRow label="Template" value="Pizza Restaurant Daily" />
              <DataRow label="Sections" value="4" />
              <DataRow label="Tasks" value="28" />
              <DataRow label="Current window" value="Pre-Rush" />
              <DataRow label="Structure" value="Checklist -> Task" />
            </View>

            {ACTIVE_CHECKLIST.map((section) => (
              <SectionAccordion
                key={section.id}
                title={section.title}
                meta={section.meta}
                status={section.status}
              >
                {section.tasks.map((task) => (
                  <TaskRow
                    key={task.title}
                    title={task.title}
                    meta={task.meta}
                    status={task.status}
                    onPress={() => onNavigate?.('taskdetail')}
                  />
                ))}
              </SectionAccordion>
            ))}
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
  },
  layout: {
    gap: 16,
  },
  layoutWide: {
    flexDirection: 'row',
    alignItems: 'flex-start',
  },
  formColumn: {
    flex: 1.1,
    gap: 16,
  },
  previewColumn: {
    flex: 1,
    gap: 12,
  },
  permissionPanel: {
    backgroundColor: colors.surfaceElevated,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 16,
    padding: 16,
    gap: 6,
  },
  permissionTitle: {
    fontSize: 14,
    fontWeight: '800',
    color: colors.text,
  },
  permissionCopy: {
    fontSize: 13,
    lineHeight: 18,
    color: colors.textSecondary,
  },
  modeSwitch: {
    flexDirection: 'row',
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 16,
    padding: 4,
    gap: 4,
  },
  modeButton: {
    flex: 1,
    minHeight: 44,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 12,
  },
  modeButtonActive: {
    backgroundColor: colors.accent,
  },
  modeText: {
    fontSize: 15,
    fontWeight: '700',
    color: colors.textSecondary,
  },
  modeTextActive: {
    color: colors.textInverse,
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
  fieldLabel: {
    fontSize: 14,
    fontWeight: '700',
    color: colors.textSecondary,
  },
  chipRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  chip: {
    minHeight: 38,
    justifyContent: 'center',
    paddingHorizontal: 12,
    borderRadius: 999,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
  },
  chipActive: {
    backgroundColor: colors.surfacePressed,
    borderColor: colors.accent,
  },
  chipText: {
    fontSize: 13,
    fontWeight: '700',
    color: colors.textSecondary,
  },
  chipTextActive: {
    color: colors.accent,
  },
  chipSubText: {
    fontSize: 10,
    fontWeight: '700',
    color: colors.textMuted,
    marginTop: 2,
  },
  createdRow: {
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: 12,
    gap: 12,
  },
  createdText: {
    gap: 6,
  },
  createdTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: colors.text,
  },
  createdMeta: {
    fontSize: 13,
    lineHeight: 18,
    color: colors.textSecondary,
  },
  bottomPadding: {
    height: 90,
  },
});

export default ChecklistBuilderScreen;
