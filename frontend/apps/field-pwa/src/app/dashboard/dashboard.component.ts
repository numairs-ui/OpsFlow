import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { TaskService, TodayTasksDto } from '@org/data-access-tasks';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly taskSvc = inject(TaskService);

  readonly userRole = signal<string>('');
  readonly userName = signal<string>('');
  readonly storeId = signal<string>('');
  readonly summary = signal<TodayTasksDto | null>(null);
  readonly loading = signal(true);

  ngOnInit() {
    const user = this.auth.currentUser();
    if (user) {
      this.userRole.set(user.role || 'user');
      this.userName.set('User');
      if (user.storeId) {
        this.storeId.set(user.storeId);
        this.loadSummary(user.storeId);
      } else {
        this.loading.set(false);
      }
    } else {
      this.router.navigate(['/login']);
    }
  }

  loadSummary(storeId: string) {
    this.taskSvc.getTodayTasks(storeId).subscribe({
      next: (data) => {
        this.summary.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  isAdmin() {
    return this.userRole() === 'admin' || this.userRole() === 'store_manager';
  }

  getProgressPct() {
    const s = this.summary();
    if (!s) return 0;
    const total = s.taskGroups.reduce((acc: number, g: any) => acc + g.totalCount, 0);
    const done = s.taskGroups.reduce((acc: number, g: any) => acc + g.completedCount, 0);
    return total > 0 ? Math.round((done / total) * 100) : 0;
  }
}
