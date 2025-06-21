// app/services/config.service.ts
import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  private readonly DAYS_PREVIOUS_KEY = 'dashboard_days_previous';
  private daysPrevious = new BehaviorSubject<number>(2); // default value

  constructor() {
    // Load from localStorage if exists
    const saved = localStorage.getItem(this.DAYS_PREVIOUS_KEY);
    if (saved) {
      this.daysPrevious.next(Number(saved));
    }
  }

  getDaysPrevious() {
    return this.daysPrevious.asObservable();
  }

  setDaysPrevious(days: number) {
    localStorage.setItem(this.DAYS_PREVIOUS_KEY, days.toString());
    this.daysPrevious.next(days);
  }
}
