import { Component, input, output } from '@angular/core';
import type { PropagationType } from '@org/data-access-templates';

interface PropagationOption {
  value: PropagationType;
  label: string;
  description: string;
}

@Component({
  selector: 'lib-propagation-type-picker',
  imports: [],
  templateUrl: './propagation-type-picker.component.html',
  styleUrl: './propagation-type-picker.component.scss',
})
export class PropagationTypePickerComponent {
  readonly value = input<PropagationType>('Sequential');
  readonly valueChange = output<PropagationType>();

  readonly options: PropagationOption[] = [
    {
      value: 'Sequential',
      label: 'Sequential',
      description: 'Reviewers approve in order — step 2 only sees the submission after step 1 approves.',
    },
    {
      value: 'Parallel',
      label: 'Parallel',
      description: 'All reviewers notified at once. First action (approve/reject/return) wins and resolves the submission.',
    },
    {
      value: 'NotificationOnly',
      label: 'Notification Only',
      description: 'No approval required. Submission is recorded immediately and all reviewers are notified.',
    },
  ];

  select(value: PropagationType): void {
    this.valueChange.emit(value);
  }
}
