import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { OrgService, type Region, type Store } from '@org/data-access-org';
import { noWhitespace } from '@org/ui-core';
import {
  FormTemplateService,
  type ApprovalStep,
  type FormTemplateDetailDto,
  type FormTemplateDto,
  type PropagationType,
  type TemplateScope,
} from '@org/data-access-templates';
import { FieldBuilderComponent, type TemplateField } from '@org/ui-field-builder';
import { ApprovalStepsBuilderComponent, PropagationTypePickerComponent } from '@org/ui-template-builder';

@Component({
  selector: 'app-form-templates',
  imports: [ReactiveFormsModule, FieldBuilderComponent, PropagationTypePickerComponent, ApprovalStepsBuilderComponent, RouterLink],
  templateUrl: './form-templates.component.html',
  styleUrl: './form-templates.component.scss',
})
export class FormTemplatesComponent implements OnInit {
  private readonly templateSvc = inject(FormTemplateService);
  private readonly orgSvc = inject(OrgService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly templates = signal<FormTemplateDto[]>([]);
  readonly totalCount = signal(0);
  readonly loadingMore = signal(false);
  private readonly pageSize = 20;
  private page = 1;
  readonly hasMore = computed(() => this.templates().length < this.totalCount());
  readonly regions = signal<Region[]>([]);
  readonly stores = signal<Store[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly saved = signal(false);
  readonly showForm = signal(false);
  readonly editingTemplate = signal<FormTemplateDetailDto | null>(null);
  readonly fields = signal<TemplateField[]>([]);
  readonly propagationType = signal<PropagationType>('Sequential');
  readonly approvalSteps = signal<ApprovalStep[]>([{ role: 'store_manager', order: 1 }]);
  readonly approvalStepsError = signal<string | null>(null);

  readonly filterScope = signal<string>('');
  readonly filterActive = signal<boolean | undefined>(true);
  readonly filterSearch = signal('');

  readonly currentUser = this.auth.currentUser;

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, noWhitespace]],
    description: [''],
    scope: ['System' as TemplateScope, Validators.required],
    regionId: [''],
    storeId: [''],
  });

  readonly selectedScope = signal<string>('System');
  readonly scopes: TemplateScope[] = ['System', 'Regional', 'Store'];

  ngOnInit(): void {
    this.load();
    this.orgSvc.getRegions(true).subscribe({ next: (r) => this.regions.set(r) });
    this.orgSvc.getStores(undefined, true).subscribe({ next: (s) => this.stores.set(s) });
    this.form.controls['scope'].valueChanges.subscribe((v) => this.selectedScope.set(v ?? 'System'));
  }

  private load(): void {
    this.page = 1;
    this.loading.set(true);
    this.templateSvc.getFormTemplates({ ...this.currentFilter(), page: 1, pageSize: this.pageSize }).subscribe({
      next: (r) => { this.templates.set(r.items); this.totalCount.set(r.totalCount); this.loading.set(false); },
      error: () => { this.error.set('Failed to load form templates.'); this.loading.set(false); },
    });
  }

  loadMore(): void {
    if (this.loadingMore() || !this.hasMore()) return;
    const nextPage = this.page + 1;
    this.loadingMore.set(true);
    this.templateSvc.getFormTemplates({ ...this.currentFilter(), page: nextPage, pageSize: this.pageSize }).subscribe({
      next: (r) => {
        this.templates.update((list) => [...list, ...r.items]);
        this.totalCount.set(r.totalCount);
        this.page = nextPage;
        this.loadingMore.set(false);
      },
      error: () => { this.error.set('Failed to load more form templates.'); this.loadingMore.set(false); },
    });
  }

  private currentFilter() {
    return {
      scope: (this.filterScope() as TemplateScope) || undefined,
      isActive: this.filterActive(),
      search: this.filterSearch() || undefined,
    };
  }

  applyFilters(): void { this.load(); }

  readonly hasActiveFilters = computed(() =>
    !!this.filterSearch() || !!this.filterScope() || this.filterActive() !== true
  );

  clearFilters(): void {
    this.filterSearch.set('');
    this.filterScope.set('');
    this.filterActive.set(true);
    this.load();
  }

  openCreate(): void {
    this.editingTemplate.set(null);
    this.fields.set([]);
    this.propagationType.set('Sequential');
    this.approvalSteps.set([{ role: 'store_manager', order: 1 }]);
    this.form.reset({ scope: 'System' });
    this.selectedScope.set('System');
    this.showForm.set(true);
  }

  openEdit(t: FormTemplateDto): void {
    this.templateSvc.getFormTemplate(t.id).subscribe({
      next: (detail) => {
        this.editingTemplate.set(detail);
        this.form.patchValue({
          name: detail.name,
          description: detail.description ?? '',
          scope: detail.scope,
          regionId: detail.regionId ?? '',
          storeId: detail.storeId ?? '',
        });
        this.selectedScope.set(detail.scope);
        this.propagationType.set(detail.propagationType);
        try { this.approvalSteps.set(JSON.parse(detail.approvalStepsJson)); } catch { this.approvalSteps.set([]); }
        try { this.fields.set(JSON.parse(detail.fieldsJson)); } catch { this.fields.set([]); }
        this.showForm.set(true);
      },
      error: () => this.error.set('Failed to load form template. Please try again.'),
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
    if (this.approvalSteps().length === 0) {
      this.approvalStepsError.set('At least one approval step is required.');
      return;
    }
    this.approvalStepsError.set(null);
    const { name, description, scope, regionId, storeId } = this.form.getRawValue();
    const fieldsJson = JSON.stringify(this.fields());
    this.saving.set(true);
    const editing = this.editingTemplate();

    if (editing) {
      this.templateSvc.updateFormTemplate(editing.id, {
        name, description,
        propagationType: this.propagationType(), approvalSteps: this.approvalSteps(), fieldsJson,
      }).subscribe({
        next: () => { this.saving.set(false); this.saved.set(true); setTimeout(() => this.saved.set(false), 2500); this.closeForm(); this.load(); },
        error: () => { this.error.set('Failed to save form template.'); this.saving.set(false); },
      });
    } else {
      this.templateSvc.createFormTemplate({
        name, description,
        scope: scope as TemplateScope,
        regionId: regionId || undefined,
        storeId: storeId || undefined,
        propagationType: this.propagationType(),
        approvalSteps: this.approvalSteps(),
        fieldsJson,
      }).subscribe({
        next: () => { this.saving.set(false); this.saved.set(true); setTimeout(() => this.saved.set(false), 2500); this.closeForm(); this.load(); },
        error: () => { this.error.set('Failed to save form template.'); this.saving.set(false); },
      });
    }
  }

  toggleActive(t: FormTemplateDto): void {
    const action = t.isActive
      ? (confirm(`Deactivate form template "${t.name}"?`) ? this.templateSvc.deactivateFormTemplate(t.id) : null)
      : this.templateSvc.activateFormTemplate(t.id);
    action?.subscribe({
      next: () => this.load(),
      error: () => this.error.set('Could not deactivate — active submissions may still reference this template.'),
    });
  }

  canEditScope(scope: string): boolean {
    const role = this.currentUser()?.role ?? '';
    if (scope === 'System') return role === 'super_admin' || role === 'admin';
    if (scope === 'Regional') return role === 'super_admin' || role === 'admin' || role === 'supervisor';
    return true;
  }

  allowedScopes(): TemplateScope[] {
    const role = this.currentUser()?.role ?? '';
    if (role === 'super_admin' || role === 'admin') return ['System', 'Regional', 'Store'];
    if (role === 'supervisor') return ['Regional', 'Store'];
    return ['Store'];
  }
}
