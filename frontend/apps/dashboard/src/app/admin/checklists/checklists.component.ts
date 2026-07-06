import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '@org/data-access-auth';
import { noWhitespace } from '@org/ui-core';
import { OrgService, type Region, type Store } from '@org/data-access-org';
import {
  ChecklistService, TemplateService,
  type ChecklistDetailDto, type ChecklistDto, type ChecklistItemDto,
  type TemplateDto, type TemplateScope, type UpdateChecklistRequest,
} from '@org/data-access-templates';

@Component({
  selector: 'app-checklists',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './checklists.component.html',
  styleUrl: './checklists.component.scss',
})
export class ChecklistsComponent implements OnInit {
  private readonly checklistSvc = inject(ChecklistService);
  private readonly templateSvc = inject(TemplateService);
  private readonly orgSvc = inject(OrgService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly checklists = signal<ChecklistDto[]>([]);
  readonly templates = signal<TemplateDto[]>([]);
  readonly regions = signal<Region[]>([]);
  readonly stores = signal<Store[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly saved = signal(false);
  readonly showForm = signal(false);
  readonly editingChecklist = signal<ChecklistDetailDto | null>(null);
  readonly items = signal<ChecklistItemDto[]>([]);
  readonly selectedTemplateId = signal('');

  readonly currentUser = this.auth.currentUser;
  readonly scopes: TemplateScope[] = ['System', 'Regional', 'Store'];
  readonly selectedScope = signal<string>('System');

  readonly form = this.fb.group({
    name: ['', [Validators.required, noWhitespace]],
    description: [''],
    scope: ['System' as TemplateScope, Validators.required],
    regionId: [''],
    storeId: [''],
  });

  ngOnInit(): void {
    this.load();
    this.templateSvc.getTemplates({ isActive: true, pageSize: 100 }).subscribe({ next: (r) => this.templates.set(r.items) });
    this.orgSvc.getRegions(true).subscribe({ next: (r) => this.regions.set(r) });
    this.orgSvc.getStores(undefined, true).subscribe({ next: (s) => this.stores.set(s) });
    this.form.controls['scope'].valueChanges.subscribe((v) => this.selectedScope.set(v ?? 'System'));
  }

  private load(): void {
    this.loading.set(true);
    this.checklistSvc.getChecklists(undefined, undefined).subscribe({
      next: (data) => { this.checklists.set(data); this.loading.set(false); },
      error: () => { this.error.set('Failed to load checklists.'); this.loading.set(false); },
    });
  }

  openCreate(): void {
    this.editingChecklist.set(null);
    this.items.set([]);
    this.selectedTemplateId.set('');
    this.form.reset({ scope: 'System' });
    this.selectedScope.set('System');
    this.showForm.set(true);
  }

  openEdit(c: ChecklistDto): void {
    this.checklistSvc.getChecklist(c.id).subscribe({
      next: (detail) => {
        this.editingChecklist.set(detail);
        this.items.set([...detail.items]);
        this.selectedTemplateId.set('');
        this.form.patchValue({
          name: detail.name, description: detail.description ?? '',
          scope: detail.scope, regionId: detail.regionId ?? '', storeId: detail.storeId ?? '',
        });
        this.selectedScope.set(detail.scope);
        this.showForm.set(true);
      },
    });
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editingChecklist.set(null);
    this.items.set([]);
  }

  addItem(): void {
    const tid = this.selectedTemplateId();
    if (!tid) return;
    if (this.items().some((i) => i.templateId === tid)) return; // no duplicates
    const t = this.templates().find((t) => t.id === tid);
    if (!t) return;
    const next: ChecklistItemDto = { templateId: tid, templateName: t.name, order: this.items().length };
    this.items.update((list) => [...list, next]);
    this.selectedTemplateId.set('');
  }

  removeItem(templateId: string): void {
    this.items.update((list) => list.filter((i) => i.templateId !== templateId).map((i, idx) => ({ ...i, order: idx })));
  }

  moveItemUp(index: number): void {
    if (index === 0) return;
    this.items.update((list) => {
      const copy = [...list];
      [copy[index - 1], copy[index]] = [copy[index], copy[index - 1]];
      return copy.map((i, idx) => ({ ...i, order: idx }));
    });
  }

  moveItemDown(index: number): void {
    if (index === this.items().length - 1) return;
    this.items.update((list) => {
      const copy = [...list];
      [copy[index], copy[index + 1]] = [copy[index + 1], copy[index]];
      return copy.map((i, idx) => ({ ...i, order: idx }));
    });
  }

  onSubmit(): void {
    if (this.form.invalid || this.saving()) return;
    const { name, description, scope, regionId, storeId } = this.form.getRawValue();
    const itemInputs = this.items().map((i, idx) => ({ templateId: i.templateId, order: idx }));
    this.saving.set(true);
    const editing = this.editingChecklist();

    if (editing) {
      const header: UpdateChecklistRequest = {
        name: name!, description: description ?? undefined,
        scope: scope as TemplateScope,
        regionId: regionId || undefined,
        storeId: storeId || undefined,
      };
      this.checklistSvc.updateChecklist(editing.id, header).subscribe({
        next: () => {
          this.checklistSvc.updateItems(editing.id, itemInputs).subscribe({
            next: () => { this.saving.set(false); this.saved.set(true); setTimeout(() => this.saved.set(false), 2500); this.closeForm(); this.load(); },
            error: () => { this.error.set('Failed to save checklist items.'); this.saving.set(false); },
          });
        },
        error: () => { this.error.set('Failed to save checklist.'); this.saving.set(false); },
      });
    } else {
      this.checklistSvc.createChecklist({
        name: name!, description: description ?? undefined,
        scope: scope as TemplateScope,
        regionId: regionId || undefined,
        storeId: storeId || undefined,
        items: itemInputs,
      }).subscribe({
        next: () => { this.saving.set(false); this.closeForm(); this.load(); },
        error: () => { this.error.set('Failed to save checklist.'); this.saving.set(false); },
      });
    }
  }

  toggleActive(c: ChecklistDto): void {
    const op$ = c.isActive
      ? (confirm(`Deactivate checklist "${c.name}"?`) ? this.checklistSvc.deactivateChecklist(c.id) : null)
      : this.checklistSvc.activateChecklist(c.id);
    op$?.subscribe({ next: () => this.load() });
  }

  allowedScopes(): TemplateScope[] {
    const role = this.currentUser()?.role ?? '';
    if (role === 'admin') return ['System', 'Regional', 'Store'];
    if (role === 'supervisor') return ['Regional', 'Store'];
    return ['Store'];
  }

  availableTemplates(): TemplateDto[] {
    const usedIds = new Set(this.items().map((i) => i.templateId));
    return this.templates().filter((t) => !usedIds.has(t.id));
  }
}
