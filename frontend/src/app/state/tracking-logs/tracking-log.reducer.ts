// src/app/state/tracking-logs/tracking-log.reducer.ts
import { EntityState, EntityAdapter, createEntityAdapter } from '@ngrx/entity';
import { createReducer, on } from '@ngrx/store';
import { TrackingLogActions } from './tracking-log.actions';
import { TrackingLogModel } from '../../models/tracking-log.model';

export interface TrackingLogState extends EntityState<TrackingLogModel> {
  loading: boolean;
  error: string | null;
}

export const adapter: EntityAdapter<TrackingLogModel> = createEntityAdapter<TrackingLogModel>();

export const initialState: TrackingLogState = adapter.getInitialState({
  loading: false,
  error: null
});

export const trackingLogReducer = createReducer(
  initialState,
  on(TrackingLogActions.loadTrackingLogs, (state) => ({
    ...state,
    loading: true
  })),
  on(TrackingLogActions.loadTrackingLogsSuccess, (state, { trackingLogs }) => {
    console.log('Reducer receiving logs:', trackingLogs); // Log in reducer
    return adapter.setAll(trackingLogs, { ...state, error: null });
  }),
  on(TrackingLogActions.loadTrackingLogsFailure, (state, { error }) => ({
    ...state,
    error,
    loading: false
  })),
  on(TrackingLogActions.addTrackingLog, (state, { trackingLog }) =>
    adapter.addOne(trackingLog, state)
  ),
  // Add this new case for handling deleteTrackingLog
  on(TrackingLogActions.deleteTrackingLog, (state, { id }) =>
    adapter.removeOne(id, state)
  ),
  on(TrackingLogActions.updateTrackingLog, (state, { trackingLog }) =>
    adapter.updateOne(trackingLog, state)
  )
);

export const {
  selectIds,
  selectEntities,
  selectAll,
  selectTotal,
} = adapter.getSelectors();
