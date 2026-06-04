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
  getRoleLabel,
  requiresTypedCompletionIdentity,
} from '../data/accounts';
import {
  DataRow,
  ScreenHeader,
  StickyActionBar,
} from '../components/OperationalPrimitives';

const TASK = {
  title: 'Set Up Make Line',
  section: 'Opening Manager',
  deadline: '11:00 AM',
  assignee: 'F0890 Shared Chromebook',
  status: 'in_progress',
  requiredData: ['Temperature reading', 'Dough batch number'],
};

const TASK_REQUIREMENTS = [
  'Verify labels, MDOG prep, portion cups, dough pulls, proofing, expiration checks, and working thermometers.',
  'Record the required temperature and dough batch number before submitting.',
  'If product is missing, mislabeled, expired, or out of range, add a note and flag the manager.',
];

const AUDIT_TRAIL = [
  { label: 'Created from template', value: 'Pizza Restaurant Daily' },
  { label: 'Assigned by', value: 'Maria R. at 9:18 AM' },
  { label: 'Started by', value: 'Marcus R. at 10:04 AM' },
  { label: 'Last completed', value: 'May 1 by Marcus R.' },
];

export const TaskDetailScreen = ({ activeAccount }) => {
  const [temperature, setTemperature] = useState('');
  const [batch, setBatch] = useState('');
  const [notes, setNotes] = useState('');
  const [unableReason, setUnableReason] = useState('');
  const [completionName, setCompletionName] = useState('');
  const [completionInitials, setCompletionInitials] = useState('');
  const [acknowledged, setAcknowledged] = useState(false);
  const [status, setStatus] = useState(TASK.status);
  const { width } = useWindowDimensions();
  const isWide = width >= 900;
  const needsTypedIdentity = requiresTypedCompletionIdentity(activeAccount);

  const requiredDataDone = temperature.trim().length > 0 && batch.trim().length > 0;
  const identityDone = !needsTypedIdentity || (completionName.trim().length > 0 && completionInitials.trim().length > 0);
  const canComplete = acknowledged && requiredDataDone && identityDone;

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
              <DataRow label="Active role/profile" value={getRoleLabel(activeAccount?.role)} />
              <DataRow label="Required data" value={TASK.requiredData.join(', ')} />
              <DataRow
                label="Completion identity"
                value={needsTypedIdentity ? 'Typed name + initials required' : activeAccount?.displayName}
              />
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
              <Text style={styles.panelTitle}>Task Instructions</Text>
              {TASK_REQUIREMENTS.map((requirement) => (
                <View key={requirement} style={styles.requirementRow}>
                  <Text style={styles.requirementMarker}>-</Text>
                  <Text style={styles.requirementText}>{requirement}</Text>
                </View>
              ))}
              <TouchableOpacity
                style={styles.ackRow}
                onPress={() => setAcknowledged((current) => !current)}
                activeOpacity={0.8}
              >
                <View style={[styles.checkbox, acknowledged && styles.checkboxDone]}>
                  {acknowledged && <Text style={styles.checkmark}>OK</Text>}
                </View>
                <Text style={styles.ackText}>
                  I verified the task instructions and required observations.
                </Text>
              </TouchableOpacity>
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

            {needsTypedIdentity && (
              <View style={styles.panel}>
                <Text style={styles.panelTitle}>Shared Device Completion</Text>
                <Text style={styles.identityCopy}>
                  Store Account submissions require the employee name and initials because the Chromebook login is shared.
                </Text>
                <Input
                  label="Employee name"
                  value={completionName}
                  onChangeText={setCompletionName}
                  placeholder="Who completed this task?"
                />
                <Input
                  label="Initials"
                  value={completionInitials}
                  onChangeText={setCompletionInitials}
                  placeholder="Example: SP"
                  autoCapitalize="characters"
                />
              </View>
            )}

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
              <DataRow label="Completion mode" value={needsTypedIdentity ? 'Shared device' : 'Named account'} />
            </View>
          </View>
        </View>

        <View style={styles.bottomPadding} />
      </ScrollView>

      <StickyActionBar>
        {status === 'completed' ? (
          <View style={styles.completedBanner}>
            <Text style={styles.completedText}>
              Completed with timestamp, role/profile, and {needsTypedIdentity ? 'typed name + initials' : 'account identity'} captured.
            </Text>
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
  requirementRow: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 10,
  },
  requirementMarker: {
    fontSize: 15,
    lineHeight: 22,
    color: colors.accent,
    fontWeight: '800',
  },
  requirementText: {
    flex: 1,
    fontSize: 15,
    lineHeight: 22,
    color: colors.text,
  },
  ackRow: {
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
  ackText: {
    flex: 1,
    fontSize: 15,
    lineHeight: 20,
    color: colors.text,
  },
  identityCopy: {
    fontSize: 13,
    lineHeight: 18,
    color: colors.textSecondary,
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
