import { CdkDrag, CdkDragDrop, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import { Component, input, output } from '@angular/core';
import type { ApprovalRole, ApprovalStep, PropagationType } from '@org/data-access-templates';

@Component({
  selector: 'lib-approval-steps-builder',
  imports: [CdkDropList, CdkDrag],
  templateUrl: './approval-steps-builder.component.html',
  styleUrl: './approval-steps-builder.component.scss',
})
export class ApprovalStepsBuilderComponent {
  readonly steps = input<ApprovalStep[]>([]);
  readonly propagationType = input<PropagationType>('Sequential');
  readonly stepsChange = output<ApprovalStep[]>();

  readonly roles: ApprovalRole[] = ['store_employee', 'store_manager', 'supervisor', 'admin'];

  readonly roleLabels: Record<string, string> = {
    store_employee: 'Store Employee',
    store_manager: 'Store Manager',
    supervisor: 'Supervisor',
    admin: 'Administrator',
  };

  get reorderDisabled(): boolean {
    return this.propagationType() !== 'Sequential';
  }

  addStep(): void {
    const next = [...this.steps()];
    next.push({ role: 'store_manager', order: next.length + 1 });
    this.emitReordered(next);
  }

  removeStep(index: number): void {
    const next = this.steps().filter((_, i) => i !== index);
    this.emitReordered(next);
  }

  updateRole(index: number, role: ApprovalRole): void {
    const next = this.steps().map((s, i) => (i === index ? { ...s, role } : s));
    this.stepsChange.emit(next);
  }

  drop(event: CdkDragDrop<ApprovalStep[]>): void {
    if (this.reorderDisabled) return;
    const next = [...this.steps()];
    moveItemInArray(next, event.previousIndex, event.currentIndex);
    this.emitReordered(next);
  }

  private emitReordered(steps: ApprovalStep[]): void {
    this.stepsChange.emit(steps.map((s, i) => ({ ...s, order: i + 1 })));
  }
}
