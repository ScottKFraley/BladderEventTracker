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

  // Test specifically for 0 value submission
  it('should successfully submit survey with all zero values', () => {
    const mockSurveyDataWithZeros: SurveyResponse = {
      EventDate: '2024-01-20T10:30:00',
      Accident: false,
      ChangePadOrUnderware: false,
      LeakAmount: 0,    // Zero should be valid
      Urgency: 0,       // Zero should be valid (no urgency)
      AwokeFromSleep: false,
      PainLevel: 0,     // Zero should be valid (no pain)
      Notes: 'No urgency, no pain, no leakage'
    };

    const expectedData = {
      ...mockSurveyDataWithZeros,
      userId: 'test-user-id'
    };

    service.submitSurvey(mockSurveyDataWithZeros).subscribe(response => {
      expect(response).toBeTruthy();
    });

    const req = httpMock.expectOne(`${API_BASE}/tracker`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(expectedData);
    expect(req.request.body.LeakAmount).toBe(0);
    expect(req.request.body.Urgency).toBe(0);
    expect(req.request.body.PainLevel).toBe(0);
    
    req.flush({ success: true, message: 'Zero values accepted' });
  });

  // Test boundary values
  it('should successfully submit survey with boundary values', () => {
    const mockSurveyDataBoundary: SurveyResponse = {
      EventDate: '2024-01-20T15:45:00',
      Accident: true,
      ChangePadOrUnderware: true,
      LeakAmount: 3,    // Maximum valid value
      Urgency: 4,       // Maximum valid value
      AwokeFromSleep: true,
      PainLevel: 10,    // Maximum valid value
      Notes: 'Maximum intensity event'
    };

    const expectedData = {
      ...mockSurveyDataBoundary,
      userId: 'test-user-id'
    };

    service.submitSurvey(mockSurveyDataBoundary).subscribe(response => {
      expect(response).toBeTruthy();
    });

    const req = httpMock.expectOne(`${API_BASE}/tracker`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(expectedData);
    expect(req.request.body.LeakAmount).toBe(3);
    expect(req.request.body.Urgency).toBe(4);
    expect(req.request.body.PainLevel).toBe(10);
    
    req.flush({ success: true, message: 'Boundary values accepted' });
  });

  // Test error handling when backend validation fails
  it('should handle validation error from backend', () => {
    const mockSurveyDataInvalid: SurveyResponse = {
      EventDate: '2024-01-20T10:30:00',
      Accident: false,
      ChangePadOrUnderware: false,
      LeakAmount: 0,
      Urgency: 0,
      AwokeFromSleep: false,
      PainLevel: 0,
      Notes: 'Test validation error handling'
    };

    service.submitSurvey(mockSurveyDataInvalid).subscribe({
      next: () => fail('Should not succeed'),
      error: (error) => {
        expect(error.message).toContain('Validation failed');
      }
    });

    const req = httpMock.expectOne(`${API_BASE}/tracker`);
    req.flush(
      { message: 'Validation failed: Invalid field values' }, 
      { status: 400, statusText: 'Bad Request' }
    );
  });
});
