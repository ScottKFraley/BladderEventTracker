// tracking-log.selectors.spec.ts
import { TrackingLogState } from './tracking-log.reducer';
import * as fromTrackingLog from './tracking-log.selectors';
import { TrackingLogModel } from '../../models/tracking-log.model';

describe('TrackingLog Selectors', () => {
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

  const trackingLog1 = createTrackingLog('1');
  const trackingLog2 = createTrackingLog('2');

  let state: TrackingLogState;

  beforeEach(() => {
    state = {
      ids: ['1', '2'],
      entities: {
        '1': trackingLog1,
        '2': trackingLog2
      },
      loading: false,
      error: null
    };
  });

  describe('TrackingLog Selectors', () => {
    it('selectAllTrackingLogs should return all tracking logs', () => {
      const result = fromTrackingLog.selectAllTrackingLogs.projector(state);
      expect(result.length).toBe(2);
      expect(result).toEqual([trackingLog1, trackingLog2]);
    });

    it('selectTrackingLogEntities should return the entities object', () => {
      const result = fromTrackingLog.selectTrackingLogEntities.projector(state);
      expect(result).toEqual(state.entities);
    });

    it('selectTrackingLogIds should return the ids array', () => {
      const result = fromTrackingLog.selectTrackingLogIds.projector(state);
      expect(result).toEqual(['1', '2']);
    });

    it('selectTrackingLogTotal should return the total number of tracking logs', () => {
      const result = fromTrackingLog.selectTrackingLogTotal.projector(state);
      expect(result).toBe(2);
    });

    it('selectTrackingLogById should return the specified tracking log', () => {
      const result = fromTrackingLog.selectTrackingLogById('1').projector(state.entities);
      expect(result).toEqual(trackingLog1);
    });

    it('selectTrackingLogLoading should return the loading state', () => {
      const result = fromTrackingLog.selectTrackingLogLoading.projector(state);
      expect(result).toBe(false);
    });

    it('selectTrackingLogError should return the error state', () => {
      const result = fromTrackingLog.selectTrackingLogError.projector(state);
      expect(result).toBeNull();

      const errorState = { ...state, error: 'Test error' };
      const errorResult = fromTrackingLog.selectTrackingLogError.projector(errorState);
      expect(errorResult).toBe('Test error');
    });

    // Testing with loading state
    it('should handle loading state correctly', () => {
      const loadingState = { ...state, loading: true };
      const result = fromTrackingLog.selectTrackingLogLoading.projector(loadingState);
      expect(result).toBe(true);
    });

    // Testing with empty state
    it('should handle empty state correctly', () => {
      const emptyState: TrackingLogState = {
        ids: [],
        entities: {},
        loading: false,
        error: null
      };
      
      const result = fromTrackingLog.selectAllTrackingLogs.projector(emptyState);
      expect(result).toEqual([]);
      expect(fromTrackingLog.selectTrackingLogTotal.projector(emptyState)).toBe(0);
    });
  });
});
