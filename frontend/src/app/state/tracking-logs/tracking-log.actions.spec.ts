// tracking-log.actions.spec.ts
import { TrackingLogActions } from './tracking-log.actions';
import { TrackingLogModel } from '../../models/tracking-log.model';

describe('TrackingLog Actions', () => {
  const mockTrackingLog: TrackingLogModel = {
    id: '1',
    eventDate: '2024-01-01',
    accident: false,
    changePadOrUnderware: false,
    leakAmount: 0,
    urgency: 0,
    awokeFromSleep: false,
    painLevel: 0,
    notes: 'Test note'
  };

  describe('Load TrackingLogs', () => {
    it('should create Load TrackingLogs action', () => {
      // Create test parameters instead of trackingLogs
      const numDays = 7;
      const userId = '123';
      const action = TrackingLogActions.loadTrackingLogs({ numDays, userId });

      expect(action.type).toBe('[TrackingLog/API] Load TrackingLogs');
      expect(action.numDays).toBe(numDays);
      expect(action.userId).toBe(userId);
    });
  });

  // The rest of your test cases remain the same
  describe('Add TrackingLog', () => {
    it('should create Add TrackingLog action', () => {
      const action = TrackingLogActions.addTrackingLog({ trackingLog: mockTrackingLog });

      expect(action.type).toBe('[TrackingLog/API] Add TrackingLog');
      expect(action.trackingLog).toEqual(mockTrackingLog);
    });
  });

  describe('Upsert TrackingLog', () => {
    it('should create Upsert TrackingLog action', () => {
      const action = TrackingLogActions.upsertTrackingLog({ trackingLog: mockTrackingLog });

      expect(action.type).toBe('[TrackingLog/API] Upsert TrackingLog');
      expect(action.trackingLog).toEqual(mockTrackingLog);
    });
  });

  describe('Add TrackingLogs', () => {
    it('should create Add TrackingLogs action', () => {
      const trackingLogs = [mockTrackingLog];
      const action = TrackingLogActions.addTrackingLogs({ trackingLogs });

      expect(action.type).toBe('[TrackingLog/API] Add TrackingLogs');
      expect(action.trackingLogs).toEqual(trackingLogs);
    });
  });

  describe('Upsert TrackingLogs', () => {
    it('should create Upsert TrackingLogs action', () => {
      const trackingLogs = [mockTrackingLog];
      const action = TrackingLogActions.upsertTrackingLogs({ trackingLogs });

      expect(action.type).toBe('[TrackingLog/API] Upsert TrackingLogs');
      expect(action.trackingLogs).toEqual(trackingLogs);
    });
  });

  describe('Update TrackingLog', () => {
    it('should create Update TrackingLog action', () => {
      const update = {
        id: '1',
        changes: { notes: 'Updated note' }
      };
      const action = TrackingLogActions.updateTrackingLog({ trackingLog: update });

      expect(action.type).toBe('[TrackingLog/API] Update TrackingLog');
      expect(action.trackingLog).toEqual(update);
    });
  });

  describe('Update TrackingLogs', () => {
    it('should create Update TrackingLogs action', () => {
      const updates = [{
        id: '1',
        changes: { notes: 'Updated note' }
      }];
      const action = TrackingLogActions.updateTrackingLogs({ trackingLogs: updates });

      expect(action.type).toBe('[TrackingLog/API] Update TrackingLogs');
      expect(action.trackingLogs).toEqual(updates);
    });
  });

  describe('Delete TrackingLog', () => {
    it('should create Delete TrackingLog action', () => {
      const action = TrackingLogActions.deleteTrackingLog({ id: '1' });

      expect(action.type).toBe('[TrackingLog/API] Delete TrackingLog');
      expect(action.id).toBe('1');
    });
  });

  describe('Delete TrackingLogs', () => {
    it('should create Delete TrackingLogs action', () => {
      const ids = ['1', '2'];
      const action = TrackingLogActions.deleteTrackingLogs({ ids });

      expect(action.type).toBe('[TrackingLog/API] Delete TrackingLogs');
      expect(action.ids).toEqual(ids);
    });
  });

  describe('Clear TrackingLogs', () => {
    it('should create Clear TrackingLogs action', () => {
      const action = TrackingLogActions.clearTrackingLogs();

      expect(action.type).toBe('[TrackingLog/API] Clear TrackingLogs');
    });
  });
});
