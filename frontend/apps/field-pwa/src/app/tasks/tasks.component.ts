import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { DashboardService, TaskService } from '@org/data-access-tasks';
import type { MyCompletionDto, TaskBoardItemDto, TaskGroupDto, TaskStatus, TodayTasksDto } from '@org/data-access-tasks';

const POLL_INTERVAL_MS = 30_000;

function statusRank(s: TaskStatus): number {
  return s === 'Overdue' ? 0 : s === 'InProgress' ? 1 : s === 'Pending' ? 2 : 3;
}

@Component({
  selector: 'app-tasks',
  imports: [DatePipe],
  templateUrl: './tasks.component.html',
  styleUrl: './tasks.component.scss',
})
export class TasksComponent implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly taskSvc = inject(TaskService);
  private readonly dashboardSvc = inject(DashboardService);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly board = signal<TodayTasksDto | null>(null);
  readonly history = signal<MyCompletionDto[]>([]);
  readonly historyOpen = signal(false);

  private pollTimer?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.load();
    this.pollTimer = setInterval(() => this.load(), POLL_INTERVAL_MS);
  }

  ngOnDestroy(): void {
    clearInterval(this.pollTimer);
  }

  load(): void {
    const storeId = this.auth.currentUser()?.storeId;
    if (!storeId) { this.error.set('No store assigned to your account.'); this.loading.set(false); return; }

    this.taskSvc.getTodayTasks(storeId).subscribe({
      next: (data) => { this.board.set(data); this.loading.set(false); this.error.set(null); },
      error: () => { this.error.set('Failed to load tasks.'); this.loading.set(false); },
    });
  }

  toggleHistory(): void {
    const open = !this.historyOpen();
    this.historyOpen.set(open);
    if (open && this.history().length === 0) {
      this.dashboardSvc.getMyCompletions(7).subscribe({
        next: (h) => this.history.set(h),
      });
    }
  }

  storeProgress(): number {
    const b = this.board();
    if (!b) return 0;
    const total = b.taskGroups.reduce((s: number, g: TaskGroupDto) => s + g.totalCount, 0);
    const done = b.taskGroups.reduce((s: number, g: TaskGroupDto) => s + g.completedCount, 0);
    return total > 0 ? Math.round((done / total) * 100) : 0;
  }

  sortedTasks(tasks: TaskBoardItemDto[]): TaskBoardItemDto[] {
    return [...tasks].sort((a, b) => statusRank(a.status) - statusRank(b.status));
  }

  progressPct(group: TaskGroupDto): number {
    return group.totalCount ? Math.round((group.completedCount / group.totalCount) * 100) : 0;
  }

  openTask(id: string): void {
    this.router.navigate(['/tasks', id]);
  }

  openKiosk(): void {
    this.router.navigate(['/kiosk']);
  }

  isManager(): boolean {
    return this.auth.currentUser()?.role === 'store_manager';
  }

  openSubmissions(): void {
    this.router.navigate(['/submissions']);
  }
}
