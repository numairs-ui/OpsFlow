import { SlicePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrgService, type Region } from '@org/data-access-org';
import { noWhitespace } from '@org/ui-core';

@Component({
  selector: 'app-regions',
  imports: [ReactiveFormsModule, SlicePipe],
  templateUrl: './regions.component.html',
  styleUrl: './regions.component.scss',
})
export class RegionsComponent implements OnInit {
  private readonly org = inject(OrgService);
  private readonly fb = inject(FormBuilder);

  readonly regions = signal<Region[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly saved = signal(false);
  readonly showForm = signal(false);
  readonly editingRegion = signal<Region | null>(null);

  readonly form = this.fb.group({
    name: ['', [Validators.required, noWhitespace]],
    description: [''],
  });

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.org.getRegions(false).subscribe({
      next: (data) => { this.regions.set(data); this.loading.set(false); },
      error: () => { this.error.set('Failed to load regions.'); this.loading.set(false); },
    });
  }

  openCreate(): void {
    this.editingRegion.set(null);
    this.form.reset();
    this.showForm.set(true);
  }

  openEdit(region: Region): void {
    this.editingRegion.set(region);
    this.form.patchValue({ name: region.name, description: region.description ?? '' });
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editingRegion.set(null);
    this.form.reset();
  }

  onSubmit(): void {
    if (this.form.invalid || this.saving()) return;
    const { name, description } = this.form.getRawValue();
    const body = { name: name!, description: description ?? undefined };
    const onSuccess = () => { this.saving.set(false); this.saved.set(true); setTimeout(() => this.saved.set(false), 2500); this.closeForm(); this.load(); };
    const onError = () => { this.error.set('Failed to save region.'); this.saving.set(false); };
    this.saving.set(true);
    const editing = this.editingRegion();
    if (editing) {
      this.org.updateRegion(editing.id, body).subscribe({ next: onSuccess, error: onError });
    } else {
      this.org.createRegion(body).subscribe({ next: onSuccess, error: onError });
    }
  }

  deactivate(region: Region): void {
    if (!confirm(`Deactivate region "${region.name}"?`)) return;
    this.org.deactivateRegion(region.id).subscribe({ next: () => this.load() });
  }
}
