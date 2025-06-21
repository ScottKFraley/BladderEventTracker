import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideMockStore, MockStore } from '@ngrx/store/testing';
import { DashboardTotalsComponent } from './dashboard-totals.component';
import * as TrackingLogSelectors from '../state/tracking-logs/tracking-log.selectors';


describe('DashboardTotalsComponent', () => {
  let component: DashboardTotalsComponent;
  let fixture: ComponentFixture<DashboardTotalsComponent>;
  let store: MockStore;

  const initialState = {
    trackingLogs: {
      logs: [],
      loading: false,
      error: null
    }
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardTotalsComponent],
      providers: [
        provideMockStore({ initialState })
      ]
    }).compileComponents();

    store = TestBed.inject(MockStore);
    fixture = TestBed.createComponent(DashboardTotalsComponent);
    component = fixture.componentInstance;

    // Mock the selectors
    store.overrideSelector(TrackingLogSelectors.selectAllTrackingLogs, []);
    store.overrideSelector(TrackingLogSelectors.selectAveragePainLevel, 0);
    store.overrideSelector(TrackingLogSelectors.selectAverageUrgencyLevel, 0);
    store.overrideSelector(TrackingLogSelectors.selectDailyLogCounts, {});

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display metrics when data is available', () => {
    store.overrideSelector(TrackingLogSelectors.selectAllTrackingLogs, [
      {
        id: '123e4567-e89b-12d3-a456-426614174000',
        eventDate: '2024-01-15T10:30:00Z',
        accident: false,
        changePadOrUnderware: false,
        leakAmount: 0,
        urgency: 4,
        awokeFromSleep: false,
        painLevel: 3,
        notes: 'Test log 1'
      },
      {
        id: '987fcdeb-51a2-43d7-9012-345678901234',
        eventDate: '2024-01-15T14:45:00Z',
        accident: false,
        changePadOrUnderware: false,
        leakAmount: 0,
        urgency: 5,
        awokeFromSleep: false,
        painLevel: 4,
        notes: 'Test log 2'
      }
    ]);
    store.overrideSelector(TrackingLogSelectors.selectAveragePainLevel, 3.5);
    store.overrideSelector(TrackingLogSelectors.selectAverageUrgencyLevel, 4.5);

    store.refreshState();
    fixture.detectChanges();

    const painLevelElement = fixture.nativeElement.querySelector('.metric-card:nth-child(1) .metric-value');
    expect(painLevelElement.textContent).toContain('3.5');
  });

  // Add more tests as needed

});
