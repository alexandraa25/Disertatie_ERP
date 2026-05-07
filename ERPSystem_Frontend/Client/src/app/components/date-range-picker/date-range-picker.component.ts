import { Component, EventEmitter, Output, Input, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-date-range-picker',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
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

  isOpen = false;

  today = new Date();
  tempFrom: Date | null = null;
  tempTo: Date | null = null;

  selectedPreset: string | null = null;

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

  @HostListener('document:click', ['$event'])
  onClickOutside(event: any) {
    const clickedInside = event.target.closest('.picker-container');

    const isDatepicker =
      event.target.closest('.cdk-overlay-pane') ||
      event.target.closest('.mat-datepicker-content');

    if (!clickedInside && !isDatepicker) {
      this.close();
    }
  }

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

    return { from: new Date(today), to: new Date() };
  }

  setRealtime(minutes: number) {
    const now = new Date();
    const from = new Date(now.getTime() - minutes * 60000);

    this.tempFrom = from;
    this.tempTo = now;

    this.selectedPreset = `rt-${minutes}`;
  }

  applyRealtime() {
    if (!this.customValue || this.customValue <= 0) return;
    let minutes = this.customValue;
    if (this.customUnit === 'hours') {
      minutes = minutes * 60;
    }
    this.setRealtime(minutes);
  }

  onManualChange() {
    this.selectedPreset = null;
  }

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
    this.isCustomMode = false; 
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
    this.tempFrom = null;
    this.tempTo = null;

    this.from = null;
    this.to = null;

    this.selectedPreset = null;
    this.isCustomMode = false;

    this.rangeChange.emit({
      from: null,
      to: null
    });

    this.close(); 
  }

}