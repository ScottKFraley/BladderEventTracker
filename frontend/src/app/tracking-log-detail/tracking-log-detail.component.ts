import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { TrackingLogModel } from '../models/tracking-log.model';
import { TrackingLogService } from '../services/tracking-log.service';

@Component({
  selector: 'app-tracking-log-detail',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './tracking-log-detail.component.html',
})
export class TrackingLogDetailComponent implements OnInit {
  @Input() trackingLogSig = signal<TrackingLogModel | null>(null);
  private route = inject(ActivatedRoute);
  private trackingLogService = inject(TrackingLogService);

  ngOnInit() {
    const logId = this.route.snapshot.paramMap.get('id');
    if (logId) {
      const logRecord = this.trackingLogService.getTrackingLogById(logId);
      if (logRecord) {
        this.trackingLogSig.set(logRecord);
      }
    }
  }
}
