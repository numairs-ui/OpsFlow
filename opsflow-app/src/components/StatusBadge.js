import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { colors } from '../theme/colors';
import { typography } from '../theme/typography';

export const StatusBadge = ({ status, label }) => {
  const getStatusColor = () => {
    switch (status) {
      case 'completed':
      case 'signed':
        return { bg: colors.successLight, text: colors.success, dot: colors.success };
      case 'in_progress':
        return { bg: colors.infoLight, text: colors.info, dot: colors.info };
      case 'pending':
        return { bg: colors.surfacePressed, text: colors.accent, dot: colors.accent };
      case 'due_soon':
        return { bg: colors.warningLight, text: colors.warning, dot: colors.warning };
      case 'overdue':
      case 'variance':
      case 'unable':
        return { bg: colors.dangerLight, text: colors.danger, dot: colors.danger };
      default:
        return { bg: colors.borderLight, text: colors.textSecondary, dot: colors.textMuted };
    }
  };

  const statusStyle = getStatusColor();

  return (
    <View style={[styles.badge, { backgroundColor: statusStyle.bg }]}>
      <View style={[styles.dot, { backgroundColor: statusStyle.dot }]} />
      <Text style={[styles.label, { color: statusStyle.text }]}>{label}</Text>
    </View>
  );
};

const styles = StyleSheet.create({
  badge: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 5,
    paddingHorizontal: 10,
    borderRadius: 20,
    gap: 6,
  },
  dot: {
    width: 6,
    height: 6,
    borderRadius: 3,
  },
  label: {
    ...typography.badge,
    textTransform: 'uppercase',
  },
});
