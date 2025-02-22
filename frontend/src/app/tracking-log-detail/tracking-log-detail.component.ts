import { Component, Input, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TrackingLog } from '../models/tracking-log.model'; // Adjust the import path as needed


@Component({
  selector: 'app-tracking-log-detail',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './tracking-log-detail.component.html',
})
export class TrackingLogDetailComponent {
  @Input() trackingLog = signal<TrackingLog | null>(null);
}
