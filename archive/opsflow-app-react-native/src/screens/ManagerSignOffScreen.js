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

const SAMPLE_SECTIONS = [
  {
    id: '1',
    name: 'Opening Manager',
    completedBy: 'Marcus R.',
    completedAt: '10:45 AM',
    tasksCompleted: 8,
    totalTasks: 8,
  },
  {
    id: '2',
    name: 'Product Management',
    completedBy: 'Sarah L.',
    completedAt: '11:30 AM',
    tasksCompleted: 5,
    totalTasks: 5,
  },
  {
    id: '3',
    name: 'Pre-Rush Walk Through',
    completedBy: 'Jake T.',
    completedAt: '3:45 PM',
    tasksCompleted: 6,
    totalTasks: 6,
  },
];

export const ManagerSignOffScreen = () => {
  const [initials, setInitials] = useState('');
  const [notes, setNotes] = useState('');
  const [sections, setSections] = useState(SAMPLE_SECTIONS);
  const [signedSections, setSignedSections] = useState([]);
  const [checklistComplete, setChecklistComplete] = useState(false);

  const signSection = (sectionId) => {
    if (initials.length === 0) return;
    setSignedSections([...signedSections, sectionId]);
  };

  const allSigned = signedSections.length === sections.length;

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity style={styles.backButton}>
          <Text style={styles.backText}>←</Text>
        </TouchableOpacity>
        <View style={styles.headerContent}>
          <Text style={styles.headerTitle}>Manager Sign-off</Text>
          <Text style={styles.headerDate}>May 2, 2026 - Closing</Text>
        </View>
      </View>

      <View style={styles.instructionsCard}>
        <Text style={styles.instructionsTitle}>Review & Sign Off</Text>
        <Text style={styles.instructionsText}>
          Review each section's completion. Enter your initials to confirm 
          each section has been properly completed.
        </Text>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        <Text style={styles.sectionLabel}>SECTIONS TO REVIEW</Text>
        
        {sections.map((section) => {
          const isSigned = signedSections.includes(section.id);
          return (
            <Card 
              key={section.id} 
              style={[styles.sectionCard, isSigned && styles.sectionCardSigned]}
            >
              <View style={styles.sectionHeader}>
                <View>
                  <Text style={styles.sectionName}>{section.name}</Text>
                  <Text style={styles.completedBy}>
                    Completed by {section.completedBy} at {section.completedAt}
                  </Text>
                </View>
                {isSigned ? (
                  <View style={styles.signedBadge}>
                    <Text style={styles.signedBadgeText}>✓ Signed</Text>
                  </View>
                ) : (
                  <View style={styles.pendingBadge}>
                    <Text style={styles.pendingBadgeText}>Pending</Text>
                  </View>
                )}
              </View>

              <View style={styles.tasksSummary}>
                <Text style={styles.tasksSummaryText}>
                  {section.tasksCompleted}/{section.totalTasks} tasks completed
                </Text>
                <View style={styles.tasksBar}>
                  <View 
                    style={[
                      styles.tasksBarFill, 
                      { width: `${(section.tasksCompleted / section.totalTasks) * 100}%` }
                    ]} 
                  />
                </View>
              </View>

              {!isSigned && (
                <View style={styles.signOffSection}>
                  <View style={styles.initialsRow}>
                    <Text style={styles.initialsLabel}>Enter Initials:</Text>
                    <TextInput
                      style={styles.initialsInput}
                      value={initials}
                      onChangeText={setInitials}
                      placeholder="XX"
                      placeholderTextColor={colors.textMuted}
                      maxLength={3}
                      autoCapitalize="characters"
                    />
                  </View>
                  
                  <Button 
                    title={`Sign Off: ${section.name}`}
                    onPress={() => signSection(section.id)}
                    variant="primary"
                    disabled={initials.length < 2}
                    size="small"
                  />
                </View>
              )}

              {isSigned && (
                <View style={styles.signedInfo}>
                  <Text style={styles.signedInfoText}>
                    Signed by {initials.toUpperCase()} at {new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </Text>
                </View>
              )}
            </Card>
          );
        })}

        <View style={styles.notesSection}>
          <Text style={styles.sectionLabel}>MANAGER NOTES (Optional)</Text>
          <TextInput
            style={styles.notesInput}
            value={notes}
            onChangeText={setNotes}
            placeholder="Add any notes or observations about today's operations..."
            placeholderTextColor={colors.textMuted}
            multiline
            numberOfLines={4}
            textAlignVertical="top"
          />
        </View>

        {allSigned && (
          <View style={styles.completeBanner}>
            <View style={styles.completeIcon}>
              <Text style={styles.completeIconText}>✓</Text>
            </View>
            <View style={styles.completeTextContainer}>
              <Text style={styles.completeTitle}>All Sections Signed</Text>
              <Text style={styles.completeSubtitle}>Daily checklist complete</Text>
            </View>
          </View>
        )}
        {checklistComplete && (
          <Card style={styles.completeCard}>
            <Text style={styles.completeTitle}>Checklist closed</Text>
            <Text style={styles.completeSubtitle}>Final sign-off captured with initials {initials}.</Text>
          </Card>
        )}

        <View style={styles.bottomPadding} />
      </ScrollView>

      <View style={styles.footer}>
        <Button 
          title={checklistComplete ? "Checklist Complete" : allSigned ? "Complete Checklist" : "Sign Off All Sections"} 
          onPress={() => setChecklistComplete(true)}
          variant={allSigned ? "primary" : "secondary"}
          size="large"
          style={styles.footerButton}
          disabled={!allSigned || checklistComplete}
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
  headerDate: {
    fontSize: 13,
    color: colors.textMuted,
    marginTop: 2,
  },
  instructionsCard: {
    backgroundColor: colors.accent + '15',
    margin: 16,
    padding: 16,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.accent + '30',
  },
  instructionsTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: colors.accent,
    marginBottom: 8,
  },
  instructionsText: {
    fontSize: 14,
    color: colors.textSecondary,
    lineHeight: 20,
  },
  content: {
    flex: 1,
    paddingHorizontal: 16,
  },
  sectionLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 1,
    marginBottom: 12,
    marginTop: 8,
  },
  sectionCard: {
    marginBottom: 16,
  },
  sectionCardSigned: {
    borderColor: colors.success,
    borderWidth: 2,
  },
  sectionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 16,
  },
  sectionName: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.text,
    marginBottom: 4,
  },
  completedBy: {
    fontSize: 13,
    color: colors.textMuted,
  },
  signedBadge: {
    backgroundColor: colors.success + '20',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 8,
  },
  signedBadgeText: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.success,
  },
  pendingBadge: {
    backgroundColor: colors.warning + '20',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 8,
  },
  pendingBadgeText: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.warning,
  },
  tasksSummary: {
    marginBottom: 16,
  },
  tasksSummaryText: {
    fontSize: 14,
    color: colors.textSecondary,
    marginBottom: 8,
  },
  tasksBar: {
    height: 6,
    backgroundColor: colors.border,
    borderRadius: 3,
    overflow: 'hidden',
  },
  tasksBarFill: {
    height: '100%',
    backgroundColor: colors.success,
    borderRadius: 3,
  },
  signOffSection: {
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  initialsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 12,
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
  signedInfo: {
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  signedInfoText: {
    fontSize: 13,
    color: colors.success,
    fontWeight: '500',
  },
  notesSection: {
    marginTop: 8,
  },
  notesInput: {
    backgroundColor: colors.surfaceElevated,
    borderRadius: 12,
    padding: 16,
    fontSize: 15,
    color: colors.text,
    minHeight: 100,
    borderWidth: 1,
    borderColor: colors.border,
  },
  completeBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: colors.success + '15',
    padding: 16,
    borderRadius: 16,
    marginTop: 24,
    borderWidth: 1,
    borderColor: colors.success + '30',
  },
  completeIcon: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: colors.success,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 16,
  },
  completeIconText: {
    fontSize: 24,
    fontWeight: '700',
    color: colors.text,
  },
  completeTextContainer: {
    flex: 1,
  },
  completeTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.success,
  },
  completeSubtitle: {
    fontSize: 14,
    color: colors.textSecondary,
    marginTop: 2,
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
  footerButton: {
    width: '100%',
  },
});

export default ManagerSignOffScreen;
