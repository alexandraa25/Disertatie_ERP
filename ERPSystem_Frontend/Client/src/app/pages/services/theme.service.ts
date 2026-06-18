import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly KEY = 'theme';
  isDark = false;

  constructor() {
    const saved = localStorage.getItem(this.KEY);
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    this.isDark = saved ? saved === 'dark' : prefersDark;
    this.apply();
  }

  toggle() {
    this.isDark = !this.isDark;
    localStorage.setItem(this.KEY, this.isDark ? 'dark' : 'light');
    this.apply();
  }

  private apply() {
    document.documentElement.setAttribute('data-theme', this.isDark ? 'dark' : 'light');
  }
}
