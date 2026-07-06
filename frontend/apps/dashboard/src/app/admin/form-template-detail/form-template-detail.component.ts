import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormTemplateService } from '@org/data-access-templates';
import type { ApprovalStep, FormTemplateDetailDto } from '@org/data-access-templates';
import type { TemplateField } from '@org/ui-field-builder';
import { roleLabel } from '@org/ui-core';

@Component({
  selector: 'app-form-template-detail',
  templateUrl: './form-template-detail.component.html',
  styleUrl: './form-template-detail.component.scss',
})
export class FormTemplateDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formTemplateSvc = inject(FormTemplateService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly template = signal<FormTemplateDetailDto | null>(null);
  readonly roleLabel = roleLabel;

  readonly fields = computed<TemplateField[]>(() => {
    const t = this.template();
    if (!t?.fieldsJson) return [];
    try { return JSON.parse(t.fieldsJson) as TemplateField[]; }
    catch { return []; }
  });

  readonly approvalSteps = computed<ApprovalStep[]>(() => {
    const t = this.template();
    if (!t?.approvalStepsJson) return [];
    try { return (JSON.parse(t.approvalStepsJson) as ApprovalStep[]).sort((a, b) => a.order - b.order); }
    catch { return []; }
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.goBack(); return; }
    this.formTemplateSvc.getFormTemplate(id).subscribe({
      next: (t) => { this.template.set(t); this.loading.set(false); },
      error: () => { this.error.set('Failed to load form template.'); this.loading.set(false); },
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/form-templates']);
  }
}
