import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, concatMap } from 'rxjs/operators';
import { of } from 'rxjs';
import { TrackingLogActions } from './tracking-log.actions';
import { TrackingLogService } from '../../services/tracking-log.service';

@Injectable()
export class TrackingLogEffects {
  loadTrackingLogs$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(TrackingLogActions.loadTrackingLogs),
      concatMap((action) =>
        this.trackingLogService.getTrackingLogs(action.numDays, action.userId).pipe(
          map(trackingLogs => TrackingLogActions.loadTrackingLogsSuccess({ trackingLogs })),
          catchError(error =>
            of(TrackingLogActions.loadTrackingLogsFailure({ error: error.message })))
        )
      )
    );
  });

  constructor(
    private actions$: Actions,
    private trackingLogService: TrackingLogService
  ) { }
}
