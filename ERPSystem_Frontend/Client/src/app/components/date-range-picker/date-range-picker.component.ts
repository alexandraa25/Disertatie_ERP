import { Component, EventEmitter, Output, Input, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

// Angular Material
import { MatTabsModule } from '@angular/material/tabs';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-date-range-picker',
  standalone: true, // 🔥 IMPORTANT
  imports: [
    CommonModule,     // 👉 pentru date pipe
    FormsModule,      // 👉 pentru ngModel
    MatTabsModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatNativeDateModule,
    MatButtonModule
  ],
  templateUrl: './date-range-picker.component.html',
  styleUrls: ['./date-range-picker.component.css']
})
export class DateRangePickerComponent {

  @Input() from: Date | null = null;
  @Input() to: Date | null = null;

  @Output() rangeChange = new EventEmitter<any>();

  // dropdown
  isOpen = false;

  today = new Date();
  // preview
  tempFrom: Date | null = null;
  tempTo: Date | null = null;

  // preset
  selectedPreset: string | null = null;

  // realtime
  isCustomMode = false;
  customValue = 5;
  customUnit: 'minutes' | 'hours' = 'minutes';

  presets = [
    { key: 'today', label: 'Today' },
    { key: 'yesterday', label: 'Yesterday' },
    { key: 'week', label: 'Last 7 Days' },
    { key: 'month', label: 'Last 30 Days' }
  ];

  toggle() {
    this.isOpen = !this.isOpen;

    if (this.isOpen) {
      this.tempFrom = this.from;
      this.tempTo = this.to;
    }
  }

  close() {
    this.isOpen = false;
  }

  // click outside
  @HostListener('document:click', ['$event'])
onClickOutside(event: any) {
  const clickedInside = event.target.closest('.picker-container');

  const isDatepicker =
    event.target.closest('.cdk-overlay-pane') || // 🔥 important
    event.target.closest('.mat-datepicker-content');

  if (!clickedInside && !isDatepicker) {
    this.close();
  }
}
  // presets
  selectPreset(type: string) {
    this.selectedPreset = type;

    const { from, to } = this.calculatePreset(type);
    this.tempFrom = from;
    this.tempTo = to;
  }

  calculatePreset(type: string): { from: Date; to: Date } {
  const today = new Date();

  if (type === 'today') {
    return { from: new Date(today), to: new Date() };
  }

  if (type === 'yesterday') {
    const from = new Date(today);
    from.setDate(today.getDate() - 1);
    return { from, to: new Date(from) };
  }

  if (type === 'week') {
    const from = new Date(today);
    from.setDate(today.getDate() - 7);
    return { from, to: new Date() };
  }

  if (type === 'month') {
    const from = new Date(today);
    from.setDate(today.getDate() - 30);
    return { from, to: new Date() };
  }

  // fallback
  return { from: new Date(today), to: new Date() };
}

 setRealtime(minutes: number) {
  const now = new Date();
  const from = new Date(now.getTime() - minutes * 60000);

  this.tempFrom = from;
  this.tempTo = now;

  this.selectedPreset = `rt-${minutes}`; // highlight
}

  applyRealtime() {
  if (!this.customValue || this.customValue <= 0) return;

  let minutes = this.customValue;

  if (this.customUnit === 'hours') {
    minutes = minutes * 60;
  }

  this.setRealtime(minutes);
}

  // manual change
  onManualChange() {
    this.selectedPreset = null;
  }

  // actions
  apply() {
    this.from = this.tempFrom;
    this.to = this.tempTo;

    this.rangeChange.emit({
      from: this.from,
      to: this.to
    });

    this.close();
  }

  cancel() {
    this.close();
  }

 selectRealtimePreset(minutes: number) {
  this.isCustomMode = false; // 🔥 ieși din custom
  this.selectedPreset = `rt-${minutes}`;
  this.setRealtime(minutes);
}
activateCustom() {
  this.isCustomMode = true;
  this.selectedPreset = null;

  this.customValue = 5;
  this.customUnit = 'minutes';
}

clear() {
  // reset preview
  this.tempFrom = null;
  this.tempTo = null;

  // reset final values
  this.from = null;
  this.to = null;

  // reset UI state
  this.selectedPreset = null;
  this.isCustomMode = false;

  // emit către parent
  this.rangeChange.emit({
    from: null,
    to: null
  });

  this.close(); // 👈 închide dropdown
}

}