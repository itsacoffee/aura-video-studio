import { describe, it, expect } from 'vitest';

// Define engine state types
type EngineStatus = 'idle' | 'starting' | 'running' | 'stopping' | 'stopped' | 'error';

interface EngineState {
  id: string;
  status: EngineStatus;
  progress: number;
  error: string | null;
  pid: number | null;
}

type EngineAction =
  | { type: 'START_ENGINE'; payload: string }
  | { type: 'ENGINE_STARTED'; payload: { id: string; pid: number } }
  | { type: 'ENGINE_PROGRESS'; payload: { id: string; progress: number } }
  | { type: 'ENGINE_RUNNING'; payload: string }
  | { type: 'STOP_ENGINE'; payload: string }
  | { type: 'ENGINE_STOPPED'; payload: string }
  | { type: 'ENGINE_ERROR'; payload: { id: string; error: string } }
  | { type: 'RESET_ENGINE'; payload: string };

// Simple engine reducer
function engineReducer(state: EngineState, action: EngineAction): EngineState {
  switch (action.type) {
    case 'START_ENGINE':
      return { ...state, status: 'starting', error: null, progress: 0 };
    case 'ENGINE_STARTED':
      if (action.payload.id !== state.id) return state;
      return { ...state, status: 'running', pid: action.payload.pid };
    case 'ENGINE_PROGRESS':
      if (action.payload.id !== state.id) return state;
      return { ...state, progress: action.payload.progress };
    case 'ENGINE_RUNNING':
      if (action.payload !== state.id) return state;
      return { ...state, status: 'running', progress: 100 };
    case 'STOP_ENGINE':
      return { ...state, status: 'stopping' };
    case 'ENGINE_STOPPED':
      if (action.payload !== state.id) return state;
      return { ...state, status: 'stopped', pid: null, progress: 0 };
    case 'ENGINE_ERROR':
      if (action.payload.id !== state.id) return state;
      return { ...state, status: 'error', error: action.payload.error };
    case 'RESET_ENGINE':
      if (action.payload !== state.id) return state;
      return { ...state, status: 'idle', error: null, progress: 0, pid: null };
    default:
      return state;
  }
}

describe('Engine State Machine', () => {
  const initialState: EngineState = {
    id: 'test-engine',
    status: 'idle',
    progress: 0,
    error: null,
    pid: null,
  };

  describe('Starting engine', () => {
    it('should transition from idle to starting', () => {
      const state = engineReducer(initialState, {
        type: 'START_ENGINE',
        payload: 'test-engine',
      });
      expect(state.status).toBe('starting');
      expect(state.error).toBeNull();
      expect(state.progress).toBe(0);
    });

    it('should transition to running when started', () => {
      let state = engineReducer(initialState, {
        type: 'START_ENGINE',
        payload: 'test-engine',
      });
      state = engineReducer(state, {
        type: 'ENGINE_STARTED',
        payload: { id: 'test-engine', pid: 12345 },
      });
      expect(state.status).toBe('running');
      expect(state.pid).toBe(12345);
    });

    it('should handle error during startup', () => {
      let state = engineReducer(initialState, {
        type: 'START_ENGINE',
        payload: 'test-engine',
      });
      state = engineReducer(state, {
        type: 'ENGINE_ERROR',
        payload: { id: 'test-engine', error: 'Failed to start: port already in use' },
      });
      expect(state.status).toBe('error');
      expect(state.error).toBe('Failed to start: port already in use');
    });
  });

  describe('Progress tracking', () => {
    it('should update progress during startup', () => {
      let state = engineReducer(initialState, {
        type: 'START_ENGINE',
        payload: 'test-engine',
      });

      state = engineReducer(state, {
        type: 'ENGINE_PROGRESS',
        payload: { id: 'test-engine', progress: 25 },
      });
      expect(state.progress).toBe(25);

      state = engineReducer(state, {
        type: 'ENGINE_PROGRESS',
        payload: { id: 'test-engine', progress: 50 },
      });
      expect(state.progress).toBe(50);

      state = engineReducer(state, {
        type: 'ENGINE_PROGRESS',
        payload: { id: 'test-engine', progress: 75 },
      });
      expect(state.progress).toBe(75);
    });

    it('should set progress to 100 when running', () => {
      let state = engineReducer(initialState, {
        type: 'START_ENGINE',
        payload: 'test-engine',
      });
      state = engineReducer(state, {
        type: 'ENGINE_RUNNING',
        payload: 'test-engine',
      });
      expect(state.progress).toBe(100);
      expect(state.status).toBe('running');
    });

    it('should reset progress when stopped', () => {
      let state: EngineState = { ...initialState, status: 'running', progress: 100 };
      state = engineReducer(state, {
        type: 'STOP_ENGINE',
        payload: 'test-engine',
      });
      state = engineReducer(state, {
        type: 'ENGINE_STOPPED',
        payload: 'test-engine',
      });
      expect(state.progress).toBe(0);
    });
  });

  describe('Stopping engine', () => {
    it('should transition from running to stopping', () => {
      const runningState: EngineState = {
        ...initialState,
        status: 'running',
        pid: 12345,
      };
      const state = engineReducer(runningState, {
        type: 'STOP_ENGINE',
        payload: 'test-engine',
      });
      expect(state.status).toBe('stopping');
    });

    it('should transition to stopped when complete', () => {
      let state: EngineState = {
        ...initialState,
        status: 'running',
        pid: 12345,
      };
      state = engineReducer(state, {
        type: 'STOP_ENGINE',
        payload: 'test-engine',
      });
      state = engineReducer(state, {
        type: 'ENGINE_STOPPED',
        payload: 'test-engine',
      });
      expect(state.status).toBe('stopped');
      expect(state.pid).toBeNull();
    });
  });

  describe('Error handling', () => {
    it('should transition to error state from any state', () => {
      const states: EngineStatus[] = ['idle', 'starting', 'running', 'stopping'];

      states.forEach((status) => {
        const state: EngineState = { ...initialState, status };
        const errorState = engineReducer(state, {
          type: 'ENGINE_ERROR',
          payload: { id: 'test-engine', error: 'Test error' },
        });
        expect(errorState.status).toBe('error');
        expect(errorState.error).toBe('Test error');
      });
    });

    it('should clear error on reset', () => {
      const errorState: EngineState = {
        ...initialState,
        status: 'error',
        error: 'Previous error',
      };
      const state = engineReducer(errorState, {
        type: 'RESET_ENGINE',
        payload: 'test-engine',
      });
      expect(state.status).toBe('idle');
      expect(state.error).toBeNull();
    });

    it('should allow restart after error', () => {
      let state: EngineState = {
        ...initialState,
        status: 'error',
        error: 'Failed to start',
      };

      // Reset
      state = engineReducer(state, {
        type: 'RESET_ENGINE',
        payload: 'test-engine',
      });
      expect(state.status).toBe('idle');

      // Start again
      state = engineReducer(state, {
        type: 'START_ENGINE',
        payload: 'test-engine',
      });
      expect(state.status).toBe('starting');
      expect(state.error).toBeNull();
    });
  });

  describe('Complete lifecycle', () => {
    it('should follow successful lifecycle: idle → starting → running → stopping → stopped', () => {
      let state = initialState;
      expect(state.status).toBe('idle');

      // Start
      state = engineReducer(state, {
        type: 'START_ENGINE',
        payload: 'test-engine',
      });
      expect(state.status).toBe('starting');

      // Started
      state = engineReducer(state, {
        type: 'ENGINE_STARTED',
        payload: { id: 'test-engine', pid: 99999 },
      });
      expect(state.status).toBe('running');
      expect(state.pid).toBe(99999);

      // Stop
      state = engineReducer(state, {
        type: 'STOP_ENGINE',
        payload: 'test-engine',
      });
      expect(state.status).toBe('stopping');

      // Stopped
      state = engineReducer(state, {
        type: 'ENGINE_STOPPED',
        payload: 'test-engine',
      });
      expect(state.status).toBe('stopped');
      expect(state.pid).toBeNull();
    });

    it('should handle error recovery lifecycle: idle → starting → error → reset → starting → running', () => {
      let state = initialState;

      // Start
      state = engineReducer(state, {
        type: 'START_ENGINE',
        payload: 'test-engine',
      });
      expect(state.status).toBe('starting');

      // Error
      state = engineReducer(state, {
        type: 'ENGINE_ERROR',
        payload: { id: 'test-engine', error: 'Port conflict' },
      });
      expect(state.status).toBe('error');

      // Reset
      state = engineReducer(state, {
        type: 'RESET_ENGINE',
        payload: 'test-engine',
      });
      expect(state.status).toBe('idle');

      // Retry
      state = engineReducer(state, {
        type: 'START_ENGINE',
        payload: 'test-engine',
      });
      expect(state.status).toBe('starting');

      // Success
      state = engineReducer(state, {
        type: 'ENGINE_STARTED',
        payload: { id: 'test-engine', pid: 54321 },
      });
      expect(state.status).toBe('running');
    });
  });

  describe('Edge cases', () => {
    it('should ignore actions for different engine IDs', () => {
      const state = engineReducer(initialState, {
        type: 'ENGINE_STARTED',
        payload: { id: 'different-engine', pid: 12345 },
      });
      expect(state.status).toBe('idle'); // Should not change
      expect(state.pid).toBeNull();
    });

    it('should handle rapid state changes', () => {
      let state = initialState;

      // Rapid fire actions
      state = engineReducer(state, { type: 'START_ENGINE', payload: 'test-engine' });
      state = engineReducer(state, {
        type: 'ENGINE_PROGRESS',
        payload: { id: 'test-engine', progress: 10 },
      });
      state = engineReducer(state, {
        type: 'ENGINE_PROGRESS',
        payload: { id: 'test-engine', progress: 20 },
      });
      state = engineReducer(state, {
        type: 'ENGINE_PROGRESS',
        payload: { id: 'test-engine', progress: 30 },
      });
      state = engineReducer(state, {
        type: 'ENGINE_STARTED',
        payload: { id: 'test-engine', pid: 111 },
      });

      expect(state.status).toBe('running');
      expect(state.progress).toBe(30);
      expect(state.pid).toBe(111);
    });
  });
});
