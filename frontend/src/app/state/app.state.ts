// app/state/app.state.ts
import { ConfigState } from './config/config.state';
import { TrackingLogState } from './tracking-logs/tracking-log.reducer';

export interface AppState {
    config: ConfigState;
    trackingLogs: TrackingLogState;
}
