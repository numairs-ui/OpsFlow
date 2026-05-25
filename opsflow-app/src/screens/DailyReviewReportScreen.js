import React, { useState } from 'react';
import { 
  View, 
  Text, 
  StyleSheet, 
  ScrollView, 
  TouchableOpacity,
} from 'react-native';
import { colors, shadows } from '../theme/colors';
import { Card } from '../components/Card';
import { Button } from '../components/Button';
import { StatusBadge } from '../components/StatusBadge';

const REPORT_DATA = {
  date: 'May 2, 2026',
  day: 'Friday',
  checklist: 'Pizza Restaurant Daily Operations',
  manager: 'Marcus R.',
  totalSections: 4,
  completedSections: 4,
  totalTasks: 28,
  completedTasks: 27,
  overdueTasks: 1,
  notes: 'One task (Cash Reconciliation) was completed 15 min late due to rush. All other tasks completed on time.',
};

const SECTIONS = [
  {
    id: '1',
    name: 'Opening Manager',
    status: 'completed',
    completedBy: 'Marcus R.',
    time: '10:45 AM',
    tasksTotal: 8,
    tasksCompleted: 8,
    overdue: 0,
  },
  {
    id: '2',
    name: 'Product Management',
    status: 'completed',
    completedBy: 'Sarah L.',
    time: '11:30 AM',
    tasksTotal: 5,
    tasksCompleted: 5,
    overdue: 0,
  },
  {
    id: '3',
    name: 'Pre-Rush Walk Through',
    status: 'completed',
    completedBy: 'Jake T.',
    time: '3:45 PM',
    tasksTotal: 6,
    tasksCompleted: 6,
    overdue: 0,
  },
  {
    id: '4',
    name: 'Closing Checklist',
    status: 'completed_late',
    completedBy: 'Marcus R.',
    time: '7:15 PM',
    tasksTotal: 9,
    tasksCompleted: 8,
    overdue: 1,
    lateTask: 'Cash Reconciliation (15 min)',
  },
];

const ISSUES = [
  {
    id: '1',
    severity: 'medium',
    title: 'Cash Reconciliation Late',
    description: 'Completed 15 minutes past deadline due to evening rush',
    resolved: true,
  },
];

export const DailyReviewReportScreen = () => {
  const [exported, setExported] = useState(false);
  const [shared, setShared] = useState(false);

  const getStatusLabel = (status) => {
    switch (status) {
      case 'completed': return 'Complete';
      case 'completed_late': return 'Complete (Late)';
      default: return 'Pending';
    }
  };

  const getStatusColor = (status) => {
    switch (status) {
      case 'completed': return colors.success;
      case 'completed_late': return colors.warning;
      default: return colors.textMuted;
    }
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity style={styles.backButton}>
          <Text style={styles.backText}>←</Text>
        </TouchableOpacity>
        <View style={styles.headerContent}>
          <Text style={styles.headerTitle}>Daily Report</Text>
          <Text style={styles.headerDate}>{REPORT_DATA.date}</Text>
        </View>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        <Card style={styles.summaryCard}>
          <View style={styles.summaryHeader}>
            <View>
              <Text style={styles.checklistName}>{REPORT_DATA.checklist}</Text>
              <Text style={styles.dayText}>{REPORT_DATA.day} - {REPORT_DATA.date}</Text>
            </View>
            <StatusBadge status="completed" label="Complete" />
          </View>

          <View style={styles.statsGrid}>
            <View style={styles.statBox}>
              <Text style={styles.statValue}>
                {REPORT_DATA.completedSections}/{REPORT_DATA.totalSections}
              </Text>
              <Text style={styles.statLabel}>Sections</Text>
            </View>
            <View style={styles.statBox}>
              <Text style={[styles.statValue, { color: colors.success }]}>
                {REPORT_DATA.completedTasks}
              </Text>
              <Text style={styles.statLabel}>Tasks Done</Text>
            </View>
            <View style={styles.statBox}>
              <Text style={[styles.statValue, { color: colors.warning }]}>
                {REPORT_DATA.overdueTasks}
              </Text>
              <Text style={styles.statLabel}>Late</Text>
            </View>
            <View style={styles.statBox}>
              <Text style={styles.statValue}>
                {Math.round((REPORT_DATA.completedTasks / REPORT_DATA.totalTasks) * 100)}%
              </Text>
              <Text style={styles.statLabel}>Score</Text>
            </View>
          </View>

          <View style={styles.managerInfo}>
            <Text style={styles.managerLabel}>Manager on Duty</Text>
            <Text style={styles.managerName}>{REPORT_DATA.manager}</Text>
          </View>
        </Card>

        <Text style={styles.sectionTitle}>Section Breakdown</Text>

        {SECTIONS.map((section) => (
          <Card key={section.id} style={styles.sectionCard}>
            <View style={styles.sectionHeader}>
              <View style={styles.sectionInfo}>
                <Text style={styles.sectionName}>{section.name}</Text>
                <Text style={styles.sectionMeta}>
                  by {section.completedBy} at {section.time}
                </Text>
              </View>
              <View style={[
                styles.statusBadge,
                { backgroundColor: getStatusColor(section.status) + '20' }
              ]}>
                <Text style={[
                  styles.statusText,
                  { color: getStatusColor(section.status) }
                ]}>
                  {getStatusLabel(section.status)}
                </Text>
              </View>
            </View>

            <View style={styles.tasksSummary}>
              <Text style={styles.tasksText}>
                {section.tasksCompleted}/{section.tasksTotal} tasks
              </Text>
              <View style={styles.progressBar}>
                <View 
                  style={[
                    styles.progressFill,
                    { 
                      width: `${(section.tasksCompleted / section.tasksTotal) * 100}%`,
                      backgroundColor: section.overdue > 0 ? colors.warning : colors.success,
                    }
                  ]} 
                />
              </View>
            </View>

            {section.lateTask && (
              <View style={styles.lateIndicator}>
                <Text style={styles.lateText}>
                  ⚠️ {section.lateTask}
                </Text>
              </View>
            )}
          </Card>
        ))}

        {ISSUES.length > 0 && (
          <>
            <Text style={styles.sectionTitle}>Issues & Notes</Text>
            {ISSUES.map((issue) => (
              <Card 
                key={issue.id} 
                style={[
                  styles.issueCard,
                  { borderLeftWidth: 4, borderLeftColor: colors.warning }
                ]}
              >
                <View style={styles.issueHeader}>
                  <View style={[
                    styles.issueBadge,
                    { backgroundColor: colors.warning + '20' }
                  ]}>
                    <Text style={styles.issueBadgeText}>Issue</Text>
                  </View>
                  {issue.resolved && (
                    <View style={styles.resolvedBadge}>
                      <Text style={styles.resolvedText}>Resolved</Text>
                    </View>
                  )}
                </View>
                <Text style={styles.issueTitle}>{issue.title}</Text>
                <Text style={styles.issueDescription}>{issue.description}</Text>
              </Card>
            ))}
          </>
        )}

        {REPORT_DATA.notes && (
          <>
            <Text style={styles.sectionTitle}>Manager Notes</Text>
            <Card style={styles.notesCard}>
              <Text style={styles.notesText}>{REPORT_DATA.notes}</Text>
            </Card>
          </>
        )}

        <Card style={styles.signOffCard}>
          <View style={styles.signOffHeader}>
            <Text style={styles.signOffTitle}>Manager Sign-off</Text>
            <Text style={styles.signOffDate}>May 2, 2026 at 7:30 PM</Text>
          </View>
          <View style={styles.signatureBox}>
            <Text style={styles.signature}>MR</Text>
            <Text style={styles.signatureLabel}>Marcus R.</Text>
          </View>
        </Card>

        <View style={styles.actionButtons}>
          <Button 
            title={exported ? "PDF Exported" : "Export PDF"}
            onPress={() => setExported(true)}
            variant="secondary"
            size="medium"
            style={styles.actionButton}
          />
          <Button 
            title={shared ? "Shared" : "Share"}
            onPress={() => setShared(true)}
            variant="primary"
            size="medium"
            style={styles.actionButton}
          />
        </View>
        {(exported || shared) && (
          <Text style={styles.actionFeedback}>
            {shared ? 'Daily report shared with store leadership.' : 'PDF export prepared for review.'}
          </Text>
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
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingTop: 16,
    paddingBottom: 12,
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  backButton: {
    width: 40,
    height: 40,
    borderRadius: 12,
    backgroundColor: colors.surfaceElevated,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
  },
  backText: {
    fontSize: 20,
    color: colors.text,
  },
  headerContent: {
    flex: 1,
  },
  headerTitle: {
    fontSize: 24,
    fontWeight: '700',
    color: colors.text,
  },
  headerDate: {
    fontSize: 13,
    color: colors.textMuted,
    marginTop: 2,
  },
  content: {
    flex: 1,
    padding: 16,
  },
  summaryCard: {
    marginBottom: 24,
  },
  summaryHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 20,
  },
  checklistName: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.text,
  },
  dayText: {
    fontSize: 14,
    color: colors.textMuted,
    marginTop: 4,
  },
  statsGrid: {
    flexDirection: 'row',
    marginBottom: 20,
  },
  statBox: {
    flex: 1,
    alignItems: 'center',
    paddingVertical: 12,
    backgroundColor: colors.surfaceElevated,
    borderRadius: 12,
    marginHorizontal: 4,
  },
  statValue: {
    fontSize: 24,
    fontWeight: '700',
    color: colors.text,
  },
  statLabel: {
    fontSize: 11,
    color: colors.textMuted,
    textTransform: 'uppercase',
    marginTop: 4,
  },
  managerInfo: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  managerLabel: {
    fontSize: 12,
    color: colors.textMuted,
    textTransform: 'uppercase',
  },
  managerName: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: colors.text,
    marginBottom: 12,
    marginTop: 8,
  },
  sectionCard: {
    marginBottom: 12,
  },
  sectionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 12,
  },
  sectionInfo: {},
  sectionName: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
  },
  sectionMeta: {
    fontSize: 12,
    color: colors.textMuted,
    marginTop: 2,
  },
  statusBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 6,
  },
  statusText: {
    fontSize: 12,
    fontWeight: '600',
  },
  tasksSummary: {
    marginTop: 8,
  },
  tasksText: {
    fontSize: 13,
    color: colors.textSecondary,
    marginBottom: 8,
  },
  progressBar: {
    height: 6,
    backgroundColor: colors.border,
    borderRadius: 3,
    overflow: 'hidden',
  },
  progressFill: {
    height: '100%',
    borderRadius: 3,
  },
  lateIndicator: {
    backgroundColor: colors.warning + '15',
    padding: 10,
    borderRadius: 8,
    marginTop: 12,
  },
  lateText: {
    fontSize: 13,
    color: colors.warning,
  },
  issueCard: {
    marginBottom: 12,
  },
  issueHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  issueBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 4,
  },
  issueBadgeText: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.warning,
    textTransform: 'uppercase',
  },
  resolvedBadge: {
    backgroundColor: colors.success + '20',
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 4,
  },
  resolvedText: {
    fontSize: 11,
    fontWeight: '600',
    color: colors.success,
  },
  issueTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
    marginBottom: 4,
  },
  issueDescription: {
    fontSize: 14,
    color: colors.textSecondary,
    lineHeight: 20,
  },
  notesCard: {
    marginBottom: 24,
  },
  notesText: {
    fontSize: 14,
    color: colors.textSecondary,
    lineHeight: 22,
    fontStyle: 'italic',
  },
  signOffCard: {
    marginBottom: 24,
  },
  signOffHeader: {
    marginBottom: 16,
  },
  signOffTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
  },
  signOffDate: {
    fontSize: 12,
    color: colors.textMuted,
    marginTop: 2,
  },
  signatureBox: {
    alignItems: 'center',
    paddingVertical: 16,
    backgroundColor: colors.surfaceElevated,
    borderRadius: 12,
  },
  signature: {
    fontSize: 32,
    fontWeight: '700',
    color: colors.accent,
    fontStyle: 'italic',
  },
  signatureLabel: {
    fontSize: 14,
    color: colors.textMuted,
    marginTop: 4,
  },
  actionButtons: {
    flexDirection: 'row',
    gap: 12,
  },
  actionFeedback: {
    fontSize: 13,
    fontWeight: '700',
    color: colors.success,
    textAlign: 'center',
    marginTop: 10,
  },
  actionButton: {
    flex: 1,
  },
  bottomPadding: {
    height: 40,
  },
});

export default DailyReviewReportScreen;
