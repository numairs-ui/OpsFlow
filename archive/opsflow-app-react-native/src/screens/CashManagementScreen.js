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

const DENOMINATIONS = [
  { label: '$100', key: '100', count: 0 },
  { label: '$50', key: '50', count: 0 },
  { label: '$20', key: '20', count: 0 },
  { label: '$10', key: '10', count: 0 },
  { label: '$5', key: '5', count: 0 },
  { label: '$1', key: '1', count: 0 },
  { label: 'Quarters', key: 'quarters', count: 0 },
  { label: 'Dimes', key: 'dimes', count: 0 },
  { label: 'Nickels', key: 'nickels', count: 0 },
  { label: 'Pennies', key: 'pennies', count: 0 },
];

const TILL_TYPES = [
  { id: 'till_a', name: 'Till A', type: 'Opening' },
  { id: 'till_b', name: 'Till B', type: 'Opening' },
  { id: 'safe', name: 'Safe', type: 'Opening' },
];

export const CashManagementScreen = () => {
  const [selectedTill, setSelectedTill] = useState(TILL_TYPES[0]);
  const [counts, setCounts] = useState(
    DENOMINATIONS.reduce((acc, d) => ({ ...acc, [d.key]: 0 }), {})
  );
  const [shouldBe, setShouldBe] = useState('250');
  const [managerInitials, setManagerInitials] = useState('');
  const [signed, setSigned] = useState(false);

  const updateCount = (key, value) => {
    setCounts({ ...counts, [key]: parseInt(value) || 0 });
  };

  const calculateTotal = () => {
    return (
      counts['100'] * 100 +
      counts['50'] * 50 +
      counts['20'] * 20 +
      counts['10'] * 10 +
      counts['5'] * 5 +
      counts['1'] * 1 +
      counts['quarters'] * 0.25 +
      counts['dimes'] * 0.10 +
      counts['nickels'] * 0.05 +
      counts['pennies'] * 0.01
    );
  };

  const calculateVariance = () => {
    const total = calculateTotal();
    const expected = parseFloat(shouldBe) || 0;
    return total - expected;
  };

  const isVarianceOK = () => calculateVariance() === 0;

  const signOff = () => {
    if (managerInitials.length >= 2) {
      setSigned(true);
    }
  };

  const reset = () => {
    setCounts(DENOMINATIONS.reduce((acc, d) => ({ ...acc, [d.key]: 0 }), {}));
    setSigned(false);
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity style={styles.backButton}>
          <Text style={styles.backText}>←</Text>
        </TouchableOpacity>
        <View style={styles.headerContent}>
          <Text style={styles.headerTitle}>Cash Management</Text>
          <Text style={styles.headerSubtitle}>Till counting & reconciliation</Text>
        </View>
      </View>

      <View style={styles.tillSelector}>
        <Text style={styles.selectorLabel}>Select Till</Text>
        <View style={styles.tillOptions}>
          {TILL_TYPES.map((till) => (
            <TouchableOpacity
              key={till.id}
              style={[
                styles.tillOption,
                selectedTill.id === till.id && styles.tillOptionActive,
              ]}
              onPress={() => {
                setSelectedTill(till);
                setSigned(false);
              }}
            >
              <Text style={[
                styles.tillOptionText,
                selectedTill.id === till.id && styles.tillOptionTextActive,
              ]}>
                {till.name}
              </Text>
              <Text style={styles.tillTypeText}>{till.type}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        <Card style={styles.countsCard}>
          <Text style={styles.cardTitle}>Count Each Denomination</Text>
          
          <View style={styles.denominationList}>
            {DENOMINATIONS.map((denom) => (
              <View key={denom.key} style={styles.denomRow}>
                <Text style={styles.denomLabel}>{denom.label}</Text>
                <View style={styles.denomInputWrapper}>
                  <TouchableOpacity 
                    style={styles.denomButton}
                    onPress={() => updateCount(denom.key, Math.max(0, counts[denom.key] - 1))}
                  >
                    <Text style={styles.denomButtonText}>−</Text>
                  </TouchableOpacity>
                  <TextInput
                    style={styles.denomInput}
                    value={String(counts[denom.key])}
                    onChangeText={(v) => updateCount(denom.key, v)}
                    keyboardType="numeric"
                  />
                  <TouchableOpacity 
                    style={styles.denomButton}
                    onPress={() => updateCount(denom.key, counts[denom.key] + 1)}
                  >
                    <Text style={styles.denomButtonText}>+</Text>
                  </TouchableOpacity>
                </View>
              </View>
            ))}
          </View>
        </Card>

        <Card style={styles.summaryCard}>
          <View style={styles.summaryRow}>
            <Text style={styles.summaryLabel}>Should Be</Text>
            <TextInput
              style={styles.shouldBeInput}
              value={shouldBe}
              onChangeText={setShouldBe}
              keyboardType="numeric"
            />
          </View>
          
          <View style={styles.summaryDivider} />
          
          <View style={styles.summaryRow}>
            <Text style={styles.summaryLabel}>Actual Count</Text>
            <Text style={styles.actualTotal}>${calculateTotal().toFixed(2)}</Text>
          </View>
          
          <View style={styles.summaryDivider} />
          
          <View style={styles.varianceRow}>
            <Text style={styles.summaryLabel}>Variance</Text>
            <View style={[
              styles.varianceBadge,
              { backgroundColor: isVarianceOK() ? colors.success + '20' : colors.danger + '20' }
            ]}>
              <Text style={[
                styles.varianceText,
                { color: isVarianceOK() ? colors.success : colors.danger }
              ]}>
                {calculateVariance() >= 0 ? '+' : ''}${calculateVariance().toFixed(2)}
              </Text>
            </View>
          </View>
          
          {!isVarianceOK() && (
            <View style={styles.varianceWarning}>
              <Text style={styles.varianceWarningText}>
                ⚠️ Variance detected! Please recount before signing off.
              </Text>
            </View>
          )}
        </Card>

        {!signed && (
          <Card style={styles.signOffCard}>
            <Text style={styles.cardTitle}>Manager Sign-off</Text>
            <Text style={styles.signOffInstructions}>
              Confirm the count is accurate and complete the reconciliation.
            </Text>
            
            <View style={styles.initialsRow}>
              <Text style={styles.initialsLabel}>Manager Initials:</Text>
              <TextInput
                style={styles.initialsInput}
                value={managerInitials}
                onChangeText={setManagerInitials}
                placeholder="XX"
                placeholderTextColor={colors.textMuted}
                maxLength={3}
                autoCapitalize="characters"
              />
            </View>
            
            <Button 
              title="Sign Off & Complete"
              onPress={signOff}
              variant="primary"
              disabled={managerInitials.length < 2 || !isVarianceOK()}
              size="large"
            />
          </Card>
        )}

        {signed && (
          <Card style={styles.completedCard}>
            <View style={styles.completedHeader}>
              <View style={styles.completedIcon}>
                <Text style={styles.completedIconText}>✓</Text>
              </View>
              <View>
                <Text style={styles.completedTitle}>Reconciliation Complete</Text>
                <Text style={styles.completedSubtitle}>
                  Signed by {managerInitials.toUpperCase()} at {new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </Text>
              </View>
            </View>
            <Button 
              title="Start New Count"
              onPress={reset}
              variant="secondary"
              size="medium"
            />
          </Card>
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
  headerSubtitle: {
    fontSize: 13,
    color: colors.textMuted,
    marginTop: 2,
  },
  tillSelector: {
    paddingHorizontal: 16,
    paddingVertical: 16,
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  selectorLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
    marginBottom: 12,
  },
  tillOptions: {
    flexDirection: 'row',
    gap: 8,
  },
  tillOption: {
    flex: 1,
    paddingVertical: 12,
    paddingHorizontal: 16,
    backgroundColor: colors.surfaceElevated,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: 'center',
  },
  tillOptionActive: {
    backgroundColor: colors.accent,
    borderColor: colors.accent,
  },
  tillOptionText: {
    fontSize: 16,
    fontWeight: '700',
    color: colors.text,
  },
  tillOptionTextActive: {
    color: colors.primary,
  },
  tillTypeText: {
    fontSize: 11,
    color: colors.textMuted,
    marginTop: 2,
  },
  content: {
    flex: 1,
    padding: 16,
  },
  countsCard: {
    marginBottom: 16,
  },
  cardTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: colors.text,
    marginBottom: 16,
  },
  denominationList: {
    gap: 8,
  },
  denomRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: 8,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  denomLabel: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
    width: 80,
  },
  denomInputWrapper: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  denomButton: {
    width: 36,
    height: 36,
    borderRadius: 8,
    backgroundColor: colors.surfaceElevated,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.border,
  },
  denomButtonText: {
    fontSize: 20,
    fontWeight: '600',
    color: colors.text,
  },
  denomInput: {
    width: 60,
    backgroundColor: colors.surfaceElevated,
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
    fontSize: 18,
    fontWeight: '600',
    color: colors.text,
    textAlign: 'center',
    borderWidth: 1,
    borderColor: colors.border,
  },
  summaryCard: {
    marginBottom: 16,
  },
  summaryRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 12,
  },
  summaryLabel: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.textSecondary,
  },
  shouldBeInput: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 8,
    paddingHorizontal: 16,
    paddingVertical: 10,
    fontSize: 20,
    fontWeight: '700',
    color: colors.text,
    width: 100,
    textAlign: 'right',
    borderWidth: 1,
    borderColor: colors.border,
  },
  actualTotal: {
    fontSize: 24,
    fontWeight: '700',
    color: colors.text,
  },
  summaryDivider: {
    height: 1,
    backgroundColor: colors.border,
  },
  varianceRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 12,
  },
  varianceBadge: {
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 8,
  },
  varianceText: {
    fontSize: 20,
    fontWeight: '700',
  },
  varianceWarning: {
    backgroundColor: colors.danger + '15',
    padding: 12,
    borderRadius: 8,
    marginTop: 8,
  },
  varianceWarningText: {
    fontSize: 14,
    color: colors.danger,
  },
  signOffCard: {
    marginBottom: 16,
  },
  signOffInstructions: {
    fontSize: 14,
    color: colors.textSecondary,
    marginBottom: 16,
  },
  initialsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 16,
  },
  initialsLabel: {
    fontSize: 14,
    color: colors.textSecondary,
    marginRight: 12,
  },
  initialsInput: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 8,
    paddingHorizontal: 16,
    paddingVertical: 10,
    fontSize: 20,
    fontWeight: '700',
    color: colors.text,
    width: 80,
    textAlign: 'center',
    borderWidth: 2,
    borderColor: colors.accent,
  },
  completedCard: {
    marginBottom: 16,
    borderWidth: 2,
    borderColor: colors.success,
  },
  completedHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 16,
  },
  completedIcon: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: colors.success,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 16,
  },
  completedIconText: {
    fontSize: 24,
    fontWeight: '700',
    color: colors.text,
  },
  completedTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.success,
  },
  completedSubtitle: {
    fontSize: 13,
    color: colors.textMuted,
    marginTop: 2,
  },
  bottomPadding: {
    height: 40,
  },
});

export default CashManagementScreen;