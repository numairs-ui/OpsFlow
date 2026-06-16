import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '@org/data-access-auth';
import { DepositService } from '@org/data-access-tasks';
import type { DepositLogDto } from '@org/data-access-tasks';

@Component({
  selector: 'app-deposit',
  imports: [DatePipe, CurrencyPipe, FormsModule],
  templateUrl: './deposit.component.html',
  styleUrl: './deposit.component.scss',
})
export class DepositComponent implements OnInit {
  private readonly depositSvc = inject(DepositService);
  private readonly auth = inject(AuthService);

  readonly storeId = computed(() => this.auth.currentUser()?.storeId ?? '');
  readonly amount = signal('');
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly todayDeposit = signal<DepositLogDto | null>(null);
  readonly history = signal<DepositLogDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly loading = signal(false);
  readonly loadingHistory = signal(false);

  readonly today = new Date().toISOString().slice(0, 10);

  ngOnInit(): void {
    const sid = this.storeId();
    if (!sid) return;
    this.checkTodayDeposit(sid);
    this.loadHistory(sid);
  }

  private checkTodayDeposit(storeId: string): void {
    this.depositSvc.getDepositByDate(storeId, this.today).subscribe({
      next: (d) => this.todayDeposit.set(d),
      error: () => this.todayDeposit.set(null),
    });
  }

  private loadHistory(storeId: string, page = 1): void {
    this.loadingHistory.set(true);
    this.depositSvc.getDepositLog(storeId, { page, pageSize: 14 }).subscribe({
      next: (res) => {
        this.history.set(res.items);
        this.totalCount.set(res.totalCount);
        this.page.set(res.page);
        this.loadingHistory.set(false);
      },
      error: () => this.loadingHistory.set(false),
    });
  }

  submit(): void {
    const sid = this.storeId();
    const val = parseFloat(this.amount());
    if (!sid || isNaN(val) || val <= 0 || this.submitting()) return;

    this.submitting.set(true);
    this.error.set(null);

    this.depositSvc.recordDeposit(sid, { amount: val }).subscribe({
      next: (d) => {
        this.submitting.set(false);
        this.todayDeposit.set(d);
        this.amount.set('');
        this.loadHistory(sid);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err?.error?.detail ?? 'Failed to record deposit.');
      },
    });
  }

  prevPage(): void {
    if (this.page() <= 1) return;
    this.loadHistory(this.storeId(), this.page() - 1);
  }

  nextPage(): void {
    if (this.page() * 14 >= this.totalCount()) return;
    this.loadHistory(this.storeId(), this.page() + 1);
  }

  hasNextPage = computed(() => this.page() * 14 < this.totalCount());
}
