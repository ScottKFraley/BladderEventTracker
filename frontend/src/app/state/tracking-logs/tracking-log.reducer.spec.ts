// tracking-log.reducer.spec.ts
import { TrackingLogActions } from './tracking-log.actions';
import { TrackingLogState, trackingLogReducer, adapter, initialState } from './tracking-log.reducer';
import { TrackingLogModel } from '../../models/tracking-log.model';

describe('TrackingLog Reducer', () => {
  const createTrackingLog = (id: string): TrackingLogModel => ({
    id,
    eventDate: '2024-01-01',
    accident: false,
    changePadOrUnderware: false,
    leakAmount: 0,
    urgency: 0,
    awokeFromSleep: false,
    painLevel: 0,
    notes: 'Test note'
  });

  // tracking-log.reducer.spec.ts
  describe('valid TrackingLog actions', () => {
    it('loadTrackingLogs should set loading to true', () => {
      const action = TrackingLogActions.loadTrackingLogs({
        numDays: 7,
        userId: '123'
      });
      const result = trackingLogReducer(initialState, action);

      expect(result.loading).toBe(true);
      expect(result.error).toBeNull();
    });
  });

  it('loadTrackingLogsSuccess should load trackingLogs and set loading to false', () => {
    const trackingLogs = [
      createTrackingLog('1'),
      createTrackingLog('2')
    ];
    const action = TrackingLogActions.loadTrackingLogsSuccess({ trackingLogs });
    const result = trackingLogReducer(initialState, action);

    expect(result.loading).toBe(false);
    expect(result.error).toBeNull();
    expect(result.ids.length).toBe(2);
    expect(result.entities['1']).toEqual(trackingLogs[0]);
    expect(result.entities['2']).toEqual(trackingLogs[1]);
  });

  it('loadTrackingLogsFailure should set error and set loading to false', () => {
    const error = 'Error loading tracking logs';
    const action = TrackingLogActions.loadTrackingLogsFailure({ error });
    const result = trackingLogReducer(initialState, action);

    expect(result.loading).toBe(false);
    expect(result.error).toBe(error);
  });

  it('addTrackingLog should add a tracking log', () => {
    const trackingLog = createTrackingLog('1');
    const action = TrackingLogActions.addTrackingLog({ trackingLog });
    const result = trackingLogReducer(initialState, action);

    expect(result.ids.length).toBe(1);
    expect(result.entities['1']).toEqual(trackingLog);
  });

  it('updateTrackingLog should update a tracking log', () => {
    // First add a tracking log
    const trackingLog = createTrackingLog('1');
    let state = trackingLogReducer(initialState,
      TrackingLogActions.addTrackingLog({ trackingLog }));

    // Then update it
    const update = {
      id: '1',
      changes: {
        notes: 'Updated note'
      }
    };
    const action = TrackingLogActions.updateTrackingLog({ trackingLog: update });
    const result = trackingLogReducer(state, action);

    expect(result.entities['1']?.notes).toBe('Updated note');
  });

  it('deleteTrackingLog should remove a tracking log', () => {
    // First add a tracking log
    const trackingLog = createTrackingLog('1');
    let state = trackingLogReducer(initialState,
      TrackingLogActions.addTrackingLog({ trackingLog }));

    // Then delete it
    const action = TrackingLogActions.deleteTrackingLog({ id: '1' });
    const result = trackingLogReducer(state, action);

    expect(result.ids.length).toBe(0);
    expect(result.entities['1']).toBeUndefined();
  });

  it('clearTrackingLogs should remove all tracking logs', () => {
    // First add some tracking logs
    const trackingLogs = [
      createTrackingLog('1'),
      createTrackingLog('2')
    ];
    let state = trackingLogReducer(initialState,
      TrackingLogActions.addTrackingLogs({ trackingLogs }));

    // Then clear them
    const action = TrackingLogActions.clearTrackingLogs();
    const result = trackingLogReducer(state, action);

    expect(result.ids.length).toBe(0);
    expect(Object.keys(result.entities).length).toBe(0);
  });
});

describe('unknown action', () => {
  it('should return the previous state', () => {
    const action = {} as any;
    const result = trackingLogReducer(initialState, action);

    expect(result).toBe(initialState);
  });
});

