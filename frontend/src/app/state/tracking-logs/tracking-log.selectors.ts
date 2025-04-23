import { createFeatureSelector, createSelector } from '@ngrx/store';
import { TrackingLogState } from './tracking-log.reducer';
import * as fromTrackingLog from './tracking-log.reducer';

// Feature selector
export const selectTrackingLogState = (state: any) => state.trackingLogs;

// Use the adapter's selectAll selector
export const selectAllTrackingLogs = createSelector(
  selectTrackingLogState,
  fromTrackingLog.selectAll  // This uses the adapter's built-in selectAll
);

// Add this to your existing tracking-log.selectors.ts
export const selectError = createSelector(
  selectTrackingLogState,
  (state: TrackingLogState) => state.error
);

export const selectAveragePainLevel = createSelector(
  selectAllTrackingLogs,
  (logs) => {
    if (!logs.length) return 0;
    const sum = logs.reduce((acc, log) => acc + log.painLevel, 0);
    return sum / logs.length;
  }
);

export const selectDailyLogCounts = createSelector(
  selectAllTrackingLogs,
  (logs) => {
    return logs.reduce((acc, log) => {
      const date = new Date(log.eventDate).toLocaleDateString();
      acc[date] = (acc[date] || 0) + 1;
      return acc;
    }, {} as { [key: string]: number });
  }
);

export const selectAverageUrgencyLevel = createSelector(
  selectAllTrackingLogs,
  (logs) => {
    if (!logs.length) return 0;
    const sum = logs.reduce((acc, log) => acc + log.urgency, 0);
    return sum / logs.length;
  }
);


// Entity selectors
export const selectTrackingLogEntities = createSelector(
  selectTrackingLogState,
  state => state.entities
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
