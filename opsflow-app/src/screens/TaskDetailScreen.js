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
  DataRow,
  ScreenHeader,
  StickyActionBar,
} from '../components/OperationalPrimitives';

const TASK = {
  title: 'Set Up Make Line',
  section: 'Opening Manager',
  deadline: '11:00 AM',
  assignee: 'Marcus R.',
  status: 'in_progress',
  requiredData: ['Temperature reading', 'Dough batch number'],
};

const INITIAL_SUBTASKS = [
  { id: 'labels', title: 'Date all products with label system', done: true },
  { id: 'mdog', title: 'Prep products according to MDOG', done: true },
  { id: 'cups', title: 'Place portion cups in each product', done: false },
  { id: 'print', title: 'Print fresh MDOG for station', done: false },
  { id: 'dough', title: 'Pull dough for lunch usage', done: false },
  { id: 'proof', title: 'Confirm all dough sizes are properly proofed', done: false },
  { id: 'expiry', title: 'Check expiration dates and discard if needed', done: false },
  { id: 'thermo', title: 'Place working thermometers in dough sizes', done: false },
];

const AUDIT_TRAIL = [
  { label: 'Created from template', value: 'Pizza Restaurant Daily' },
  { label: 'Assigned by', value: 'Maria R. at 9:18 AM' },
  { label: 'Started by', value: 'Marcus R. at 10:04 AM' },
  { label: 'Last completed', value: 'May 1 by Marcus R.' },
];

export const TaskDetailScreen = () => {
  const [subtasks, setSubtasks] = useState(INITIAL_SUBTASKS);
  const [temperature, setTemperature] = useState('');
  const [batch, setBatch] = useState('');
  const [notes, setNotes] = useState('');
  const [unableReason, setUnableReason] = useState('');
  const [status, setStatus] = useState(TASK.status);
  const { width } = useWindowDimensions();
  const isWide = width >= 900;

  const completedCount = subtasks.filter((task) => task.done).length;
  const allSubtasksDone = completedCount === subtasks.length;
  const requiredDataDone = temperature.trim().length > 0 && batch.trim().length > 0;
  const canComplete = allSubtasksDone && requiredDataDone;

  const toggleSubtask = (id) => {
    setSubtasks((current) => current.map((task) => (
      task.id === id ? { ...task, done: !task.done } : task
    )));
  };

  const markUnable = () => {
    setStatus('unable');
  };

  const markComplete = () => {
    setStatus('completed');
  };

  return (
    <View style={styles.container}>
      <ScreenHeader
        eyebrow={TASK.section}
        title={TASK.title}
        subtitle="Follow the station setup instructions, capture required data, and leave an audit trail."
        action={isWide ? <StatusBadge status={status} label={status.replace('_', ' ')} /> : null}
      />

      <ScrollView
        style={styles.scroll}
        contentContainerStyle={[styles.content, isWide && styles.contentWide]}
        showsVerticalScrollIndicator={false}
      >
        <View style={[styles.layout, isWide && styles.layoutWide]}>
          <View style={styles.mainColumn}>
            <View style={styles.panel}>
              <View style={styles.panelHeader}>
                <Text style={styles.panelTitle}>Task Requirements</Text>
                {!isWide && <StatusBadge status={status} label={status.replace('_', ' ')} />}
              </View>
              <DataRow label="Deadline" value={TASK.deadline} />
              <DataRow label="Assignee" value={TASK.assignee} />
              <DataRow label="Progress" value={`${completedCount}/${subtasks.length} subtasks`} />
              <DataRow label="Required data" value={TASK.requiredData.join(', ')} />
            </View>

            <View style={styles.panel}>
              <Text style={styles.panelTitle}>Instructions</Text>
              <Text style={styles.instructions}>
                Make line setup must be complete before lunch readiness. Confirm product labels,
                MDOG prep, portion cups, dough pulls, proofing, expiration checks, and working
                thermometers. Any missing product, bad label, or failed temperature should be
                noted before completion.
              </Text>
            </View>

            <View style={styles.panel}>
              <Text style={styles.panelTitle}>Subtasks</Text>
              {subtasks.map((task) => (
                <TouchableOpacity
                  key={task.id}
                  style={styles.subtaskRow}
                  onPress={() => toggleSubtask(task.id)}
                  activeOpacity={0.8}
                >
                  <View style={[styles.checkbox, task.done && styles.checkboxDone]}>
                    {task.done && <Text style={styles.checkmark}>OK</Text>}
                  </View>
                  <Text style={[styles.subtaskText, task.done && styles.subtaskTextDone]}>
                    {task.title}
                  </Text>
                </TouchableOpacity>
              ))}
            </View>
          </View>

          <View style={styles.sideColumn}>
            <View style={styles.panel}>
              <Text style={styles.panelTitle}>Required Data</Text>
              <Input
                label="Temperature Reading"
                value={temperature}
                onChangeText={setTemperature}
                placeholder="Enter degrees F"
                keyboardType="numeric"
                helper="Target range: 34-40 F"
                error={temperature && Number(temperature) > 40 ? 'Out of range. Add a note before completion.' : undefined}
              />
              <Input
                label="Dough Batch Number"
                value={batch}
                onChangeText={setBatch}
                placeholder="Example: DB-2026-001"
              />
              <Input
                label="Notes"
                value={notes}
                onChangeText={setNotes}
                placeholder="Add observations or issues"
                multiline
                numberOfLines={4}
              />
            </View>

            {status === 'unable' && (
              <View style={styles.panel}>
                <Text style={styles.panelTitle}>Unable to Complete</Text>
                <Input
                  label="Reason"
                  value={unableReason}
                  onChangeText={setUnableReason}
                  placeholder="What blocked the task?"
                  multiline
                  numberOfLines={3}
                />
              </View>
            )}

            <View style={styles.panel}>
              <Text style={styles.panelTitle}>Audit Trail</Text>
              {AUDIT_TRAIL.map((item) => (
                <DataRow key={item.label} label={item.label} value={item.value} />
              ))}
            </View>
          </View>
        </View>

        <View style={styles.bottomPadding} />
      </ScrollView>

      <StickyActionBar>
        {status === 'completed' ? (
          <View style={styles.completedBanner}>
            <Text style={styles.completedText}>Completed with timestamp and assignee captured.</Text>
          </View>
        ) : (
          <View style={[styles.actions, isWide && styles.actionsWide]}>
            <Button
              title="Unable"
              variant="danger"
              onPress={markUnable}
              style={styles.actionButton}
            />
            <Button
              title="Mark Complete"
              onPress={markComplete}
              disabled={!canComplete}
              style={styles.actionButton}
            />
          </View>
        )}
      </StickyActionBar>
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
  mainColumn: {
    flex: 1.35,
    gap: 16,
  },
  sideColumn: {
    flex: 1,
    gap: 16,
  },
  panel: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 16,
    padding: 16,
    gap: 12,
  },
  panelHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: 12,
  },
  panelTitle: {
    fontSize: 17,
    fontWeight: '700',
    color: colors.text,
  },
  instructions: {
    fontSize: 15,
    lineHeight: 22,
    color: colors.text,
  },
  subtaskRow: {
    minHeight: 56,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: 12,
  },
  checkbox: {
    width: 34,
    height: 34,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.surface,
  },
  checkboxDone: {
    backgroundColor: colors.success,
    borderColor: colors.success,
  },
  checkmark: {
    fontSize: 10,
    fontWeight: '800',
    color: colors.textInverse,
  },
  subtaskText: {
    flex: 1,
    fontSize: 15,
    lineHeight: 20,
    color: colors.text,
  },
  subtaskTextDone: {
    color: colors.textSecondary,
    textDecorationLine: 'line-through',
  },
  actions: {
    flexDirection: 'row',
    gap: 12,
  },
  actionsWide: {
    justifyContent: 'flex-end',
  },
  actionButton: {
    flex: 1,
  },
  completedBanner: {
    minHeight: 52,
    borderRadius: 14,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.successLight,
  },
  completedText: {
    fontSize: 15,
    fontWeight: '700',
    color: colors.success,
  },
  bottomPadding: {
    height: 120,
  },
});

export default TaskDetailScreen;
