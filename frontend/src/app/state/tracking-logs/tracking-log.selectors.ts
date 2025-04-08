// tracking-log.selectors.ts
import { createFeatureSelector, createSelector } from '@ngrx/store';
import { TrackingLogState } from './tracking-log.reducer';
import * as fromTrackingLog from './tracking-log.reducer';

// This selectors file includes:
// 
// A feature selector to get the tracking logs state from the store
// 
// Entity selectors that use the adapter's generated selectors
// 
// Additional selectors for loading and error states
// 
// A selector factory for getting a tracking log by ID
// 
// These selectors match the tests we wrote in the spec file. The selectors 
// use the entity adapter's helper functions ( selectAll, selectIds, 
// selectTotal) that we exported from the reducer.

// Feature selector
export const selectTrackingLogState = createFeatureSelector<TrackingLogState>('trackingLogs');

// Entity selectors
export const selectTrackingLogEntities = createSelector(
  selectTrackingLogState,
  state => state.entities
);

export const selectAllTrackingLogs = createSelector(
  selectTrackingLogState,
  fromTrackingLog.selectAll
);

export const selectTrackingLogIds = createSelector(
  selectTrackingLogState,
  fromTrackingLog.selectIds
);

export const selectTrackingLogTotal = createSelector(
  selectTrackingLogState,
  fromTrackingLog.selectTotal
);

// Additional selectors
export const selectTrackingLogLoading = createSelector(
  selectTrackingLogState,
  state => state.loading
);

export const selectTrackingLogError = createSelector(
  selectTrackingLogState,
  state => state.error
);

export const selectTrackingLogById = (id: string) => createSelector(
  selectTrackingLogEntities,
  entities => entities[id]
);
