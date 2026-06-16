import { Component, computed, input, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type Preset = 'hourly' | 'daily' | 'weekly' | 'monthly' | 'custom';
const DOW_LABELS = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'];
const DOW_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

@Component({
  selector: 'lib-cron-picker',
  imports: [FormsModule],
  templateUrl: './cron-picker.component.html',
  styleUrl: './cron-picker.component.scss',
})
export class CronPickerComponent implements OnInit {
  readonly value = input<string>('0 0 9 * * ?');
  readonly valueChange = output<string>();

  readonly activePreset = signal<Preset>('daily');
  readonly minute = signal(0);
  readonly hour = signal(9);
  readonly dayOfWeek = signal(1); // 0=SUN … 6=SAT → Quartz DOW: SUN=1…SAT=7
  readonly dayOfMonth = signal(1);
  readonly customCron = signal('0 0 9 * * ?');

  readonly dowOptions = DOW_NAMES.map((name, i) => ({ label: name, quartz: DOW_LABELS[i] }));

  readonly humanReadable = computed(() => {
    const hh = String(this.hour()).padStart(2, '0');
    const mm = String(this.minute()).padStart(2, '0');
    switch (this.activePreset()) {
      case 'hourly': return `Every hour at minute :${String(this.minute()).padStart(2, '0')}`;
      case 'daily': return `Every day at ${hh}:${mm}`;
      case 'weekly': return `Every ${DOW_NAMES[this.dayOfWeek()]} at ${hh}:${mm}`;
      case 'monthly': return `On day ${this.dayOfMonth()} of every month at ${hh}:${mm}`;
      case 'custom': return `Custom: ${this.customCron()}`;
    }
  });

  ngOnInit(): void {
    this.parseInitial(this.value());
  }

  private parseInitial(expr: string): void {
    const parts = expr.trim().split(/\s+/);
    if (parts.length !== 6) {
      this.activePreset.set('custom');
      this.customCron.set(expr);
      return;
    }
    const [, min, hr, dom, , dow] = parts;
    if (dow !== '?' && dow !== '*') {
      this.activePreset.set('weekly');
      this.minute.set(+min || 0);
      this.hour.set(+hr || 0);
      const idx = DOW_LABELS.indexOf(dow.toUpperCase());
      this.dayOfWeek.set(idx >= 0 ? idx : 1);
    } else if (dom !== '?' && dom !== '*') {
      this.activePreset.set('monthly');
      this.minute.set(+min || 0);
      this.hour.set(+hr || 0);
      this.dayOfMonth.set(+dom || 1);
    } else if (hr === '*') {
      this.activePreset.set('hourly');
      this.minute.set(+min || 0);
    } else {
      this.activePreset.set('daily');
      this.minute.set(+min || 0);
      this.hour.set(+hr || 0);
    }
  }

  selectPreset(p: Preset): void {
    this.activePreset.set(p);
    this.emit();
  }

  emit(): void {
    const m = String(this.minute()).padStart(2, '0') === '0' ? '0' : String(this.minute());
    const h = String(this.hour());
    const dom = String(this.dayOfMonth());
    let cron: string;
    switch (this.activePreset()) {
      case 'hourly':  cron = `0 ${m} * * * ?`; break;
      case 'daily':   cron = `0 ${m} ${h} * * ?`; break;
      case 'weekly':  cron = `0 ${m} ${h} ? * ${DOW_LABELS[this.dayOfWeek()]}`; break;
      case 'monthly': cron = `0 ${m} ${h} ${dom} * ?`; break;
      case 'custom':  cron = this.customCron(); break;
    }
    this.valueChange.emit(cron!);
  }

  onTimeInput(val: string): void {
    const [hh, mm] = val.split(':').map(Number);
    this.hour.set(hh || 0);
    this.minute.set(mm || 0);
    this.emit();
  }

  timeValue(): string {
    return `${String(this.hour()).padStart(2, '0')}:${String(this.minute()).padStart(2, '0')}`;
  }
}
