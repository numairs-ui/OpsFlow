import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TemplateService } from '@org/data-access-templates';
import type { TemplateDetailDto } from '@org/data-access-templates';
import type { TemplateField } from '@org/ui-field-builder';

@Component({
  selector: 'app-template-detail',
  templateUrl: './template-detail.component.html',
  styleUrl: './template-detail.component.scss',
})
export class TemplateDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly templateSvc = inject(TemplateService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly template = signal<TemplateDetailDto | null>(null);

  readonly fields = computed<TemplateField[]>(() => {
    const t = this.template();
    if (!t?.fieldsJson) return [];
    try { return JSON.parse(t.fieldsJson) as TemplateField[]; }
    catch { return []; }
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.goBack(); return; }
    this.templateSvc.getTemplate(id).subscribe({
      next: (t) => { this.template.set(t); this.loading.set(false); },
      error: () => { this.error.set('Failed to load template.'); this.loading.set(false); },
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/templates/task-templates']);
  }
}
