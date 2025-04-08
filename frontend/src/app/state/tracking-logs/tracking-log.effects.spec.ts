// tracking-log.effects.spec.ts
import { TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Observable, ReplaySubject } from 'rxjs';
import { TrackingLogEffects } from './tracking-log.effects';
import { TrackingLogService } from '../../services/tracking-log.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideMockStore } from '@ngrx/store/testing';

describe('TrackingLogEffects', () => {
  let actions$: ReplaySubject<any>;
  let effects: TrackingLogEffects;
  let service: jasmine.SpyObj<TrackingLogService>;

  beforeEach(() => {
    actions$ = new ReplaySubject(1);
    const serviceSpy = jasmine.createSpyObj('TrackingLogService', [
      'getTrackingLogs',
      'addTrackingLog'
      // add other methods your service uses
    ]);

    TestBed.configureTestingModule({
      imports: [
        HttpClientTestingModule
      ],
      providers: [
        TrackingLogEffects,
        provideMockActions(() => actions$),
        provideMockStore({}),
        { provide: TrackingLogService, useValue: serviceSpy }
      ]
    });

    effects = TestBed.inject(TrackingLogEffects);
    service = TestBed.inject(TrackingLogService) as jasmine.SpyObj<TrackingLogService>;
  });

  it('should be created', () => {
    expect(effects).toBeTruthy();
  });

  // Add your other effect tests here
});
