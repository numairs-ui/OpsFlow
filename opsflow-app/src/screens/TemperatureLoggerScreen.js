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

const LOCATIONS = [
  { id: 'walkin', name: 'Walk-in Cooler', targetMin: 34, targetMax: 40 },
  { id: 'makeline', name: 'Make Line', targetMin: 38, targetMax: 45 },
  { id: 'oven1', name: 'Oven 1', targetMin: 450, targetMax: 550 },
  { id: 'oven2', name: 'Oven 2', targetMin: 450, targetMax: 550 },
  { id: 'freezer', name: 'Walk-in Freezer', targetMin: -10, targetMax: 0 },
  { id: 'drystorage', name: 'Dry Storage', targetMin: 50, targetMax: 70 },
];

const HISTORY_DATA = [
  { id: '1', location: 'Walk-in Cooler', temp: 38, time: '9:30 AM', status: 'ok', date: 'May 2' },
  { id: '2', location: 'Make Line', temp: 42, time: '9:30 AM', status: 'ok', date: 'May 2' },
  { id: '3', location: 'Walk-in Cooler', temp: 36, time: '11:00 AM', status: 'ok', date: 'May 2' },
  { id: '4', location: 'Oven 1', temp: 520, time: '11:30 AM', status: 'ok', date: 'May 2' },
];

export const TemperatureLoggerScreen = () => {
  const [selectedLocation, setSelectedLocation] = useState(null);
  const [reading, setReading] = useState('');
  const [notes, setNotes] = useState('');
  const [history, setHistory] = useState(HISTORY_DATA);
  const [logged, setLogged] = useState(false);

  const isInRange = (temp, location) => {
    if (!location) return false;
    const tempNum = parseFloat(temp);
    return tempNum >= location.targetMin && tempNum <= location.targetMax;
  };

  const logReading = () => {
    if (!selectedLocation || !reading) return;
    
    const newReading = {
      id: Date.now().toString(),
      location: selectedLocation.name,
      temp: parseFloat(reading),
      time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      status: isInRange(reading, selectedLocation) ? 'ok' : 'alert',
      date: 'May 2',
    };
    
    setHistory([newReading, ...history]);
    setLogged(true);
    setReading('');
    setNotes('');
  };

  const reset = () => {
    setSelectedLocation(null);
    setReading('');
    setNotes('');
    setLogged(false);
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity style={styles.backButton}>
          <Text style={styles.backText}>←</Text>
        </TouchableOpacity>
        <View style={styles.headerContent}>
          <Text style={styles.headerTitle}>Temperature Log</Text>
          <Text style={styles.headerSubtitle}>Record & monitor temps</Text>
        </View>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        <Text style={styles.sectionLabel}>SELECT LOCATION</Text>
        
        <View style={styles.locationGrid}>
          {LOCATIONS.map((loc) => (
            <TouchableOpacity
              key={loc.id}
              style={[
                styles.locationCard,
                selectedLocation?.id === loc.id && styles.locationCardActive,
              ]}
              onPress={() => {
                setSelectedLocation(loc);
                setLogged(false);
              }}
            >
              <Text style={styles.locationIcon}>🌡️</Text>
              <Text style={[
                styles.locationName,
                selectedLocation?.id === loc.id && styles.locationNameActive,
              ]}>{loc.name}</Text>
              <Text style={styles.targetRange}>
                {loc.targetMin}°F - {loc.targetMax}°F
              </Text>
            </TouchableOpacity>
          ))}
        </View>

        {selectedLocation && !logged && (
          <Card style={styles.entryCard}>
            <Text style={styles.cardTitle}>Record Reading</Text>
            <Text style={styles.locationSelected}>
              {selectedLocation.name} (Target: {selectedLocation.targetMin}°F - {selectedLocation.targetMax}°F)
            </Text>

            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Temperature (°F)</Text>
              <TextInput
                style={[
                  styles.tempInput,
                  reading && !isInRange(reading, selectedLocation) && styles.tempInputAlert,
                ]}
                value={reading}
                onChangeText={setReading}
                placeholder="Enter reading"
                placeholderTextColor={colors.textMuted}
                keyboardType="numeric"
              />
              {reading && (
                <View style={[
                  styles.statusIndicator,
                  { backgroundColor: isInRange(reading, selectedLocation) ? colors.success : colors.danger }
                ]}>
                  <Text style={styles.statusText}>
                    {isInRange(reading, selectedLocation) ? '✓ In Range' : '⚠️ Out of Range'}
                  </Text>
                </View>
              )}
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.inputLabel}>Notes (Optional)</Text>
              <TextInput
                style={styles.notesInput}
                value={notes}
                onChangeText={setNotes}
                placeholder="Add any observations..."
                placeholderTextColor={colors.textMuted}
                multiline
                numberOfLines={2}
              />
            </View>

            <Button 
              title="Log Temperature"
              onPress={logReading}
              variant="primary"
              size="large"
              disabled={!reading}
            />
          </Card>
        )}

        {logged && (
          <Card style={styles.successCard}>
            <View style={styles.successHeader}>
              <View style={styles.successIcon}>
                <Text style={styles.successIconText}>✓</Text>
              </View>
              <View>
                <Text style={styles.successTitle}>Temperature Logged</Text>
                <Text style={styles.successTime}>
                  {new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </Text>
              </View>
            </View>
            <Button 
              title="Log Another"
              onPress={reset}
              variant="secondary"
              size="medium"
            />
          </Card>
        )}

        <Text style={styles.sectionLabel}>RECENT READINGS</Text>
        
        {history.map((item) => (
          <Card key={item.id} style={styles.historyCard}>
            <View style={styles.historyHeader}>
              <Text style={styles.historyLocation}>{item.location}</Text>
              <View style={[
                styles.historyBadge,
                { backgroundColor: item.status === 'ok' ? colors.success + '20' : colors.danger + '20' }
              ]}>
                <Text style={[
                  styles.historyBadgeText,
                  { color: item.status === 'ok' ? colors.success : colors.danger }
                ]}>
                  {item.status === 'ok' ? 'OK' : 'ALERT'}
                </Text>
              </View>
            </View>
            <View style={styles.historyDetails}>
              <View style={styles.historyItem}>
                <Text style={styles.historyValue}>{item.temp}°F</Text>
                <Text style={styles.historyLabel}>Reading</Text>
              </View>
              <View style={styles.historyItem}>
                <Text style={styles.historyValue}>{item.time}</Text>
                <Text style={styles.historyLabel}>Time</Text>
              </View>
              <View style={styles.historyItem}>
                <Text style={styles.historyValue}>{item.date}</Text>
                <Text style={styles.historyLabel}>Date</Text>
              </View>
            </View>
          </Card>
        ))}

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
  content: {
    flex: 1,
    padding: 16,
  },
  sectionLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 1,
    marginBottom: 12,
    marginTop: 16,
  },
  locationGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 10,
  },
  locationCard: {
    width: '47%',
    backgroundColor: colors.surface,
    borderRadius: 12,
    padding: 16,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: colors.border,
  },
  locationCardActive: {
    backgroundColor: colors.accent + '20',
    borderColor: colors.accent,
  },
  locationIcon: {
    fontSize: 28,
    marginBottom: 8,
  },
  locationName: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.text,
    textAlign: 'center',
  },
  locationNameActive: {
    color: colors.accent,
  },
  targetRange: {
    fontSize: 11,
    color: colors.textMuted,
    marginTop: 4,
  },
  entryCard: {
    marginTop: 24,
  },
  cardTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.text,
    marginBottom: 8,
  },
  locationSelected: {
    fontSize: 14,
    color: colors.accent,
    marginBottom: 20,
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
  tempInput: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 12,
    paddingHorizontal: 16,
    paddingVertical: 14,
    fontSize: 24,
    fontWeight: '700',
    color: colors.text,
    textAlign: 'center',
    borderWidth: 2,
    borderColor: colors.border,
  },
  tempInputAlert: {
    borderColor: colors.danger,
  },
  statusIndicator: {
    marginTop: 8,
    padding: 10,
    borderRadius: 8,
    alignItems: 'center',
  },
  statusText: {
    fontSize: 14,
    fontWeight: '600',
  },
  notesInput: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 12,
    paddingHorizontal: 16,
    paddingVertical: 12,
    fontSize: 15,
    color: colors.text,
    minHeight: 60,
    borderWidth: 1,
    borderColor: colors.border,
  },
  successCard: {
    marginTop: 24,
    borderWidth: 2,
    borderColor: colors.success,
  },
  successHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 16,
  },
  successIcon: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: colors.success,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 16,
  },
  successIconText: {
    fontSize: 24,
    fontWeight: '700',
    color: colors.text,
  },
  successTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.success,
  },
  successTime: {
    fontSize: 13,
    color: colors.textMuted,
    marginTop: 2,
  },
  historyCard: {
    marginBottom: 10,
  },
  historyHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  historyLocation: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
  },
  historyBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 6,
  },
  historyBadgeText: {
    fontSize: 12,
    fontWeight: '700',
  },
  historyDetails: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  historyItem: {
    alignItems: 'center',
  },
  historyValue: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.text,
  },
  historyLabel: {
    fontSize: 11,
    color: colors.textMuted,
    textTransform: 'uppercase',
    marginTop: 2,
  },
  bottomPadding: {
    height: 40,
  },
});

export default TemperatureLoggerScreen;