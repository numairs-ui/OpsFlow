import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'formatStatus', standalone: true })
export class FormatStatusPipe implements PipeTransform {
  transform(status: string | null | undefined): string {
    if (!status) return '';
    return status.replace(/([A-Z])/g, ' $1').trim();
  }
}
