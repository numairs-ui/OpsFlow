import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { PercentPipe } from '@angular/common';
import { AuthService } from '@org/data-access-auth';
import { DashboardService } from '@org/data-access-tasks';
import type { RegionDashboardDto, StoreScoreDto } from '@org/data-access-tasks';

@Component({
  selector: 'app-supervisor-overview',
  imports: [PercentPipe],
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.scss',
})
export class SupervisorOverviewComponent implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly dashboardSvc = inject(DashboardService);

  readonly regionId = computed(() => this.auth.currentUser()?.regionId ?? '');
  readonly data = signal<RegionDashboardDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  private pollTimer?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.load();
    this.pollTimer = setInterval(() => this.load(), 60_000);
  }

  ngOnDestroy(): void { clearInterval(this.pollTimer); }

  private load(): void {
    const rid = this.regionId();
    if (!rid) return;
    this.dashboardSvc.getRegionDashboard(rid).subscribe({
      next: (d) => { this.data.set(d); this.loading.set(false); },
      error: () => { this.error.set('Failed to load regional dashboard.'); this.loading.set(false); },
    });
  }

  scoreColor(score: number): string {
    if (score >= 80) return 'score--green';
    if (score >= 60) return 'score--amber';
    return 'score--red';
  }

  missedDeposit(store: StoreScoreDto): boolean { return !store.depositLoggedToday; }

  criticalAlerts(stores: StoreScoreDto[]): StoreScoreDto[] {
    return stores.filter(s => !s.depositLoggedToday || s.correctiveActionCount > 0);
  }
}
