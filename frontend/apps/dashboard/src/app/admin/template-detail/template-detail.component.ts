import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '@org/data-access-auth';
import { TemplateService } from '@org/data-access-templates';
import type { TemplateDetailDto } from '@org/data-access-templates';
import { FieldBuilderComponent, type TemplateField } from '@org/ui-field-builder';
import { noWhitespace } from '@org/ui-core';

@Component({
  selector: 'app-template-detail',
  imports: [ReactiveFormsModule, FieldBuilderComponent],
  templateUrl: './template-detail.component.html',
  styleUrl: './template-detail.component.scss',
})
export class TemplateDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly templateSvc = inject(TemplateService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly currentUser = this.auth.currentUser;
  readonly categories = ['Operations', 'Safety', 'Food Safety', 'Inventory', 'Cleaning', 'Finance', 'Other'];

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly template = signal<TemplateDetailDto | null>(null);

  readonly editing = signal(false);
  readonly saving = signal(false);
  readonly saveError = signal<string | null>(null);
  readonly saved = signal(false);
  readonly editFields = signal<TemplateField[]>([]);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, noWhitespace]],
    description: [''],
    category: ['', Validators.required],
  });

  readonly fields = computed<TemplateField[]>(() => {
    const t = this.template();
    if (!t?.fieldsJson) return [];
    try { return JSON.parse(t.fieldsJson) as TemplateField[]; }
    catch { return []; }
  });

  readonly canEdit = computed(() => {
    const t = this.template();
    const role = this.currentUser()?.role ?? '';
    if (!t) return false;
    if (t.scope === 'System') return role === 'super_admin' || role === 'admin';
    if (t.scope === 'Regional') return role === 'super_admin' || role === 'admin' || role === 'supervisor';
    return true;
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.goBack(); return; }
    this.loadTemplate(id);
  }

  private loadTemplate(id: string): void {
    this.templateSvc.getTemplate(id).subscribe({
      next: (t) => { this.template.set(t); this.loading.set(false); },
      error: () => { this.error.set('Failed to load template.'); this.loading.set(false); },
    });
  }

  openEdit(): void {
    const t = this.template();
    if (!t) return;
    this.form.patchValue({ name: t.name, description: t.description ?? '', category: t.category });
    this.editFields.set(this.fields());
    this.saveError.set(null);
    this.editing.set(true);
  }

  cancelEdit(): void {
    this.editing.set(false);
  }

  onFieldsChange(fields: TemplateField[]): void {
    this.editFields.set(fields);
  }

  onSubmit(): void {
    const t = this.template();
    if (!t || this.form.invalid || this.saving()) return;
    const { name, description, category } = this.form.getRawValue();
    this.saving.set(true);
    this.saveError.set(null);
    this.templateSvc.updateTemplate(t.id, {
      name, description, category,
      fieldsJson: JSON.stringify(this.editFields()),
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.editing.set(false);
        this.saved.set(true);
        setTimeout(() => this.saved.set(false), 2500);
        this.loadTemplate(t.id);
      },
      error: () => { this.saveError.set('Failed to save template.'); this.saving.set(false); },
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/templates/task-templates']);
  }
}
