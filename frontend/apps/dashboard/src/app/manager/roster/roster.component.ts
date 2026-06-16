import { SlicePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { AuthService } from '@org/data-access-auth';
import { OrgService, type Store, type StoreEmployee } from '@org/data-access-org';

@Component({
  selector: 'app-roster',
  imports: [SlicePipe],
  templateUrl: './roster.component.html',
  styleUrl: './roster.component.scss',
})
export class RosterComponent implements OnInit {
  private readonly org = inject(OrgService);
  private readonly auth = inject(AuthService);

  readonly stores = signal<Store[]>([]);
  readonly employees = signal<StoreEmployee[]>([]);
  readonly selectedStoreId = signal<string>('');
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const user = this.auth.currentUser();
    if (!user) return;

    if (user.role === 'admin' || user.role === 'supervisor') {
      // Load all stores so they can pick one
      this.org.getStores(undefined, true).subscribe({
        next: (s) => {
          this.stores.set(s);
          if (s.length > 0) this.loadRoster(s[0].id);
        },
      });
    } else if (user.storeId) {
      // Store manager goes directly to their primary store
      this.loadRoster(user.storeId);
    }
  }

  selectStore(storeId: string): void {
    this.selectedStoreId.set(storeId);
    this.loadRoster(storeId);
  }

  private loadRoster(storeId: string): void {
    this.selectedStoreId.set(storeId);
    this.loading.set(true);
    this.error.set(null);
    this.org.getStoreEmployees(storeId).subscribe({
      next: (e) => { this.employees.set(e); this.loading.set(false); },
      error: () => { this.error.set('Failed to load roster.'); this.loading.set(false); },
    });
  }

  selectedStoreName(): string {
    return this.stores().find((s) => s.id === this.selectedStoreId())?.name ?? '';
  }
}
