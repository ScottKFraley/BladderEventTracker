import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DashboardTotalsComponent } from '../dashboard-totals/dashboard-totals.component';
import { AuthService } from '../auth/auth.service';
import { ConfigService } from '../services/config.service';
import { MobileDebugService } from '../services/mobile-debug.service';

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
  private mobileDebug = inject(MobileDebugService);

  trackingLogs$ = this.store.select(TrackingLogSelectors.selectAllTrackingLogs);
  error$ = this.store.select(TrackingLogSelectors.selectError);

  constructor(private store: Store) { }
  
  // Helper methods for debugging (can be called from browser console)
  getUserAgent(): string {
    return navigator.userAgent;
  }
  
  getCurrentTimestamp(): string {
    return new Date().toISOString();
  }
  
  getCurrentUrl(): string {
    return window.location.href;
  }

  ngOnInit(): void {
    console.log('Dashboard component initializing');
    const userId = this.authService.getCurrentUserId();
    console.log('Current user ID:', userId);
    
    if (userId) {
      this.configService.getDaysPrevious().subscribe({
        next: (numDays) => {
          console.log('Loading tracking logs for:', { numDays, userId });
          this.store.dispatch(
            TrackingLogActions.loadTrackingLogs({
              numDays,
              userId
            })
          );
        },
        error: (error) => {
          console.error('Error getting days configuration:', error);
        }
      });
    } else {
      console.error('No user ID available for loading tracking logs');
    }

    // Enhanced debug subscription
    this.trackingLogs$.subscribe(logs => {
      console.log('Tracking logs from store:', {
        count: logs?.length || 0,
        logs: logs,
        userAgent: navigator.userAgent,
        timestamp: new Date().toISOString()
      });
    });
    
    // Debug error subscription with mobile debug integration
    this.error$.subscribe(error => {
      if (error) {
        console.error('Dashboard error from store:', {
          error: error,
          userAgent: navigator.userAgent,
          timestamp: new Date().toISOString()
        });
        
        // Log error to mobile debug service
        this.mobileDebug.logError(error, 'Dashboard - Tracking logs fetch');
      }
    });
  }
}
