import { Component, input, output } from '@angular/core';

export type SaveToggleMode = 'fill' | 'review';

@Component({
  selector: 'lib-save-toggle',
  imports: [],
  templateUrl: './save-toggle.component.html',
  styleUrl: './save-toggle.component.scss',
})
export class SaveToggleComponent {
  /** 'fill' = user filling/editing form; 'review' = approver reviewing */
  readonly mode = input<SaveToggleMode>('fill');
  /** True while an async action is in flight */
  readonly busy = input(false);
  /** True when this is a brand-new submission (no existing draft ID yet) */
  readonly isNew = input(false);
  /** Set false to hide review actions when a modal (reject/return) is already open */
  readonly reviewActionsVisible = input(true);

  readonly saveDraft = output<void>();
  readonly submit = output<void>();
  readonly approve = output<void>();
  readonly reject = output<void>();
  readonly return = output<void>();
}
