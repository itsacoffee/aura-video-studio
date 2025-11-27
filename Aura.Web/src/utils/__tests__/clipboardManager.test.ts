import { describe, it, expect, beforeEach } from 'vitest';
import { clipboardManager } from '../clipboardManager';

describe('ClipboardManager', () => {
  beforeEach(() => {
    clipboardManager.clear();
  });

  describe('copy', () => {
    it('should store single clip data', () => {
      const clipData = { id: 'clip-1', type: 'video', startTime: 0 };
      clipboardManager.copy(clipData);

      expect(clipboardManager.hasData()).toBe(true);
      expect(clipboardManager.paste()).toEqual(clipData);
    });

    it('should set type to "clip" for single object', () => {
      const clipData = { id: 'clip-1' };
      clipboardManager.copy(clipData);

      expect(clipboardManager.getDataType()).toBe('clip');
    });

    it('should set type to "clips" for array', () => {
      const clipsData = [{ id: 'clip-1' }, { id: 'clip-2' }];
      clipboardManager.copy(clipsData);

      expect(clipboardManager.getDataType()).toBe('clips');
    });

    it('should overwrite previous data', () => {
      clipboardManager.copy({ id: 'clip-1' });
      clipboardManager.copy({ id: 'clip-2' });

      expect(clipboardManager.paste()).toEqual({ id: 'clip-2' });
    });
  });

  describe('paste', () => {
    it('should return null when clipboard is empty', () => {
      expect(clipboardManager.paste()).toBeNull();
    });

    it('should return copied data', () => {
      const clipData = { id: 'clip-1', name: 'Test Clip' };
      clipboardManager.copy(clipData);

      expect(clipboardManager.paste()).toEqual(clipData);
    });

    it('should not clear data after paste', () => {
      clipboardManager.copy({ id: 'clip-1' });

      clipboardManager.paste();
      clipboardManager.paste();

      expect(clipboardManager.hasData()).toBe(true);
    });
  });

  describe('cut', () => {
    it('should store data like copy', () => {
      const clipData = { id: 'clip-1' };
      clipboardManager.cut(clipData);

      expect(clipboardManager.hasData()).toBe(true);
      expect(clipboardManager.paste()).toEqual(clipData);
    });

    it('should return the cut data', () => {
      const clipData = { id: 'clip-1' };
      const result = clipboardManager.cut(clipData);

      expect(result).toEqual(clipData);
    });
  });

  describe('hasData', () => {
    it('should return false when clipboard is empty', () => {
      expect(clipboardManager.hasData()).toBe(false);
    });

    it('should return true when clipboard has data', () => {
      clipboardManager.copy({ id: 'clip-1' });
      expect(clipboardManager.hasData()).toBe(true);
    });
  });

  describe('clear', () => {
    it('should remove all data from clipboard', () => {
      clipboardManager.copy({ id: 'clip-1' });
      expect(clipboardManager.hasData()).toBe(true);

      clipboardManager.clear();
      expect(clipboardManager.hasData()).toBe(false);
      expect(clipboardManager.paste()).toBeNull();
    });
  });

  describe('getDataType', () => {
    it('should return null when clipboard is empty', () => {
      expect(clipboardManager.getDataType()).toBeNull();
    });

    it('should return "clip" for single object', () => {
      clipboardManager.copy({ id: 'clip-1' });
      expect(clipboardManager.getDataType()).toBe('clip');
    });

    it('should return "clips" for array', () => {
      clipboardManager.copy([{ id: 'clip-1' }]);
      expect(clipboardManager.getDataType()).toBe('clips');
    });
  });

  describe('getTimestamp', () => {
    it('should return null when clipboard is empty', () => {
      expect(clipboardManager.getTimestamp()).toBeNull();
    });

    it('should return timestamp after copy', () => {
      const beforeTime = Date.now();
      clipboardManager.copy({ id: 'clip-1' });
      const afterTime = Date.now();

      const timestamp = clipboardManager.getTimestamp();
      expect(timestamp).toBeGreaterThanOrEqual(beforeTime);
      expect(timestamp).toBeLessThanOrEqual(afterTime);
    });
  });
});
