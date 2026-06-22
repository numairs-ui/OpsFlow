import { Component, effect, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  TemplateField, FieldType, ChecklistSubItem,
  createField, createSubItem,
} from '../field.models.js';

@Component({
  selector: 'lib-field-builder',
  imports: [FormsModule],
  templateUrl: './field-builder.component.html',
  styleUrl: './field-builder.component.scss',
})
export class FieldBuilderComponent {
  readonly initialFields = input<TemplateField[]>([]);
  readonly fieldsChange = output<TemplateField[]>();

  readonly fields = signal<TemplateField[]>([]);
  readonly expandedFieldId = signal<string | null>(null);

  // Draft label per checklist field (keyed by field.id)
  readonly draftSubLabel = signal<Record<string, string>>({});

  constructor() {
    effect(() => {
      const initial = this.initialFields();
      if (initial.length > 0) {
        this.fields.set(initial);
      }
    });
  }

  readonly fieldTypes: FieldType[] = ['Numeric', 'Boolean', 'Text', 'Photo', 'Checklist'];

  addField(type: FieldType): void {
    const field = createField(type);
    this.fields.update((f) => [...f, field]);
    this.expandedFieldId.set(field.id);
    this.emit();
  }

  removeField(id: string): void {
    this.fields.update((f) => f.filter((x) => x.id !== id));
    if (this.expandedFieldId() === id) this.expandedFieldId.set(null);
    this.emit();
  }

  toggleExpand(id: string): void {
    this.expandedFieldId.set(this.expandedFieldId() === id ? null : id);
  }

  moveUp(index: number): void {
    if (index === 0) return;
    this.fields.update((f) => {
      const copy = [...f];
      [copy[index - 1], copy[index]] = [copy[index], copy[index - 1]];
      return copy;
    });
    this.emit();
  }

  moveDown(index: number): void {
    const len = this.fields().length;
    if (index === len - 1) return;
    this.fields.update((f) => {
      const copy = [...f];
      [copy[index], copy[index + 1]] = [copy[index + 1], copy[index]];
      return copy;
    });
    this.emit();
  }

  updateField(id: string, patch: Partial<TemplateField>): void {
    this.fields.update((f) =>
      f.map((x) => (x.id === id ? { ...x, ...patch } : x))
    );
    this.emit();
  }

  getDraft(fieldId: string): string {
    return this.draftSubLabel()[fieldId] ?? '';
  }

  setDraft(fieldId: string, value: string): void {
    this.draftSubLabel.update(d => ({ ...d, [fieldId]: value }));
  }

  commitSubItem(fieldId: string): void {
    const label = this.getDraft(fieldId).trim();
    if (!label) return;
    const sub: ChecklistSubItem = { ...createSubItem(), label };
    this.fields.update((f) =>
      f.map((x) =>
        x.id === fieldId
          ? { ...x, subItems: [...(x.subItems ?? []), sub] }
          : x
      )
    );
    this.setDraft(fieldId, '');
    this.emit();
  }

  onSubItemKeydown(event: KeyboardEvent, fieldId: string): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.commitSubItem(fieldId);
    }
  }

  updateSubItem(fieldId: string, subItemId: string, patch: Partial<ChecklistSubItem>): void {
    this.fields.update((f) =>
      f.map((x) =>
        x.id === fieldId
          ? {
              ...x,
              subItems: (x.subItems ?? []).map((s) =>
                s.id === subItemId ? { ...s, ...patch } : s
              ),
            }
          : x
      )
    );
    this.emit();
  }

  removeSubItem(fieldId: string, subItemId: string): void {
    this.fields.update((f) =>
      f.map((x) =>
        x.id === fieldId
          ? { ...x, subItems: (x.subItems ?? []).filter((s) => s.id !== subItemId) }
          : x
      )
    );
    this.emit();
  }

  loadInitial(): void {
    if (this.initialFields().length > 0) {
      this.fields.set(this.initialFields());
    }
  }

  private emit(): void {
    this.fieldsChange.emit(this.fields());
  }
}
