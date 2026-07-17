import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AuthService } from '@org/data-access-auth';
import { RecurringAssignmentService, type RecurringHealthDto } from '@org/data-access-tasks';
import { StatTileComponent, StatsStripComponent } from '@org/ui-core';

@Component({
  selector: 'app-recurring-performance',
  imports: [DatePipe, StatTileComponent, StatsStripComponent],
  templateUrl: './recurring-performance.component.html',
  styleUrl: './recurring-performance.component.scss',
})
export class RecurringPerformanceComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly recurringSvc = inject(RecurringAssignmentService);

  readonly currentUser = this.auth.currentUser;
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly data = signal<RecurringHealthDto | null>(null);

  readonly eyebrow = computed(() => {
    const role = this.currentUser()?.role ?? '';
    if (role === 'super_admin') return 'Org-Wide';
    if (role === 'admin' || role === 'supervisor') return 'Your Region(s)';
    return 'Your Store';
  });

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.recurringSvc.getRecurringHealth().subscribe({
      next: (d) => { this.data.set(d); this.loading.set(false); },
      error: () => { this.error.set('Failed to load recurring assignment health.'); this.loading.set(false); },
    });
  }
}
