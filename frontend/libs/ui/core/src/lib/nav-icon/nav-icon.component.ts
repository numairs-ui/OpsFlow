import { Component, input } from '@angular/core';

export type NavIconName =
  | 'overview'
  | 'stores'
  | 'users'
  | 'templates'
  | 'more'
  | 'roster'
  | 'deposit'
  | 'submissions'
  | 'tasks'
  | 'checklists'
  | 'recurring';

/**
 * Minimal inline-SVG icon set for the mobile bottom tab bars. Scoped to just the
 * names the nav needs today — not a general icon system. Thin-line style to match
 * the design system's "schematic" voice (mono labels, hairline rules).
 */
@Component({
  selector: 'app-nav-icon',
  template: `
    @switch (name()) {
      @case ('overview') {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
          <rect x="3.5" y="3.5" width="7" height="7" rx="1.2" />
          <rect x="13.5" y="3.5" width="7" height="7" rx="1.2" />
          <rect x="3.5" y="13.5" width="7" height="7" rx="1.2" />
          <rect x="13.5" y="13.5" width="7" height="7" rx="1.2" />
        </svg>
      }
      @case ('stores') {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
          <path d="M4 9.5 5 4h14l1 5.5" stroke-linecap="round" stroke-linejoin="round" />
          <path d="M4 9.5a2.5 2.5 0 0 0 5 0 2.5 2.5 0 0 0 5 0 2.5 2.5 0 0 0 5 0" stroke-linecap="round" stroke-linejoin="round" />
          <path d="M5.5 11v8.5h13V11" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      }
      @case ('users') {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
          <circle cx="9" cy="8" r="3" />
          <path d="M3.5 19.5c0-3 2.5-5 5.5-5s5.5 2 5.5 5" stroke-linecap="round" />
          <circle cx="17" cy="8.5" r="2.3" />
          <path d="M15.5 11.5c2.4.2 4 1.9 4 4.3" stroke-linecap="round" />
        </svg>
      }
      @case ('templates') {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
          <rect x="4.5" y="3.5" width="15" height="17" rx="1.4" />
          <path d="M8 8h8M8 12h8M8 16h5" stroke-linecap="round" />
        </svg>
      }
      @case ('more') {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
          <circle cx="5.5" cy="12" r="1.4" fill="currentColor" stroke="none" />
          <circle cx="12" cy="12" r="1.4" fill="currentColor" stroke="none" />
          <circle cx="18.5" cy="12" r="1.4" fill="currentColor" stroke="none" />
        </svg>
      }
      @case ('roster') {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
          <path d="M5 4.5h14v15H5z" stroke-linejoin="round" />
          <path d="M8 9h8M8 13h8M8 17h5" stroke-linecap="round" />
        </svg>
      }
      @case ('deposit') {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
          <path d="M4 10 12 4l8 6" stroke-linecap="round" stroke-linejoin="round" />
          <path d="M5.5 10v9.5h13V10" stroke-linecap="round" stroke-linejoin="round" />
          <path d="M10 19.5v-5h4v5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      }
      @case ('submissions') {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
          <path d="M7 3.5h7l4 4v13H7z" stroke-linejoin="round" />
          <path d="M14 3.5V8h4" stroke-linejoin="round" />
          <path d="M9.5 12.5l2 2 3-3.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      }
      @case ('tasks') {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
          <rect x="4.5" y="4.5" width="15" height="15" rx="2" />
          <path d="M8.5 12l2 2 4.5-4.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      }
      @case ('checklists') {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
          <path d="M7 3.5h10a1 1 0 0 1 1 1v15a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1v-15a1 1 0 0 1 1-1Z" stroke-linejoin="round" />
          <path d="M9 2.5h6v2H9z" stroke-linejoin="round" />
          <path d="M8.5 10.5l1.3 1.3L12.5 9M8.5 15.5l1.3 1.3L12.5 14" stroke-linecap="round" stroke-linejoin="round" />
          <path d="M14.5 10h2M14.5 15h2" stroke-linecap="round" />
        </svg>
      }
      @case ('recurring') {
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
          <path d="M4.5 12a7.5 7.5 0 0 1 12.6-5.5" stroke-linecap="round" />
          <path d="M19.5 12a7.5 7.5 0 0 1-12.6 5.5" stroke-linecap="round" />
          <path d="M17.5 3.5v3.3h-3.3" stroke-linecap="round" stroke-linejoin="round" />
          <path d="M6.5 20.5v-3.3h3.3" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      }
    }
  `,
  styles: `
    :host { display: inline-flex; width: 1.3rem; height: 1.3rem; }
    svg { width: 100%; height: 100%; }
  `,
})
export class NavIconComponent {
  readonly name = input.required<NavIconName>();
}
