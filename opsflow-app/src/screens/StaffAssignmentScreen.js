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

const STAFF_MEMBERS = [
  { id: '1', name: 'Marcus R.', role: 'Shift Lead', avatar: 'M' },
  { id: '2', name: 'Sarah L.', role: 'Crew', avatar: 'S' },
  { id: '3', name: 'Jake T.', role: 'Crew', avatar: 'J' },
  { id: '4', name: 'Emily K.', role: 'Crew', avatar: 'E' },
];

const INITIAL_TASKS = [
  {
    id: '1',
    title: 'Stock Pepsi Cooler',
    instructions: 'Ensure all sizes are stocked. Check expiration dates.',
    estimatedMinutes: 10,
    assignedTo: null,
    deadline: '5:30 PM',
    status: 'pending',
  },
  {
    id: '2',
    title: 'Trash out & replace liner',
    instructions: 'Remove all bags, tie off, place new liner. Check dumpsters.',
    estimatedMinutes: 8,
    assignedTo: null,
    deadline: '6:00 PM',
    status: 'pending',
  },
  {
    id: '3',
    title: 'Bathroom cleaned',
    instructions: 'Clean sink, toilet, floor. Restock paper products.',
    estimatedMinutes: 15,
    assignedTo: null,
    deadline: '6:30 PM',
    status: 'pending',
  },
  {
    id: '4',
    title: 'Walk-in swept and mopped',
    instructions: 'Sweep floor, mop with sanitizer. Organize boxes.',
    estimatedMinutes: 20,
    assignedTo: null,
    deadline: '7:00 PM',
    status: 'pending',
  },
  {
    id: '5',
    title: 'Prep sauce station',
    instructions: 'Restock ladles, spoons. Clean sauces containers.',
    estimatedMinutes: 10,
    assignedTo: null,
    deadline: '5:00 PM',
    status: 'pending',
  },
  {
    id: '6',
    title: 'Clean dining tables',
    instructions: 'Wipe all tables, check chairs, sweep floor area.',
    estimatedMinutes: 12,
    assignedTo: null,
    deadline: '6:15 PM',
    status: 'pending',
  },
];

export const StaffAssignmentScreen = () => {
  const [tasks, setTasks] = useState(INITIAL_TASKS);
  const [selectedTask, setSelectedTask] = useState(null);
  const [showAssignModal, setShowAssignModal] = useState(false);
  const [sent, setSent] = useState(false);

  const assignTask = (taskId, staffId) => {
    setTasks(tasks.map(task => 
      task.id === taskId 
        ? { ...task, assignedTo: staffId, status: 'assigned' } 
        : task
    ));
    setSent(false);
    setShowAssignModal(false);
    setSelectedTask(null);
  };

  const unassignTask = (taskId) => {
    setTasks(tasks.map(task => 
      task.id === taskId 
        ? { ...task, assignedTo: null, status: 'pending' } 
        : task
    ));
    setSent(false);
  };

  const getStaffById = (id) => STAFF_MEMBERS.find(s => s.id === id);

  const assignedTasks = tasks.filter(t => t.assignedTo);
  const unassignedTasks = tasks.filter(t => !t.assignedTo);

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>Deployment Guide</Text>
        <Text style={styles.headerSubtitle}>Assign closing tasks to staff</Text>
      </View>

      <View style={styles.statsBar}>
        <View style={styles.statItem}>
          <Text style={styles.statValue}>{tasks.length}</Text>
          <Text style={styles.statLabel}>Total</Text>
        </View>
        <View style={styles.statItem}>
          <Text style={styles.statValue}>{unassignedTasks.length}</Text>
          <Text style={styles.statLabel}>Unassigned</Text>
        </View>
        <View style={styles.statItem}>
          <Text style={styles.statValue}>{assignedTasks.length}</Text>
          <Text style={styles.statLabel}>Assigned</Text>
        </View>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        {unassignedTasks.length > 0 && (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Unassigned Tasks</Text>
            {unassignedTasks.map((task) => (
              <Card key={task.id} style={styles.taskCard}>
                <View style={styles.taskHeader}>
                  <Text style={styles.taskTitle}>{task.title}</Text>
                  <TouchableOpacity 
                    style={styles.assignButton}
                    onPress={() => {
                      setSelectedTask(task);
                      setShowAssignModal(true);
                    }}
                  >
                    <Text style={styles.assignButtonText}>Assign</Text>
                  </TouchableOpacity>
                </View>
                <Text style={styles.taskInstructions}>{task.instructions}</Text>
                <View style={styles.taskMeta}>
                  <View style={styles.metaItem}>
                    <Text style={styles.metaLabel}>Est.</Text>
                    <Text style={styles.metaValue}>{task.estimatedMinutes} min</Text>
                  </View>
                  <View style={styles.metaItem}>
                    <Text style={styles.metaLabel}>Due</Text>
                    <Text style={styles.metaValue}>{task.deadline}</Text>
                  </View>
                </View>
              </Card>
            ))}
          </View>
        )}

        {assignedTasks.length > 0 && (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Assigned Tasks</Text>
            {assignedTasks.map((task) => {
              const staff = getStaffById(task.assignedTo);
              return (
                <Card key={task.id} style={styles.taskCard}>
                  <View style={styles.assignedHeader}>
                    <View style={styles.staffInfo}>
                      <View style={styles.staffAvatar}>
                        <Text style={styles.staffAvatarText}>{staff.avatar}</Text>
                      </View>
                      <View>
                        <Text style={styles.staffName}>{staff.name}</Text>
                        <Text style={styles.staffRole}>{staff.role}</Text>
                      </View>
                    </View>
                    <TouchableOpacity 
                      style={styles.unassignButton}
                      onPress={() => unassignTask(task.id)}
                    >
                      <Text style={styles.unassignButtonText}>Reassign</Text>
                    </TouchableOpacity>
                  </View>
                  <Text style={styles.taskTitle}>{task.title}</Text>
                  <Text style={styles.taskInstructions}>{task.instructions}</Text>
                  <View style={styles.taskMeta}>
                    <View style={styles.metaItem}>
                      <Text style={styles.metaLabel}>Est.</Text>
                      <Text style={styles.metaValue}>{task.estimatedMinutes} min</Text>
                    </View>
                    <View style={styles.metaItem}>
                      <Text style={styles.metaLabel}>Due</Text>
                      <Text style={styles.metaValue}>{task.deadline}</Text>
                    </View>
                    <StatusBadge status="assigned" label="Assigned" />
                  </View>
                </Card>
              );
            })}
          </View>
        )}

        <View style={styles.bottomPadding} />
      </ScrollView>

      {showAssignModal && selectedTask && (
        <View style={styles.modalOverlay}>
          <View style={styles.modal}>
            <Text style={styles.modalTitle}>Assign Task</Text>
            <Text style={styles.modalTaskName}>{selectedTask.title}</Text>
            
            <Text style={styles.modalSubtitle}>Select Staff Member</Text>
            
            {STAFF_MEMBERS.map((staff) => (
              <TouchableOpacity
                key={staff.id}
                style={styles.staffOption}
                onPress={() => assignTask(selectedTask.id, staff.id)}
              >
                <View style={styles.staffAvatarLarge}>
                  <Text style={styles.staffAvatarTextLarge}>{staff.avatar}</Text>
                </View>
                <View style={styles.staffOptionInfo}>
                  <Text style={styles.staffOptionName}>{staff.name}</Text>
                  <Text style={styles.staffOptionRole}>{staff.role}</Text>
                </View>
                <View style={styles.staffCheckmark}>
                  <Text style={styles.staffCheckmarkText}>→</Text>
                </View>
              </TouchableOpacity>
            ))}

            <Button 
              title="Cancel" 
              variant="ghost" 
              onPress={() => {
                setShowAssignModal(false);
                setSelectedTask(null);
              }}
              style={styles.cancelButton}
            />
          </View>
        </View>
      )}

      <View style={styles.footer}>
        {sent && <Text style={styles.sentText}>Assignments sent to employee task queues.</Text>}
        <Button 
          title={sent ? "Assignments Sent" : "Send Assignments"} 
          onPress={() => setSent(true)}
          variant="primary"
          size="large"
          style={styles.sendButton}
          disabled={sent || tasks.every(task => !task.assignedTo)}
        />
      </View>
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
  headerTitle: {
    fontSize: 28,
    fontWeight: '700',
    color: colors.text,
    letterSpacing: -0.5,
  },
  headerSubtitle: {
    fontSize: 14,
    color: colors.textMuted,
    marginTop: 4,
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
    fontSize: 24,
    fontWeight: '700',
    color: colors.accent,
  },
  statLabel: {
    fontSize: 12,
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
    marginTop: 2,
  },
  content: {
    flex: 1,
    padding: 16,
  },
  section: {
    marginBottom: 24,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.textSecondary,
    marginBottom: 12,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
  },
  taskCard: {
    marginBottom: 12,
  },
  taskHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 8,
  },
  taskTitle: {
    fontSize: 17,
    fontWeight: '600',
    color: colors.text,
    flex: 1,
  },
  assignButton: {
    backgroundColor: colors.accent,
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 8,
  },
  assignButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.primary,
  },
  taskInstructions: {
    fontSize: 14,
    color: colors.textSecondary,
    lineHeight: 20,
    marginBottom: 12,
  },
  taskMeta: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 16,
  },
  metaItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  metaLabel: {
    fontSize: 12,
    color: colors.textMuted,
    textTransform: 'uppercase',
  },
  metaValue: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.textSecondary,
  },
  assignedHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
    paddingBottom: 12,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  staffInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  staffAvatar: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: colors.accent,
    alignItems: 'center',
    justifyContent: 'center',
  },
  staffAvatarText: {
    fontSize: 16,
    fontWeight: '700',
    color: colors.primary,
  },
  staffName: {
    fontSize: 15,
    fontWeight: '600',
    color: colors.text,
  },
  staffRole: {
    fontSize: 12,
    color: colors.textMuted,
  },
  unassignButton: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    backgroundColor: colors.surfaceElevated,
    borderRadius: 6,
  },
  unassignButtonText: {
    fontSize: 13,
    color: colors.textSecondary,
  },
  modalOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: colors.overlay,
    justifyContent: 'center',
    alignItems: 'center',
    zIndex: 100,
  },
  modal: {
    backgroundColor: colors.surface,
    borderRadius: 20,
    padding: 24,
    width: '85%',
    maxWidth: 340,
    borderWidth: 1,
    borderColor: colors.border,
  },
  modalTitle: {
    fontSize: 22,
    fontWeight: '700',
    color: colors.text,
    textAlign: 'center',
    marginBottom: 8,
  },
  modalTaskName: {
    fontSize: 15,
    color: colors.textSecondary,
    textAlign: 'center',
    marginBottom: 24,
  },
  modalSubtitle: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
    marginBottom: 16,
  },
  staffOption: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
    backgroundColor: colors.surfaceElevated,
    borderRadius: 12,
    marginBottom: 8,
    borderWidth: 1,
    borderColor: colors.border,
  },
  staffAvatarLarge: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: colors.accent,
    alignItems: 'center',
    justifyContent: 'center',
  },
  staffAvatarTextLarge: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.primary,
  },
  staffOptionInfo: {
    flex: 1,
    marginLeft: 16,
  },
  staffOptionName: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
  },
  staffOptionRole: {
    fontSize: 13,
    color: colors.textMuted,
  },
  staffCheckmark: {
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: colors.border,
    alignItems: 'center',
    justifyContent: 'center',
  },
  staffCheckmarkText: {
    fontSize: 16,
    color: colors.text,
  },
  cancelButton: {
    marginTop: 16,
  },
  bottomPadding: {
    height: 100,
  },
  footer: {
    padding: 16,
    backgroundColor: colors.surface,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  sentText: {
    fontSize: 13,
    fontWeight: '700',
    color: colors.success,
    textAlign: 'center',
    marginBottom: 8,
  },
  sendButton: {
    width: '100%',
  },
});

export default StaffAssignmentScreen;
