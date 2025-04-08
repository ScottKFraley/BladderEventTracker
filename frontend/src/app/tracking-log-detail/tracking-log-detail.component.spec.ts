// tracking-log-detail.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { TrackingLogDetailComponent } from './tracking-log-detail.component';
import { TrackingLogService } from '../services/tracking-log.service';
import { DatePipe } from '@angular/common';

describe('TrackingLogDetailComponent', () => {
  let component: TrackingLogDetailComponent;
  let fixture: ComponentFixture<TrackingLogDetailComponent>;
  let trackingLogService: jasmine.SpyObj<TrackingLogService>;

  beforeEach(async () => {
    // Create spy for TrackingLogService
    trackingLogService = jasmine.createSpyObj('TrackingLogService', ['getTrackingLogById']);

    // Mock ActivatedRoute with a paramMap
    const activatedRouteMock = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue('123') // Mock ID value
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [
        TrackingLogDetailComponent,
        DatePipe
      ],
      providers: [
        { provide: TrackingLogService, useValue: trackingLogService },
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TrackingLogDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // Add more tests as needed
});
