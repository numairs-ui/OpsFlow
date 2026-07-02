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

const ALERTS_DATA = [
  {
    id: '1',
    type: 'overdue',
    title: 'Cash Reconciliation',
    section: 'Closing Checklist',
    deadline: '6:00 PM',
    timePast: '45 min overdue',
    assignedTo: 'Sarah L.',
    severity: 'high',
    description: 'Till counting and safe deposit not completed',
  },
  {
    id: '2',
    type: 'overdue',
    title: 'Walk-in Organization',
    section: 'Closing Checklist',
    deadline: '6:30 PM',
    timePast: '15 min overdue',
    assignedTo: 'Jake T.',
    severity: 'medium',
    description: 'Walk-in freezer not organized, boxes not stacked',
  },
  {
    id: '3',
    type: 'warning',
    title: 'Deep Clean Make Line',
    section: 'Closing Checklist',
    deadline: '7:00 PM',
    timePast: 'Due in 15 min',
    assignedTo: 'Marcus R.',
    severity: 'low',
    description: 'Make line deep clean and sanitation pending',
  },
  {
    id: '4',
    type: 'pending',
    title: 'Temperature Check',
    section: 'Opening Manager',
    deadline: '11:00 AM',
    timePast: 'Completed on time',
    assignedTo: 'Marcus R.',
    severity: 'none',
    description: 'Walk-in, make line, and oven temps logged',
  },
];

export const OverdueAlertScreen = () => {
  const [alerts, setAlerts] = useState(ALERTS_DATA);
  const [selectedFilter, setSelectedFilter] = useState('all');
  const [dismissedAlerts, setDismissedAlerts] = useState([]);

  const filterAlerts = (filter) => {
    setSelectedFilter(filter);
  };

  const getFilteredAlerts = () => {
    let filtered = alerts;
    if (selectedFilter === 'overdue') {
      filtered = alerts.filter(a => a.type === 'overdue');
    } else if (selectedFilter === 'warning') {
      filtered = alerts.filter(a => a.type === 'warning');
    } else if (selectedFilter === 'pending') {
      filtered = alerts.filter(a => a.type === 'pending');
    }
    return filtered.filter(a => !dismissedAlerts.includes(a.id));
  };

  const dismissAlert = (id, note = '') => {
    setDismissedAlerts([...dismissedAlerts, id]);
  };

  const markComplete = (id) => {
    setAlerts(alerts.map(a => 
      a.id === id ? { ...a, type: 'completed', timePast: 'Completed' } : a
    ));
  };

  const assignToSelf = (id) => {
    setAlerts(alerts.map(a => 
      a.id === id ? { ...a, assignedTo: 'You' } : a
    ));
  };

  const overdueCount = alerts.filter(a => a.type === 'overdue' && !dismissedAlerts.includes(a.id)).length;
  const warningCount = alerts.filter(a => a.type === 'warning' && !dismissedAlerts.includes(a.id)).length;

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>Alert Center</Text>
        <Text style={styles.headerSubtitle}>Tasks requiring attention</Text>
      </View>

      <View style={styles.summaryBar}>
        <View style={styles.summaryItem}>
          <View style={[styles.summaryBadge, { backgroundColor: colors.danger }]}>
            <Text style={styles.summaryBadgeText}>{overdueCount}</Text>
          </View>
          <Text style={styles.summaryLabel}>Overdue</Text>
        </View>
        <View style={styles.summaryItem}>
          <View style={[styles.summaryBadge, { backgroundColor: colors.warning }]}>
            <Text style={styles.summaryBadgeText}>{warningCount}</Text>
          </View>
          <Text style={styles.summaryLabel}>Due Soon</Text>
        </View>
        <View style={styles.summaryItem}>
          <View style={[styles.summaryBadge, { backgroundColor: colors.success }]}>
            <Text style={styles.summaryBadgeText}>
              {alerts.filter(a => a.type === 'completed').length}
            </Text>
          </View>
          <Text style={styles.summaryLabel}>Resolved</Text>
        </View>
      </View>

      <View style={styles.filterBar}>
        {['all', 'overdue', 'warning'].map((filter) => (
          <TouchableOpacity
            key={filter}
            style={[
              styles.filterButton,
              selectedFilter === filter && styles.filterButtonActive,
            ]}
            onPress={() => filterAlerts(filter)}
          >
            <Text style={[
              styles.filterButtonText,
              selectedFilter === filter && styles.filterButtonTextActive,
            ]}>
              {filter.charAt(0).toUpperCase() + filter.slice(1)}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        {getFilteredAlerts().map((alert) => (
          <Card 
            key={alert.id} 
            style={[
              styles.alertCard,
              alert.type === 'overdue' && styles.alertCardOverdue,
              alert.type === 'warning' && styles.alertCardWarning,
              alert.type === 'completed' && styles.alertCardCompleted,
            ]}
          >
            <View style={styles.alertHeader}>
              <View style={[
                styles.alertTypeBadge,
                { 
                  backgroundColor: alert.type === 'overdue' 
                    ? colors.danger 
                    : alert.type === 'warning' 
                      ? colors.warning 
                      : colors.success 
                }
              ]}>
                <Text style={styles.alertTypeText}>
                  {alert.type === 'overdue' ? 'OVERDUE' : alert.type === 'warning' ? 'DUE SOON' : 'DONE'}
                </Text>
              </View>
              <Text style={[
                styles.alertTime,
                { color: alert.type === 'overdue' ? colors.danger : colors.warning }
              ]}>
                {alert.timePast}
              </Text>
            </View>

            <Text style={styles.alertTitle}>{alert.title}</Text>
            <Text style={styles.alertSection}>{alert.section}</Text>
            <Text style={styles.alertDescription}>{alert.description}</Text>

            <View style={styles.alertMeta}>
              <View style={styles.metaItem}>
                <Text style={styles.metaLabel}>Deadline</Text>
                <Text style={styles.metaValue}>{alert.deadline}</Text>
              </View>
              <View style={styles.metaItem}>
                <Text style={styles.metaLabel}>Assigned</Text>
                <Text style={styles.metaValue}>{alert.assignedTo}</Text>
              </View>
            </View>

            {alert.type !== 'completed' && (
              <View style={styles.alertActions}>
                <Button 
                  title="Mark Done"
                  onPress={() => markComplete(alert.id)}
                  variant="primary"
                  size="small"
                  style={styles.actionButton}
                />
                <Button 
                  title="Assign to Me"
                  onPress={() => assignToSelf(alert.id)}
                  variant="secondary"
                  size="small"
                  style={styles.actionButton}
                />
                <TouchableOpacity 
                  style={styles.dismissButton}
                  onPress={() => dismissAlert(alert.id)}
                >
                  <Text style={styles.dismissText}>Dismiss</Text>
                </TouchableOpacity>
              </View>
            )}
          </Card>
        ))}

        {getFilteredAlerts().length === 0 && (
          <View style={styles.emptyState}>
            <Text style={styles.emptyIcon}>✓</Text>
            <Text style={styles.emptyTitle}>All Clear!</Text>
            <Text style={styles.emptyText}>No alerts in this category</Text>
          </View>
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
  summaryBar: {
    flexDirection: 'row',
    backgroundColor: colors.surface,
    paddingVertical: 16,
    paddingHorizontal: 20,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    gap: 32,
  },
  summaryItem: {
    alignItems: 'center',
  },
  summaryBadge: {
    width: 40,
    height: 40,
    borderRadius: 20,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 4,
  },
  summaryBadgeText: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.text,
  },
  summaryLabel: {
    fontSize: 12,
    color: colors.textMuted,
    textTransform: 'uppercase',
  },
  filterBar: {
    flexDirection: 'row',
    padding: 12,
    gap: 8,
  },
  filterButton: {
    flex: 1,
    paddingVertical: 10,
    paddingHorizontal: 16,
    borderRadius: 20,
    backgroundColor: colors.surfaceElevated,
    alignItems: 'center',
  },
  filterButtonActive: {
    backgroundColor: colors.accent,
  },
  filterButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.textSecondary,
  },
  filterButtonTextActive: {
    color: colors.primary,
  },
  content: {
    flex: 1,
    padding: 16,
  },
  alertCard: {
    marginBottom: 12,
    borderLeftWidth: 4,
    borderLeftColor: colors.danger,
  },
  alertCardOverdue: {
    borderLeftColor: colors.danger,
  },
  alertCardWarning: {
    borderLeftColor: colors.warning,
  },
  alertCardCompleted: {
    borderLeftColor: colors.success,
    opacity: 0.7,
  },
  alertHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  alertTypeBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 4,
  },
  alertTypeText: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.text,
    letterSpacing: 0.5,
  },
  alertTime: {
    fontSize: 13,
    fontWeight: '600',
  },
  alertTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.text,
    marginBottom: 4,
  },
  alertSection: {
    fontSize: 13,
    color: colors.textMuted,
    marginBottom: 8,
  },
  alertDescription: {
    fontSize: 14,
    color: colors.textSecondary,
    lineHeight: 20,
    marginBottom: 12,
  },
  alertMeta: {
    flexDirection: 'row',
    gap: 24,
    marginBottom: 16,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  metaItem: {},
  metaLabel: {
    fontSize: 11,
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
    marginBottom: 2,
  },
  metaValue: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.text,
  },
  alertActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  actionButton: {
    flex: 1,
  },
  dismissButton: {
    paddingVertical: 10,
    paddingHorizontal: 12,
  },
  dismissText: {
    fontSize: 14,
    color: colors.textMuted,
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

export default OverdueAlertScreen;