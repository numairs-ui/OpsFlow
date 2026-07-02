# MealDynamics OpsFlow - UI Completion Roadmap
## Based on F0890 Daily Operations Checklist Requirements

---

## 📊 Current vs. Required Scope

### Current Figma Implementation
- ✅ Basic checklist categories
- ✅ Simple task lists with completion status
- ✅ Tab-based filtering
- ✅ Search functionality
- ✅ Basic notifications
- ✅ Request management

### New Requirements (F0890 Pizza Checklist)
The pizza restaurant daily checklist reveals **5 major complexity layers** not yet in the design:

1. **Time-Based Sections** - Tasks grouped by time windows (9am, 11am, 3:30pm, Close)
2. **Data Entry Fields** - Inventory counts, temperature tracking, cash management tables
3. **Hierarchical Dependencies** - Parent tasks with detailed sub-tasks and specific instructions
4. **Manager Assignment** - Ability to delegate specific deployment tasks to staff
5. **Sign-off & Accountability** - Manager initials, timestamps, multi-person approvals

---

## 🎯 Required New Screens & Flows

### CATEGORY 1: Enhanced Checklist Creation & Management

#### Screen 1.1: Checklist Template Builder (NEW)
**Purpose:** Allow managers to create custom checklists with complex structures
**Key Elements:**
- Checklist name field
- Category/Time-of-day selector
- Multiple section management
- Add sections button
- Section naming (e.g., "Opening Manager", "Set Up Make Line")
- Manage time deadlines for sections
- Reorder sections (drag-drop)
- Save as template vs. one-time checklist

**Data Input Needs:**
- Text fields for section names
- Time picker for completion deadlines
- Toggle for recurring/one-time

---

#### Screen 1.2: Checklist Section Builder (NEW)
**Purpose:** Build individual sections with tasks and sub-tasks
**Key Elements:**
- Section name header
- Task list with expandable details
- Add new task button
- Task description field
- Add sub-task option
- Mark as "has data fields" (for inventory, temps, etc.)
- Assign minimum clearance level (opening mgr only, etc.)
- Delete/reorder tasks

**Example Section:** "Set Up Make Line"
```
├─ Date with label system (specific instruction)
├─ All product prepped for MDOG (specific instruction)
├─ Portion cups in each product (specific instruction)
├─ Print MDOG (specific instruction)
├─ Pull dough for lunch usage, see MDOG (specific instruction)
├─ Make sure all dough sizes are pulled & properly proofed (specific instruction)
├─ Check expiration dates and discard if necessary (specific instruction)
└─ Place working thermometers in all dough sizes (specific instruction with note)
```

---

#### Screen 1.3: Advanced Task Configuration (NEW)
**Purpose:** Configure complex task types with data capture
**Key Elements:**
- Task type selector (Simple Checkbox / Data Entry / Temperature / Inventory)
- Task title
- Detailed instructions/notes area
- Associated data fields (for inventory-type tasks)
- Required vs. optional
- Assign to specific role/person
- Add supporting images/diagrams
- Notes for special handling

**Task Type Options:**
1. **Simple Checkbox** - Just mark complete
2. **Data Entry** - Requires numerical input (e.g., cash counts)
3. **Temperature Log** - Records temps with time/location
4. **Inventory Count** - Multi-item inventory with quantities
5. **Sign-off** - Requires manager initials/name
6. **Time-based Alert** - Must be done by specific time or flags as overdue

---

### CATEGORY 2: Data Entry & Tracking

#### Screen 2.1: Inventory Management Screen (NEW)
**Purpose:** Handle the "3-Day Dough/Cheese Management Plan" type tracking
**Key Elements:**
- Product name column (12", 14", 16" dough, Cheese, etc.)
- On-Hand Amount field (with unit selector)
- Production Date
- Expiration Date
- Today's Need, Day 2 Need, Day 3 Need
- Total Needed calculation
- Action To Be Taken (dropdown: A/B/C from the pdf)
- Row add/delete capability
- Save/complete button

**Example from PDF:**
```
12" Dough
├─ On Hand: [___] units
├─ Production Date: [___]
├─ Expiration Date: [___]
├─ Today's Need: [___]
├─ Day 2's Need: [___]
├─ Day 3's Need: [___]
├─ Total Needed: [calculated]
└─ Action: [A: We have enough / B: We have more / C: We don't have enough]
```

---

#### Screen 2.2: Cash/Till Management Screen (NEW)
**Purpose:** Track till counts and safe management
**Key Elements:**
- Till name selector (Till A, Till B, Safe - Opening/Closing/Shift Change)
- Denominations list (100s/50s, 20s, 10s, 5s, 1s, Quarters, Dimes, Nickels, Pennies)
- For each denomination: Count field
- Running total calculation
- "Should Be" expected amount
- Variance calculation (with color coding: green if matches, red if variance)
- Manager signature required
- Timestamp

---

#### Screen 2.3: Temperature Logger (NEW)
**Purpose:** Log walk-in, make-line, and other temperature readings
**Key Elements:**
- Location dropdown (Walk-in, Make Line, Oven, etc.)
- Current reading field
- Timestamp (auto-populated)
- Target range display (e.g., "Should be 38°F")
- Alert if out of range
- Notes field
- Logged by: [auto-filled user]

---

#### Screen 2.4: Communications/Blast Entry (NEW)
**Purpose:** Manager to communicate daily messages/alerts to staff
**Key Elements:**
- Large text area for communications
- Bold/italic formatting options
- Attach documents/images
- Priority level selector
- Recipient filter (all staff, crew, managers only, etc.)
- Schedule send time option
- Send now button
- Preview before sending

---

### CATEGORY 3: Time-Based Task Organization

#### Screen 3.1: Daily Shift Timeline View (NEW)
**Purpose:** Show checklist organized by time windows
**Current:** List view of all tasks
**New:** Timeline showing when things need to happen

**Visual Structure:**
```
9:00 AM - OPENING MANAGER (Deadline: 11:00 AM)
├─ Arrival time
├─ Conduct security walk through
├─ Check for delayed orders
├─ Set up make line
└─ [etc. - expandable]

11:00 AM - PRODUCT MANAGEMENT (Deadline: 11:00 AM)
├─ Complete 3-day dough and hourly dough pulls
├─ Count tills and store cash
├─ Turn on open signs
└─ [etc.]

3:30 PM - PRE-RUSH WALK THROUGH (Deadline: 3:30 PM)
├─ Make line stocked
├─ Sweep entire restaurant
├─ [etc.]

CLOSING - CLOSING CHECKLIST (Deadline: End of shift)
├─ Closing Checklist tasks
└─ Closing Admin Checklist tasks
```

**Features:**
- Visual timeline/progress bar
- Time remaining indicator (in green/yellow/red)
- Collapse/expand sections
- Mark section complete to advance
- Warnings if deadline approaching

---

#### Screen 3.2: Overdue/Pending Alert Center (NEW)
**Purpose:** Alert managers to tasks not completed by deadline
**Key Elements:**
- List of overdue sections/tasks
- Time past deadline indicator
- Quick action buttons (Mark Done, Assign to Staff, Acknowledge)
- Filter by severity
- Dismiss alerts (with note)

---

### CATEGORY 4: Staff Assignment & Delegation

#### Screen 4.1: Deployment Guide / Staff Assignment (NEW)
**Purpose:** Assign closing/deployment tasks to specific staff members
**Key Elements:**
- Deployment tasks list (from the checklist)
- Staff member selector for each task
- Assigned time/deadline
- Task details/instructions expandable
- Save assignments
- Send notification to assigned staff
- Track completion by staff member

**Example Tasks:**
- Stocking Pepsi Cooler → [Assign to: Person A] by [Time]
- Trash out and replace liner → [Assign to: Person B] by [Time]
- Bathroom cleaned → [Assign to: Person C] by [Time]
- Walk-in swept and mopped → [Assign to: Person D] by [Time]
- etc.

**Features:**
- Bulk assignment options
- Task swapping between staff
- Estimated time per task
- Track who completed what
- Notes field for special instructions

---

#### Screen 4.2: Staff Task Assignment View (NEW) - EMPLOYEE PERSPECTIVE
**Purpose:** Show assigned deployment tasks to employees
**Key Elements:**
- "My Tasks" or "Assignments" tab
- List of assigned tasks
- Task details and special instructions
- Estimated completion time
- "Mark Complete" button with timestamp
- Notes/questions field to ask manager
- View all assigned tasks history

---

### CATEGORY 5: Sign-off & Accountability

#### Screen 5.1: Manager Sign-off / Initials Screen (NEW)
**Purpose:** Manager acknowledges completion of sections
**Key Elements:**
- Section name header
- Review summary of what was completed
- Manager name/ID display
- Signature pad OR initial entry fields
- Timestamp (auto-captured)
- "Confirm & Sign Off" button
- Option to decline sign-off with reason
- Ability to add notes/comments

---

#### Screen 5.2: Daily Checklist Review Report (NEW)
**Purpose:** Generate daily summary of what was completed
**Key Elements:**
- Date and day of week
- Checklist name
- Section-by-section completion status
- Who completed each section
- Timestamps for each section completion
- Any overdue items flagged
- Notes/issues recorded
- Manager sign-off status
- Print/share option

---

### CATEGORY 6: Edge Cases & Special States

#### Screen 6.1: Task Detail View - ENHANCED (MODIFICATION)
**Current State:** Simple task with description and assign buttons
**Required Enhancement:**
- Full instructions with formatting (bold, bullets)
- Related images/diagrams
- Video reference link option
- Temperature ranges if applicable
- Stock levels if inventory-related
- Time deadline (if section has deadline)
- Assign to specific person
- Add/view comments from staff
- Task history (who completed this before, when)
- Mark as complete / In Progress / Unable to Complete
- If unable: reason dropdown and notes

---

#### Screen 6.2: Communication/Training Material Viewer (NEW)
**Purpose:** Display communications from opening manager
**Key Elements:**
- Today's message box
- Expandable content area
- Formatted text (headings, bold, bullets)
- Embedded images
- Linked documents
- Staff acknowledgment ("I read this") option

---

#### Screen 6.3: Variance Management (NEW)
**Purpose:** Handle inventory variances and alerts
**Key Elements:**
- Inventory vs. Expected comparison
- Variance percentage
- Auto-flag if >5% variance
- Root cause dropdown
- Manager notes on why variance occurred
- Resolution notes
- Track repeat variances by location

---

## 🎨 New Component Library Needed

### Components to Design

1. **Time Picker** - Select hours/minutes with validation
2. **Currency Input** - Format currency with validation
3. **Temperature Input** - With target range visualization
4. **Signature Pad** - Capture digital signatures
5. **Inventory Table** - Multi-row data entry with calculations
6. **Timeline Component** - Visual timeline of daily sections
7. **Status Badge Enhancements:**
   - Overdue (red)
   - Pending by deadline (yellow)
   - In Progress (blue)
   - Completed on time (green)
8. **Collapsible Manager Notes** - Expandable rich text area
9. **Staff Assignment Card** - Show who's assigned to what
10. **Alert Banner** - Deadline approaching warnings
11. **Data Validation Messages** - Real-time validation feedback

---

## 📱 Screen Flow Map

```
OPENING SHIFT FLOW:
Dashboard 
  → Select Checklist 
    → Daily Checklist (Timeline View)
      → Opening Manager Section (expandable)
        → Task Detail
          → Task Completion
      → Product Management Section
      → [etc. for all time windows]
      → Final Sign-off
        → Manager Initials Screen
      → Daily Review Report

EMPLOYEE ASSIGNMENT FLOW:
Dashboard
  → "My Assignments"
    → List of deployment tasks
      → Task Detail
        → Mark Complete (with photo/notes optional)
        → Submit completion

MANAGER DEPLOYMENT FLOW:
Dashboard
  → Select Checklist
    → Deployment Guide
      → Assign tasks to staff
      → Track completion
      → Review completion status

INVENTORY MANAGEMENT FLOW:
Dashboard
  → Checklist
    → Inventory Section
      → Edit 3-Day Dough/Cheese
        → Input quantities and dates
        → Select action (A/B/C)
        → View variance alerts
```

---

## 🚦 Priority & Phasing

### Phase 1: CORE ENHANCEMENTS (Priority: HIGH - Required for MVP)
- Screen 2.1: Inventory Management Screen
- Screen 3.1: Daily Shift Timeline View
- Screen 4.1: Staff Assignment Deployment
- Screen 5.1: Manager Sign-off Screen
- Enhanced task details with full instructions
- Time-based alerts/warnings

**Timeline:** 2 weeks

### Phase 2: ADMINISTRATIVE FEATURES (Priority: HIGH)
- Screen 2.2: Cash/Till Management
- Screen 3.2: Overdue Alert Center
- Screen 6.2: Communication Viewer
- Daily checklist review report

**Timeline:** 1.5 weeks

### Phase 3: ADVANCED FEATURES (Priority: MEDIUM)
- Screen 1.1-1.3: Checklist Template Builder
- Screen 2.3: Temperature Logger
- Screen 4.2: Employee task assignment view
- Screen 6.3: Variance Management

**Timeline:** 2 weeks

### Phase 4: OPTIMIZATION & POLISH (Priority: MEDIUM)
- Complete component library finalization
- Responsive design validation
- Accessibility audit
- Interaction animations
- Empty states for all screens

**Timeline:** 1 week

---

## 📋 Design System Additions Needed

### New Input Types
- Date/Time Picker
- Currency formatter
- Temperature input with validation
- Signature capture
- Photo capture for task verification
- Voice notes

### New Icons Needed
- Clock/deadline icon
- Warning/alert icons
- Temperature icon
- Till/cash icon
- Assignment/delegation icon
- Checkmark with timestamp

### New Color States
- Overdue: #EF4444 (red)
- Deadline Soon: #F59E0B (amber)
- In Progress: #3B82F6 (blue)
- Completed: #10B981 (green)
- Variance Alert: #EF4444 (red)

### Typography Additions
- Code/monospace for cash amounts, quantities
- Smaller text for timestamps
- Larger text for deadline alerts

---

## ✅ Acceptance Criteria

For "UI/Design Finalization" to be complete:

1. All 13+ new screens designed in Figma
2. All interaction flows documented (user journeys)
3. All edge cases and error states documented
4. Component library finalized with all new components
5. Design system documentation updated
6. High-fidelity prototypes created (clickable flows)
7. Responsive behavior defined for all screen sizes
8. Accessibility guidelines documented (WCAG 2.1 AA)
9. Animation/transition specs documented
10. Design handoff documentation prepared for developers
11. Design review completed with stakeholders
12. All feedback incorporated

---

## 📊 Deliverables

When complete, provide:

1. **Design Files** (Figma)
   - All screens
   - All components
   - All variations/states
   - All flows/prototypes

2. **Documentation**
   - UI Specifications document
   - Component library guide
   - Design system documentation
   - User flow diagrams
   - Wireframes (if needed)

3. **Design Assets**
   - Color palette specs
   - Typography specs
   - Icon library
   - Component specs with dimensions
   - Animation specs (timing, easing, duration)

4. **Developer Handoff**
   - Design markup with specs
   - API/data model documentation
   - State management requirements
   - Performance considerations

---

## 🎯 Next Steps

1. **Confirm Requirements** - Verify all F0890 features should be in Phase 1
2. **Create High-Fidelity Wireframes** - Sketch key screens
3. **Design Phase 1 Screens** - Build detailed Figma mockups
4. **Create Prototype** - Link screens together for user testing
5. **Stakeholder Review** - Get feedback before proceeding to Phase 2

---

**Status:** Ready to begin Phase 1 design work
**Owner:** [Design Team]
**Updated:** May 1, 2026
