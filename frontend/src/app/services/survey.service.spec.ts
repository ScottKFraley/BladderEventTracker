import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SurveyService } from './survey.service';
import { SurveyResponse } from '../models/survey-response-model';
import { ApiEndpointsService } from './api-endpoints.service';
import { AuthService } from '../auth/auth.service';

describe('SurveyService', () => {
  let service: SurveyService;
  let httpMock: HttpTestingController;
  let apiEndpoints: ApiEndpointsService;
  let authService: AuthService;

  // Mock API endpoint
  const API_BASE = '/api/v1';
  const mockApiEndpoints = {
    getTrackerEndpoints: () => ({
      base: `${API_BASE}/tracker`
    })
  };

  // Mock AuthService
  const mockAuthService = {
    getCurrentUserId: () => 'test-user-id'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        SurveyService,
        { provide: ApiEndpointsService, useValue: mockApiEndpoints },
        { provide: AuthService, useValue: mockAuthService }
      ]
    });
    service = TestBed.inject(SurveyService);
    httpMock = TestBed.inject(HttpTestingController);
    apiEndpoints = TestBed.inject(ApiEndpointsService);
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

    // Expected data with userId
    const expectedData = {
      ...mockSurveyData,
      userId: 'test-user-id'
    };

    service.submitSurvey(mockSurveyData).subscribe(response => {
      expect(response).toBeTruthy();
    });

    const req = httpMock.expectOne(`${API_BASE}/tracker`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(expectedData);
    
    req.flush({ success: true });
  });
});
