import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SurveyResponse } from '../models/survey-response-model';
import { AuthService } from '../auth/auth.service';
import { ApiEndpointsService } from './api-endpoints.service';

@Injectable({
  providedIn: 'root'
})
export class SurveyService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private apiEndpoints = inject(ApiEndpointsService);

  submitSurvey(surveyData: SurveyResponse): Observable<any> {
    const userId = this.authService.getCurrentUserId();
    
    if (!userId) {
      return throwError(() => new Error('User must be authenticated to submit survey'));
    }

    const enrichedData = {
      ...surveyData,
      userId: userId
    };
    
    return this.http.post(
      this.apiEndpoints.getTrackerEndpoints().base,
      enrichedData
    ).pipe(
      catchError(error => {
        console.error('Survey submission error:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to submit survey'));
      })
    );
  }
}
