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
    { regionId: 'r1', regionName: 'London Central', storeCount: 4, averageCompletionRate: 0.91, criticalAlertCount: 0 },
    { regionId: 'r2', regionName: 'South London',   storeCount: 3, averageCompletionRate: 0.83, criticalAlertCount: 1 },
    { regionId: 'r3', regionName: 'East London',    storeCount: 3, averageCompletionRate: 0.76, criticalAlertCount: 2 },
    { regionId: 'r4', regionName: 'North London',   storeCount: 4, averageCompletionRate: 0.68, criticalAlertCount: 3 },
  ],
};

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

  readonly arcColor = computed(() => {
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
    this.dashboardSvc.getSystemDashboard().subscribe({
      next: (d) => { this.rawData.set(d); this.loading.set(false); },
      error: () => { this.error.set('Failed to load dashboard.'); this.loading.set(false); },
    });
  }

  scoreClass(rate: number): string {
    if (rate >= 0.8) return 'score--green';
    if (rate >= 0.6) return 'score--amber';
    return 'score--red';
  }
}
