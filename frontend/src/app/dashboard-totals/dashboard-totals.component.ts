import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
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

  constructor(private store: Store) {
    this.trackingLogs$ = this.store.select(TrackingLogSelectors.selectAllTrackingLogs);
    this.averagePainLevel$ = this.store.select(TrackingLogSelectors.selectAveragePainLevel);
    this.averageUrgencyLevel$ = this.store.select(TrackingLogSelectors.selectAverageUrgencyLevel);
    this.dailyLogCounts$ = this.store.select(TrackingLogSelectors.selectDailyLogCounts);
  }
}
