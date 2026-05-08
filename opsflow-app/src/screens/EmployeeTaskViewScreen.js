import React, { useState } from 'react';
import { 
  View, 
  Text, 
  StyleSheet, 
  ScrollView, 
  TouchableOpacity,
  TextInput,
} from 'react-native';
import { colors, shadows } from '../theme/colors';
import { Card } from '../components/Card';
import { Button } from '../components/Button';
import { StatusBadge } from '../components/StatusBadge';

const MY_TASKS = [
  {
    id: '1',
    title: 'Stock Pepsi Cooler',
    section: 'Closing Checklist',
    assignedBy: 'Marcus R.',
    deadline: '5:30 PM',
    estimatedTime: 10,
    instructions: 'Ensure all sizes are stocked. Check expiration dates. Rotate older products to front.',
    priority: 'medium',
    status: 'pending',
  },
  {
    id: '2',
    title: 'Bathroom cleaned',
    section: 'Closing Checklist',
    assignedBy: 'Marcus R.',
    deadline: '6:30 PM',
    estimatedTime: 15,
    instructions: 'Clean sink, toilet, floor. Restock paper products. Check supplies.',
    priority: 'high',
    status: 'pending',
  },
  {
    id: '3',
    title: 'Walk-in swept and mopped',
    section: 'Closing Checklist',
    assignedBy: 'Marcus R.',
    deadline: '7:00 PM',
    estimatedTime: 20,
    instructions: 'Sweep floor, mop with sanitizer. Organize boxes. Move older items to front.',
    priority: 'medium',
    status: 'in_progress',
  },
];

const COMPLETED_HISTORY = [
  {
    id: '101',
    title: 'Clean dining tables',
    section: 'Pre-Rush',
    completedAt: '4:30 PM',
    notes: 'All tables cleaned and chairs stacked',
  },
  {
    id: '102',
    title: 'Prep sauce station',
    section: 'Product Management',
    completedAt: '11:15 AM',
    notes: 'Ladles restocked, containers cleaned',
  },
];

export const EmployeeTaskViewScreen = () => {
  const [tasks, setTasks] = useState(MY_TASKS);
  const [selectedTask, setSelectedTask] = useState(null);
  const [notes, setNotes] = useState('');
  const [managerPingedTaskId, setManagerPingedTaskId] = useState(null);
  const [showHistory, setShowHistory] = useState(false);

  const pendingTasks = tasks.filter(t => t.status !== 'completed');
  const inProgressTasks = tasks.filter(t => t.status === 'in_progress');

  const startTask = (taskId) => {
    setTasks(tasks.map(t => 
      t.id === taskId ? { ...t, status: 'in_progress' } : t
    ));
  };

  const completeTask = (taskId) => {
    setTasks(tasks.map(t => 
      t.id === taskId ? { ...t, status: 'completed', completionNotes: notes } : t
    ));
    setNotes('');
    setSelectedTask(null);
  };

  const getPriorityColor = (priority) => {
    switch (priority) {
      case 'high': return colors.danger;
      case 'medium': return colors.warning;
      default: return colors.textMuted;
    }
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>My Tasks</Text>
        <Text style={styles.headerSubtitle}>Your assigned duties</Text>
      </View>

      <View style={styles.statsBar}>
        <View style={styles.statItem}>
          <Text style={styles.statValue}>{pendingTasks.length}</Text>
          <Text style={styles.statLabel}>Pending</Text>
        </View>
        <View style={styles.statItem}>
          <Text style={[styles.statValue, { color: colors.info }]}>{inProgressTasks.length}</Text>
          <Text style={styles.statLabel}>In Progress</Text>
        </View>
        <View style={styles.statItem}>
          <Text style={[styles.statValue, { color: colors.success }]}>
            {tasks.filter(t => t.status === 'completed').length}
          </Text>
          <Text style={styles.statLabel}>Done Today</Text>
        </View>
      </View>

      <View style={styles.toggleBar}>
        <TouchableOpacity 
          style={[styles.toggleButton, !showHistory && styles.toggleButtonActive]}
          onPress={() => setShowHistory(false)}
        >
          <Text style={[styles.toggleText, !showHistory && styles.toggleTextActive]}>
            Active ({pendingTasks.length})
          </Text>
        </TouchableOpacity>
        <TouchableOpacity 
          style={[styles.toggleButton, showHistory && styles.toggleButtonActive]}
          onPress={() => setShowHistory(true)}
        >
          <Text style={[styles.toggleText, showHistory && styles.toggleTextActive]}>
            History ({COMPLETED_HISTORY.length})
          </Text>
        </TouchableOpacity>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        {!showHistory ? (
          pendingTasks.length > 0 ? (
            pendingTasks.map((task) => (
              <TouchableOpacity
                key={task.id}
                onPress={() => setSelectedTask(selectedTask?.id === task.id ? null : task)}
              >
                <Card 
                  style={[
                    styles.taskCard,
                    task.status === 'in_progress' && styles.taskCardActive,
                  ]}
                >
                  <View style={styles.taskHeader}>
                    <View style={[
                      styles.priorityDot,
                      { backgroundColor: getPriorityColor(task.priority) }
                    ]} />
                    <Text style={styles.taskTitle}>{task.title}</Text>
                  </View>
                  
                  <Text style={styles.taskSection}>{task.section}</Text>
                  
                  <View style={styles.taskMeta}>
                    <View style={styles.metaItem}>
                      <Text style={styles.metaLabel}>Deadline</Text>
                      <Text style={styles.metaValue}>{task.deadline}</Text>
                    </View>
                    <View style={styles.metaItem}>
                      <Text style={styles.metaLabel}>Est. Time</Text>
                      <Text style={styles.metaValue}>{task.estimatedTime} min</Text>
                    </View>
                    <View style={styles.metaItem}>
                      <Text style={styles.metaLabel}>Assigned By</Text>
                      <Text style={styles.metaValue}>{task.assignedBy}</Text>
                    </View>
                  </View>

                  {selectedTask?.id === task.id && (
                    <View style={styles.expandedContent}>
                      <Text style={styles.instructionsLabel}>Instructions</Text>
                      <Text style={styles.instructions}>{task.instructions}</Text>
                      
                      <View style={styles.notesSection}>
                        <Text style={styles.notesLabel}>Add completion notes (optional)</Text>
                        <TextInput
                          style={styles.notesInput}
                          value={notes}
                          onChangeText={setNotes}
                          placeholder="Any observations..."
                          placeholderTextColor={colors.textMuted}
                          multiline
                          numberOfLines={2}
                        />
                      </View>

                      <View style={styles.taskActions}>
                        {task.status !== 'in_progress' ? (
                          <Button 
                            title="Start Task"
                            onPress={() => startTask(task.id)}
                            variant="primary"
                            size="medium"
                          />
                        ) : (
                          <Button 
                            title="Mark Complete"
                            onPress={() => completeTask(task.id)}
                            variant="primary"
                            size="medium"
                          />
                        )}
                        <Button 
                          title={managerPingedTaskId === task.id ? "Manager Asked" : "Ask Manager"}
                          onPress={() => setManagerPingedTaskId(task.id)}
                          variant="secondary"
                          size="medium"
                        />
                        {managerPingedTaskId === task.id && (
                          <Text style={styles.managerAskedText}>Manager notified for this task.</Text>
                        )}
                      </View>
                    </View>
                  )}

                  <View style={styles.expandHint}>
                    <Text style={styles.expandHintText}>
                      {selectedTask?.id === task.id ? 'Tap to collapse' : 'Tap for details'}
                    </Text>
                  </View>
                </Card>
              </TouchableOpacity>
            ))
          ) : (
            <View style={styles.emptyState}>
              <Text style={styles.emptyIcon}>✓</Text>
              <Text style={styles.emptyTitle}>All Done!</Text>
              <Text style={styles.emptyText}>No pending tasks</Text>
            </View>
          )
        ) : (
          COMPLETED_HISTORY.map((task) => (
            <Card key={task.id} style={styles.historyCard}>
              <View style={styles.historyHeader}>
                <Text style={styles.historyTitle}>{task.title}</Text>
                <StatusBadge status="completed" label="Done" />
              </View>
              <Text style={styles.historySection}>{task.section}</Text>
              <View style={styles.historyMeta}>
                <Text style={styles.historyTime}>Completed at {task.completedAt}</Text>
              </View>
              {task.notes && (
                <Text style={styles.historyNotes}>Notes: {task.notes}</Text>
              )}
            </Card>
          ))
        )}

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
  header: {
    paddingHorizontal: 20,
    paddingTop: 20,
    paddingBottom: 16,
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  managerAskedText: {
    fontSize: 12,
    fontWeight: '700',
    color: colors.success,
    marginTop: 8,
  },
  headerTitle: {
    fontSize: 28,
    fontWeight: '700',
    color: colors.text,
  },
  headerSubtitle: {
    fontSize: 14,
    color: colors.textMuted,
    marginTop: 2,
  },
  statsBar: {
    flexDirection: 'row',
    backgroundColor: colors.surface,
    paddingVertical: 16,
    paddingHorizontal: 20,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    gap: 24,
  },
  statItem: {
    alignItems: 'center',
  },
  statValue: {
    fontSize: 28,
    fontWeight: '700',
    color: colors.text,
  },
  statLabel: {
    fontSize: 12,
    color: colors.textMuted,
    textTransform: 'uppercase',
    marginTop: 2,
  },
  toggleBar: {
    flexDirection: 'row',
    padding: 12,
    gap: 8,
  },
  toggleButton: {
    flex: 1,
    paddingVertical: 10,
    borderRadius: 20,
    backgroundColor: colors.surfaceElevated,
    alignItems: 'center',
  },
  toggleButtonActive: {
    backgroundColor: colors.accent,
  },
  toggleText: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.textSecondary,
  },
  toggleTextActive: {
    color: colors.primary,
  },
  content: {
    flex: 1,
    padding: 16,
  },
  taskCard: {
    marginBottom: 12,
  },
  taskCardActive: {
    borderWidth: 2,
    borderColor: colors.info,
  },
  taskHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  priorityDot: {
    width: 10,
    height: 10,
    borderRadius: 5,
    marginRight: 10,
  },
  taskTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.text,
    flex: 1,
  },
  taskSection: {
    fontSize: 13,
    color: colors.textMuted,
    marginBottom: 12,
  },
  taskMeta: {
    flexDirection: 'row',
    gap: 16,
  },
  metaItem: {},
  metaLabel: {
    fontSize: 11,
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
  },
  metaValue: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.text,
    marginTop: 2,
  },
  expandedContent: {
    marginTop: 16,
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  instructionsLabel: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.textSecondary,
    marginBottom: 8,
  },
  instructions: {
    fontSize: 14,
    color: colors.text,
    lineHeight: 22,
  },
  notesSection: {
    marginTop: 16,
  },
  notesLabel: {
    fontSize: 13,
    color: colors.textMuted,
    marginBottom: 8,
  },
  notesInput: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 10,
    padding: 12,
    fontSize: 14,
    color: colors.text,
    minHeight: 60,
    borderWidth: 1,
    borderColor: colors.border,
  },
  taskActions: {
    flexDirection: 'row',
    gap: 10,
    marginTop: 16,
  },
  expandHint: {
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    alignItems: 'center',
  },
  expandHintText: {
    fontSize: 13,
    color: colors.textMuted,
  },
  historyCard: {
    marginBottom: 12,
    opacity: 0.8,
  },
  historyHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  historyTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
  },
  historySection: {
    fontSize: 13,
    color: colors.textMuted,
    marginBottom: 8,
  },
  historyMeta: {
    marginBottom: 4,
  },
  historyTime: {
    fontSize: 13,
    color: colors.textSecondary,
  },
  historyNotes: {
    fontSize: 13,
    color: colors.textMuted,
    fontStyle: 'italic',
    marginTop: 4,
  },
  emptyState: {
    alignItems: 'center',
    paddingVertical: 48,
  },
  emptyIcon: {
    fontSize: 48,
    color: colors.success,
    marginBottom: 16,
  },
  emptyTitle: {
    fontSize: 20,
    fontWeight: '700',
    color: colors.text,
    marginBottom: 8,
  },
  emptyText: {
    fontSize: 14,
    color: colors.textMuted,
  },
  bottomPadding: {
    height: 40,
  },
});

export default EmployeeTaskViewScreen;
