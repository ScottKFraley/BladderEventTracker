import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { Update } from '@ngrx/entity';

import { TrackingLogModel } from '../../models/tracking-log.model';

// tracking-log.actions.ts
export const TrackingLogActions = createActionGroup({
  source: 'TrackingLog/API',
  events: {
    'Load TrackingLogs': props<{ numDays: number; userId: string }>(),
    'Load TrackingLogs Success': props<{ trackingLogs: TrackingLogModel[] }>(),
    'Load TrackingLogs Failure': props<{ error: string }>(),
    'Add TrackingLog': props<{ trackingLog: TrackingLogModel }>(),
    'Upsert TrackingLog': props<{ trackingLog: TrackingLogModel }>(),
    'Add TrackingLogs': props<{ trackingLogs: TrackingLogModel[] }>(),
    'Upsert TrackingLogs': props<{ trackingLogs: TrackingLogModel[] }>(),
    'Update TrackingLog': props<{ trackingLog: Update<TrackingLogModel> }>(),
    'Update TrackingLogs': props<{ trackingLogs: Update<TrackingLogModel>[] }>(),
    'Delete TrackingLog': props<{ id: string }>(),
    'Delete TrackingLogs': props<{ ids: string[] }>(),
    'Clear TrackingLogs': emptyProps(),
  }
});
