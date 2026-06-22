import { Component, input, output } from '@angular/core';
import type { FormTemplateDto } from '@org/data-access-templates';

@Component({
  selector: 'lib-template-picker',
  imports: [],
  templateUrl: './template-picker.component.html',
  styleUrl: './template-picker.component.scss',
})
export class TemplatePickerComponent {
  readonly templates = input<FormTemplateDto[]>([]);
  readonly emptyMessage = input('No active form templates available.');
  readonly templateSelected = output<FormTemplateDto>();
}
