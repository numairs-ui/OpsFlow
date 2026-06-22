# F0890 Daily Operations Sheet — Complete Field Map

> Source: `F0890 DP sample (1).pdf`  
> Mapped: 2026-06-19  
> Status: **Pending confirmation** — do not build until open questions in Summary section are resolved.

The PDF covers one repeating daily sheet (same layout Mon/Tue/Wed). It has **two physical pages** that together form one daily ops record. The content breaks into **13 logical sections** across two checklists (Morning Opening, Evening Closing). The other 3 checklists (Midday, Stock Rotation, Pre-Close Sign-Off) are **not on this form** — those fields were invented and are worth revisiting separately.

---

## PAGE 1 — Daily Page 1 of 2

---

### SECTION 1 — Communications
**Belongs to:** Morning Opening  
**Purpose:** Blast/promotions/special tasks, free-form notes from manager

| # | Field Label | Type | Notes |
|---|---|---|---|
| 1 | Communications notes | `Text` (multi-line) | Free-form blast/memo field |

---

### SECTION 2 — 3-Day Dough/Cheese Management Plan (MDOG)
**Belongs to:** Morning Opening  
**Purpose:** Per-size dough inventory, temperature log, action determination  
**This is a table — each pizza size has its own row**

**Per-size fields (repeat for 10", 12", 14", 16", Dia):**

| # | Field Label | Type | Notes |
|---|---|---|---|
| 1 | [Size] — On Hand Amount | `Numeric` | Count of dough balls |
| 2 | [Size] — Production Date | `Text` | Written as date label |
| 3 | [Size] — Expiration Date | `Text` | Written as date label |
| 4 | [Size] — Today's Need | `Numeric` | Can be calculated or entered |
| 5 | [Size] — Day 2's Need | `Numeric` | |
| 6 | [Size] — Day 3's Need | `Numeric` | |
| 7 | [Size] — Total Needed | `Numeric` | Sum of day 1+2+3 needs |
| 8 | [Size] — Action to Be Taken | `Select` (A/B/C) | A=OK, B=Too much on hand, C=Not enough |

**For Cheese row:**

| # | Field Label | Type | Notes |
|---|---|---|---|
| 9 | Cheese — On Hand Amount | `Numeric` | |
| 10 | Cheese — 7-Day Date | `Text` | Date label for cheese |
| 11 | Cheese — Action to Be Taken | `Select` (A/B/C) | |

**Temperature readings (one per MDOG session, not per size):**

| # | Field Label | Type | Notes |
|---|---|---|---|
| 12 | Makeline Temperature (°F) | `Numeric` | Single reading |
| 13 | Walk-in Temperature (°F) | `Numeric` | Single reading |

> **Open question:** `Select` A/B/C field type doesn't exist yet. Options: model as `Text` with hint label, or add a `Select` field type to the system.

---

### SECTION 3 — Cash Management
**Belongs to:** Morning Opening (Opening columns) and Evening Closing (Closing columns)  
**Purpose:** Denomination-by-denomination cash count for Safe, Till A, Till B  
**The PDF splits this into Opening / Shift Change / Closing — all on one sheet**

**Fields (repeat for each denomination, for each column — Safe / Till A / Till B):**

| Denomination | Type |
|---|---|
| Banks | `Numeric` |
| 100s/50s | `Numeric` |
| 20s | `Numeric` |
| 10s | `Numeric` |
| 5s | `Numeric` |
| 1s | `Numeric` |
| Quarters | `Numeric` |
| Dimes | `Numeric` |
| Nickels | `Numeric` |
| Pennies | `Numeric` |
| Total | `Numeric` (calculated or entered) |
| Should Be | `Numeric` |
| Variance | `Numeric` (Total minus Should Be) |

13 denominations × 3 columns (Safe, Till A, Till B) = **39 fields per sub-section** (Opening, Shift Change, Closing).

> **Open question:** Opening cash → Morning Opening checklist; Closing cash → Evening Closing; Shift Change → Midday? Or one dedicated "Cash Management" template?

---

### SECTION 4 — Opening (Initial When Complete)
**Belongs to:** Morning Opening  
**Manager's Initials required on PDF**

| # | Field Label | Type | Notes |
|---|---|---|---|
| 1 | Arrival time | `Text` | Written as "\_\_\_ AM" |
| 2 | Conduct security walk-through | `Boolean` | |
| 3 | Check for delayed orders | `Boolean` | |
| 4 | Check Red Book for mgr. communication | `Boolean` | |
| 5 | Set up make line – date with label system, note all product prepped for MDOG | `Boolean` | Bold on PDF |
| 6 | Place portion cups in each product | `Boolean` | |
| 7 | Print MDOG | `Boolean` | Bold |
| 8 | Pull dough for lunch usage, see MDOG | `Boolean` | Bold |
| 9 | Make sure all dough sizes are pulled & properly proofed | `Boolean` | |
| 10 | Check expiration dates and discard if necessary | `Boolean` | |
| 11 | Place working thermometers in all dough sizes | `Boolean` | |
| 12 | Prep thin crust | `Boolean` | |
| 13 | Fill three compartment sink | `Boolean` | |

---

### SECTION 5 — Set Up Cut Table
**Belongs to:** Morning Opening  
**Manager's Initials required on PDF**

| # | Field Label | Type |
|---|---|---|
| 1 | Set up cutter rack – ensure cutters available for all sauces/desserts | `Boolean` |
| 2 | Prep garlic cups and pepperoncini | `Boolean` |
| 3 | Garlic parm. bottle – date with label system | `Boolean` |
| 4 | White drizzle – date with label system | `Boolean` |
| 5 | Wings sauce bottles – date with label system | `Boolean` |
| 6 | Side cups full and in place. All cups except BBQ & buffalo must be refrigerated | `Boolean` |

---

### SECTION 6 — Set Up Sauce Station
**Belongs to:** Morning Opening  
**Manager's Initials required — "Initial When Complete"**

| # | Field Label | Type |
|---|---|---|
| 1 | Make fresh sauce – date with label system | `Boolean` |
| 2 | BBQ sauce – date with label system (6 oz spoodle) | `Boolean` |
| 3 | Garlic sauce bottle – date with label system | `Boolean` |
| 4 | Fill sanitizer buckets | `Boolean` |

---

### SECTION 7 — Set Up Customer Lobby
**Belongs to:** Morning Opening  
**Manager's Initials — deadline: ALL must be complete by 10:00 AM**

| # | Field Label | Type | Notes |
|---|---|---|---|
| 1 | Floor mat in place | `Boolean` | |
| 2 | Clean window ledges inside and outside | `Boolean` | |
| 3 | Make sure Pepsi cooler is stocked, vents and door rails clean and dust free, labels facing forward | `Boolean` | |
| 4 | Clean all baseboards, make sure they are free of buildup in lobby | `Boolean` | |
| 5 | Clean base of chairs, table legs & benches | `Boolean` | Bold |
| 6 | Sweep parking lot – no cig. butts, trash & debris | `Boolean` | |
| 7 | Go to bank at 9:30 AM – deposit. If unable to make deposit, supervisor must be notified by 10:00 AM | `Boolean` | Bold |

---

## PAGE 2 — Daily Page 2 of 2

---

### SECTION 8 — Set Up Customer Lobby (continued)
**Belongs to:** Morning Opening (continuation of Section 7)  
**Manager's Initials — deadline: ALL must be complete by 11:00 AM**

| # | Field Label | Type | Notes |
|---|---|---|---|
| 1 | Fill out deposit log and attach bank deposit slips to DOR | `Boolean` | |
| 2 | Post deployment chart using guide | `Boolean` | |
| 3 | Note what to prep on MDOG | `Boolean` | |
| 4 | Complete 3-day dough and hourly dough pulls | `Boolean` | |
| 5 | Count tills and store cash | `Boolean` | |
| 6 | Turn on the open & neon signs – dust if needed, top and bottom | `Boolean` | Bold |
| 7 | Every item in whole unit dated with new label system – check no items are expired! | `Boolean` | |
| 8 | Received, open and expiration dates – timed and initialed | `Boolean` | |
| 9 | Place empty dough trays outside and cover | `Boolean` | |
| 10 | Dates checked on top and bottom of make line | `Boolean` | |
| 11 | Check Pizza Academy, print and post # | `Boolean` | |

---

### SECTION 9 — Ongoing Duties During Lunch
**Belongs to:** Midday checklist  
**Manager's Initials — no specific deadline**

| # | Field Label | Type | Notes |
|---|---|---|---|
| 1 | Clean as you go – keep until clean and tight | `Boolean` | |
| 2 | Complete daily beautification checklist | `Boolean` | |
| 3 | Manage dough by moving back and forth to walk-in | `Boolean` | |
| 4 | Down stack and patty place within 24 hrs of delivery | `Boolean` | |
| 5 | When dough reaches 56°F it must be placed back into refrigeration | `Boolean` | Bold — critical food safety |

---

### SECTION 10 — Pre-Rush Walk Through
**Belongs to:** Midday checklist  
**Manager's Initials — deadline: ALL must be complete by 3:30 PM**

| # | Field Label | Type | Notes |
|---|---|---|---|
| 1 | Make line stocked for dinner sales | `Boolean` | |
| 2 | Sweep entire restaurant | `Boolean` | |
| 3 | Empty all trash cans | `Boolean` | |
| 4 | Paper towels stocked | `Boolean` | |
| 5 | Wipe down kitchen tables and make line | `Boolean` | |
| 6 | Pull first dough pull | `Boolean` | |
| 7 | Prep area is swept and clean | `Boolean` | |
| 8 | Dishes done & prep table is clean | `Boolean` | |
| 9 | Blaster printer checked | `Boolean` | |
| 10 | Talk staff into position | `Boolean` | |
| 11 | Ensure staff is aware of current promotion and answer phone with promo and up sells | `Boolean` | |

---

### SECTION 11 — Deployment Guide
**Belongs to:** Evening Closing (assigned before Opening Manager leaves)  
**Manager's Initials**

| # | Field Label | Type | Sub-items |
|---|---|---|---|
| 1 | Stocking Pepsi Cooler | `Boolean` | |
| 2 | Use F.I.F.O. to rotate. FACE LABELS OUT | `Boolean` | |
| 3 | Trash and trash cans replaced with new liner | `Boolean` | |
| 4 | All dishes caught up, washed, rinsed and sanitized | `Boolean` | |
| 5 | Drain water, clean out sink, no food particles left, replace water | `Boolean` | |
| 6 | Floor drain cleaned out, free of all food particles | `Boolean` | |
| 7 | Bathroom cleaned | `Checklist` | Mirror wiped down; Sink wiped out; Toilet cleaned; Trash taken out and liner replaced; Sweep and mop floor |
| 8 | Walk-in swept and mopped | `Boolean` | |
| 9 | Floor swept and mopped including under dough stacks | `Boolean` | |
| 10 | Mop with pink floor cleaner, wet mop then dry mop entire floor | `Boolean` | |
| 11 | Empty dough trays stacked neatly in designated area | `Boolean` | Note: if stored outside, stack in 25 and cover with tray cover |
| 12 | Oven wiped down | `Checklist` | Wipe entire oven with warm soapy water; Top, sides, oven chamber, gas lines, catch trays, legs, wheels |
| 13 | Order taking monitors wiped down | `Checklist` | Wipe down monitor fronts; Wipe down back of monitors and data cable jacks |
| 14 | Phones wiped down | `Checklist` | Handset cleaned; Wipe down base and number pad |
| 15 | All undershelves cleaned | `Checklist` | Remove all boxes and supplies, wipe down; Restock all boxes and supplies for following day |
| 16 | Pre sweep of store | `Checklist` | Lobby, production area and driver dispatch area free of paper debris and dustinator |
| 17 | Staff clocked out and sent home as per MDOG / business dictates | `Boolean` | |
| 18 | Food lexans pulled from top well, cleaned, all ice/food removed from inside well | `Boolean` | |
| 19 | Bottom well emptied and thoroughly wiped out inside on bottom and inside walls | `Boolean` | |
| 20 | Exterior wiped down — all sides especially near sauce station, top and front, warm soapy water only | `Boolean` | |

---

### SECTION 12 — Closing Checklist
**Belongs to:** Evening Closing  
**Manager's Initials**

| # | Field Label | Type | Sub-items |
|---|---|---|---|
| **Lobby** | | | |
| 1 | Lobby cleaned | `Boolean` | |
| 2 | Mat swept off | `Boolean` | |
| 3 | All interior window ledges wiped down | `Boolean` | |
| 4 | Complete sweep, including corners | `Boolean` | |
| 5 | Mop entire Lobby – wet then dry mop, no water left in cracks, warm water and pink floor cleaner | `Boolean` | |
| 6 | Target trouble areas for detailing | `Checklist` | Wipe walls/counters with marks, dirt, food using soapy water; Make sure chairs/benches are clean and free of dustinator |
| 7 | Pepsi cooler wiped down – top, door tracks, interior fan, dust vent at bottom | `Boolean` | |
| **Driver Area** | | | |
| 8 | Wipe down monitors and keyboards | `Boolean` | |
| 9 | Wipe down and organize driver table and undershelves | `Boolean` | |
| 10 | Complete and detailed sweep | `Boolean` | |
| 11 | Spot clean walls with soapy water – dirt, food, dark marks or dustinator | `Boolean` | |
| **Front Counter** | | | |
| 12 | Monitors wiped down | `Boolean` | |
| 13 | Phones wiped down | `Boolean` | |
| 14 | Boxes restocked | `Boolean` | |
| 15 | Wipe down shelves and organize | `Boolean` | |
| **Slap Station** | | | |
| 16 | Wipe down counters | `Boolean` | |
| 17 | Laminates and walls wiped down | `Boolean` | |
| 18 | Dust off and wipe down shelves and cross bars under counters | `Boolean` | |
| **Sauce Station** | | | |
| 19 | Wipe down walls and sauce table | `Boolean` | |
| 20 | Wipe down wall laminates above and near table | `Boolean` | |
| 21 | Clean side of make line, removing sauce, dustinator and food debris | `Boolean` | |
| 22 | Wipe down undershelves, cross bars free of dustinator and sauce | `Boolean` | |
| **Make Line** | | | |
| 23 | Lid hinges clean and free of buildup of food and dirt | `Boolean` | |
| 24 | Handles wiped down and clean on make line lids and doors | `Boolean` | |
| **Dish Area** | | | |
| 25 | Sinks cleaned out, free of soap and food debris | `Boolean` | |
| 26 | Walls wiped down above dish sinks (and prep sinks if apply) | `Boolean` | |
| 27 | Floor drain cleaned and free of food | `Boolean` | |
| **Miscellaneous** | | | |
| 28 | Trash cans wiped down (late night trash goes out next morning for safety and security reasons) | `Boolean` | |
| 29 | Toss all food products that will expire the next day | `Boolean` | |
| 30 | Thorough and complete sweep under counters and open spaces of production, prep and dry storage area | `Boolean` | |

---

### SECTION 13 — Closing Admin. Checklist
**Belongs to:** Evening Closing  
**Manager's Initials**

| # | Field Label | Type | Notes |
|---|---|---|---|
| 1 | Count cash | `Boolean` | |
| 2 | Ensure both till A and B are at $50 or $75 each | `Boolean` | |
| 3 | Count out remaining $450 or $500 for store cash and lock in time delay safe | `Boolean` | |
| 4 | Count out final deposit, fill out deposit slip, post and lock in time delay safe, complete Deposit Log | `Boolean` | |
| 5 | Enter closing inventory in Profit System and verify against target for variances and finalize | `Boolean` | |
| 6 | Complete Nightly Numbers sheet | `Boolean` | |
| 7 | Complete Bad Order Log | `Boolean` | |
| 8 | Highlight top 5 variances on Target Inventory Cost report | `Boolean` | |
| 9 | Review Closing Checklist with closing driver staff to verify detailed completion | `Boolean` | |
| 10 | Clean and organize office | `Boolean` | |
| 11 | Till in safe, drawers left open | `Boolean` | |
| 12 | Switch out back up discs in CPU & safe | `Boolean` | |
| 13 | Verify Instant Pay posted | `Boolean` | |
| 14 | Clock out, system closed | `Boolean` | |

---

## Summary — Open Questions Before Build

| # | Question | Options |
|---|---|---|
| 1 | **MDOG Action field (A/B/C)** | `Text` field (employee types A/B/C) — or add new `Select` field type to system |
| 2 | **Cash Management placement** | Opening cash → Morning Opening; Closing cash → Evening Closing; Shift Change → Midday — or one dedicated "Cash Management" template |
| 3 | **MDOG display structure** | Keep flat fields (simpler, works today) — or group per dough size with section headers in UI |
| 4 | **Sections 9 & 10 (Midday duties)** | Keep in existing "Midday Safety & Compliance" checklist — or rename/remap to match PDF section names |

---

## Field Count Summary

| Section | Template | Fields |
|---|---|---|
| Communications | Morning Opening | 1 |
| MDOG | Morning Opening | 13 (temps) + 8×5 sizes + 3 cheese = 56 |
| Cash Management | Morning Opening / Evening Closing | 39 per sub-section × 3 = 117 |
| Opening tasks | Morning Opening | 13 |
| Set Up Cut Table | Morning Opening | 6 |
| Set Up Sauce Station | Morning Opening | 4 |
| Set Up Customer Lobby (both pages) | Morning Opening | 18 |
| Ongoing Duties During Lunch | Midday | 5 |
| Pre-Rush Walk Through | Midday | 11 |
| Deployment Guide | Evening Closing | 20 |
| Closing Checklist | Evening Closing | 30 |
| Closing Admin. Checklist | Evening Closing | 14 |
