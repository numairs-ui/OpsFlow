import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ChecklistService } from '@org/data-access-templates';
import type { ChecklistDetailDto, ChecklistItemDto } from '@org/data-access-templates';

interface ParsedField {
  id: string;
  type: string;
  label: string;
  required: boolean;
  subItems?: { id: string; label: string; required: boolean }[];
}

@Component({
  selector: 'app-checklist-detail',
  templateUrl: './checklist-detail.component.html',
  styleUrl: './checklist-detail.component.scss',
})
export class ChecklistDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly checklistSvc = inject(ChecklistService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly checklist = signal<ChecklistDetailDto | null>(null);

  readonly parsedItems = computed((): Array<{ item: ChecklistItemDto; fields: ParsedField[] }> => {
    const c = this.checklist();
    if (!c) return [];
    return c.items.map((item) => ({ item, fields: this.parseFields(item) }));
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.goBack(); return; }
    this.checklistSvc.getChecklist(id).subscribe({
      next: (c) => { this.checklist.set(c); this.loading.set(false); },
      error: () => { this.error.set('Failed to load checklist.'); this.loading.set(false); },
    });
  }

  parseFields(item: ChecklistItemDto): ParsedField[] {
    if (!item.fieldsJson) return [];
    try { return JSON.parse(item.fieldsJson) as ParsedField[]; }
    catch { return []; }
  }

  goBack(): void {
    this.router.navigate(['/admin/checklists']);
  }
}
