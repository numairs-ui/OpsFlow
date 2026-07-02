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

const VARIANCE_DATA = [
  {
    id: '1',
    product: '12" Dough',
    expected: 50,
    actual: 45,
    variance: -5,
    variancePercent: -10,
    date: 'May 2, 2026',
    location: 'Walk-in Cooler',
    status: 'open',
    rootCause: null,
    resolution: null,
  },
  {
    id: '2',
    product: '14" Dough',
    expected: 35,
    actual: 38,
    variance: 3,
    variancePercent: 8.6,
    date: 'May 2, 2026',
    location: 'Walk-in Cooler',
    status: 'resolved',
    rootCause: 'Extra production',
    resolution: 'Adjusted next day order',
  },
  {
    id: '3',
    product: 'Cheese',
    expected: 45,
    actual: 40,
    variance: -5,
    variancePercent: -11.1,
    date: 'May 1, 2026',
    location: 'Walk-in Cooler',
    status: 'open',
    rootCause: null,
    resolution: null,
  },
];

const ROOT_CAUSES = [
  'Delivery short/over',
  'Spoilage/Waste',
  'Theft/Loss',
  'Extra production',
  'Recording error',
  'Other',
];

export const VarianceManagementScreen = () => {
  const [variances, setVariances] = useState(VARIANCE_DATA);
  const [selectedVariance, setSelectedVariance] = useState(null);
  const [rootCause, setRootCause] = useState('');
  const [notes, setNotes] = useState('');
  const [resolution, setResolution] = useState('');

  const openVariances = variances.filter(v => v.status === 'open');
  const resolvedVariances = variances.filter(v => v.status === 'resolved');

  const resolveVariance = (id) => {
    setVariances(variances.map(v => 
      v.id === id 
        ? { 
            ...v, 
            status: 'resolved', 
            rootCause, 
            resolution,
            resolvedAt: new Date().toLocaleDateString(),
          } 
        : v
    ));
    setSelectedVariance(null);
    setRootCause('');
    setNotes('');
    setResolution('');
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity style={styles.backButton}>
          <Text style={styles.backText}>←</Text>
        </TouchableOpacity>
        <View style={styles.headerContent}>
          <Text style={styles.headerTitle}>Variance Management</Text>
          <Text style={styles.headerSubtitle}>Track inventory discrepancies</Text>
        </View>
      </View>

      <View style={styles.summaryBar}>
        <View style={styles.summaryItem}>
          <Text style={[styles.summaryValue, { color: colors.danger }]}>
            {openVariances.filter(v => v.variancePercent < -5).length}
          </Text>
          <Text style={styles.summaryLabel}>Critical (>5%)</Text>
        </View>
        <View style={styles.summaryItem}>
          <Text style={[styles.summaryValue, { color: colors.warning }]}>
            {openVariances.length}
          </Text>
          <Text style={styles.summaryLabel}>Open</Text>
        </View>
        <View style={styles.summaryItem}>
          <Text style={[styles.summaryValue, { color: colors.success }]}>
            {resolvedVariances.length}
          </Text>
          <Text style={styles.summaryLabel}>Resolved</Text>
        </View>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        {openVariances.length > 0 && (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>⚠️ Open Variances</Text>
            {openVariances.map((variance) => (
              <TouchableOpacity
                key={variance.id}
                onPress={() => setSelectedVariance(selectedVariance?.id === variance.id ? null : variance)}
              >
                <Card 
                  style={[
                    styles.varianceCard,
                    variance.variancePercent <= -5 && styles.varianceCardCritical,
                  ]}
                >
                  <View style={styles.cardHeader}>
                    <Text style={styles.productName}>{variance.product}</Text>
                    <View style={[
                      styles.statusBadge,
                      { backgroundColor: variance.variancePercent <= -5 ? colors.danger + '20' : colors.warning + '20' }
                    ]}>
                      <Text style={[
                        styles.statusText,
                        { color: variance.variancePercent <= -5 ? colors.danger : colors.warning }
                      ]}>
                        {variance.variancePercent.toFixed(1)}%
                      </Text>
                    </View>
                  </View>

                  <View style={styles.varianceDetails}>
                    <View style={styles.varianceItem}>
                      <Text style={styles.varianceLabel}>Expected</Text>
                      <Text style={styles.varianceValue}>{variance.expected}</Text>
                    </View>
                    <View style={styles.varianceItem}>
                      <Text style={styles.varianceLabel}>Actual</Text>
                      <Text style={styles.varianceValue}>{variance.actual}</Text>
                    </View>
                    <View style={styles.varianceItem}>
                      <Text style={styles.varianceLabel}>Diff</Text>
                      <Text style={[
                        styles.varianceValue,
                        { color: variance.variance < 0 ? colors.danger : colors.success }
                      ]}>
                        {variance.variance > 0 ? '+' : ''}{variance.variance}
                      </Text>
                    </View>
                  </View>

                  <View style={styles.cardMeta}>
                    <Text style={styles.metaText}>{variance.date} • {variance.location}</Text>
                  </View>

                  {selectedVariance?.id === variance.id && (
                    <View style={styles.resolutionSection}>
                      <Text style={styles.resolutionTitle}>Resolve Variance</Text>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>Root Cause</Text>
                        <View style={styles.rootCauseButtons}>
                          {ROOT_CAUSES.map((cause) => (
                            <TouchableOpacity
                              key={cause}
                              style={[
                                styles.causeButton,
                                rootCause === cause && styles.causeButtonActive,
                              ]}
                              onPress={() => setRootCause(cause)}
                            >
                              <Text style={[
                                styles.causeButtonText,
                                rootCause === cause && styles.causeButtonTextActive,
                              ]}>
                                {cause}
                              </Text>
                            </TouchableOpacity>
                          ))}
                        </View>
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>Resolution Notes</Text>
                        <TextInput
                          style={styles.notesInput}
                          value={resolution}
                          onChangeText={setResolution}
                          placeholder="How was this resolved?"
                          placeholderTextColor={colors.textMuted}
                          multiline
                          numberOfLines={2}
                        />
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>Manager Notes (Optional)</Text>
                        <TextInput
                          style={styles.notesInput}
                          value={notes}
                          onChangeText={setNotes}
                          placeholder="Additional observations..."
                          placeholderTextColor={colors.textMuted}
                          multiline
                          numberOfLines={2}
                        />
                      </View>

                      <Button 
                        title="Resolve"
                        onPress={() => resolveVariance(variance.id)}
                        variant="primary"
                        size="medium"
                        disabled={!rootCause || !resolution}
                      />
                    </View>
                  )}
                </Card>
              </TouchableOpacity>
            ))}
          </View>
        )}

        {resolvedVariances.length > 0 && (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>✓ Resolved</Text>
            {resolvedVariances.map((variance) => (
              <Card key={variance.id} style={styles.resolvedCard}>
                <View style={styles.cardHeader}>
                  <Text style={styles.productName}>{variance.product}</Text>
                  <View style={[styles.statusBadge, { backgroundColor: colors.success + '20' }]}>
                    <Text style={[styles.statusText, { color: colors.success }]}>Resolved</Text>
                  </View>
                </View>
                <View style={styles.resolutionDetails}>
                  <View style={styles.resolutionItem}>
                    <Text style={styles.resolutionLabel}>Root Cause</Text>
                    <Text style={styles.resolutionValue}>{variance.rootCause}</Text>
                  </View>
                  <View style={styles.resolutionItem}>
                    <Text style={styles.resolutionLabel}>Resolution</Text>
                    <Text style={styles.resolutionValue}>{variance.resolution}</Text>
                  </View>
                  <View style={styles.resolutionItem}>
                    <Text style={styles.resolutionLabel}>Resolved</Text>
                    <Text style={styles.resolutionValue}>{variance.resolvedAt}</Text>
                  </View>
                </View>
              </Card>
            ))}
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
  summaryBar: {
    flexDirection: 'row',
    backgroundColor: colors.surface,
    paddingVertical: 16,
    paddingHorizontal: 20,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    gap: 24,
  },
  summaryItem: {
    alignItems: 'center',
  },
  summaryValue: {
    fontSize: 28,
    fontWeight: '700',
  },
  summaryLabel: {
    fontSize: 12,
    color: colors.textMuted,
    textTransform: 'uppercase',
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
    fontSize: 18,
    fontWeight: '700',
    color: colors.text,
    marginBottom: 12,
  },
  varianceCard: {
    marginBottom: 12,
  },
  varianceCardCritical: {
    borderLeftWidth: 4,
    borderLeftColor: colors.danger,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  productName: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.text,
  },
  statusBadge: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 8,
  },
  statusText: {
    fontSize: 14,
    fontWeight: '700',
  },
  varianceDetails: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 12,
  },
  varianceItem: {
    alignItems: 'center',
  },
  varianceLabel: {
    fontSize: 11,
    color: colors.textMuted,
    textTransform: 'uppercase',
    marginBottom: 4,
  },
  varianceValue: {
    fontSize: 20,
    fontWeight: '700',
    color: colors.text,
  },
  cardMeta: {
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  metaText: {
    fontSize: 13,
    color: colors.textMuted,
  },
  resolutionSection: {
    marginTop: 16,
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  resolutionTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: colors.text,
    marginBottom: 16,
  },
  inputGroup: {
    marginBottom: 16,
  },
  inputLabel: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.textSecondary,
    marginBottom: 8,
  },
  rootCauseButtons: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  causeButton: {
    paddingHorizontal: 12,
    paddingVertical: 8,
    backgroundColor: colors.surfaceElevated,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: colors.border,
  },
  causeButtonActive: {
    backgroundColor: colors.accent,
    borderColor: colors.accent,
  },
  causeButtonText: {
    fontSize: 13,
    color: colors.textSecondary,
  },
  causeButtonTextActive: {
    color: colors.primary,
    fontWeight: '600',
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
  resolvedCard: {
    marginBottom: 12,
    opacity: 0.8,
  },
  resolutionDetails: {
    gap: 8,
  },
  resolutionItem: {},
  resolutionLabel: {
    fontSize: 11,
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
  },
  resolutionValue: {
    fontSize: 14,
    color: colors.text,
    marginTop: 2,
  },
  bottomPadding: {
    height: 40,
  },
});

export default VarianceManagementScreen;