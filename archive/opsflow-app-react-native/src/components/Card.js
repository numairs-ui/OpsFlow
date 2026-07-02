import React from 'react';
import { View, StyleSheet, TouchableOpacity } from 'react-native';
import { colors, shadows } from '../theme/colors';

export const Card = ({ children, style, onPress, elevated = false }) => {
  const cardStyles = [
    styles.card,
    elevated && styles.elevated,
    style,
  ];

  if (onPress) {
    return (
      <TouchableOpacity style={cardStyles} onPress={onPress} activeOpacity={0.8}>
        {children}
      </TouchableOpacity>
    );
  }

  return <View style={cardStyles}>{children}</View>;
};

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surface,
    borderRadius: 16,
    padding: 16,
    borderWidth: 1,
    borderColor: colors.border,
  },
  elevated: {
    backgroundColor: colors.surface,
    ...shadows.small,
  },
});
