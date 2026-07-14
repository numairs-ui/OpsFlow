import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DashboardService } from '@org/data-access-tasks';
import type { RegionalSummaryDto, SystemDashboardDto } from '@org/data-access-tasks';

const CIRCUMFERENCE = 2 * Math.PI * 78; // 490.09
const TRACK_LENGTH  = CIRCUMFERENCE * (270 / 360); // 367.57

// Shown when the API returns empty/unseeded data
const DEMO: SystemDashboardDto = {
  systemCompletionRate: 0.87,
  totalOpenCount: 124,
  totalOverdueCount: 7,
  storesWithMissedDeposits: [],
  regionalSummary: [
    { regionId: 'r1', regionName: 'London Central', storeCount: 4, averageCompletionRate: 0.91, criticalAlertCount: 0, stores: [] },
    { regionId: 'r2', regionName: 'South London',   storeCount: 3, averageCompletionRate: 0.83, criticalAlertCount: 1, stores: [] },
    { regionId: 'r3', regionName: 'East London',    storeCount: 3, averageCompletionRate: 0.76, criticalAlertCount: 2, stores: [] },
    { regionId: 'r4', regionName: 'North London',   storeCount: 4, averageCompletionRate: 0.68, criticalAlertCount: 3, stores: [] },
  ],
};

// Below this fraction of the day elapsed, a low completion rate just means "not due yet" —
// judging it green/amber/red this early would read as a false alarm. Local time is used
// since due times are set by store staff working in their own timezone.
const DAY_MOSTLY_OVER_HOUR = 18;

@Component({
  selector: 'app-admin-overview',
  imports: [RouterLink],
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.scss',
})
export class AdminOverviewComponent implements OnInit, OnDestroy {
  private readonly dashboardSvc = inject(DashboardService);
  private refreshInterval?: ReturnType<typeof setInterval>;

  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);
  private readonly rawData = signal<SystemDashboardDto | null>(null);

  readonly data = computed<SystemDashboardDto | null>(() => {
    const d = this.rawData();
    if (!d) return null;
    const isEmpty = d.totalOpenCount === 0 && d.regionalSummary.length === 0;
    return isEmpty ? DEMO : d;
  });

  readonly isDemo = computed(() => {
    const d = this.rawData();
    return !!d && d.totalOpenCount === 0 && d.regionalSummary.length === 0;
  });

  // SVG arc gauge — 270° track, starts at 7:30, ends at 4:30
  readonly arcDasharray = computed(() => {
    const rate = this.data()?.systemCompletionRate ?? 0;
    const fill = Math.min(rate, 1) * TRACK_LENGTH;
    return `${fill} ${CIRCUMFERENCE - fill}`;
  });

  // Whether it's late enough in the day that a low completion rate reflects genuine
  // under-performance rather than "the day isn't over yet."
  readonly dayMostlyOver = computed(() => new Date().getHours() >= DAY_MOSTLY_OVER_HOUR);

  readonly arcColor = computed(() => {
    if (!this.dayMostlyOver()) return 'var(--indigo)';
    const rate = this.data()?.systemCompletionRate ?? 0;
    if (rate >= 0.8) return 'var(--green)';
    if (rate >= 0.6) return 'var(--amber-deep)';
    return 'var(--rust)';
  });

  readonly pct = computed(() =>
    Math.round((this.data()?.systemCompletionRate ?? 0) * 100)
  );

  readonly totalStores = computed(() =>
    this.data()?.regionalSummary.reduce((s, r) => s + r.storeCount, 0) ?? 0
  );

  readonly storesOnTarget = computed(() =>
    this.data()?.regionalSummary.filter(r => r.averageCompletionRate >= 0.8).length ?? 0
  );

  readonly sortedRegions = computed(() => {
    const d = this.data();
    if (!d) return [];
    return [...d.regionalSummary].sort((a, b) => b.averageCompletionRate - a.averageCompletionRate);
  });

  readonly selectedRegion = signal<RegionalSummaryDto | null>(null);

  openRegionDetail(region: RegionalSummaryDto): void {
    this.selectedRegion.set(region);
  }

  closeRegionDetail(): void {
    this.selectedRegion.set(null);
  }

  ngOnInit(): void {
    this.load();
    this.refreshInterval = setInterval(() => this.load(), 60_000);
  }

  ngOnDestroy(): void {
    clearInterval(this.refreshInterval);
  }

  private load(): void {
    // super_admin gets the full network; an admin/supervisor gets the same rollup narrowed to
    // their region set — the backend now scopes the response, so the client no longer re-aggregates.
    this.dashboardSvc.getSystemDashboard().subscribe({
      next: (d) => { this.rawData.set(d); this.loading.set(false); },
      error: () => { this.error.set('Failed to load dashboard.'); this.loading.set(false); },
    });
  }

  scoreClass(rate: number): string {
    if (!this.dayMostlyOver()) return 'score--neutral';
    if (rate >= 0.8) return 'score--green';
    if (rate >= 0.6) return 'score--amber';
    return 'score--red';
  }

  barClass(rate: number): string {
    if (!this.dayMostlyOver()) return 'lb-bar--neutral';
    if (rate >= 0.8) return 'lb-bar--green';
    if (rate >= 0.6) return 'lb-bar--amber';
    return 'lb-bar--red';
  }
}
