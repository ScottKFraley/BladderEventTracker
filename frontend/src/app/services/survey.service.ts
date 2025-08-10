import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError, timeout } from 'rxjs';
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
  private readonly SURVEY_TIMEOUT = 60000; // 1 minute for survey submission

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
      timeout(this.SURVEY_TIMEOUT),
      catchError(error => {
        console.error('Survey submission error:', error);
        
        let errorMessage = 'Failed to submit survey';
        if (error.message?.includes('Timeout') || (error as any).name === 'TimeoutError') {
          errorMessage = `Survey submission timed out after ${this.SURVEY_TIMEOUT/1000} seconds. Please try again.`;
        } else if (error.error?.message) {
          errorMessage = error.error.message;
        }
        
        return throwError(() => new Error(errorMessage));
      })
    );
  }
}
