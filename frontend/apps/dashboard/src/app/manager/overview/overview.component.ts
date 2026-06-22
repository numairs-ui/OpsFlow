import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, CurrencyPipe, PercentPipe } from '@angular/common';
import { AuthService } from '@org/data-access-auth';
import { DashboardService } from '@org/data-access-tasks';
import type { StoreDashboardDto } from '@org/data-access-tasks';

@Component({
  selector: 'app-manager-overview',
  imports: [DatePipe, CurrencyPipe, PercentPipe],
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.scss',
})
export class ManagerOverviewComponent implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly dashboardSvc = inject(DashboardService);

  readonly storeId = computed(() => this.auth.currentUser()?.storeId ?? '');
  readonly data = signal<StoreDashboardDto | null>(null);
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
    const sid = this.storeId();
    if (!sid) return;
    this.dashboardSvc.getStoreDashboard(sid).subscribe({
      next: (d) => { this.data.set(d); this.loading.set(false); this.lastUpdated.set(new Date()); },
      error: () => { this.error.set('Failed to load dashboard.'); this.loading.set(false); },
    });
  }

  ringDasharray(rate: number): string {
    const pct = Math.max(0, Math.min(1, rate));
    const circ = 2 * Math.PI * 40;
    return `${(pct * circ).toFixed(1)} ${circ.toFixed(1)}`;
  }

  scoreColor(rate: number): string {
    if (rate >= 0.8) return '#16a34a';
    if (rate >= 0.6) return '#d97706';
    return '#dc2626';
  }

  statusLabel(status: string): string {
    const map: Record<string, string> = {
      'Pending': 'Pending', 'InProgress': 'In Progress', 'Completed': 'Done',
      'Verified': 'Verified', 'Overdue': 'Overdue',
      'CorrectiveActionRaised': 'Action Raised',
      'Cancelled': 'Cancelled', 'Deferred': 'Deferred',
    };
    return map[status] ?? status;
  }

  elapsedLabel(minutes: number): string {
    if (minutes < 60) return `${minutes}m ago`;
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return m > 0 ? `${h}h ${m}m ago` : `${h}h ago`;
  }

}
