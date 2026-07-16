import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ChecklistService, TemplateService } from '@org/data-access-templates';
import type {
  ChecklistDetailDto, ChecklistItemInput, ScoringType, TemplateDto,
} from '@org/data-access-templates';

interface ParsedField {
  id: string;
  type: string;
  label: string;
  required: boolean;
  subItems?: { id: string; label: string; required: boolean }[];
}

/** Editable, in-memory representation of one checklist item (scoring included). */
interface EditItem {
  templateId: string;
  templateName: string;
  fieldsJson?: string;
  scoringType: ScoringType | null;
  weight: number;
  photoRequired: boolean;
  failCorrectiveActionText: string;
  failScoreThreshold: number | null;
}

@Component({
  selector: 'app-checklist-detail',
  imports: [FormsModule],
  templateUrl: './checklist-detail.component.html',
  styleUrl: './checklist-detail.component.scss',
})
export class ChecklistDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly checklistSvc = inject(ChecklistService);
  private readonly templateSvc = inject(TemplateService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly checklist = signal<ChecklistDetailDto | null>(null);

  readonly items = signal<EditItem[]>([]);
  readonly dirty = signal(false);

  // Add-item picker
  readonly allTemplates = signal<TemplateDto[]>([]);
  readonly templateSearch = signal('');
  readonly availableTemplates = computed(() => {
    const chosen = new Set(this.items().map((i) => i.templateId));
    const q = this.templateSearch().toLowerCase();
    return this.allTemplates().filter(
      (t) => !chosen.has(t.id) && (!q || t.name.toLowerCase().includes(q) || t.category.toLowerCase().includes(q)),
    );
  });

  readonly scoringTypes: { value: ScoringType | ''; label: string }[] = [
    { value: '', label: 'No scoring' },
    { value: 'PassFail', label: 'Pass / Fail' },
    { value: 'Scale1To5', label: 'Scale 1–5' },
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.goBack(); return; }
    this.checklistSvc.getChecklist(id).subscribe({
      next: (c) => {
        this.checklist.set(c);
        this.items.set(
          [...c.items]
            .sort((a, b) => a.order - b.order)
            .map((i) => ({
              templateId: i.templateId,
              templateName: i.templateName,
              fieldsJson: i.fieldsJson,
              scoringType: i.scoringType ?? null,
              weight: i.weight ?? 1,
              photoRequired: i.photoRequired ?? false,
              failCorrectiveActionText: i.failCorrectiveActionText ?? '',
              failScoreThreshold: i.failScoreThreshold ?? null,
            })),
        );
        this.loading.set(false);
      },
      error: () => { this.error.set('Failed to load checklist.'); this.loading.set(false); },
    });

    this.templateSvc.getTemplates({ isActive: true, pageSize: 200 }).subscribe({
      next: (r) => this.allTemplates.set(r.items),
    });
  }

  parseFields(item: EditItem): ParsedField[] {
    if (!item.fieldsJson) return [];
    try { return JSON.parse(item.fieldsJson) as ParsedField[]; }
    catch { return []; }
  }

  // ── Item mutations ───────────────────────────────────────────────────────────

  addTemplate(template: TemplateDto): void {
    this.items.update((list) => [
      ...list,
      {
        templateId: template.id,
        templateName: template.name,
        fieldsJson: undefined,
        scoringType: null,
        weight: 1,
        photoRequired: false,
        failCorrectiveActionText: '',
        failScoreThreshold: null,
      },
    ]);
    this.templateSearch.set('');
    this.dirty.set(true);
  }

  removeItem(index: number): void {
    this.items.update((list) => list.filter((_, i) => i !== index));
    this.dirty.set(true);
  }

  move(index: number, delta: number): void {
    const target = index + delta;
    this.items.update((list) => {
      if (target < 0 || target >= list.length) return list;
      const next = [...list];
      [next[index], next[target]] = [next[target], next[index]];
      return next;
    });
    this.dirty.set(true);
  }

  setScoringType(index: number, value: string): void {
    this.items.update((list) =>
      list.map((it, i) => {
        if (i !== index) return it;
        const scoringType = (value || null) as ScoringType | null;
        return {
          ...it,
          scoringType,
          // Threshold only applies to Scale1To5.
          failScoreThreshold: scoringType === 'Scale1To5' ? (it.failScoreThreshold ?? 3) : null,
        };
      }),
    );
    this.dirty.set(true);
  }

  patchItem(index: number, patch: Partial<EditItem>): void {
    this.items.update((list) => list.map((it, i) => (i === index ? { ...it, ...patch } : it)));
    this.dirty.set(true);
  }

  // ── Save ───────────────────────────────────────────────────────────────────

  save(): void {
    const c = this.checklist();
    if (!c || this.saving()) return;

    const payload: ChecklistItemInput[] = this.items().map((it, index) => ({
      templateId: it.templateId,
      order: index,
      scoringType: it.scoringType,
      weight: it.weight,
      photoRequired: it.photoRequired,
      failCorrectiveActionText: it.failCorrectiveActionText || null,
      failScoreThreshold: it.scoringType === 'Scale1To5' ? it.failScoreThreshold : null,
    }));

    this.saving.set(true);
    this.error.set(null);
    this.checklistSvc.updateItems(c.id, payload).subscribe({
      next: () => { this.saving.set(false); this.dirty.set(false); },
      error: () => { this.error.set('Failed to save checklist items.'); this.saving.set(false); },
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/templates/checklists']);
  }
}
