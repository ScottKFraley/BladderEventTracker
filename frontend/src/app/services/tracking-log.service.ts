import { Injectable, signal } from '@angular/core';
import { TrackingLogModel } from '../models/tracking-log.model';

@Injectable({
  providedIn: 'root'
})
export class TrackingLogService {
  private trackingLogs = signal<TrackingLogModel[]>([]);

  setTrackingLogs(logs: TrackingLogModel[]) {
    this.trackingLogs.set(logs);
  }

  getTrackingLogById(id: string): TrackingLogModel | undefined {
    return this.trackingLogs().find(log => log.id === id);
  }
}
