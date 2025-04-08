import { Component, OnInit, signal, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { TrackingLogModel } from '../models/tracking-log.model';
import { TrackingLogService } from '../services/tracking-log.service';
import { DashboardTotalsComponent } from '../dashboard-totals/dashboard-totals.component';
import { AuthService } from '../auth/auth.service';
import { ConfigService } from '../services/config.service';
import { switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, DashboardTotalsComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.sass'
})
export class DashboardComponent implements OnInit {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private configService = inject(ConfigService);
  private trackingLogService = inject(TrackingLogService);
  
  data = signal<TrackingLogModel[]>([]);
  error = signal<string | null>(null);

  ngOnInit(): void {
    // Combine configuration and user ID to make the API call
    this.configService.getDaysPrevious()
      .pipe(
        switchMap(daysPrevious => {
          const userId = this.authService.getCurrentUserId();
          if (!userId) {
            throw new Error('User not authenticated');
          }
          return this.http.get<any[]>(`/api/v1/tracker/${daysPrevious}/${userId}`);
        })
      )
      .subscribe({
        next: (response) => {
          this.data.set(response);
          this.trackingLogService.setTrackingLogs(response);
          this.error.set(null);
        },
        error: (err) => {
          console.error('Error fetching data:', err);
          this.error.set('Failed to load dashboard data');
          // Optionally redirect to login if authentication error
          if (err.status === 401) {
            this.authService.logout();
          }
        }
      });
  }
}


// import { Component, OnInit, signal, effect, inject } from '@angular/core';
// import { CommonModule } from '@angular/common';
// import { RouterModule } from '@angular/router';
// import { HttpClient } from '@angular/common/http';
// import { TrackingLogModel } from '../models/tracking-log.model';
// import { TrackingLogService } from '../services/tracking-log.service';
// import { DashboardTotalsComponent } from '../dashboard-totals/dashboard-totals.component';

// @Component({
//   selector: 'app-dashboard',
//   standalone: true,
//   imports: [CommonModule, RouterModule, DashboardTotalsComponent],
//   templateUrl: './dashboard.component.html',
//   styleUrl: './dashboard.component.sass'
// })
// export class DashboardComponent implements OnInit {
//   private http = inject(HttpClient);
//   private trackingLogService = inject(TrackingLogService);
//   data = signal<TrackingLogModel[]>([]);

//   ngOnInit(): void {
//     this.http.get<any[]>('/api/v1/tracker').subscribe({
//       next: (response) => {
//         this.data.set(response);
//         this.trackingLogService.setTrackingLogs(response);
//       },
//       error: (err) => console.error('Error fetching data:', err)
//     });
//   }
// }
