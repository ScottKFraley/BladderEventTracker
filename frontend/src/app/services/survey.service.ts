import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SurveyResponse } from '../models/survey-response-model';
import { AuthService } from '../auth/auth.service';


@Injectable({
  providedIn: 'root'
})
export class SurveyService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  // TODO: this REALLY should come from a config file or something. Why isn't it coming from the 
  private readonly apiUrl = '/api/v1/tracker';

  submitSurvey(surveyData: SurveyResponse): Observable<any> {
    const userId = this.authService.getCurrentUserId();
    
    if (!userId) {
      return throwError(() => new Error('User must be authenticated to submit survey'));
    }

    const enrichedData = {
      ...surveyData,
      userId: userId
    };
    
    return this.http.post(this.apiUrl, enrichedData).pipe(
      catchError(error => {
        console.error('Survey submission error:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to submit survey'));
      })
    );
  }
}
