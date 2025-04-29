import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DashboardTotalsComponent } from '../dashboard-totals/dashboard-totals.component';
import { AuthService } from '../auth/auth.service';
import { ConfigService } from '../services/config.service';

import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { TrackingLogActions } from '../state/tracking-logs/tracking-log.actions';
import * as TrackingLogSelectors from '../state/tracking-logs/tracking-log.selectors';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, DashboardTotalsComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.sass'
})
export class DashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private configService = inject(ConfigService);

  trackingLogs$ = this.store.select(TrackingLogSelectors.selectAllTrackingLogs);
  error$ = this.store.select(TrackingLogSelectors.selectError);

  constructor(private store: Store) { }

  ngOnInit(): void {
    const userId = this.authService.getCurrentUserId();
    if (userId) {
      this.configService.getDaysPrevious().subscribe(numDays => {
        this.store.dispatch(
          TrackingLogActions.loadTrackingLogs({
            numDays,
            userId
          })
        );
      });
    }

    // Keep the debug subscription
    this.trackingLogs$.subscribe(logs => {
      console.log('Tracking logs from store:', logs);
    });
  }
}
