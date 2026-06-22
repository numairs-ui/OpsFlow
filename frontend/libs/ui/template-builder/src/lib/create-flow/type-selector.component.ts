import { Component, output } from '@angular/core';

export type CreationType = 'task' | 'form';

@Component({
  selector: 'lib-type-selector',
  imports: [],
  templateUrl: './type-selector.component.html',
  styleUrl: './type-selector.component.scss',
})
export class TypeSelectorComponent {
  readonly typeSelected = output<CreationType>();
}
