import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { ChecklistService, type ChecklistPerformanceDto } from '@org/data-access-templates';
import { StatTileComponent, StatsStripComponent } from '@org/ui-core';

type Tier = 'network' | 'region' | 'store';

@Component({
  selector: 'app-checklist-completions',
  imports: [DatePipe, DecimalPipe, RouterLink, StatTileComponent, StatsStripComponent],
  templateUrl: './checklist-completions.component.html',
  styleUrl: './checklist-completions.component.scss',
})
export class ChecklistCompletionsComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly checklistSvc = inject(ChecklistService);

  readonly currentUser = this.auth.currentUser;
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly data = signal<ChecklistPerformanceDto | null>(null);

  readonly tier = computed<Tier>(() => {
    const role = this.currentUser()?.role ?? '';
    if (role === 'super_admin') return 'network';
    if (role === 'admin' || role === 'supervisor') return 'region';
    return 'store';
  });

  readonly eyebrow = computed(() => {
    switch (this.tier()) {
      case 'network': return 'Org-Wide';
      case 'region': return 'Your Region(s)';
      default: return 'Your Store';
    }
  });

  // Checklist definitions are only manageable from the admin shell today —
  // supervisor/store_manager have no route to that CRUD page.
  readonly createLink = computed(() => {
    const role = this.currentUser()?.role ?? '';
    return role === 'super_admin' || role === 'admin' ? '/admin/templates/checklists' : null;
  });

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.checklistSvc.getChecklistPerformance(30).subscribe({
      next: (d) => { this.data.set(d); this.loading.set(false); },
      error: () => { this.error.set('Failed to load checklist performance.'); this.loading.set(false); },
    });
  }

  scoreTone(score: number): 'ok' | 'warn' | 'danger' {
    if (score >= 85) return 'ok';
    if (score >= 70) return 'warn';
    return 'danger';
  }
}
