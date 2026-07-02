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

const MESSAGES = [
  {
    id: '1',
    from: 'Marcus R. (Opening Manager)',
    time: '9:15 AM',
    priority: 'high',
    title: 'Morning Update - May 2',
    content: `Good morning team! Here's what you need to know for today:

**Important Notes:**
• We've received a large catering order for 50 pizzas - prep team please start early
• One oven is running hot - maintenance has been notified, monitor temps closely
• New shipment of cheese arriving around 2pm - someone needs to receive it

**Reminders:**
• Complete all opening tasks before 11am
• Check expiration dates on all products
• Make sure make line is fully stocked before rush

Let me know if you have any questions!`,
    acknowledged: true,
  },
  {
    id: '2',
    from: 'Sarah L. (Shift Lead)',
    time: '2:30 PM',
    priority: 'medium',
    title: 'Afternoon Rush Prep',
    content: `Team, rush starts at 5pm. Please ensure:

• Make line is fully stocked with all toppings
• Walk-in is organized - pull forward items to front
• All tables are clean and ready
• Dish station is clear

Good luck today! 🍕`,
    acknowledged: false,
  },
];

export const CommunicationViewerScreen = () => {
  const [messages, setMessages] = useState(MESSAGES);
  const [selectedMessage, setSelectedMessage] = useState(null);
  const [showAllMessages, setShowAllMessages] = useState(true);

  const acknowledgeMessage = (id) => {
    setMessages(messages.map(m => 
      m.id === id ? { ...m, acknowledged: true } : m
    ));
  };

  const getPriorityColor = (priority) => {
    switch (priority) {
      case 'high': return colors.danger;
      case 'medium': return colors.warning;
      default: return colors.success;
    }
  };

  const unacknowledgedCount = messages.filter(m => !m.acknowledged).length;

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>Communications</Text>
        <Text style={styles.headerSubtitle}>Messages from management</Text>
      </View>

      <View style={styles.summaryBanner}>
        <View style={styles.summaryItem}>
          <Text style={styles.summaryValue}>{messages.length}</Text>
          <Text style={styles.summaryLabel}>Total</Text>
        </View>
        <View style={styles.summaryDivider} />
        <View style={styles.summaryItem}>
          <Text style={[styles.summaryValue, { color: colors.warning }]}>
            {unacknowledgedCount}
          </Text>
          <Text style={styles.summaryLabel}>Unread</Text>
        </View>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        {messages.map((message) => (
          <TouchableOpacity
            key={message.id}
            onPress={() => setSelectedMessage(selectedMessage?.id === message.id ? null : message)}
          >
            <Card 
              style={[
                styles.messageCard,
                !message.acknowledged && styles.messageCardUnread,
              ]}
            >
              <View style={styles.messageHeader}>
                <View style={styles.messageMeta}>
                  <Text style={styles.messageFrom}>{message.from}</Text>
                  <Text style={styles.messageTime}>{message.time}</Text>
                </View>
                <View style={[
                  styles.priorityBadge,
                  { backgroundColor: getPriorityColor(message.priority) + '20' }
                ]}>
                  <Text style={[
                    styles.priorityText,
                    { color: getPriorityColor(message.priority) }
                  ]}>
                    {message.priority.toUpperCase()}
                  </Text>
                </View>
              </View>

              <Text style={styles.messageTitle}>{message.title}</Text>
              
              <Text style={styles.messagePreview} numberOfLines={selectedMessage?.id === message.id ? undefined : 2}>
                {message.content.substring(0, 100)}...
              </Text>

              {selectedMessage?.id === message.id && (
                <View style={styles.expandedContent}>
                  <Text style={styles.messageBody}>{message.content}</Text>
                  
                  {!message.acknowledged && (
                    <Button 
                      title="Mark as Read"
                      onPress={() => acknowledgeMessage(message.id)}
                      variant="primary"
                      size="small"
                      style={styles.ackButton}
                    />
                  )}

                  {message.acknowledged && (
                    <View style={styles.acknowledgedBadge}>
                      <Text style={styles.acknowledgedText}>
                        ✓ You acknowledged this
                      </Text>
                    </View>
                  )}
                </View>
              )}

              <View style={styles.messageFooter}>
                <Text style={[
                  styles.expandText,
                  selectedMessage?.id === message.id && styles.expandTextActive,
                ]}>
                  {selectedMessage?.id === message.id ? 'Show less' : 'View details'}
                </Text>
                {message.acknowledged && (
                  <View style={styles.readBadge}>
                    <Text style={styles.readBadgeText}>✓ Read</Text>
                  </View>
                )}
              </View>
            </Card>
          </TouchableOpacity>
        ))}

        <View style={styles.emptyState}>
          <Text style={styles.emptyIcon}>📬</Text>
          <Text style={styles.emptyTitle}>All Caught Up!</Text>
          <Text style={styles.emptyText}>No new messages</Text>
        </View>

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
  summaryBanner: {
    flexDirection: 'row',
    backgroundColor: colors.surface,
    paddingVertical: 16,
    paddingHorizontal: 20,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  summaryItem: {
    flex: 1,
    alignItems: 'center',
  },
  summaryValue: {
    fontSize: 28,
    fontWeight: '700',
    color: colors.text,
  },
  summaryLabel: {
    fontSize: 12,
    color: colors.textMuted,
    textTransform: 'uppercase',
    marginTop: 2,
  },
  summaryDivider: {
    width: 1,
    backgroundColor: colors.border,
    marginHorizontal: 20,
  },
  content: {
    flex: 1,
    padding: 16,
  },
  messageCard: {
    marginBottom: 12,
  },
  messageCardUnread: {
    borderLeftWidth: 4,
    borderLeftColor: colors.accent,
  },
  messageHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 12,
  },
  messageMeta: {},
  messageFrom: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.text,
  },
  messageTime: {
    fontSize: 12,
    color: colors.textMuted,
    marginTop: 2,
  },
  priorityBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 4,
  },
  priorityText: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
  messageTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.text,
    marginBottom: 8,
  },
  messagePreview: {
    fontSize: 14,
    color: colors.textSecondary,
    lineHeight: 20,
  },
  expandedContent: {
    marginTop: 16,
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  messageBody: {
    fontSize: 15,
    color: colors.text,
    lineHeight: 24,
  },
  ackButton: {
    marginTop: 16,
  },
  acknowledgedBadge: {
    backgroundColor: colors.success + '15',
    padding: 12,
    borderRadius: 8,
    marginTop: 16,
    alignItems: 'center',
  },
  acknowledgedText: {
    fontSize: 14,
    color: colors.success,
    fontWeight: '500',
  },
  messageFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  expandText: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.accent,
  },
  expandTextActive: {
    color: colors.textMuted,
  },
  readBadge: {
    backgroundColor: colors.success + '15',
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 4,
  },
  readBadgeText: {
    fontSize: 12,
    color: colors.success,
    fontWeight: '500',
  },
  emptyState: {
    alignItems: 'center',
    paddingVertical: 48,
  },
  emptyIcon: {
    fontSize: 48,
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

export default CommunicationViewerScreen;