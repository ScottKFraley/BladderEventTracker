import { Component, OnInit, signal, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { TrackingLogModel } from '../models/tracking-log.model';
import { TrackingLogService } from '../services/tracking-log.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.sass'
})
export class DashboardComponent implements OnInit {
  private http = inject(HttpClient);
  private trackingLogService = inject(TrackingLogService);
  data = signal<TrackingLogModel[]>([]);

  ngOnInit(): void {
    this.http.get<any[]>('/api/v1/tracker').subscribe({
      next: (response) => {
        this.data.set(response);
        this.trackingLogService.setTrackingLogs(response);
      },
      error: (err) => console.error('Error fetching data:', err)
    });
  }
}
