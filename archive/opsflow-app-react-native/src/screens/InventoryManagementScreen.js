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

const INITIAL_INVENTORY = [
  {
    id: '1',
    product: '12" Dough',
    onHand: 45,
    productionDate: '2026-05-01',
    expirationDate: '2026-05-03',
    todayNeed: 30,
    day2Need: 35,
    day3Need: 40,
    action: 'A',
  },
  {
    id: '2',
    product: '14" Dough',
    onHand: 30,
    productionDate: '2026-05-01',
    expirationDate: '2026-05-03',
    todayNeed: 25,
    day2Need: 28,
    day3Need: 30,
    action: 'A',
  },
  {
    id: '3',
    product: '16" Dough',
    onHand: 20,
    productionDate: '2026-05-01',
    expirationDate: '2026-05-03',
    todayNeed: 18,
    day2Need: 20,
    day3Need: 22,
    action: 'B',
  },
  {
    id: '4',
    product: 'Cheese',
    onHand: 50,
    productionDate: '2026-04-30',
    expirationDate: '2026-05-05',
    todayNeed: 40,
    day2Need: 45,
    day3Need: 45,
    action: 'A',
  },
];

const ACTION_LABELS = {
  A: 'We have enough',
  B: 'We have more than needed',
  C: 'We don\'t have enough',
};

export const InventoryManagementScreen = () => {
  const [inventory, setInventory] = useState(INITIAL_INVENTORY);
  const [editingId, setEditingId] = useState(null);
  const [saved, setSaved] = useState(false);

  const updateField = (id, field, value) => {
    setSaved(false);
    setInventory(inventory.map(item => 
      item.id === id ? { ...item, [field]: value } : item
    ));
  };

  const calculateTotal = (item) => {
    return item.todayNeed + item.day2Need + item.day3Need;
  };

  const calculateVariance = (item) => {
    const total = calculateTotal(item);
    return item.onHand - total;
  };

  const getVarianceColor = (item) => {
    const variance = calculateVariance(item);
    if (variance >= 0) return colors.success;
    return colors.danger;
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity style={styles.backButton}>
          <Text style={styles.backText}>←</Text>
        </TouchableOpacity>
        <View style={styles.headerContent}>
          <Text style={styles.headerTitle}>3-Day Inventory</Text>
          <Text style={styles.headerSubtitle}>Dough & Cheese Management</Text>
        </View>
      </View>

      <View style={styles.legend}>
        <View style={styles.legendItem}>
          <View style={[styles.legendDot, { backgroundColor: colors.success }]} />
          <Text style={styles.legendText}>Action A: Have enough</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendDot, { backgroundColor: colors.warning }]} />
          <Text style={styles.legendText}>Action B: Extra stock</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendDot, { backgroundColor: colors.danger }]} />
          <Text style={styles.legendText}>Action C: Need more</Text>
        </View>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        <View style={styles.tableHeader}>
          <Text style={[styles.th, styles.thProduct]}>Product</Text>
          <Text style={[styles.th, styles.thSmall]}>On Hand</Text>
          <Text style={[styles.th, styles.thSmall]}>Total Need</Text>
          <Text style={[styles.th, styles.thSmall]}>Variance</Text>
          <Text style={[styles.th, styles.thAction]}>Action</Text>
        </View>

        {inventory.map((item) => (
          <Card key={item.id} style={styles.inventoryCard}>
            <View style={styles.cardHeader}>
              <Text style={styles.productName}>{item.product}</Text>
              <View style={styles.dateRow}>
                <Text style={styles.dateLabel}>Prod: </Text>
                <Text style={styles.dateValue}>{item.productionDate}</Text>
                <Text style={styles.dateLabel}>  Exp: </Text>
                <Text style={styles.dateValue}>{item.expirationDate}</Text>
              </View>
            </View>

            <View style={styles.inputGrid}>
              <View style={styles.inputGroup}>
                <Text style={styles.inputLabel}>On Hand</Text>
                <TextInput
                  style={styles.input}
                  value={String(item.onHand)}
                  onChangeText={(v) => updateField(item.id, 'onHand', parseInt(v) || 0)}
                  keyboardType="numeric"
                />
              </View>
              
              <View style={styles.inputGroup}>
                <Text style={styles.inputLabel}>Today</Text>
                <TextInput
                  style={styles.input}
                  value={String(item.todayNeed)}
                  onChangeText={(v) => updateField(item.id, 'todayNeed', parseInt(v) || 0)}
                  keyboardType="numeric"
                />
              </View>
              
              <View style={styles.inputGroup}>
                <Text style={styles.inputLabel}>Day 2</Text>
                <TextInput
                  style={styles.input}
                  value={String(item.day2Need)}
                  onChangeText={(v) => updateField(item.id, 'day2Need', parseInt(v) || 0)}
                  keyboardType="numeric"
                />
              </View>
              
              <View style={styles.inputGroup}>
                <Text style={styles.inputLabel}>Day 3</Text>
                <TextInput
                  style={styles.input}
                  value={String(item.day3Need)}
                  onChangeText={(v) => updateField(item.id, 'day3Need', parseInt(v) || 0)}
                  keyboardType="numeric"
                />
              </View>
            </View>

            <View style={styles.summaryRow}>
              <View style={styles.summaryItem}>
                <Text style={styles.summaryLabel}>Total Needed</Text>
                <Text style={styles.summaryValue}>{calculateTotal(item)}</Text>
              </View>
              <View style={styles.summaryItem}>
                <Text style={styles.summaryLabel}>Variance</Text>
                <Text style={[styles.summaryValue, { color: getVarianceColor(item) }]}>
                  {calculateVariance(item) >= 0 ? '+' : ''}{calculateVariance(item)}
                </Text>
              </View>
              <View style={styles.actionSelector}>
                <Text style={styles.inputLabel}>Action</Text>
                <View style={styles.actionButtons}>
                  {['A', 'B', 'C'].map((action) => (
                    <TouchableOpacity
                      key={action}
                      style={[
                        styles.actionButton,
                        item.action === action && styles.actionButtonActive,
                        item.action === action && action === 'A' && { backgroundColor: colors.success },
                        item.action === action && action === 'B' && { backgroundColor: colors.warning },
                        item.action === action && action === 'C' && { backgroundColor: colors.danger },
                      ]}
                      onPress={() => updateField(item.id, 'action', action)}
                    >
                      <Text style={[
                        styles.actionButtonText,
                        item.action === action && styles.actionButtonTextActive,
                      ]}>{action}</Text>
                    </TouchableOpacity>
                  ))}
                </View>
              </View>
            </View>

            <Text style={styles.actionDescription}>
              {ACTION_LABELS[item.action]}
            </Text>
          </Card>
        ))}

        <View style={styles.bottomPadding} />
      </ScrollView>

      <View style={styles.footer}>
        {saved && <Text style={styles.savedText}>Inventory saved for manager review.</Text>}
        <Button 
          title={saved ? "Inventory Saved" : "Save Inventory"} 
          onPress={() => setSaved(true)}
          variant="primary"
          size="large"
          style={styles.saveButton}
          disabled={saved}
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
  legend: {
    flexDirection: 'row',
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: colors.surface,
    gap: 16,
    flexWrap: 'wrap',
  },
  legendItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  legendDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
  },
  legendText: {
    fontSize: 11,
    color: colors.textSecondary,
  },
  content: {
    flex: 1,
    padding: 16,
  },
  tableHeader: {
    flexDirection: 'row',
    paddingBottom: 8,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    marginBottom: 12,
  },
  th: {
    fontSize: 11,
    fontWeight: '600',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
  },
  thProduct: {
    flex: 1.5,
  },
  thSmall: {
    flex: 1,
    textAlign: 'center',
  },
  thAction: {
    flex: 1,
    textAlign: 'center',
  },
  inventoryCard: {
    marginBottom: 12,
  },
  cardHeader: {
    marginBottom: 16,
  },
  productName: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.text,
    marginBottom: 4,
  },
  dateRow: {
    flexDirection: 'row',
  },
  dateLabel: {
    fontSize: 12,
    color: colors.textMuted,
  },
  dateValue: {
    fontSize: 12,
    color: colors.textSecondary,
    fontWeight: '500',
  },
  inputGrid: {
    flexDirection: 'row',
    gap: 8,
    marginBottom: 16,
  },
  inputGroup: {
    flex: 1,
  },
  inputLabel: {
    fontSize: 11,
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
    marginBottom: 4,
  },
  input: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
    textAlign: 'center',
    borderWidth: 1,
    borderColor: colors.border,
  },
  summaryRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  summaryItem: {
    alignItems: 'center',
  },
  summaryLabel: {
    fontSize: 10,
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
    marginBottom: 4,
  },
  summaryValue: {
    fontSize: 20,
    fontWeight: '700',
    color: colors.text,
  },
  actionSelector: {
    alignItems: 'center',
  },
  actionButtons: {
    flexDirection: 'row',
    gap: 4,
  },
  actionButton: {
    width: 32,
    height: 32,
    borderRadius: 8,
    backgroundColor: colors.surfaceElevated,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: 'center',
    justifyContent: 'center',
  },
  actionButtonActive: {
    borderWidth: 0,
  },
  actionButtonText: {
    fontSize: 14,
    fontWeight: '700',
    color: colors.textMuted,
  },
  actionButtonTextActive: {
    color: colors.text,
  },
  actionDescription: {
    fontSize: 12,
    color: colors.textSecondary,
    textAlign: 'center',
    marginTop: 12,
    fontStyle: 'italic',
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
  savedText: {
    fontSize: 13,
    fontWeight: '700',
    color: colors.success,
    textAlign: 'center',
    marginBottom: 8,
  },
  saveButton: {
    width: '100%',
  },
});

export default InventoryManagementScreen;
