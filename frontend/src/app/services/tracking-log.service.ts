import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TrackingLogModel } from '../models/tracking-log.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TrackingLogService {
  private trackingLogs = signal<TrackingLogModel[]>([]);
  private apiUrl = `${environment.apiUrl}/api/tracker/{numDays}/{userId}`;

  constructor(private http: HttpClient) {}

  getTrackingLogs(numDays: number, userId: string): Observable<TrackingLogModel[]> {
    const url = this.apiUrl
      .replace('{numDays}', numDays.toString())
      .replace('{userId}', userId);
    
    return this.http.get<TrackingLogModel[]>(url);
  }

  setTrackingLogs(logs: TrackingLogModel[]) {
    this.trackingLogs.set(logs);
  }

  getTrackingLogById(id: string): TrackingLogModel | undefined {
    return this.trackingLogs().find(log => log.id === id);
  }
}
