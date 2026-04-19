# WindowSill.Date — UX Specification & Implementation Plan

## Overview

A Dato-inspired calendar/meeting/timezone extension for WindowSill. Two logical sills:
1. **Date Sill** — Always visible. Shows calendar icon or date/time. Opens a popup with mini-calendar, event list, and world clocks.
2. **Next Meeting Sill(s)** — Contextual. Appear ~30min before upcoming meetings (configurable). Up to N sills shown (default 5, configurable).

---

## Sill 1: Date Sill

### Implementation
- `ISillActivatedByDefault`, `ISillListView`
- First item in `ViewList` is a `SillListViewPopupItem` that opens the main popup.

### Bar Content (configurable in settings)
- **Option A (default)**: Calendar icon (SVG, similar to 📅).
- **Option B**: Current date/time in a user-chosen format (e.g., "Sat Apr 18", "9:17 PM", "Apr 18 9:17 PM").
- User toggles between these in Settings > Display.

### Popup (SillPopupContent, ~380×550px)

#### Section 1: Mini Calendar
- **Control**: WinUI 3 `CalendarView`.
  - Compact, fits naturally in a popup.
  - Built-in month/year navigation.
  - Supports `CalendarViewDayItemChangingEventArgs` to add density bars (colored dots per calendar).
  - Supports `SelectedDatesChanged` to filter the event list.
  - Set `FirstDayOfWeek` from settings.
- **Behavior**:
  - Today is highlighted by default.
  - Days with events show colored density bars (one color per calendar source).
  - Selecting a day updates the event list below.
  - Clicking the month/year header navigates (built-in CalendarView behavior).

#### Section 2: Event List (scrollable)
- Shows events for the selected day.
- **Grouping**:
  - All-day events at the top (hidden by default; setting to show them).
  - Timed events sorted by start time.
  - Past events grayed out or collapsed.
- **Each event row**:
  - Colored left border or dot matching the calendar color.
  - Title (truncated with ellipsis if long).
  - Time range: "9:00 AM – 9:30 AM".
  - If in progress: accent highlight + "Now" badge.
  - If video call detected: small camera icon (🎥) next to title.
  - Click an event → expand inline or open detail view showing:
    - Full title, time, location, attendees, description snippet.
    - "Join" button if video call URL detected.
    - "Open in Outlook/Google" button (launch web link).
- **Empty state**: "No events for [date]." with subtle calendar illustration.
- **Deduplication**: Events with matching title + overlapping time range across calendars → show once, with multi-calendar indicator.

#### Section 3: World Clocks
- List of user-configured timezones.
- **Each row**:
  - **Day/Night icon**: ☀️ sun if local time is 6 AM–6 PM (configurable?), 🌙 moon otherwise. Use simple sunrise/sunset heuristic or fixed 6–18 range.
  - **Display name**: User-configurable label (e.g., "New York", "Tokyo Office", "Mom").
  - **Current time**: Live-updating, format matches user preference.
  - **Offset**: "+5h" or "−8h" relative to local time.
- **Time travel slider**:
  - Horizontal slider at the bottom of the world clocks section.
  - Drag to offset all displayed times simultaneously (±24h range).
  - Label above slider: "Now" or "+3h" / "−2h".
  - Snaps to 15-minute increments when holding Shift (or by default, with option for finer control).
  - Resets to "Now" on release (or tap "Reset" button).
  - All world clock times update live as slider moves.

---

## Sill 2: Next Meeting Sill(s)

### Implementation
- Additional `SillListViewItem` entries in the same DateSill's `ViewList` (simplest architecture — no separate ISill export needed).
- Each upcoming meeting within the reminder window gets its own `SillListViewMenuFlyoutItem`.
- `ShouldAppearInSill` controls dynamic visibility.

### Visibility Logic
- **Default**: Sill appears **30 minutes** before meeting start (configurable: 5 / 10 / 15 / 30 / 60 min).
- **Physical location meetings**: Sill appears **30 minutes before estimated departure time** (departure time = meeting start − travel time).
- **Maximum sills shown**: 5 (configurable: 1–10).
- **Whole-day events**: Hidden by default. Setting to include them.
- **Deduplication**: Same logic as event list.
- **Disappears**: After meeting ends (or after user hides it).

### Bar Content
- **Text format**: `"[Meeting Title] in N min"` or `"[Meeting Title] in N:SS"` (when <1 min).
- **Title trimming**: Meeting title is trimmed/ellipsized so that "in N min" is always fully visible. Measure available width, reserve space for countdown suffix, allocate remainder to title.
- **Progressive urgency**:

| Phase | Bar Text | Visual | Audio |
|-------|----------|--------|-------|
| ≥5 min before | `"Team Sync in 22 min"` | Normal style | None |
| <5 min before | `"Team Sync in 4:32"` | Accent color background | None |
| ≤30 sec before | `"Team Sync in 0:28"` | `StartFlashing()` | Optional countdown beeps (setting) |
| Start time reached | `"Team Sync is live!"` | Intense flash + optional full-screen notification | Optional chime (setting) |
| Meeting in progress | `"Team Sync • 12 min"` | Calm, elapsed timer | None |
| Meeting ended | Item removed | `ShouldAppearInSill = false` | None |

- **Physical location meetings** — additional phase:

| Phase | Bar Text | Visual |
|-------|----------|--------|
| Time to leave reached | `"Team Sync — Leave now!"` | `StartFlashing()` + notification (full-screen or toast, per settings) |

- **Join button**: When the meeting start time is reached AND a video call URL is detected, show a **"Join" button directly in the sill** (camera icon `\uE714`). Click → `Launcher.LaunchUriAsync(meetingUrl)`.

### Preview Flyout (hover tooltip)
- **Full meeting title** (no truncation).
- **Due time**: "in 22 minutes".
- **Exact date/time**: "Saturday, April 18, 2026 at 9:30 PM".

### Menu Flyout (click)
- **Full meeting title** (bold, top).
- **Time info**: "in 22 minutes — 9:30 PM – 10:00 PM, Saturday April 18".
- **Location** (if present): Address text.
- **Travel time** (if physical location): "🚗 ~25 min travel" (source: estimated via external API or user-configured default commute time for v1).
- **Separator**.
- **Actions**:
  - 🎥 **"Join Meeting"** (if video call URL detected) → launch URL.
  - 📅 **"Open in Calendar"** → launch event's `WebLink` (opens in Outlook/Google web).
  - 🔕 **"Hide This Reminder"** → remove sill item, suppress until next occurrence.
- **Separator**.
- **Calendar source**: "Outlook – Work Calendar" (small, muted text).

### Notification Options (per-user settings)
When it's meeting time (or travel-departure time for physical meetings):
- **Full-screen notification**: Reuse `FullScreenNotificationWindow` pattern from ShortTermReminder. Multi-monitor. Shows meeting title, time, Join button (if video), Dismiss button.
- **Toast notification**: Windows native toast with meeting info + Join action.
- **Sill flashing**: `StartFlashing()` on the meeting sill item.
- User can enable **any combination** of these three (checkboxes in settings, all enabled by default).

---

## Settings

### Accounts Tab (existing)
- Add/remove calendar accounts (Outlook, Google, CalDAV).
- Per-calendar toggle (show/hide specific calendars).
- Sync interval (5 / 15 / 30 / 60 min, default 15).

### Display Tab
- **Sill content**: Calendar icon (default) / Date-time.
- **Date-time format** (if date-time mode): Dropdown presets + custom format string.
  - Presets: "Apr 18", "Sat Apr 18", "9:17 PM", "Apr 18 9:17 PM", "2026-04-18".
- **Show seconds**: On/Off (default Off).
- **Calendar first day of week**: Sunday (default) / Monday / Saturday.
- **Show week numbers**: On/Off (default Off).
- **Show all-day events**: On/Off (default Off).

### Meetings Tab
- **Upcoming meeting sills**:
  - Maximum sills displayed: 1–10 (default 5).
  - Reminder window: 5 / 10 / 15 / 30 / 60 min before (default 30).
- **Notifications**:
  - Full-screen notification: On/Off (default On).
  - Toast notification: On/Off (default Off).
  - Sill flashing: On/Off (default On).
  - Sound effects: On/Off (default On).
  - Sound choice: dropdown (Windows Calendar / Chime / Beeps / None).
- **"It's time!" countdown**: On/Off (default On). Controls the dramatic countdown beep experience in the last 30 seconds.
- **Join button in sill**: On/Off (default On).
- **Physical location meetings**:
  - Default commute time: 0–120 min (default 30 min). Used when no external travel estimate is available.
  - Notify at departure time: On/Off (default On).

### World Clocks Tab
- Add/remove/reorder timezones.
- Searchable city selector (NodaTime TZDB — 15k+ cities).
- Custom display name per timezone.
- Time format: 12h / 24h / System default.

---

## Architecture Notes

### Data Flow
```
Calendar Providers (Outlook/Google/CalDAV)
        ↓ sync (periodic + on-demand)
CalendarDataStore (DPAPI-encrypted local cache)
        ↓
CalendarAccountManager → CalendarEvent[]
        ↓
DateSill (orchestrates both sill behaviors)
  ├─ ViewList[0]: Date/Calendar popup item (SillListViewPopupItem)
  ├─ ViewList[1..N]: Next meeting items (SillListViewMenuFlyoutItem)
  ↓
MeetingCountdownService (1-sec timer per active meeting)
  ├─ Updates bar text + countdown
  ├─ Triggers StartFlashing() at thresholds
  └─ Triggers notifications (full-screen / toast)
```

### Key Components to Build
1. **DateSill.cs** — Entry point, manages ViewList.
2. **DatePopupView.xaml** — Main popup with CalendarView + event list + world clocks.
3. **DatePopupViewModel.cs** — Popup state, selected day, event filtering, world clock time travel.
4. **MeetingCountdownService.cs** — Timer-driven countdown, flashing triggers, notification dispatch.
5. **MeetingSillItem.cs** — Custom SillListViewMenuFlyoutItem per meeting (text updates, flyout content).
6. **MeetingNotificationService.cs** — Full-screen + toast notification orchestration.
7. **WorldClockModel.cs** — Timezone display name, IANA zone, day/night icon logic.
8. **TravelTimeEstimator.cs** — v1: fixed commute time from settings. v2: maps API integration.
9. **VideoCallDetector.cs** — Already exists. Detects Teams/Zoom/Meet/Webex/Slack URLs.
10. **EventDeduplicator.cs** — Matches events across calendars by title + time overlap.
11. **SettingsView updates** — New tabs: Display, Meetings, World Clocks.

### Implementation Phases

#### Phase 1: Core Infrastructure
- Calendar sync working (providers already exist).
- DateSill entry point with basic ViewList.
- Settings storage for all configurable options.

#### Phase 2: Date Popup
- CalendarView integration with event density bars.
- Event list with calendar colors, filtering by selected day.
- Basic event detail (title, time, location).

#### Phase 3: World Clocks
- Timezone management (add/remove/reorder).
- Day/night icon logic.
- Time travel slider.

#### Phase 4: Next Meeting Sills
- MeetingCountdownService with 1-sec timer.
- SillListViewMenuFlyoutItem per meeting.
- Text trimming logic for bar.
- Preview flyout (hover).
- Menu flyout with actions (Join, Open in Calendar, Hide).

#### Phase 5: Notifications & Urgency
- Progressive urgency (color, flashing, sound).
- Full-screen notification (multi-monitor).
- Toast notification.
- "It's time!" dramatic countdown.
- Physical location: travel time departure alerts.

#### Phase 6: Polish
- Meeting deduplication.
- All-day event toggle.
- Settings UI for all options.
- Localization (all user-visible strings in .resw files).

---

## Out of Scope (v2.0+)
- Quick event creation (natural language).
- Date calculator.
- Focus time / DND detection.
- Hourly chime.
- Global keyboard shortcuts.
- Maps API travel time integration (v1 uses fixed commute setting).
