// survey.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { SurveyComponent } from './survey.component';
import { SurveyService } from '../services/survey.service';
import { CommonModule } from '@angular/common';
import { SurveyModule } from 'survey-angular-ui';
import { Model } from 'survey-core';
import { of } from 'rxjs';

describe('SurveyComponent', () => {
  let component: SurveyComponent;
  let fixture: ComponentFixture<SurveyComponent>;
  let surveyService: jasmine.SpyObj<SurveyService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    surveyService = jasmine.createSpyObj('SurveyService', ['submitSurvey']);
    router = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [
        CommonModule,
        SurveyModule,
        SurveyComponent // since it's standalone
      ],
      providers: [
        { provide: SurveyService, useValue: surveyService },
        { provide: Router, useValue: router }
      ]
    }).compileComponents();

    surveyService.submitSurvey.and.returnValue(of({}));
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(SurveyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should navigate to dashboard after successful submission', () => {
    // Get the survey model instance
    const surveyModel = component.surveyModel as Model;
    
    // Create test data that matches your SurveyResponse model
    const testData = {
      EventDate: '2024-01-20',
      Accident: false,
      ChangePadOrUnderware: false,
      LeakAmount: 0,
      Urgency: 1,
      AwokeFromSleep: false,
      PainLevel: 0,
      Notes: 'Test notes'
    };

    // Set the data
    surveyModel.data = testData;

    // Create the options object for the fire method
    const options = {
      isCompleteOnTrigger: true,
      clearSaveMessages: () => {},
      showSaveSuccess: () => {},
      showSaveError: () => {},
      showSaveInProgress: () => {},
      showDataSaving: () => {},
      showDataSavingError: () => {},
      showDataSavingSuccess: () => {},
      showDataSavingClear: () => {}
    };

    // Simulate survey completion with both required arguments
    surveyModel.onComplete.fire(surveyModel, options);

    // Verify the service was called with correct data
    expect(surveyService.submitSurvey).toHaveBeenCalledWith(testData);
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('should successfully submit survey with all zero values', () => {
    // Get the survey model instance
    const surveyModel = component.surveyModel as Model;
    
    // Create test data with all zero values for validation fields
    const testDataWithZeros = {
      EventDate: '2024-01-20T10:30:00',
      Accident: false,
      ChangePadOrUnderware: false,
      LeakAmount: 0,    // Test zero leak amount (no leakage)
      Urgency: 0,       // Test zero urgency (no urgency)
      AwokeFromSleep: false,
      PainLevel: 0,     // Test zero pain level (no pain)
      Notes: 'Testing all zero values - no urgency, no pain, no leak'
    };

    // Set the data
    surveyModel.data = testDataWithZeros;

    const options = {
      isCompleteOnTrigger: true,
      clearSaveMessages: () => {},
      showSaveSuccess: () => {},
      showSaveError: () => {},
      showSaveInProgress: () => {},
      showDataSaving: () => {},
      showDataSavingError: () => {},
      showDataSavingSuccess: () => {},
      showDataSavingClear: () => {}
    };

    // Simulate survey completion
    surveyModel.onComplete.fire(surveyModel, options);

    // Verify the service was called with zero values preserved
    expect(surveyService.submitSurvey).toHaveBeenCalledWith(testDataWithZeros);
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('should successfully submit survey with maximum boundary values', () => {
    // Get the survey model instance  
    const surveyModel = component.surveyModel as Model;
    
    // Create test data with maximum boundary values
    const testDataMaxValues = {
      EventDate: '2024-01-20T15:45:00',
      Accident: true,
      ChangePadOrUnderware: true, 
      LeakAmount: 3,    // Maximum valid value
      Urgency: 4,       // Maximum valid value
      AwokeFromSleep: true,
      PainLevel: 10,    // Maximum valid value
      Notes: 'Testing maximum boundary values'
    };

    // Set the data
    surveyModel.data = testDataMaxValues;

    const options = {
      isCompleteOnTrigger: true,
      clearSaveMessages: () => {},
      showSaveSuccess: () => {},
      showSaveError: () => {},
      showSaveInProgress: () => {},
      showDataSaving: () => {},
      showDataSavingError: () => {},
      showDataSavingSuccess: () => {},
      showDataSavingClear: () => {}
    };

    // Simulate survey completion
    surveyModel.onComplete.fire(surveyModel, options);

    // Verify the service was called with maximum values preserved
    expect(surveyService.submitSurvey).toHaveBeenCalledWith(testDataMaxValues);
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('should verify survey dropdown choices include zero values', () => {
    // Test that the survey configuration includes 0 as valid choice
    const urgencyElement = component.surveyModel.getQuestionByName('Urgency');
    const painLevelElement = component.surveyModel.getQuestionByName('PainLevel');
    const leakAmountElement = component.surveyModel.getQuestionByName('LeakAmount');

    // Verify 0 is a valid choice for each field using bracket notation
    expect(urgencyElement['choices']).toContain(jasmine.objectContaining({ value: 0 }));
    expect(painLevelElement['choices']).toContain(jasmine.objectContaining({ value: 0 }));
    expect(leakAmountElement['choices']).toContain(jasmine.objectContaining({ value: 0 }));

    // Verify the text descriptions for zero values
    const urgencyZeroChoice = urgencyElement['choices'].find((c: any) => c.value === 0);
    const painZeroChoice = painLevelElement['choices'].find((c: any) => c.value === 0);
    const leakZeroChoice = leakAmountElement['choices'].find((c: any) => c.value === 0);

    expect(urgencyZeroChoice.text).toBe('0 - No real urgency');
    expect(painZeroChoice.text).toBe('0 - No Pain');
    expect(leakZeroChoice.text).toBe('0 - None');
  });
});

// import { ComponentFixture, TestBed } from '@angular/core/testing';
// import { SurveyComponent } from './survey.component';
// import { SurveyService } from '../services/survey.service';
// import { SurveyResponse } from '../models/survey-response-model';
// import { Router } from '@angular/router';
// import { of, throwError } from 'rxjs';
// import { Model } from 'survey-core';

// describe('SurveyComponent', () => {
//   let component: SurveyComponent;
//   let fixture: ComponentFixture<SurveyComponent>;
//   let surveyService: jasmine.SpyObj<SurveyService>;
//   let router: jasmine.SpyObj<Router>;

//   beforeEach(async () => {
//     const surveyServiceSpy = jasmine.createSpyObj('SurveyService', ['submitSurvey']);
//     const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

//     await TestBed.configureTestingModule({
//       imports: [SurveyComponent],
//       providers: [
//         { provide: SurveyService, useValue: surveyServiceSpy },
//         { provide: Router, useValue: routerSpy }
//       ]
//     }).compileComponents();

//     surveyService = TestBed.inject(SurveyService) as jasmine.SpyObj<SurveyService>;
//     router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
//   });

//   beforeEach(() => {
//     fixture = TestBed.createComponent(SurveyComponent);
//     component = fixture.componentInstance;
//     fixture.detectChanges();
//   });

//   it('should create', () => {
//     expect(component).toBeTruthy();
//   });

//   it('should navigate to dashboard after successful submission', () => {
//     surveyService.submitSurvey.and.returnValue(of({ success: true }));
    
//     const mockData: SurveyResponse = {
//       EventDate: '2024-01-20',
//       Accident: false,
//       ChangePadOrUnderware: false,
//       LeakAmount: 0,
//       Urgency: 1,
//       AwokeFromSleep: false,
//       PainLevel: 0,
//       Notes: 'Test notes'
//     };

//     component.surveyModel = new Model({
//       elements: [
//         { type: "text", name: "EventDate", title: "Event Date" }
//       ]
//     });
//     component.surveyModel.data = mockData;

//     // Simulate survey completion with empty function implementations
//     component.surveyModel.onComplete.fire(component.surveyModel, {
//       isCompleteOnTrigger: true,
//       clearSaveMessages: () => {},
//       showSaveSuccess: () => {},
//       showSaveError: () => {},
//       showSaveInProgress: () => {},
//       showDataSaving: () => {},
//       showDataSavingError: () => {},
//       showDataSavingSuccess: () => {},
//       showDataSavingClear: () => {}
//     });

//     expect(surveyService.submitSurvey).toHaveBeenCalledWith(mockData);
//     expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
//   });

//   it('should handle submission error', () => {
//     spyOn(console, 'error');
//     surveyService.submitSurvey.and.returnValue(throwError(() => new Error('API Error')));
    
//     const mockData: SurveyResponse = {
//       EventDate: '2024-01-20',
//       Accident: false,
//       ChangePadOrUnderware: false,
//       LeakAmount: 0,
//       Urgency: 1,
//       AwokeFromSleep: false,
//       PainLevel: 0,
//       Notes: 'Test notes'
//     };

//     component.surveyModel = new Model({
//       elements: [
//         { type: "text", name: "EventDate", title: "Event Date" }
//       ]
//     });
//     component.surveyModel.data = mockData;

//     // Simulate survey completion with empty function implementations
//     component.surveyModel.onComplete.fire(component.surveyModel, {
//       isCompleteOnTrigger: true,
//       clearSaveMessages: () => {},
//       showSaveSuccess: () => {},
//       showSaveError: () => {},
//       showSaveInProgress: () => {},
//       showDataSaving: () => {},
//       showDataSavingError: () => {},
//       showDataSavingSuccess: () => {},
//       showDataSavingClear: () => {}
//     });

//     expect(surveyService.submitSurvey).toHaveBeenCalled();
//     expect(console.error).toHaveBeenCalled();
//     expect(router.navigate).not.toHaveBeenCalled();
//   });
// });
