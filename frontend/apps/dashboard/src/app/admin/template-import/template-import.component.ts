import { Component, inject, signal } from '@angular/core';
import { TemplateService } from '@org/data-access-templates';
import type { ImportFailure, ImportTemplateItem } from '@org/data-access-templates';

type PreviewCounts = { task: number; checklist: number; total: number };

@Component({
  selector: 'app-template-import',
  imports: [],
  templateUrl: './template-import.component.html',
  styleUrl: './template-import.component.scss',
})
export class TemplateImportComponent {
  private readonly templateSvc = inject(TemplateService);

  readonly jsonText = signal('');
  readonly parseError = signal<string | null>(null);
  readonly preview = signal<PreviewCounts | null>(null);
  readonly parsed = signal<ImportTemplateItem[]>([]);
  readonly importing = signal(false);
  readonly result = signal<{ created: number; failed: ImportFailure[] } | null>(null);
  readonly importError = signal<string | null>(null);

  onFileChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (e) => {
      const text = (e.target?.result as string) ?? '';
      this.jsonText.set(text);
      this.parseJson(text);
    };
    reader.readAsText(file);
  }

  onTextChange(event: Event): void {
    const text = (event.target as HTMLTextAreaElement).value;
    this.jsonText.set(text);
    this.parseJson(text);
  }

  private parseJson(text: string): void {
    this.parseError.set(null);
    this.preview.set(null);
    this.parsed.set([]);
    this.result.set(null);

    if (!text.trim()) return;

    try {
      const data = JSON.parse(text);
      const templates: ImportTemplateItem[] = Array.isArray(data)
        ? data
        : Array.isArray(data.templates)
          ? data.templates
          : null!;

      if (!Array.isArray(templates)) {
        this.parseError.set('JSON must be an array of templates, or an object with a "templates" array.');
        return;
      }

      this.parsed.set(templates);
      const task = templates.filter(t => t.type === 'Task').length;
      const checklist = templates.filter(t => t.type === 'Checklist').length;
      this.preview.set({ task, checklist, total: templates.length });
    } catch {
      this.parseError.set('Invalid JSON. Please check your input.');
    }
  }

  confirm(): void {
    const templates = this.parsed();
    if (!templates.length || this.importing()) return;
    this.importing.set(true);
    this.importError.set(null);
    this.result.set(null);

    this.templateSvc.importTemplates({ templates }).subscribe({
      next: (r) => { this.result.set(r); this.importing.set(false); },
      error: () => { this.importError.set('Import request failed.'); this.importing.set(false); },
    });
  }

  reset(): void {
    this.jsonText.set('');
    this.preview.set(null);
    this.parsed.set([]);
    this.result.set(null);
    this.parseError.set(null);
    this.importError.set(null);
  }
}
