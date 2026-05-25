# MealDynamics OpsFlow - Complete Strategy Document

## Restaurant Back-Office Operations Management Platform

**Version:** 1.0  
**Date:** May 3, 2026  
**Tech Stack:** Angular 17 (Web) | React Native (Mobile) | .NET 9 (Backend) | SQL Server

---

## Executive Summary

**MealDynamics OpsFlow** is a comprehensive restaurant back-office operations management platform designed to standardize, track, and optimize daily restaurant tasks across single or multi-unit operations. The system provides real-time task visibility, automated checklist generation, role-based access control, and actionable analytics.

The platform enables restaurant operators to:
- **Standardize Operations** - Create and deploy consistent checklists across locations
- **Increase Accountability** - Track task completion with timestamps and user attribution
- **Improve Productivity** - Automate recurring task generation and reduce manual follow-up
- **Ensure Compliance** - Real-time alerts for overdue tasks and non-compliance events
- **Enable Data-Driven Decisions** - Comprehensive dashboards and reporting

---

## Table of Contents

1. [System Overview & Scope](#1-system-overview--scope)
2. [Feature Breakdown](#2-feature-breakdown)
3. [User Roles & Permissions](#3-user-roles--permissions)
4. [Technical Architecture](#4-technical-architecture)
5. [Data Model](#5-data-model)
6. [Design System](#6-design-system)
7. [User Flows](#7-user-flows)
8. [AI-Enabled Build Strategy](#8-ai-enabled-build-strategy)
9. [Development Phases](#9-development-phases)
10. [Skills & Tooling Requirements](#10-skills--tooling-requirements)
11. [Success Metrics](#11-success-metrics)
12. [Risks & Mitigations](#12-risks--mitigations)

---

## 1. System Overview & Scope

### 1.1 What This App Includes

| Category | Features |
|----------|----------|
| **Task Management** | Create, assign, track, complete tasks with timestamps, GPS, attachments |
| **Checklist System** | Multi-task checklists, templates, recurring schedules, progress tracking |
| **Recurrence Engine** | Daily, weekly, monthly, yearly, custom recurrence patterns |
| **Notification System** | Push, email, in-app alerts for due/overdue/compliance events |
| **Role-Based Dashboards** | Individual, store, multi-store, company-wide views |
| **Reporting & Analytics** | Completion rates, performance rankings, trend analysis |
| **Incident Reporting** | Bottom-up escalation, corrective action workflows |
| **Offline Support** | Mobile offline mode with automatic sync |
| **Template Library** | Central repository for checklist templates |

### 1.2 Multi-Platform Delivery

| Platform | Technology | Use Case |
|----------|------------|----------|
| **Web Dashboard** | Angular 17 | HQ, supervisors, managers, desktop users |
| **Mobile (iOS)** | React Native | Field operations, on-the-floor tasks |
| **Mobile (Android)** | React Native | Field operations, on-the-floor tasks |

### 1.3 Core Differentiators

- **Mobile-First Design** - Optimized for restaurant floor usage
- **Offline Capability** - Works without reliable WiFi
- **GPS Tracking** - Optional location capture for task submissions
- **Compliance Automation** - Automatic follow-up task generation for failed items
- **Scalable Architecture** - Support for hundreds of stores and thousands of daily tasks

---

## 2. Feature Breakdown

### 2.1 Task Types Supported

| Type | Description | Use Case |
|------|-------------|----------|
| Checkbox | Simple yes/no completion | Sanitation checks |
| Yes/No | Binary response | Equipment inspection |
| Single Select | One option from list | Priority level |
| Multi Select | Multiple options | Category selection |
| Numeric | Number input | Count verification |
| Dollar | Currency input | Cash reconciliation |
| Percent | Percentage input | Temperature compliance |
| Temperature | Temperature with range validation | Walk-in cooler checks |
| Attachment | Photo/document upload | Evidence documentation |
| Action Task | Multi-step workflow | Opening procedures |

### 2.2 Task Input Fields

- **Core Properties:** Title, Priority, Visibility, Start/Due DateTime
- **Content:** Details, Attachments, Comments with @tagging
- **Assignment:** Assignee (user or store), Audit trail
- **Compliance:** Trigger values, Reference ranges, Corrective actions

### 2.3 Checklist Features

- Multi-task composition
- Progress tracking (X of Y completed)
- Parent checklist references
- Template inheritance
- Master template vs. instance distinction

### 2.4 Recurrence Engine

| Type | Configuration Options |
|------|----------------------|
| One-time | No recurrence |
| Daily | Every day |
| Weekly | Specific days (Mon, Wed, Fri) |
| Monthly | Day of month (1st, 15th) OR occurrence (first Monday) |
| Yearly | Specific dates |
| Custom | User-defined pattern |

### 2.5 Notification Capabilities

- **Task Reminders** - Alerts before due time
- **Compliance Alerts** - Out-of-range value warnings
- **Escalation Notices** - Management alerts for overdue items
- **Company Announcements** - Broadcast messages from HQ

### 2.6 Dashboard Views by Role

| Role | Dashboard Features |
|------|---------------------|
| Employee | Personal task list, completion status |
| Store Manager | Store-level overview, team performance |
| Supervisor | Multi-store comparison, regional metrics |
| HQ/Ops | Company-wide analytics, cross-org insights |
| Auditor | Audit-focused view, company-wide read access |

---

## 3. User Roles & Permissions

### Role Hierarchy (Lowest to Highest)

```
Employee (Lowest)
    ↓
Store Account (Shared Device)
    ↓
Store Manager
    ↓
Supervisor / Multi-unit Manager
    ↓
Auditor / Inspector
    ↓
HQ / Ops Leadership
    ↓
System Admin (Highest)
```

### 3.1 Employee / Staff

- View own assigned tasks
- View store-level tasks
- Report incidents
- Complete assigned tasks
- Create personal tasks/checklists (visibility to self only)

### 3.2 Store Account

- Shared device account for store teams
- View and complete store-level tasks
- View store dashboard
- Cannot create personal items

### 3.3 Store Manager

- Create tasks/checklists for store
- Assign to employees within store
- Full store dashboard access
- Report access
- Cross-store assignment NOT allowed

### 3.4 Supervisor / Multi-unit Manager

- Manage multiple stores
- Create/assign to employees and stores in jurisdiction
- Multi-store dashboard
- Comparative reporting
- Cross-store visibility

### 3.5 Auditor / Inspector

- Create personal tasks/checklists
- Complete assigned audits
- Tag users in comments for escalation
- Company-wide read access
- **Cannot** assign tasks to others

### 3.6 HQ / Ops Leadership

- All permissions from below roles
- Assign to any store/user in company
- Master template management
- Company-wide analytics
- Template governance authority

### 3.7 System Admin

- Configure checklist types
- System-wide settings and feature toggles
- User account management
- Role assignment
- Store list management
- **No** operational task involvement

---

## 4. Technical Architecture

### 4.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLIENT APPLICATIONS                           │
├─────────────────────────┬─────────────────────────┬─────────────────┤
│     Angular 17 Web      │   React Native Mobile   │   PWA (Web)     │
│      (Dashboard)       │     (iOS/Android)       │   (Optional)    │
└───────────┬─────────────┴───────────┬─────────────┴────────┬────────┘
            │                         │                      │
            │    API Gateway (.NET 9) │                      │
            │            ┌───────────┴───────────┐           │
            │            │                       │           │
            │       Auth Service            Core Services     │
            │            │                       │           │
            │            │      ┌───────────────┴───────────┐│
            │            │      │                           ││
            │            │  Task Engine                   ││
            │            │  Recurrence Scheduler          ││
            │            │  Notification Service        ││
            │            │  Reporting API                 ││
            │            │  GPS Tracking Service          ││
            │            │      │                          ││
            └────────────┴──────┴──────────────────────────┘│
                                 │                           │
                        ┌────────┴─────────┐               │
                        │   SQL Server     │               │
                        │   (Database)     │               │
                        └──────────────────┘               │
                                 │                           │
                        ┌────────┴─────────┐               │
                        │  File Storage    │               │
                        │  (Attachments)  │               │
                        └──────────────────┘               │
```

### 4.2 Backend Architecture (.NET 9)

| Service | Responsibility |
|---------|---------------|
| **Auth Service** | JWT authentication, role-based authorization, SSO integration |
| **Task Engine** | CRUD operations, state management, completion tracking |
| **Recurrence Engine** | Background job processing, automatic task/checklist generation |
| **Notification Service** | Push notifications, email triggers, in-app alerts |
| **Reporting Service** | Analytics aggregation, KPI calculations, export generation |
| **GPS Service** | Location tracking, geofencing |
| **File Service** | Attachment upload/download, image processing |

### 4.3 Frontend Architecture

#### Angular 17 Web (Dashboard)
```
src/
├── app/
│   ├── core/                 # Singleton services, guards, interceptors
│   │   ├── services/        # API services, auth, state management
│   │   ├── guards/          # Route guards (auth, role)
│   │   └── interceptors/    # HTTP interceptors (auth token, error)
│   ├── shared/              # Shared components, pipes, directives
│   │   ├── components/      # Reusable UI components
│   │   ├── pipes/           # Data transformation pipes
│   │   └── directives/      # Custom directives
│   ├── features/            # Feature modules (lazy loaded)
│   │   ├── dashboard/       # Role-based dashboard views
│   │   ├── tasks/           # Task management
│   │   ├── checklists/      # Checklist management
│   │   ├── templates/       # Template library
│   │   ├── reports/        # Analytics & reporting
│   │   ├── settings/        # System configuration
│   │   └── admin/           # Admin panel
│   └── layout/              # Shell components (header, sidebar, footer)
├── assets/                  # Static assets, images, fonts
├── environments/            # Environment configurations
└── styles/                  # Global styles, themes
```

#### React Native Mobile
```
src/
├── components/              # Reusable UI components
│   ├── common/             # Buttons, inputs, cards
│   ├── forms/              # Form field components
│   └── lists/              # List, grid components
├── screens/                # Screen components (one per feature)
├── navigation/             # Navigation configuration (React Navigation)
├── services/               # API services, offline storage
├── store/                  # State management (Redux or Context)
├── hooks/                  # Custom React hooks
├── utils/                  # Helper functions
└── constants/              # Theme, configuration constants
```

### 4.4 Database Schema (SQL Server)

| Table | Purpose |
|-------|---------|
| **Users** | User accounts, roles, profile data |
| **Roles** | Role definitions and permissions |
| **Stores** | Store/location definitions |
| **StoreUserMappings** | User-store assignments |
| **Tasks** | Task definitions and assignments |
| **TaskAttachments** | Task media files |
| **TaskComments** | Task discussion threads |
| **TaskAuditLogs** | Task change history |
| **Checklists** | Checklist definitions |
| **ChecklistTasks** | Tasks within checklists |
| **ChecklistTemplates** | Master templates |
| **RecurrencePatterns** | Recurrence configuration |
| **Notifications** | Notification queue and history |
| **Incidents** | Incident reports |
| **CorrectiveActions** | Follow-up task links |
| **Analytics** | Aggregated metrics (optional) |

---

## 5. Data Model

### 5.1 Core Entities

#### Task Entity
```csharp
class Task {
    // Core Properties
    Id: GUID
    Title: string
    Description: string
    Priority: enum (Critical, High, Medium, Low)
    Visibility: enum (Personal, Store, CompanyWide)
    StartDateTime: DateTime
    DueDateTime: DateTime
    Status: enum (Pending, InProgress, Completed, Overdue, Failed, Deferred)
    InputType: enum (Checkbox, YesNo, SingleSelect, MultiSelect, Numeric, Dollar, Percent, Temperature, Attachment)
    
    // Completion Data
    CompletedBy: GUID (FK -> User)
    CompletedDateTime: DateTime
    SubmissionLocation: string
    GPSCoordinates: string (optional)
    SubmissionValue: string (for numeric/dollar/percent types)
    
    // Relationships
    AssigneeId: GUID (FK -> User)
    AssigneeStoreId: GUID (FK -> Store)
    ParentChecklistId: GUID (FK -> Checklist, optional)
    CreatedBy: GUID (FK -> User)
    
    // Compliance
    ReferenceRangeMin: decimal (optional)
    ReferenceRangeMax: decimal (optional)
    CorrectiveAction: string (optional)
    
    // Metadata
    Details: string
    CreatedAt: DateTime
    UpdatedAt: DateTime
    IsDeleted: bool
}
```

#### Checklist Entity
```csharp
class Checklist {
    Id: GUID
    Title: string
    Description: string
    Type: string (e.g., "Opening", "Closing", "Safety")
    Priority: enum
    Visibility: enum
    AssigneeStoreId: GUID (FK -> Store)
    AssigneeUserId: GUID (FK -> User, optional)
    StartDateTime: DateTime
    DueDateTime: DateTime
    Status: enum
    Progress: string (e.g., "5/10 completed")
    
    // Completion
    CompletedBy: GUID (FK -> User)
    CompletedDateTime: DateTime
    
    // Template Link
    TemplateId: GUID (FK -> ChecklistTemplate, optional)
    RecurrencePatternId: GUID (FK -> RecurrencePattern, optional)
    
    // Metadata
    CreatedBy: GUID
    CreatedAt: DateTime
    UpdatedAt: DateTime
}
```

#### User Entity
```csharp
class User {
    Id: GUID
    Email: string
    PasswordHash: string
    FirstName: string
    LastName: string
    Phone: string (optional)
    RoleId: GUID (FK -> Role)
    IsActive: bool
    LastLoginDateTime: DateTime (optional)
    CreatedAt: DateTime
    UpdatedAt: DateTime
}
```

---

## 6. Design System

### 6.1 Design Philosophy

The UI follows an **Industrial Warmth** aesthetic - designed for quick glance readability in busy restaurant environments while maintaining professional appeal. The dark theme reduces eye strain during long shifts, while the warm amber accent provides clear call-to-action visibility.

**Core Design Principles:**
- **Glance-Friendly** - Key information readable in < 2 seconds
- **Touch-Optimized** - Large touch targets (44px minimum) for mobile use
- **High Contrast** - Text readable in bright and dim lighting
- **Consistent Patterns** - Familiar UI patterns across all screens

---

### 6.2 Color Palette

#### Primary Colors

| Color | Hex Code | Usage |
|-------|----------|-------|
| **Background** | `#0F0F0F` | Main app background |
| **Surface** | `#1A1A1A` | Cards, elevated surfaces |
| **Surface Elevated** | `#242424` | Modals, dropdowns |
| **Primary** | `#1A1A1A` | Primary text |
| **Text** | `#FAFAFA` | Main text on dark backgrounds |
| **Text Secondary** | `#A1A1AA` | Subdued text |
| **Text Muted** | `#71717A` | Disabled, placeholder text |

#### Accent Colors

| Color | Hex Code | Usage |
|-------|----------|-------|
| **Accent** | `#F59E0B` | Primary actions, highlights |
| **Accent Light** | `#FBBF24` | Hover states |
| **Accent Dark** | `#D97706` | Pressed states |

#### Semantic Colors

| Color | Hex Code | Usage |
|-------|----------|-------|
| **Success** | `#10B981` | Completed, verified, approved |
| **Success Light** | `#34D399` | Success hover |
| **Warning** | `#F59E0B` | Pending, deadline soon |
| **Warning Light** | `#FBBF24` | Warning hover |
| **Danger** | `#EF4444` | Overdue, error, critical |
| **Danger Light** | `#F87171` | Danger hover |
| **Info** | `#3B82F6` | In-progress, informational |

#### Border Colors

| Color | Hex Code | Usage |
|-------|----------|-------|
| **Border** | `#333333` | Default borders |
| **Border Light** | `#404040` | Elevated borders |
| **Overlay** | `rgba(0,0,0,0.7)` | Modal/dialog overlays |

---

### 6.3 Typography

#### Font Stack

| Platform | Primary Font | Use Case |
|----------|-------------|----------|
| **iOS** | System (San Francisco) | All text |
| **Android** | Roboto | All text |
| **Web** | Inter, system-ui | Fallback fonts |

#### Type Scale

| Style | Size | Weight | Line Height | Usage |
|-------|------|--------|--------------|-------|
| **H1** | 32px | 700 | 40px | Screen titles |
| **H2** | 24px | 700 | 32px | Section headers |
| **H3** | 20px | 600 | 28px | Subsection headers |
| **H4** | 18px | 600 | 26px | Card titles |
| **Body** | 16px | 400 | 24px | Primary content |
| **Body Small** | 14px | 400 | 20px | Secondary content |
| **Caption** | 12px | 500 | 16px | Labels, timestamps |
| **Button** | 16px | 600 | 24px | Button text |
| **Label** | 14px | 600 | 20px | Form labels |

#### Text Styles

```css
/* Example styles */
.heading-1 {
  font-size: 32px;
  font-weight: 700;
  letter-spacing: -0.5px;
  color: #FAFAFA;
}

.heading-2 {
  font-size: 24px;
  font-weight: 700;
  letter-spacing: -0.3px;
  color: #FAFAFA;
}

.body-text {
  font-size: 16px;
  font-weight: 400;
  line-height: 24px;
  color: #FAFAFA;
}

.caption-text {
  font-size: 12px;
  font-weight: 500;
  letter-spacing: 0.5px;
  text-transform: uppercase;
  color: #71717A;
}
```

---

### 6.4 Spacing System

#### Spacing Scale

| Token | Value | Usage |
|-------|-------|-------|
| **xs** | 4px | Tight spacing, icon gaps |
| **sm** | 8px | Compact elements |
| **md** | 16px | Default spacing |
| **lg** | 24px | Section spacing |
| **xl** | 32px | Major sections |
| **xxl** | 48px | Page margins |

#### Common Spacing Patterns

| Pattern | Spacing | Usage |
|---------|---------|-------|
| Card padding | 16px | Standard card content |
| Screen padding | 16-20px | Screen edge margins |
| List item spacing | 12px | Between list items |
| Button padding | 14px vertical, 24px horizontal | Button touch area |
| Input padding | 14px vertical, 16px horizontal | Form field padding |

---

### 6.5 Component Library

#### 6.5.1 Buttons

| Component | Variants | States |
|-----------|----------|--------|
| **Button** | Primary, Secondary, Outline, Ghost, Danger | Default, Pressed, Disabled, Loading |

**Primary Button (Default)**
```jsx
// Properties
variant: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger'
size: 'small' | 'medium' | 'large'
disabled: boolean
loading: boolean
icon: ReactNode

// Style: Amber background (#F59E0B), dark text (#0F0F0F)
// Height: Small=36px, Medium=44px, Large=52px
// Border Radius: 12px
// Shadow: small (elevation 2)
```

#### 6.5.2 Cards

| Component | Properties |
|-----------|------------|
| **Card** | Elevated boolean |

**Card Styles**
```jsx
// Standard Card
backgroundColor: #1A1A1A
borderRadius: 16px
padding: 16px
border: 1px solid #333333

// Elevated Card
backgroundColor: #242424
boxShadow: 0 4px 8px rgba(0,0,0,0.3)
```

#### 6.5.3 Input Fields

| Component | Properties |
|-----------|------------|
| **Input** | label, value, onChangeText, placeholder, keyboardType, multiline, error, helper |

**Input Styles**
```jsx
backgroundColor: #242424
borderRadius: 12px
padding: 14px horizontal, 14px vertical
border: 1px solid #333333
fontSize: 16px
color: #FAFAFA
placeholderColor: #71717A

// Error state: borderColor: #EF4444
```

#### 6.5.4 Status Badges

| Status | Background | Text Color | Icon |
|--------|------------|------------|------|
| **Completed** | `#10B98120` | `#10B981` | Green dot |
| **In Progress** | `#3B82F620` | `#3B82F6` | Blue dot |
| **Pending** | `#F59E0B20` | `#F59E0B` | Yellow dot |
| **Overdue** | `#EF444420` | `#EF4444` | Red dot |

#### 6.5.5 Tab Bar (Mobile)

```jsx
// Bottom tab bar
backgroundColor: #1A1A1A
borderTop: 1px solid #333333
paddingBottom: 20px (for home indicator)
paddingTop: 8px

// Tab item
activeBackground: #F59E0B20
activeIconColor: #F59E0B
inactiveIconColor: #71717A

// Tab label
fontSize: 10px
fontWeight: 600
textTransform: uppercase
letterSpacing: 0.3px
```

#### 6.5.6 Modals & Overlays

```jsx
// Modal overlay
backgroundColor: rgba(0, 0, 0, 0.7)

// Modal content
backgroundColor: #1A1A1A
borderRadius: 20px
border: 1px solid #333333
width: 85%
maxWidth: 340px
padding: 24px
```

---

### 6.6 Screen Layouts

#### 6.6.1 Navigation Structure

**Mobile Navigation (React Native)**
```
Tab Bar (Bottom - 5 tabs)
├── Home (Dashboard)
├── Timeline (Daily Checklist)
├── Tasks (My Assignments)
├── Comm (Communications)
└── More (Grid Menu)
    ├── Inventory
    ├── Cash Mgmt
    ├── Temp Log
    ├── Alerts
    ├── Deploy
    ├── Sign Off
    ├── Report
    ├── Variance
    ├── Builder
    └── Task Detail
```

**Web Navigation (Angular 17)**
```
Top Header
├── Logo
├── Search
├── Notifications
└── User Menu

Left Sidebar (Collapsible)
├── Dashboard
├── Tasks
├── Checklists
├── Templates
├── Reports
├── Team (Store/Users)
├── Settings
└── Admin (role-based)
```

#### 6.6.2 Screen Patterns

**Dashboard Screen Pattern**
```
┌────────────────────────────┐
│ Header (Title + Profile)  │
├────────────────────────────┤
│ Stats Card (Progress Ring) │
├────────────────────────────┤
│ Quick Actions (Grid)       │
├────────────────────────────┤
│ Active Checklist (Card)     │
├────────────────────────────┤
│ Alerts Banner (if any)     │
├────────────────────────────┤
│ Manager Tools (Grid)       │
└────────────────────────────┘
```

**List Screen Pattern**
```
┌────────────────────────────┐
│ Header (Back + Title)       │
├────────────────────────────┤
│ Filter/Search Bar           │
├────────────────────────────┤
│ Summary Stats               │
├────────────────────────────┤
│ Scrollable List             │
│ ┌────────────────────────┐  │
│ │ List Item Card        │  │
│ │ - Title + Status     │  │
│ │ - Meta info          │  │
│ │ - Actions            │  │
│ └────────────────────────┘  │
├────────────────────────────┤
│ Floating Action Button     │
└────────────────────────────┘
```

**Detail Screen Pattern**
```
┌────────────────────────────┐
│ Header (Back + Title +...)  │
├────────────────────────────┤
│ Status Banner              │
├────────────────────────────┤
│ Progress Section           │
├────────────────────────────┤
│ Content Area               │
│ - Instructions             │
│ - Data Fields              │
│ - Subtasks                │
├────────────────────────────┤
│ Notes Section              │
├────────────────────────────┤
│ History/Audit Trail        │
├────────────────────────────┤
│ Action Buttons (Sticky)    │
└────────────────────────────┘
```

#### 6.6.3 Responsive Behavior

| Breakpoint | Layout Changes |
|------------|----------------|
| **Mobile** (< 375px) | Single column, stacked elements |
| **Tablet** (375-768px) | 2-column grids where appropriate |
| **Desktop** (768-1024px) | Sidebar visible, 2-3 column grids |
| **Large Desktop** (> 1024px) | Full sidebar, 3-4 column grids |

---

### 6.7 Theme Variants

#### 6.7.1 Dark Theme (Default)

The dark theme is optimized for:
- Restaurant environments with varying lighting
- Extended use during long shifts
- Reducing eye strain

**CSS Variables (Dark)**
```css
:root {
  --color-background: #0F0F0F;
  --color-surface: #1A1A1A;
  --color-surface-elevated: #242424;
  --color-text: #FAFAFA;
  --color-text-secondary: #A1A1AA;
  --color-text-muted: #71717A;
  --color-accent: #F59E0B;
  --color-success: #10B981;
  --color-warning: #F59E0B;
  --color-danger: #EF4444;
  --color-info: #3B82F6;
  --color-border: #333333;
  --color-border-light: #404040;
}
```

#### 6.7.2 Light Theme (Future)

Future enhancement for:
- Corporate office use
- Bright environments
- Print-friendly reports

**Planned Light Theme**
```css
:root {
  --color-background: #FAFAFA;
  --color-surface: #FFFFFF;
  --color-surface-elevated: #F5F5F5;
  --color-text: #1A1A1A;
  --color-text-secondary: #52525B;
  --color-text-muted: #71717A;
  /* Accent and semantic remain similar */
  --color-border: #E4E4E7;
}
```

---

### 6.8 Icons & Graphics

#### Icon Style
- **Style:** Simple, outlined, 24px base size
- **Library:** Custom restaurant-themed icons + system icons

#### Custom Icons Required

| Icon | Purpose |
|------|---------|
| ⏰ | Deadline/Time |
| ⚠️ | Alert/Warning |
| 🌡️ | Temperature |
| 💵 | Cash/Till |
| 👤 | Assignment |
| ✓ | Completed |
| 📊 | Data/Analytics |
| 📋 | Checklist |
| 📝 | Task |
| 🔔 | Notification |

---

### 6.9 Accessibility (WCAG 2.1 AA Compliance)

#### Color Contrast Requirements

| Element | Minimum Contrast Ratio |
|---------|------------------------|
| **Normal Text** | 4.5:1 |
| **Large Text** (18px+) | 3:1 |
| **UI Components** | 3:1 |
| **Text over Images** | 3:1 |

#### Touch Targets

| Element | Minimum Size |
|---------|--------------|
| **Buttons** | 44x44px (Apple) / 48x48dp (Android) |
| **List Items** | 44px height minimum |
| **Form Fields** | 44px height minimum |
| **Icons** | 24px with 44px touch area |

#### Screen Reader Support

| Requirement | Implementation |
|-------------|----------------|
| **Alt Text** | All images have descriptive labels |
| **Labels** | Form fields have associated labels |
| **Focus Indicators** | Visible focus states on all interactive elements |
| **Reading Order** | Logical tab order, proper heading hierarchy |
| **ARIA** | Proper ARIA roles and states |

#### Accessibility Features

- [ ] Semantic HTML structure
- [ ] Keyboard navigation support (web)
- [ ] Focus management
- [ ] Screen reader announcements for dynamic content
- [ ] Sufficient color contrast (verified)
- [ ] Touch target sizes meet requirements
- [ ] Text resizable to 200% without loss of functionality

---

### 6.10 Animation & Transitions

#### Animation Principles

- **Purposeful** - Animations serve a function (feedback, orientation, continuation)
- **Quick** - Duration between 150-300ms
- **Subtle** - Motion shouldn't distract from content

#### Animation Specifications

| Animation | Duration | Easing | Use Case |
|-----------|----------|--------|----------|
| **Screen Transition** | 300ms | ease-in-out | Page navigation |
| **Modal Open** | 250ms | ease-out | Dialog appearance |
| **Button Press** | 100ms | ease-in | Press feedback |
| **List Item Appear** | 200ms | ease-out | Staggered list loading |
| **Progress Update** | 300ms | ease-in-out | Progress bar changes |

#### Implementation

```css
/* Example transition */
transition: all 0.3s ease-in-out;

/* React Native */
animationDuration: 300,
animation: 'slide',
easing: Easing.inOut(Easing.ease)
```

---

### 6.11 Screen Inventory

The application includes 16 core screens across mobile and web:

#### Mobile Screens (React Native)

| # | Screen | Purpose |
|---|--------|---------|
| 1 | Dashboard | Central hub, today's progress, quick actions |
| 2 | Daily Shift Timeline | Time-based task sections (9am, 11am, etc.) |
| 3 | Inventory Management | 3-day dough/cheese tracking |
| 4 | Staff Assignment (Deploy) | Assign closing tasks to staff |
| 5 | Manager Sign-off | Section completion confirmation |
| 6 | Task Detail | Enhanced task with subtasks, instructions |
| 7 | Cash/Till Management | Till counting, variance tracking |
| 8 | Overdue Alert Center | Overdue task alerts and actions |
| 9 | Communication Viewer | Messages from management |
| 10 | Daily Review Report | End-of-day summary |
| 11 | Temperature Logger | Temperature recording |
| 12 | Employee Task View | Employee's assigned tasks |
| 13 | Variance Management | Inventory discrepancy tracking |
| 14 | Checklist Template Builder | Create/modify templates |

#### Web Screens (Angular 17)

| # | Screen | Purpose |
|---|--------|---------|
| 1 | Dashboard | Role-based analytics overview |
| 2 | Task Management | Full CRUD, filtering, assignment |
| 3 | Checklist Management | Template and instance management |
| 4 | Template Library | Central repository browser |
| 5 | Reports & Analytics | Performance metrics, exports |
| 6 | User Management | User/role CRUD |
| 7 | Store Management | Location configuration |
| 8 | Settings | System configuration |
| 9 | Admin Panel | System administration |

---

### 6.12 Design Assets Deliverables

When design is complete, provide:

| Asset | Description |
|-------|-------------|
| **Figma File** | All screens, components, variants |
| **Color Palette** | Hex codes for all colors |
| **Typography Spec** | Font families, sizes, weights |
| **Icon Library** | SVG icons for all UI elements |
| **Component Specs** | Dimensions, states, behaviors |
| **Animation Specs** | Duration, easing, triggers |
| **Responsive Breakpoints** | At what points layouts change |
| **Accessibility Notes** | Screen reader flow, keyboard order |

---

### 6.13 Implementation Notes

#### Mobile (React Native)
- Built with Expo for faster builds and easier deployment
- Uses `StyleSheet.create()` for styles (not CSS-in-JS libraries)
- All screens already implemented in `/opsflow-app/src/screens/`

#### Web (Angular 17)
- Component library to be generated matching mobile designs
- Angular Material as base with custom theme overlay
- Lazy-loaded feature modules for performance

#### Design Tokens
- All colors, spacing, typography stored as constants
- Theme changes propagate through token system
- Both platforms use matching values for consistency

---

## 7. User Flows

### 6.1 Daily Opening Checklist Flow

```
┌─────────────┐     ┌──────────────┐     ┌─────────────────┐
│ Recurrence  │────▶│   System     │────▶│  Employee       │
│ Engine      │     │  Generates   │     │  Receives       │
│ (6:00 AM)   │     │  Checklist   │     │  Push Notif     │
└─────────────┘     └──────────────┘     └────────┬────────┘
                                                   │
                    ┌──────────────┐              ▼
                    │   Opens      │     ┌─────────────────┐
                    │   Mobile App │────▶│  Completes     │
                    │   or Web     │     │  Tasks          │
                    └──────────────┘     │  (Checkbox,     │
                                         │  Temperature,   │
                    ┌──────────────┐     │  Photo)         │
                    │   Stores     │◀────┘                 │
                    │   Completed  │     ┌─────────────────┐
                    │   Tasks      │────▶│  Submits        │
                    └──────────────┘     │  Checklist      │
                                         └────────┬────────┘
                                                  │
                    ┌──────────────┐              ▼
                    │   Notifies   │     ┌─────────────────┐
                    │   Manager    │────▶│  Manager        │
                    └──────────────┘     │  Reviews        │
                                         └────────┬────────┘
                                                  │
                    ┌──────────────┐              ▼
                    │   Signs Off │     ┌─────────────────┐
                    │   (Initials)│────▶│  Dashboard      │
                    └──────────────┘     │  Updates        │
                                         └─────────────────┘
```

### 6.2 Incident Reporting Flow

```
┌─────────────┐     ┌──────────────┐     ┌─────────────────┐
│  Employee   │────▶│  Creates     │────▶│  Incident       │
│  Identifies │     │  Incident    │     │  Recorded       │
│  Issue      │     │  Report      │     └────────┬────────┘
└─────────────┘     └──────────────┘              │
                                                   │
                    ┌──────────────┐              ▼
                    │   Tags       │     ┌─────────────────┐
                    │   Manager    │────▶│  Manager        │
                    │   in Comments│     │  Notified        │
                    └──────────────┘     └────────┬────────┘
                                                   │
                    ┌──────────────┐              ▼
                    │   Reviews    │     ┌─────────────────┐
                    │   Incident   │────▶│  Creates        │
                    └──────────────┘     │  Corrective     │
                                         │  Action Task    │
                                         └────────┬────────┘
                                                  │
                    ┌──────────────┐              ▼
                    │   Task       │     ┌─────────────────┐
                    │   Assigned   │────▶│  Team Member    │
                    │   to Staff   │     │  Completes      │
                    └──────────────┘     └─────────────────┘
```

### 6.3 Multi-Store Audit Flow

```
┌─────────────┐     ┌──────────────┐     ┌─────────────────┐
│  Supervisor │────▶│  Assigns     │────▶│  Store Managers │
│  (HQ/Ops)   │     │  Audit       │     │  Receive        │
│             │     │  Checklist   │     │  Notifcation    │
└─────────────┘     └──────────────┘     └────────┬────────┘
                                                   │
                    ┌──────────────┐              ▼
                    │   Complete   │     ┌─────────────────┐
                    │   Audit at   │────▶│  Records        │
                    │   Each Store │     │  Results        │
                    │   (with GPS) │     └────────┬────────┘
                    └──────────────┘              │
                                                   │
                    ┌──────────────┐              ▼
                    │   Tags       │     ┌─────────────────┐
                    │   Issues in  │────▶│  Non-Compliance │
                    │   Comments   │     │  Escalated      │
                    └──────────────┘     └────────┬────────┘
                                                   │
                    ┌──────────────┐              ▼
                    │   Auto       │     ┌─────────────────┐
                    │   Creates    │────▶│  Corrective     │
                    │   Follow-up  │     │  Tasks Created  │
                    └──────────────┘     └─────────────────┘
```

---

## 8. AI-Enabled Build Strategy

### 7.1 Where AI Helps in Development

| Phase | AI Application | Tools/Approach |
|-------|----------------|----------------|
| **Requirements Analysis** | Generate user stories, acceptance criteria from SRS | Claude, GPT |
| **Code Generation** | Generate API endpoints, services, components | Codex, Copilot |
| **Testing** | Generate unit tests, integration tests, e2e tests | AI test generators |
| **Documentation** | Auto-generate API docs, user guides, code comments | AI documentation tools |
| **Code Review** | Static analysis, security scanning, best practices | AI code review tools |
| **Performance** | Query optimization, load testing scenarios | AI profiling tools |

### 7.2 AI Skills Required

| Skill | Purpose |
|-------|---------|
| **Frontend Designer** | Generate Angular/React components with consistent styling |
| **Backend Developer** | Generate .NET services, controllers, EF Core code |
| **Database Designer** | Generate SQL schemas, stored procedures, indexes |
| **Test Engineer** | Generate unit tests, integration tests, Playwright e2e tests |
| **Documentation Writer** | Generate API docs, user guides, architecture docs |

### 7.3 Build Approach with AI

**Step 1: Scaffold First**
- Let AI generate project structure and basic files
- Ensure architecture decisions are made upfront
- AI creates: folder structure, base components, service stubs

**Step 2: Human-in-the-Loop**
- Developer reviews and approves AI-generated code
- Makes adjustments for business logic specifics
- Ensures alignment with requirements

**Step 3: Pattern-Based Generation**
- For repetitive components (CRUD screens, forms, lists)
- AI generates based on patterns from existing code
- Developer focuses on unique business logic

**Step 4: Testing Augmentation**
- AI generates test coverage for generated code
- Developer adds edge cases and integration tests
- Focus on critical business paths

### 7.4 Code Quality Assurance with AI

```
AI Generated Code ──▶ Human Review ──▶ Adjustments ──▶ Unit Tests
       │                    │                  │
       │                    │                  │
       ▼                    ▼                  ▼
  Pattern-based        Business logic      Coverage
  boilerplate          customization        verification
```

---

## 9. Development Phases

### Phase 1: Foundation (Weeks 1-4)

| Deliverable | Description |
|-------------|-------------|
| **Project Setup** | Angular 17, React Native, .NET 9 projects initialized |
| **Authentication** | JWT auth, role-based login, SSO preparation |
| **User Management** | CRUD for users, roles, stores |
| **Core Data Model** | Database schema, EF Core migrations |
| **API Foundation** | Core endpoints for tasks, checklists |

**AI Contribution:**
- Generate project scaffolding
- Create base components and services
- Generate database migrations

### Phase 2: Task Management (Weeks 5-8)

| Deliverable | Description |
|-------------|-------------|
| **Task CRUD** | Create, read, update, delete tasks |
| **Task Completion** | Status updates, timestamps, GPS |
| **Task Assignment** | User and store assignment |
| **Task Types** | All input types supported |
| **Attachments** | File upload/download |

**AI Contribution:**
- Form components for each task type
- API service methods
- Validation logic

### Phase 3: Checklist System (Weeks 9-12)

| Deliverable | Description |
|-------------|-------------|
| **Checklist Management** | Create, manage checklists |
| **Template Library** | Central repository for templates |
| **Template Builder** | Visual template editor |
| **Progress Tracking** | X of Y completion |

**AI Contribution:**
- Checklist form builders
- Template configuration UI
- Progress calculation services

### Phase 4: Recurrence & Notifications (Weeks 13-16)

| Deliverable | Description |
|-------------|-------------|
| **Recurrence Engine** | Background job processing |
| **Auto-generation** | Daily, weekly, monthly patterns |
| **Notification Service** | Push, email, in-app |
| **Alert Logic** | Due/overdue alerts |

**AI Contribution:**
- Recurrence calculation logic
- Notification templates
- Alert rule engines

### Phase 5: Dashboards & Reporting (Weeks 17-20)

| Deliverable | Description |
|-------------|-------------|
| **Role-Based Dashboards** | Individual, store, multi-store, HQ |
| **Analytics Engine** | KPI calculations |
| **Report Generation** | Export to PDF, Excel |
| **Performance Metrics** | Completion rates, rankings |

**AI Contribution:**
- Dashboard components
- Chart configurations
- Report templates

### Phase 6: Mobile & Offline (Weeks 21-24)

| Deliverable | Description |
|-------------|-------------|
| **React Native App** | iOS and Android builds |
| **Offline Mode** | Local storage, sync queue |
| **GPS Tracking** | Location capture |
| **Push Notifications** | Mobile alerts |

**AI Contribution:**
- Mobile screen components
- Offline sync logic
- Native feature integration

### Phase 7: Integration & Polish (Weeks 25-28)

| Deliverable | Description |
|-------------|-------------|
| **Security Audit** | Penetration testing |
| **Performance Tuning** | Load testing, optimization |
| **Integration Ready** | API documentation |
| **User Acceptance Testing** | Business validation |

---

## 10. Skills & Tooling Requirements

### 9.1 Required Skills

| Role | Skills Required |
|------|-----------------|
| **Angular Developer** | Angular 17, TypeScript, RxJS, Angular Material, NGXS/Zustand |
| **React Native Developer** | React Native, Expo, React Navigation, Redux/Context API |
| **.NET Backend Developer** | .NET 9, ASP.NET Core, EF Core, JWT, Hangfire (background jobs) |
| **SQL Server DBA** | SQL Server, stored procedures, indexing, performance tuning |
| **DevOps Engineer** | Docker, CI/CD, Azure/AWS deployment, monitoring |
| **QA Engineer** | Playwright, Jest, Cypress, automated testing |

### 9.2 Recommended AI Tools

| Tool | Purpose |
|------|---------|
| **Claude/OpenAI** | Requirements analysis, code generation, documentation |
| **GitHub Copilot** | Code completion, pattern generation |
| **Playwright (Skill)** | Browser automation for testing |
| **Frontend Design (Skill)** | UI component generation |

### 9.3 Development Tools

| Category | Tool |
|----------|------|
| **IDE** | VS Code, Visual Studio 2022 |
| **API Client** | Postman, Swagger |
| **Version Control** | Git, GitHub/GitLab |
| **CI/CD** | GitHub Actions, Azure DevOps |
| **Containerization** | Docker, Docker Compose |
| **Monitoring** | Application Insights, Serilog |

---

## 11. Success Metrics

### 10.1 Development Metrics

| Metric | Target |
|--------|--------|
| Code Coverage | ≥ 80% |
| API Response Time | < 200ms (p95) |
| Build Success Rate | 100% |
| Sprint Velocity | Consistent across teams |

### 10.2 Application Metrics

| Metric | Target |
|--------|--------|
| Task Completion Rate | ≥ 95% on-time |
| User Adoption | ≥ 80% daily active users |
| Offline Sync Success | ≥ 99% |
| Push Notification Delivery | ≥ 95% |
| Dashboard Load Time | < 3 seconds |

### 10.3 Business Metrics

| Metric | Target |
|--------|--------|
| Operational Compliance | 100% checklist completion |
| Manager Time Savings | 30% reduction in follow-up tasks |
| Incident Response Time | < 2 hours to resolution |
| Multi-location Consistency | Standardized across 100% of stores |

---

## 12. Risks & Mitigations

### 11.1 Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Offline sync conflicts | High | High | Conflict resolution algorithm, last-write-wins with user confirmation |
| Performance at scale | Medium | High | Load testing from Phase 1, database indexing strategy |
| Mobile app store approval delays | Medium | Medium | Early engagement with App Store guidelines, Expo for faster builds |

### 11.2 Adoption Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| User resistance to new system | High | High | Change management, training program, phased rollout |
| Alert fatigue | Medium | Medium | Configurable notifications, smart grouping |
| Training gap | Medium | Medium | Comprehensive docs, video tutorials, in-app help |

### 11.3 Integration Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| POS integration complexity | Medium | High | API-first design, phased integration |
| SSO configuration challenges | Low | Medium | Standard protocols (OAuth2/SAML), early IT engagement |

---

## Appendix A: UI Reference

The existing V1 UI screens (62 images in `V1 MealDynamics OpsFlow Mobile App/`) provide reference for:
- Dashboard layouts and visualizations
- Task and checklist UI patterns
- Navigation and tab structures
- Form designs for data entry

The F0890 pizza checklist requirements (`UI_Completion_Roadmap.md`) specify:
- Time-based section organization (9am, 11am, 3:30pm, Close)
- Data entry fields (inventory, temperature, cash)
- Manager sign-off workflows
- Staff assignment delegation

---

## Appendix B: Key Decisions Needed

Before proceeding, the following decisions are required:

1. **Offline Strategy** - Full offline or partial? Which features must work offline?
2. **SSO Provider** - Which identity provider? (Azure AD, Okta, custom)
3. **Deployment Target** - Cloud (Azure/AWS) or on-premise?
4. **Notification Provider** - SendGrid, Firebase, or built-in?
5. **File Storage** - Azure Blob, AWS S3, or local?

---

**Document Status:** Ready for Review  
**Next Steps:** Approval to proceed with Phase 1 development