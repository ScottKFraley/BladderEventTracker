import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
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
    
    console.log('Fetching tracking logs:', {
      url: url,
      numDays: numDays,
      userId: userId,
      userAgent: navigator.userAgent,
      timestamp: new Date().toISOString()
    });
    
    return this.http.get<TrackingLogModel[]>(url).pipe(
      tap(response => {
        console.log('Tracking logs response received:', {
          count: response?.length || 0,
          url: url,
          userAgent: navigator.userAgent,
          timestamp: new Date().toISOString()
        });
      }),
      catchError((error: HttpErrorResponse) => {
        console.error('Tracking logs fetch error:', {
          status: error.status,
          statusText: error.statusText,
          message: error.message,
          url: url,
          error: error.error,
          userAgent: navigator.userAgent,
          timestamp: new Date().toISOString()
        });
        
        let errorMessage: string;
        if (error.status === 0) {
          errorMessage = 'Network connection failed. Please check your internet connection.';
        } else if (error.status === 401) {
          errorMessage = 'Authentication failed. Please log in again.';
        } else if (error.status >= 500) {
          errorMessage = `Server error (${error.status}): Unable to fetch tracking data.`;
        } else if (error.status >= 400) {
          errorMessage = `Request error (${error.status}): ${error.statusText || 'Bad request'}.`;
        } else {
          errorMessage = `Unexpected error (${error.status}): Unable to fetch tracking data.`;
        }
        
        return throwError(() => errorMessage);
      })
    );
  }

  setTrackingLogs(logs: TrackingLogModel[]) {
    this.trackingLogs.set(logs);
  }

  getTrackingLogById(id: string): TrackingLogModel | undefined {
    return this.trackingLogs().find(log => log.id === id);
  }
}
