import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TrackingLogModel } from '../models/tracking-log.model';
import { ApiEndpointsService } from './api-endpoints.service';

@Injectable({
  providedIn: 'root'
})
export class TrackingLogService {
  private trackingLogs = signal<TrackingLogModel[]>([]);

  constructor(
    private http: HttpClient,
    private apiEndpoints: ApiEndpointsService
  ) { }

  getTrackingLogs(numDays: number, userId: string): Observable<TrackingLogModel[]> {
    const url = this.apiEndpoints.getByDaysAndUserEndpoint(numDays, userId);
    return this.http.get<TrackingLogModel[]>(url);
  }

  setTrackingLogs(logs: TrackingLogModel[]) {
    this.trackingLogs.set(logs);
  }

  getTrackingLogById(id: string): TrackingLogModel | undefined {
    return this.trackingLogs().find(log => log.id === id);
  }
}
