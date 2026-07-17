import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { OrgService } from '@org/data-access-org';
import { TaskService } from '@org/data-access-tasks';
import type { TaskInstanceDto, TaskStatsDto, TaskStatus } from '@org/data-access-tasks';
import { StatTileComponent, StatsStripComponent } from '@org/ui-core';

export type TaskFilter = 'open' | 'upcoming' | 'overdue';

const FILTER_STATUSES: Record<TaskFilter, TaskStatus[]> = {
  open: ['Pending', 'InProgress'],
  upcoming: ['Pending', 'InProgress'],
  overdue: ['Overdue', 'CorrectiveActionRaised'],
};

const FILTER_LABEL: Record<TaskFilter, string> = {
  open: 'Open Tasks',
  upcoming: 'Upcoming Tasks',
  overdue: 'Overdue Tasks',
};

const FILTER_SUBTITLE: Record<TaskFilter, string> = {
  open: 'Tasks due today, across every store you can see.',
  upcoming: 'Tasks due after today, across every store you can see.',
  overdue: 'Tasks past their due date and not yet resolved, across every store you can see.',
};

@Component({
  selector: 'app-tasks',
  imports: [RouterLink, DatePipe, StatTileComponent, StatsStripComponent],
  templateUrl: './tasks.component.html',
  styleUrl: './tasks.component.scss',
})
export class TasksComponent implements OnInit {
  private readonly taskSvc = inject(TaskService);
  private readonly orgSvc = inject(OrgService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly filter = signal<TaskFilter>('open');
  readonly tasks = signal<TaskInstanceDto[]>([]);
  readonly stats = signal<TaskStatsDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  private readonly userNames = signal<Map<string, string>>(new Map());

  readonly title = computed(() => FILTER_LABEL[this.filter()]);
  readonly subtitle = computed(() => FILTER_SUBTITLE[this.filter()]);

  // Reused as-is under both /admin and /supervisor — the store deep-link (admin-only,
  // since supervisors have no Stores page) also gates on this.
  readonly basePath = computed(() =>
    this.auth.currentUser()?.role === 'supervisor' ? '/supervisor' : '/admin'
  );

  // "Open" matches the dashboard stat card, which rolls up today's due tasks. "Overdue"
  // deliberately ignores the date window — a task is overdue precisely because its due
  // date has passed, so windowing to "today" would hide the ones that need attention most.
  private readonly todayStart = new Date();
  private readonly todayEnd = new Date();

  ngOnInit(): void {
    this.todayStart.setHours(0, 0, 0, 0);
    this.todayEnd.setHours(23, 59, 59, 999);

    this.orgSvc.getUsers({ activeOnly: false }).subscribe({
      next: (users) => this.userNames.set(new Map(users.map((u) => [u.userId, u.displayName]))),
    });

    this.taskSvc.getTaskStats().subscribe({ next: (s) => this.stats.set(s) });

    this.route.queryParams.subscribe((params) => {
      const raw = params['filter'];
      const f: TaskFilter = raw === 'overdue' || raw === 'upcoming' ? raw : 'open';
      this.filter.set(f);
      this.load(f);
    });
  }

  assigneeName(userId: string | undefined): string {
    if (!userId) return 'Unassigned';
    return this.userNames().get(userId) ?? userId;
  }

  setFilter(f: TaskFilter): void {
    this.router.navigate([], { queryParams: { filter: f }, queryParamsHandling: 'merge' });
  }

  // The row uses [routerLink] directly (no native keyboard support), and also contains
  // two nested <a> links with their own click handling — guard so Enter/Space on a nested
  // link doesn't also bubble up and double-navigate via the row's own routerLink.
  onRowKeydown(event: Event, taskId: string): void {
    if (event.target !== event.currentTarget) return;
    this.router.navigate([this.basePath(), 'tasks', taskId]);
  }

  private load(f: TaskFilter): void {
    this.loading.set(true);
    this.error.set(null);

    // "Upcoming" starts right where "Open" ends (today's window), and — like "Overdue" —
    // has no upper bound, so any future-dated one-time task is always visible somewhere.
    const from = f === 'open' ? this.todayStart.toISOString()
      : f === 'upcoming' ? new Date(this.todayEnd.getTime() + 1).toISOString()
      : undefined;
    const to = f === 'open' ? this.todayEnd.toISOString() : undefined;

    // No storeId filter — GetTasksQuery already scopes results via WhereStoreInScope,
    // so both super_admin (all stores) and region-scoped admin (their regions only) work here.
    this.taskSvc.getTasks(undefined, undefined, from, to, FILTER_STATUSES[f]).subscribe({
      next: (data) => { this.tasks.set(data); this.loading.set(false); },
      error: () => { this.error.set('Failed to load tasks.'); this.loading.set(false); },
    });
  }

  statusPillClass(status: TaskStatus): string {
    switch (status) {
      case 'Completed':
      case 'Verified':
        return 'pill--completed';
      case 'InProgress':
        return 'pill--inprogress';
      case 'Overdue':
      case 'CorrectiveActionRaised':
        return 'pill--overdue';
      case 'Cancelled':
        return 'pill--inactive';
      default:
        return 'pill--pending';
    }
  }
}
