import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { OrgService, type Region, type Store } from '@org/data-access-org';
import {
  TemplateService,
  type TemplateDetailDto,
  type TemplateDto,
  type TemplateScope,
} from '@org/data-access-templates';
import { FieldBuilderComponent, type TemplateField } from '@org/ui-field-builder';

@Component({
  selector: 'app-templates',
  imports: [ReactiveFormsModule, FieldBuilderComponent],
  templateUrl: './templates.component.html',
  styleUrl: './templates.component.scss',
})
export class TemplatesComponent implements OnInit {
  private readonly templateSvc = inject(TemplateService);
  private readonly orgSvc = inject(OrgService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  readonly templates = signal<TemplateDto[]>([]);
  readonly regions = signal<Region[]>([]);
  readonly stores = signal<Store[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly showForm = signal(false);
  readonly editingTemplate = signal<TemplateDetailDto | null>(null);
  readonly fields = signal<TemplateField[]>([]);

  // When true, scope is locked to System (System Templates view)
  readonly systemOnly = signal(false);

  // Filters
  readonly filterScope = signal<string>('');
  readonly filterActive = signal<boolean | undefined>(true);
  readonly filterSearch = signal('');

  readonly currentUser = this.auth.currentUser;

  readonly form = this.fb.group({
    name: ['', Validators.required],
    description: [''],
    category: ['', Validators.required],
    scope: ['System' as TemplateScope, Validators.required],
    regionId: [''],
    storeId: [''],
  });

  readonly selectedScope = signal<string>('System');

  readonly categories = ['Operations', 'Safety', 'Food Safety', 'Inventory', 'Cleaning', 'Finance', 'Other'];
  readonly scopes: TemplateScope[] = ['System', 'Regional', 'Store'];

  ngOnInit(): void {
    const isSystemOnly = !!this.route.snapshot.data['systemOnly'];
    this.systemOnly.set(isSystemOnly);
    if (isSystemOnly) {
      this.filterScope.set('System');
      this.form.controls['scope'].setValue('System');
      this.form.controls['scope'].disable();
    }
    this.load();
    this.orgSvc.getRegions(true).subscribe({ next: (r) => this.regions.set(r) });
    this.orgSvc.getStores(undefined, true).subscribe({ next: (s) => this.stores.set(s) });
    this.form.controls['scope'].valueChanges.subscribe((v) => this.selectedScope.set(v ?? 'System'));
  }

  private load(): void {
    this.loading.set(true);
    this.templateSvc.getTemplates({
      scope: (this.filterScope() as TemplateScope) || undefined,
      isActive: this.filterActive(),
      search: this.filterSearch() || undefined,
    }).subscribe({
      next: (r) => { this.templates.set(r.items); this.loading.set(false); },
      error: () => { this.error.set('Failed to load templates.'); this.loading.set(false); },
    });
  }

  applyFilters(): void { this.load(); }

  openCreate(): void {
    this.editingTemplate.set(null);
    this.fields.set([]);
    this.form.reset({ scope: 'System' });
    this.selectedScope.set('System');
    this.showForm.set(true);
  }

  openEdit(t: TemplateDto): void {
    this.templateSvc.getTemplate(t.id).subscribe({
      next: (detail) => {
        this.editingTemplate.set(detail);
        this.form.patchValue({
          name: detail.name,
          description: detail.description ?? '',
          category: detail.category,
          scope: detail.scope,
          regionId: detail.regionId ?? '',
          storeId: detail.storeId ?? '',
        });
        this.selectedScope.set(detail.scope);
        try { this.fields.set(JSON.parse(detail.fieldsJson)); } catch { this.fields.set([]); }
        this.showForm.set(true);
      },
    });
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editingTemplate.set(null);
    this.fields.set([]);
  }

  onFieldsChange(fields: TemplateField[]): void {
    this.fields.set(fields);
  }

  onSubmit(): void {
    if (this.form.invalid || this.saving()) return;
    const { name, description, category, scope, regionId, storeId } = this.form.getRawValue();
    const fieldsJson = JSON.stringify(this.fields());
    this.saving.set(true);
    const editing = this.editingTemplate();

    if (editing) {
      this.templateSvc.updateTemplate(editing.id, {
        name: name!, description: description ?? undefined, category: category!, fieldsJson,
      }).subscribe({
        next: () => { this.saving.set(false); this.closeForm(); this.load(); },
        error: () => { this.error.set('Failed to save template.'); this.saving.set(false); },
      });
    } else {
      this.templateSvc.createTemplate({
        name: name!, description: description ?? undefined, category: category!,
        scope: scope as TemplateScope,
        regionId: regionId || undefined,
        storeId: storeId || undefined,
        fieldsJson,
      }).subscribe({
        next: () => { this.saving.set(false); this.closeForm(); this.load(); },
        error: () => { this.error.set('Failed to save template.'); this.saving.set(false); },
      });
    }
  }

  toggleActive(t: TemplateDto): void {
    const action = t.isActive
      ? (confirm(`Deactivate template "${t.name}"?`) ? this.templateSvc.deactivateTemplate(t.id) : null)
      : this.templateSvc.activateTemplate(t.id);
    action?.subscribe({ next: () => this.load() });
  }

  canEditScope(scope: string): boolean {
    const role = this.currentUser()?.role ?? '';
    if (scope === 'System') return role === 'admin';
    if (scope === 'Regional') return role === 'admin' || role === 'supervisor';
    return true;
  }

  allowedScopes(): TemplateScope[] {
    const role = this.currentUser()?.role ?? '';
    if (role === 'admin') return ['System', 'Regional', 'Store'];
    if (role === 'supervisor') return ['Regional', 'Store'];
    return ['Store'];
  }
}
