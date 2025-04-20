import { Component, OnInit, inject } from '@angular/core';
import { Model as SurveyModel } from 'survey-core';
import { SurveyModule } from 'survey-angular-ui';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { SurveyService } from '../services/survey.service';
import { SurveyResponse } from '../models/survey-response-model';


@Component({
  selector: 'app-survey',
  standalone: true,
  imports: [CommonModule, SurveyModule],
  templateUrl: './survey.component.html',
  styleUrls: ['./survey.component.sass']
})
export class SurveyComponent implements OnInit {
  private surveyService = inject(SurveyService);
  private router = inject(Router);

  showAdditionalFields = false;
  surveyModel!: SurveyModel; // Use definite assignment assertion

  private getBaseSurveyElements() {
    return [
      {
        type: "text",
        name: "EventDate",
        title: "Event Date / Time",
        inputType: "datetime-local",
        isRequired: true,
        defaultValue: new Date().toLocaleString('sv').slice(0, 16).replace(' ', 'T')
      },
      {
        type: "rating",
        name: "Urgency",
        title: "Urgency (0-4)",
        rateMax: 4,
        rateValues: [
          { value: 0, text: "0 - No real urgency" },
          { value: 1, text: "1 - Slight urgency" },
          { value: 2, text: "2 - Pretty Urgent" },
          { value: 3, text: "3 - Very Urgent" },
          { value: 4, text: "4 - Emergency level urgency" }
        ],
        isRequired: true,
        defaultValue: 1
      },
      {
        type: "boolean",
        name: "AwokeFromSleep",
        title: "Did this wake you from sleep?",
        isRequired: true,
        defaultValue: false
      },
      {
        type: "rating",
        name: "PainLevel",
        title: "Pain Level (0-10)",
        rateMin: 0,
        rateMax: 10,
        minRateDescription: "No Pain (0)",
        maxRateDescription: "Worst Pain (10)",
        isRequired: true,
        defaultValue: 1
      },
      {
        type: "rating",
        name: "LeakAmount",
        title: "Leak Amount (0-3)",
        rateMax: 3,
        rateValues: [
          { value: 0, text: "0 - None" },
          { value: 1, text: "1 - Slight" },
          { value: 2, text: "2 - Moderate" },
          { value: 3, text: "3 - Heavy" }
        ],
        isRequired: true,
        defaultValue: 1
      },
      {
        type: "comment",
        name: "Notes",
        title: "Notes (optional)",
        maxLength: 2000,
        isRequired: false
      }
    ];
  }

  private getAdditionalElements() {
    return [
      {
        type: "boolean",
        name: "Accident",
        title: "Was there an accident?",
        isRequired: true,
        defaultValue: false
      },
      {
        type: "boolean",
        name: "ChangePadOrUnderware",
        title: "Did you have to changed your pad or underwear?",
        isRequired: true,
        defaultValue: false
      }
    ];
  }

  toggleAdditionalFields() {
    this.showAdditionalFields = !this.showAdditionalFields;
    this.updateSurveyElements();
  }

  private updateSurveyElements() {
    const elements = [...this.getBaseSurveyElements()];
    if (this.showAdditionalFields) {
      elements.splice(1, 0, ...this.getAdditionalElements());
    }

    const surveyJson = {
      title: "Log the Bladder Event",
      elements: elements
    };

    this.surveyModel = new SurveyModel(surveyJson);
    this.setupSurveyComplete();
  }

  private setupSurveyComplete() {
    this.surveyModel.onComplete.add((sender: any) => {
      const surveyData: SurveyResponse = sender.data;

      this.surveyService.submitSurvey(surveyData).subscribe({
        next: (response) => {
          console.log('Survey submitted successfully:', response);
          this.router.navigate(['/dashboard']);
        },
        error: (error) => {
          console.error('Survey submission error:', error);
          if (error.message.includes('authenticated')) {
            this.router.navigate(['/login']); // or however you handle auth redirects
          } else {
            alert('Failed to submit survey: ' + error.message);
          }
        }
      });
    });
  }

  ngOnInit() {
    this.updateSurveyElements();
  }
}

