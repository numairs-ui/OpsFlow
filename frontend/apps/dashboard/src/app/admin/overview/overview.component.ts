import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { AuthService } from '@org/data-access-auth';
import { DashboardService } from '@org/data-access-tasks';
import type { RegionDashboardDto, RegionalSummaryDto, SystemDashboardDto } from '@org/data-access-tasks';
import { OrgService } from '@org/data-access-org';
import type { Region } from '@org/data-access-org';

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
  private readonly auth = inject(AuthService);
  private readonly dashboardSvc = inject(DashboardService);
  private readonly orgSvc = inject(OrgService);
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
    const isSuperAdmin = this.auth.currentUser()?.role === 'super_admin';

    if (isSuperAdmin) {
      this.dashboardSvc.getSystemDashboard().subscribe({
        next: (d) => { this.rawData.set(d); this.loading.set(false); },
        error: () => { this.error.set('Failed to load dashboard.'); this.loading.set(false); },
      });
      return;
    }

    const regionIds = this.auth.currentUser()?.regionIds ?? [];
    if (regionIds.length === 0) {
      this.error.set('No region assigned to your account. Contact a super admin.');
      this.loading.set(false);
      return;
    }

    forkJoin({
      regions: this.orgSvc.getRegions(false),
      regionDashboards: forkJoin(
        regionIds.map((regionId) =>
          this.dashboardSvc.getRegionDashboard(regionId).pipe(
            map((d) => ({ regionId, dashboard: d })),
            catchError(() => of({ regionId, dashboard: null as RegionDashboardDto | null }))
          )
        )
      ),
    }).subscribe({
      next: ({ regions, regionDashboards }) => {
        this.rawData.set(this.toSystemDashboard(regions, regionDashboards));
        this.loading.set(false);
      },
      error: () => { this.error.set('Failed to load dashboard.'); this.loading.set(false); },
    });
  }

  private toSystemDashboard(
    regions: Region[],
    regionDashboards: { regionId: string; dashboard: RegionDashboardDto | null }[]
  ): SystemDashboardDto {
    const regionName = (regionId: string) =>
      regions.find((r) => r.id === regionId)?.name ?? 'Unknown Region';

    const regionalSummary: RegionalSummaryDto[] = regionDashboards.map(({ regionId, dashboard }) => {
      const stores = dashboard?.stores ?? [];
      const averageCompletionRate = stores.length
        ? stores.reduce((sum, s) => sum + s.completionRate, 0) / stores.length
        : 0;
      const criticalAlertCount = stores.filter(
        (s) => !s.depositLoggedToday || s.correctiveActionCount > 0
      ).length;
      return {
        regionId,
        regionName: regionName(regionId),
        storeCount: stores.length,
        averageCompletionRate,
        criticalAlertCount,
      };
    });

    const allStores = regionDashboards.flatMap(({ dashboard }) => dashboard?.stores ?? []);
    const systemCompletionRate = allStores.length
      ? allStores.reduce((sum, s) => sum + s.completionRate, 0) / allStores.length
      : 0;

    return {
      systemCompletionRate,
      totalOpenCount: allStores.reduce((sum, s) => sum + s.openCount, 0),
      totalOverdueCount: allStores.reduce((sum, s) => sum + s.overdueCount, 0),
      storesWithMissedDeposits: allStores
        .filter((s) => !s.depositLoggedToday)
        .map((s) => ({ storeId: s.storeId, storeName: s.name })),
      regionalSummary,
    };
  }

  scoreClass(rate: number): string {
    if (rate >= 0.8) return 'score--green';
    if (rate >= 0.6) return 'score--amber';
    return 'score--red';
  }
}
