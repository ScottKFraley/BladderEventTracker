import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SurveyResponse } from '../models/survey-response-model';


@Injectable({
  providedIn: 'root'
})
export class SurveyService {
  private http = inject(HttpClient);
  private readonly apiUrl = '/api/v1/tracker';

  submitSurvey(surveyData: SurveyResponse): Observable<any> {
    return this.http.post(this.apiUrl, surveyData);
  }
}
