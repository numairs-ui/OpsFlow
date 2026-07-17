import { Component, input } from '@angular/core';

export type StatTileTone = 'neutral' | 'warn' | 'action' | 'ok' | 'danger';

@Component({
  selector: 'app-stat-tile',
  templateUrl: './stat-tile.component.html',
  styleUrl: './stat-tile.component.scss',
})
export class StatTileComponent {
  readonly value = input.required<number | string>();
  readonly label = input.required<string>();
  readonly tone = input<StatTileTone>('neutral');
}
