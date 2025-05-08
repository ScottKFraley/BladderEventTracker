import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { Observable, map } from 'rxjs';  // Add map import
import * as TrackingLogSelectors from '../state/tracking-logs/tracking-log.selectors';

@Component({
  selector: 'app-dashboard-totals',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard-totals.component.html',
  styleUrl: './dashboard-totals.component.sass'
})
export class DashboardTotalsComponent {
  trackingLogs$: Observable<any[]>;
  averagePainLevel$: Observable<number>;
  averageUrgencyLevel$: Observable<number>;
  dailyLogCounts$: Observable<{ [key: string]: number }>;
  averageEntriesPerDay$: Observable<number>;  // Add this property

  constructor(private store: Store) {
    this.trackingLogs$ = this.store.select(TrackingLogSelectors.selectAllTrackingLogs);
    this.averagePainLevel$ = this.store.select(TrackingLogSelectors.selectAveragePainLevel);
    this.averageUrgencyLevel$ = this.store.select(TrackingLogSelectors.selectAverageUrgencyLevel);
    this.dailyLogCounts$ = this.store.select(TrackingLogSelectors.selectDailyLogCounts);
    
    // Initialize averageEntriesPerDay$ here
    this.averageEntriesPerDay$ = this.dailyLogCounts$.pipe(
      map(counts => {
        const totalDays = Object.keys(counts).length;
        const totalEntries = Object.values(counts).reduce((sum, count) => sum + (count as number), 0);
        return totalDays > 0 ? totalEntries / totalDays : 0;
      })
    );
  }
}
