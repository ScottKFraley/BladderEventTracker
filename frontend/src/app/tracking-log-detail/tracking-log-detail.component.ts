import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Store } from '@ngrx/store';
import { TrackingLogModel } from '../models/tracking-log.model';
import * as TrackingLogSelectors from '../state/tracking-logs/tracking-log.selectors';

@Component({
  selector: 'app-tracking-log-detail',
  standalone: true,
  imports: [DatePipe, RouterModule],
  templateUrl: './tracking-log-detail.component.html',
})
export class TrackingLogDetailComponent implements OnInit {
  @Input() trackingLogSig = signal<TrackingLogModel | null>(null);
  
  private route = inject(ActivatedRoute);
  private store = inject(Store);

  ngOnInit() {
    const logId = this.route.snapshot.paramMap.get('id');
    if (logId) {
      // Get data from NgRx store instead of service
      this.store.select(TrackingLogSelectors.selectAllTrackingLogs).subscribe(logs => {
        const logRecord = logs.find(log => log.id === logId);
        if (logRecord) {
          this.trackingLogSig.set(logRecord);
        }
      });
    }
  }
}
