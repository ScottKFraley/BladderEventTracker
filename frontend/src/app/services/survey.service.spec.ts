import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SurveyService } from './survey.service';
import { SurveyResponse } from '../models/survey-response-model';

describe('SurveyService', () => {
  let service: SurveyService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SurveyService]
    });
    service = TestBed.inject(SurveyService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should submit survey data', () => {
    const mockSurveyData: SurveyResponse = {
      EventDate: '2024-01-20',
      Accident: false,
      ChangePadOrUnderware: false,
      LeakAmount: 0,
      Urgency: 1,
      AwokeFromSleep: false,
      PainLevel: 0,
      Notes: 'Test notes'
    };

    service.submitSurvey(mockSurveyData).subscribe(response => {
      expect(response).toBeTruthy();
    });

    const req = httpMock.expectOne('/api/v1/tracker');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(mockSurveyData);
    
    req.flush({ success: true });
  });
});
