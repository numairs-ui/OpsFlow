import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, PercentPipe } from '@angular/common';
import { forkJoin } from 'rxjs';
import { AuthService } from '@org/data-access-auth';
import { DashboardService } from '@org/data-access-tasks';
import type { RegionDashboardDto, StoreScoreDto } from '@org/data-access-tasks';

@Component({
  selector: 'app-supervisor-overview',
  imports: [DatePipe, PercentPipe],
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.scss',
})
export class SupervisorOverviewComponent implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly dashboardSvc = inject(DashboardService);

  // A supervisor can be assigned to more than one region (region scope is a set) — fetch and
  // merge every assigned region's dashboard rather than just the first (`regionId` back-compat field).
  readonly regionIds = computed(() => this.auth.currentUser()?.regionIds ?? []);
  readonly data = signal<RegionDashboardDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly lastUpdated = signal<Date | null>(null);

  private pollTimer?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.load();
    this.pollTimer = setInterval(() => this.load(), 60_000);
  }

  ngOnDestroy(): void { clearInterval(this.pollTimer); }

  private load(): void {
    const rids = this.regionIds();
    if (rids.length === 0) {
      this.loading.set(false);
      this.error.set('No region assigned to your account. Contact an administrator.');
      return;
    }
    forkJoin(rids.map((id) => this.dashboardSvc.getRegionDashboard(id))).subscribe({
      next: (results) => {
        const stores = results.flatMap((r) => r.stores).sort((a, b) => b.compositeScore - a.compositeScore);
        this.data.set({ stores });
        this.loading.set(false);
        this.lastUpdated.set(new Date());
      },
      error: () => { this.error.set('Failed to load regional dashboard.'); this.loading.set(false); },
    });
  }

  scoreColor(score: number): string {
    if (score >= 80) return 'score--green';
    if (score >= 60) return 'score--amber';
    return 'score--red';
  }

  scoreTooltip(store: StoreScoreDto): string {
    return [
      `Composite score: ${store.compositeScore}/100`,
      `Completion rate: ${store.completionRate}%`,
      `Corrective actions: ${store.correctiveActionCount}`,
      store.depositLoggedToday ? 'Deposit: ✓ logged' : 'Deposit: ✗ not logged',
    ].join(' | ');
  }

  missedDeposit(store: StoreScoreDto): boolean { return !store.depositLoggedToday; }

  criticalAlerts(stores: StoreScoreDto[]): StoreScoreDto[] {
    return stores.filter(s => !s.depositLoggedToday || s.correctiveActionCount > 0);
  }
}
