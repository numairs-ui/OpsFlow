import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';
import { colors } from '../theme/colors';
import { typography } from '../theme/typography';
import { Card } from './Card';
import { StatusBadge } from './StatusBadge';

export const ScreenHeader = ({ eyebrow, title, subtitle, action }) => (
  <View style={styles.header}>
    <View style={styles.headerText}>
      {eyebrow && <Text style={styles.eyebrow}>{eyebrow}</Text>}
      <Text style={styles.title}>{title}</Text>
      {subtitle && <Text style={styles.subtitle}>{subtitle}</Text>}
    </View>
    {action}
  </View>
);

export const DataRow = ({ label, value, valueStyle }) => (
  <View style={styles.dataRow}>
    <Text style={styles.dataLabel}>{label}</Text>
    <Text style={[styles.dataValue, valueStyle]}>{value}</Text>
  </View>
);

export const MetricTile = ({ label, value, status = 'pending', helper }) => (
  <Card style={styles.metricTile}>
    <StatusBadge status={status} label={label} />
    <Text style={styles.metricValue}>{value}</Text>
    {helper && <Text style={styles.metricHelper}>{helper}</Text>}
  </Card>
);

export const SectionAccordion = ({ title, meta, status, expanded = true, onPress, children }) => (
  <Card style={styles.section}>
    <TouchableOpacity style={styles.sectionHeader} onPress={onPress} activeOpacity={0.8}>
      <View style={styles.sectionTitleBlock}>
        <Text style={styles.sectionTitle}>{title}</Text>
        {meta && <Text style={styles.sectionMeta}>{meta}</Text>}
      </View>
      {status && <StatusBadge status={status} label={status.replace('_', ' ')} />}
    </TouchableOpacity>
    {expanded && <View style={styles.sectionBody}>{children}</View>}
  </Card>
);

export const TaskRow = ({ title, meta, assignee, status, onPress }) => (
  <TouchableOpacity style={styles.taskRow} onPress={onPress} activeOpacity={0.8}>
    <View style={styles.taskText}>
      <Text style={styles.taskTitle}>{title}</Text>
      {meta && <Text style={styles.taskMeta}>{meta}</Text>}
    </View>
    <View style={styles.taskSide}>
      {assignee && <Text style={styles.assignee}>{assignee}</Text>}
      {status && <StatusBadge status={status} label={status.replace('_', ' ')} />}
    </View>
  </TouchableOpacity>
);

export const StickyActionBar = ({ children }) => (
  <View style={styles.stickyActionBar}>{children}</View>
);

const styles = StyleSheet.create({
  header: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    gap: 16,
    paddingHorizontal: 16,
    paddingTop: 16,
    paddingBottom: 12,
  },
  headerText: {
    flex: 1,
  },
  eyebrow: {
    ...typography.caption,
    color: colors.accent,
    fontWeight: '700',
    textTransform: 'uppercase',
    marginBottom: 4,
  },
  title: {
    ...typography.screenTitle,
    color: colors.text,
  },
  subtitle: {
    ...typography.bodySmall,
    color: colors.textSecondary,
    marginTop: 4,
  },
  dataRow: {
    minHeight: 32,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 16,
  },
  dataLabel: {
    ...typography.bodySmall,
    color: colors.textSecondary,
  },
  dataValue: {
    ...typography.bodySmall,
    color: colors.text,
    fontWeight: '600',
    textAlign: 'right',
  },
  metricTile: {
    flex: 1,
    minWidth: 140,
    gap: 8,
  },
  metricValue: {
    ...typography.h1,
    color: colors.text,
  },
  metricHelper: {
    ...typography.caption,
    color: colors.textSecondary,
  },
  section: {
    padding: 0,
    overflow: 'hidden',
  },
  sectionHeader: {
    minHeight: 60,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 12,
    padding: 16,
  },
  sectionTitleBlock: {
    flex: 1,
  },
  sectionTitle: {
    ...typography.h3,
    color: colors.text,
  },
  sectionMeta: {
    ...typography.caption,
    color: colors.textSecondary,
    marginTop: 3,
  },
  sectionBody: {
    borderTopWidth: 1,
    borderTopColor: colors.border,
    padding: 12,
    gap: 10,
  },
  taskRow: {
    minHeight: 56,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 12,
    backgroundColor: colors.surfaceElevated,
    borderRadius: 14,
    padding: 12,
  },
  taskText: {
    flex: 1,
  },
  taskTitle: {
    ...typography.body,
    color: colors.text,
    fontWeight: '600',
  },
  taskMeta: {
    ...typography.caption,
    color: colors.textSecondary,
    marginTop: 3,
  },
  taskSide: {
    alignItems: 'flex-end',
    gap: 6,
  },
  assignee: {
    ...typography.caption,
    color: colors.textSecondary,
  },
  stickyActionBar: {
    backgroundColor: colors.surface,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    padding: 16,
    gap: 10,
  },
});
