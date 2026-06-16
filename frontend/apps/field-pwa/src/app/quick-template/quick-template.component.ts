import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { TemplateService } from '@org/data-access-templates';
import { FieldBuilderComponent, type TemplateField } from '@org/ui-field-builder';

const MAX_FIELDS = 5;
const ALLOWED_TYPES = ['Numeric', 'Boolean', 'Text'] as const;

@Component({
  selector: 'app-quick-template',
  imports: [ReactiveFormsModule, FieldBuilderComponent],
  templateUrl: './quick-template.component.html',
  styleUrl: './quick-template.component.scss',
})
export class QuickTemplateComponent {
  private readonly templateSvc = inject(TemplateService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly fields = signal<TemplateField[]>([]);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly fieldCountWarning = signal(false);

  readonly categories = ['Operations', 'Safety', 'Food Safety', 'Inventory', 'Cleaning', 'Other'];

  readonly form = this.fb.group({
    name: ['', Validators.required],
    category: ['', Validators.required],
  });

  onFieldsChange(fields: TemplateField[]): void {
    const filtered = fields.filter((f) => ALLOWED_TYPES.includes(f.type as typeof ALLOWED_TYPES[number]));
    const capped = filtered.slice(0, MAX_FIELDS);
    this.fields.set(capped);
    this.fieldCountWarning.set(fields.length > MAX_FIELDS);
  }

  onSubmit(): void {
    if (this.form.invalid || this.saving()) return;
    const user = this.auth.currentUser();
    if (!user?.storeId) { this.error.set('No store assigned to your account.'); return; }

    const { name, category } = this.form.getRawValue();
    this.saving.set(true);
    this.templateSvc.createTemplate({
      name: name!,
      category: category!,
      scope: 'Store',
      storeId: user.storeId,
      fieldsJson: JSON.stringify(this.fields()),
    }).subscribe({
      next: () => { this.saving.set(false); this.router.navigate(['/tasks']); },
      error: () => { this.error.set('Failed to create template.'); this.saving.set(false); },
    });
  }

  cancel(): void { this.router.navigate(['/tasks']); }
}
