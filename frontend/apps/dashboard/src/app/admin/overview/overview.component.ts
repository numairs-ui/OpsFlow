import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { PercentPipe } from '@angular/common';
import { DashboardService } from '@org/data-access-tasks';
import type { SystemDashboardDto } from '@org/data-access-tasks';

@Component({
  selector: 'app-admin-overview',
  imports: [PercentPipe],
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.scss',
})
export class AdminOverviewComponent implements OnInit, OnDestroy {
  private readonly dashboardSvc = inject(DashboardService);
  private refreshInterval?: ReturnType<typeof setInterval>;

  readonly data = signal<SystemDashboardDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
    this.refreshInterval = setInterval(() => this.load(), 60_000);
  }

  ngOnDestroy(): void {
    clearInterval(this.refreshInterval);
  }

  private load(): void {
    this.dashboardSvc.getSystemDashboard().subscribe({
      next: (d) => { this.data.set(d); this.loading.set(false); },
      error: () => { this.error.set('Failed to load system dashboard.'); this.loading.set(false); },
    });
  }

  scoreColor(rate: number): string {
    if (rate >= 0.8) return 'score--green';
    if (rate >= 0.6) return 'score--amber';
    return 'score--red';
  }
}
