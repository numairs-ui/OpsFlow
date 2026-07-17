import { Component, OnDestroy, OnInit, ViewChild, ElementRef, AfterViewChecked, inject, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { TaskService } from '@org/data-access-tasks';
import type { TaskBoardItemDto, TaskGroupDto, TodayTasksDto } from '@org/data-access-tasks';

const POLL_INTERVAL_MS = 30_000;
const FINANCIAL_CHECKLIST_KEYWORDS = ['financial', 'cash', 'sales', 'revenue', 'profit'];

@Component({
  selector: 'app-kiosk',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './kiosk.component.html',
  styleUrl: './kiosk.component.scss',
})
export class KioskComponent implements OnInit, OnDestroy, AfterViewChecked {
  private readonly auth = inject(AuthService);
  private readonly taskSvc = inject(TaskService);
  private readonly router = inject(Router);

  @ViewChild('nameInput') private nameInput?: ElementRef<HTMLInputElement>;
  private nameInputFocused = false;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly board = signal<TodayTasksDto | null>(null);

  readonly selectedTask = signal<TaskBoardItemDto | null>(null);
  readonly selectedGroupName = signal('');
  readonly volunteerName = signal('');
  readonly claimStep = signal<'select' | 'name' | 'done'>('select');
  readonly claiming = signal(false);

  readonly filteredGroups = computed(() => {
    const b = this.board();
    if (!b) return [];
    return b.taskGroups.filter(g =>
      !FINANCIAL_CHECKLIST_KEYWORDS.some(kw => g.checklistName.toLowerCase().includes(kw))
    );
  });

  readonly pendingCount = computed(() =>
    this.filteredGroups().reduce((sum, g) => sum + g.tasks.filter(t => this.isAvailable(t)).length, 0)
  );

  // Shift progress for the shared station — "X of Y done today", computed from the same
  // (financial-filtered) groups the board already shows. No extra API call.
  readonly shiftDoneCount = computed(() =>
    this.filteredGroups().reduce((sum, g) => sum + g.completedCount, 0)
  );
  readonly shiftTotalCount = computed(() =>
    this.filteredGroups().reduce((sum, g) => sum + g.totalCount, 0)
  );
  readonly shiftProgressPct = computed(() => {
    const total = this.shiftTotalCount();
    return total > 0 ? Math.round((this.shiftDoneCount() / total) * 100) : 0;
  });

  private pollTimer?: ReturnType<typeof setInterval>;
  private refreshTimer?: ReturnType<typeof setInterval>;

  private readonly REFRESH_INTERVAL_MS = 13 * 60 * 1000; // refresh 2 min before token expires

  ngOnInit(): void {
    this.load();
    this.pollTimer = setInterval(() => this.load(), POLL_INTERVAL_MS);
    this.refreshTimer = setInterval(() => this.auth.refresh().then(ok => {
      if (!ok) this.router.navigate(['/login']);
    }), this.REFRESH_INTERVAL_MS);
  }

  ngOnDestroy(): void {
    clearInterval(this.pollTimer);
    clearInterval(this.refreshTimer);
  }

  ngAfterViewChecked(): void {
    if (this.claimStep() === 'name' && this.nameInput && !this.nameInputFocused) {
      this.nameInputFocused = true;
      this.nameInput.nativeElement.focus();
    } else if (this.claimStep() !== 'name') {
      this.nameInputFocused = false;
    }
  }

  load(): void {
    const storeId = this.auth.currentUser()?.storeId;
    if (!storeId) { this.error.set('No store assigned.'); this.loading.set(false); return; }

    this.taskSvc.getTodayTasks(storeId).subscribe({
      next: (data) => { this.board.set(data); this.loading.set(false); this.error.set(null); },
      error: () => { this.error.set('Failed to load tasks.'); this.loading.set(false); },
    });
  }

  selectTask(task: TaskBoardItemDto, groupName: string): void {
    if (task.status === 'Completed') return;
    this.selectedTask.set(task);
    this.selectedGroupName.set(groupName);
    this.volunteerName.set('');
    this.claimStep.set('name');
  }

  submitClaim(): void {
    const task = this.selectedTask();
    if (!task) return;

    this.claiming.set(true);
    this.taskSvc.claimTask(task.id, this.volunteerName() || undefined).subscribe({
      next: () => {
        this.taskSvc.startTask(task.id).subscribe({
          next: () => { this.claiming.set(false); this.claimStep.set('done'); this.load(); },
          error: () => { this.claiming.set(false); this.error.set('Failed to start task. Please try again.'); this.cancelClaim(); },
        });
      },
      error: () => { this.claiming.set(false); this.error.set('Failed to claim task.'); this.cancelClaim(); },
    });
  }

  cancelClaim(): void {
    this.selectedTask.set(null);
    this.selectedGroupName.set('');
    this.claimStep.set('select');
  }

  continueClaim(): void {
    this.claimStep.set('done');
  }

  resetFlow(): void {
    this.selectedTask.set(null);
    this.selectedGroupName.set('');
    this.volunteerName.set('');
    this.claimStep.set('select');
  }

  isAvailable(t: TaskBoardItemDto): boolean {
    return (t.status === 'Pending' || t.status === 'Overdue' || t.status === 'CorrectiveActionRaised') && !t.assignedToUserId;
  }

  progressPct(group: TaskGroupDto): number {
    return group.totalCount ? Math.round((group.completedCount / group.totalCount) * 100) : 0;
  }

  exitKiosk(): void { this.router.navigate(['/tasks']); }
}
