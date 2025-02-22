import { Component, OnInit, signal, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { TrackingLog } from '../models/tracking-log.model';


@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.sass'
})
export class DashboardComponent implements OnInit {
  private http = inject(HttpClient);
  data = signal<TrackingLog[]>([]);

  ngOnInit(): void {
    this.http.get<any[]>('/api/v1/tracker').subscribe({
      next: (response) => this.data.set(response),
      error: (err) => console.error('Error fetching data:', err)
    });
  }
}
