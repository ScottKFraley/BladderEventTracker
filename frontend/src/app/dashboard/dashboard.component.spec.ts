import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardComponent } from './dashboard.component';
import { AuthService } from '../auth/auth.service';
import { ConfigService } from '../services/config.service';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';
import { DashboardTotalsComponent } from '../dashboard-totals/dashboard-totals.component';
import { TrackingLogModel } from '../models/tracking-log.model';
import * as TrackingLogSelectors from '../state/tracking-logs/tracking-log.selectors';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MemoizedSelector } from '@ngrx/store';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let mockStore: jasmine.SpyObj<Store>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockConfigService: jasmine.SpyObj<ConfigService>;

  const mockTrackingLogs: TrackingLogModel[] = [
    {
      id: '1',
      eventDate: '2024-01-20T10:00:00',
      accident: false,
      changePadOrUnderware: true,
      leakAmount: 2,
      urgency: 3,
      awokeFromSleep: false,
      painLevel: 1,
      notes: 'Test note'
    }
  ];

  beforeEach(async () => {
    mockStore = jasmine.createSpyObj('Store', ['dispatch', 'select']);
    mockAuthService = jasmine.createSpyObj('AuthService', ['getCurrentUserId']);
    mockConfigService = jasmine.createSpyObj('ConfigService', ['getDaysPrevious']);

    // Set up mock store select responses
    mockStore.select.and.callFake((selector: MemoizedSelector<any, any>) => {
      // Return different values based on the selector
      if (selector === TrackingLogSelectors.selectAllTrackingLogs) {
        return of(mockTrackingLogs);
      }
      if (selector === TrackingLogSelectors.selectAveragePainLevel) {
        return of(2.5);
      }
      if (selector === TrackingLogSelectors.selectAverageUrgencyLevel) {
        return of(3.2);
      }
      if (selector === TrackingLogSelectors.selectDailyLogCounts) {
        return of({ '2024-01-20': 1 });
      }
      return of(null);
    });

    mockAuthService.getCurrentUserId.and.returnValue('test-user-id');
    mockConfigService.getDaysPrevious.and.returnValue(of(7));

    await TestBed.configureTestingModule({
      imports: [
        CommonModule,
        DashboardComponent,
        DashboardTotalsComponent
      ],
      providers: [
        { provide: Store, useValue: mockStore },
        { provide: AuthService, useValue: mockAuthService },
        { provide: ConfigService, useValue: mockConfigService },
        {
          provide: ActivatedRoute,
          useValue: {
            params: of({}),
            queryParams: of({}),
            snapshot: {
              params: {},
              queryParams: {}
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load tracking logs on init', () => {
    const userId = 'test-user-id';
    const numDays = 7;

    expect(mockConfigService.getDaysPrevious).toHaveBeenCalled();
    expect(mockAuthService.getCurrentUserId).toHaveBeenCalled();
    expect(mockStore.dispatch).toHaveBeenCalledWith(
      jasmine.objectContaining({
        numDays,
        userId
      })
    );
  });

  it('should not dispatch loadTrackingLogs if no userId is present', () => {
    // First, destroy the existing component and reset everything
    fixture.destroy();

    // Reset all mocks and spies
    mockStore.dispatch.calls.reset();
    mockAuthService.getCurrentUserId.calls.reset();
    mockConfigService.getDaysPrevious.calls.reset();

    // Set up the mock returns
    mockAuthService.getCurrentUserId.and.returnValue(null);
    // We don't even need to set up getDaysPrevious since it shouldn't be called

    // Create a fresh TestBed for this specific test
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [
        CommonModule,
        DashboardComponent,
        DashboardTotalsComponent
      ],
      providers: [
        { provide: Store, useValue: mockStore },
        { provide: AuthService, useValue: mockAuthService },
        { provide: ConfigService, useValue: mockConfigService },
        {
          provide: ActivatedRoute,
          useValue: {
            params: of({}),
            queryParams: of({}),
            snapshot: {
              params: {},
              queryParams: {}
            }
          }
        }
      ]
    }).compileComponents();

    // Create new component instance
    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;

    // Trigger ngOnInit
    fixture.detectChanges();

    // Verify our expectations
    expect(mockAuthService.getCurrentUserId).toHaveBeenCalled();
    expect(mockConfigService.getDaysPrevious).not.toHaveBeenCalled();
    expect(mockStore.dispatch).not.toHaveBeenCalled();
  });

  // Add new test cases for the totals
  it('should display average pain level', (done) => {
    component.trackingLogs$.subscribe(() => {
      const compiled = fixture.nativeElement;
      const painLevelElement = compiled.querySelector('.metric-card:nth-child(1) .metric-value');
      expect(painLevelElement.textContent).toContain('2.5');
      done();
    });
  });

  it('should display average urgency level', (done) => {
    component.trackingLogs$.subscribe(() => {
      const compiled = fixture.nativeElement;
      const urgencyLevelElement = compiled.querySelector('.metric-card:nth-child(2) .metric-value');
      expect(urgencyLevelElement.textContent).toContain('3.2');
      done();
    });
  });

  it('should display total logs count', (done) => {
    component.trackingLogs$.subscribe(() => {
      const compiled = fixture.nativeElement;
      const totalLogsElement = compiled.querySelector('.metric-card:nth-child(3) .metric-value');
      expect(totalLogsElement.textContent).toBe('1');
      done();
    });
  });

});
