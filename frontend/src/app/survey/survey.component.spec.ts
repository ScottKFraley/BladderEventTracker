import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SurveyComponent } from './survey.component';
import { SurveyService } from '../services/survey.service';
import { SurveyResponse } from '../models/survey-response-model';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { Model } from 'survey-core';

describe('SurveyComponent', () => {
  let component: SurveyComponent;
  let fixture: ComponentFixture<SurveyComponent>;
  let surveyService: jasmine.SpyObj<SurveyService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const surveyServiceSpy = jasmine.createSpyObj('SurveyService', ['submitSurvey']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [SurveyComponent],
      providers: [
        { provide: SurveyService, useValue: surveyServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    surveyService = TestBed.inject(SurveyService) as jasmine.SpyObj<SurveyService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(SurveyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should navigate to dashboard after successful submission', () => {
    surveyService.submitSurvey.and.returnValue(of({ success: true }));
    
    const mockData: SurveyResponse = {
      EventDate: '2024-01-20',
      Accident: false,
      ChangePadOrUnderware: false,
      LeakAmount: 0,
      Urgency: 1,
      AwokeFromSleep: false,
      PainLevel: 0,
      Notes: 'Test notes'
    };

    component.surveyModel = new Model({
      elements: [
        { type: "text", name: "EventDate", title: "Event Date" }
      ]
    });
    component.surveyModel.data = mockData;

    // Simulate survey completion with empty function implementations
    component.surveyModel.onComplete.fire(component.surveyModel, {
      isCompleteOnTrigger: true,
      clearSaveMessages: () => {},
      showSaveSuccess: () => {},
      showSaveError: () => {},
      showSaveInProgress: () => {},
      showDataSaving: () => {},
      showDataSavingError: () => {},
      showDataSavingSuccess: () => {},
      showDataSavingClear: () => {}
    });

    expect(surveyService.submitSurvey).toHaveBeenCalledWith(mockData);
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('should handle submission error', () => {
    spyOn(console, 'error');
    surveyService.submitSurvey.and.returnValue(throwError(() => new Error('API Error')));
    
    const mockData: SurveyResponse = {
      EventDate: '2024-01-20',
      Accident: false,
      ChangePadOrUnderware: false,
      LeakAmount: 0,
      Urgency: 1,
      AwokeFromSleep: false,
      PainLevel: 0,
      Notes: 'Test notes'
    };

    component.surveyModel = new Model({
      elements: [
        { type: "text", name: "EventDate", title: "Event Date" }
      ]
    });
    component.surveyModel.data = mockData;

    // Simulate survey completion with empty function implementations
    component.surveyModel.onComplete.fire(component.surveyModel, {
      isCompleteOnTrigger: true,
      clearSaveMessages: () => {},
      showSaveSuccess: () => {},
      showSaveError: () => {},
      showSaveInProgress: () => {},
      showDataSaving: () => {},
      showDataSavingError: () => {},
      showDataSavingSuccess: () => {},
      showDataSavingClear: () => {}
    });

    expect(surveyService.submitSurvey).toHaveBeenCalled();
    expect(console.error).toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
