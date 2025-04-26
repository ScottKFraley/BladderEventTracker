import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, concatMap, tap, mergeMap } from 'rxjs/operators';
import { of } from 'rxjs';
import { TrackingLogActions } from './tracking-log.actions';
import { TrackingLogService } from '../../services/tracking-log.service';

@Injectable()
export class TrackingLogEffects {
  loadTrackingLogs$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(TrackingLogActions.loadTrackingLogs),
      tap(action => console.log('Effect received action:', action)), // Log the action
      mergeMap(({ numDays, userId }) =>
        this.trackingLogService.getTrackingLogs(numDays, userId).pipe(
          tap(logs => console.log('API Response:', logs)), // Log the API response
          map(logs => TrackingLogActions.loadTrackingLogsSuccess({ trackingLogs: logs })),
          catchError(error => {
            console.error('Effect error:', error);
            return of(TrackingLogActions.loadTrackingLogsFailure({ error }))
          })
        ))
    );
  });

  // Optional: Handle errors
  handleErrors$ = createEffect(() => {
    return this.actions$.pipe(
      ofType(TrackingLogActions.loadTrackingLogsFailure),
      tap(({ error }) => {
        console.error('Failed to load tracking logs:', error);
        // You could add a call to a notification service here
      })
    );
  }, { dispatch: false });

  constructor(
    private actions$: Actions,
    private trackingLogService: TrackingLogService
  ) { }
}
