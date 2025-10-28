/**
 * Tests for Cancellable Requests
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import MockAdapter from 'axios-mock-adapter';
import apiClient from '../apiClient';
import {
  getCancellable,
  postCancellable,
  putCancellable,
  deleteCancellable,
  patchCancellable,
  isAbortError,
} from '../cancellableRequests';

describe('Cancellable Requests', () => {
  let mockAxios: MockAdapter;

  beforeEach(() => {
    mockAxios = new MockAdapter(apiClient);
  });

  afterEach(() => {
    mockAxios.reset();
    mockAxios.restore();
  });

  describe('getCancellable', () => {
    it('should make a successful GET request', async () => {
      const url = '/api/test';
      const responseData = { message: 'success' };

      mockAxios.onGet(url).reply(200, responseData);

      const { promise } = getCancellable(url);
      const result = await promise;

      expect(result).toEqual(responseData);
    });

    it('should provide a cancel function for GET request', () => {
      const url = '/api/test';
      
      mockAxios.onGet(url).reply(200, { message: 'success' });

      const { cancel } = getCancellable(url);

      // Should have a cancel function
      expect(cancel).toBeDefined();
      expect(typeof cancel).toBe('function');
      
      // Should be able to call cancel without errors
      expect(() => cancel()).not.toThrow();
    });
  });

  describe('postCancellable', () => {
    it('should make a successful POST request', async () => {
      const url = '/api/test';
      const requestData = { name: 'test' };
      const responseData = { id: '123', ...requestData };

      mockAxios.onPost(url, requestData).reply(200, responseData);

      const { promise } = postCancellable(url, requestData);
      const result = await promise;

      expect(result).toEqual(responseData);
    });

    it('should provide a cancel function for POST request', () => {
      const url = '/api/test';
      const requestData = { name: 'test' };

      mockAxios.onPost(url).reply(200, { id: '123' });

      const { cancel } = postCancellable(url, requestData);

      expect(cancel).toBeDefined();
      expect(typeof cancel).toBe('function');
      expect(() => cancel()).not.toThrow();
    });
  });

  describe('putCancellable', () => {
    it('should make a successful PUT request', async () => {
      const url = '/api/test/123';
      const requestData = { name: 'updated' };
      const responseData = { id: '123', ...requestData };

      mockAxios.onPut(url, requestData).reply(200, responseData);

      const { promise } = putCancellable(url, requestData);
      const result = await promise;

      expect(result).toEqual(responseData);
    });

    it('should provide a cancel function for PUT request', () => {
      const url = '/api/test/123';
      const requestData = { name: 'updated' };

      mockAxios.onPut(url).reply(200, { id: '123' });

      const { cancel } = putCancellable(url, requestData);

      expect(cancel).toBeDefined();
      expect(typeof cancel).toBe('function');
      expect(() => cancel()).not.toThrow();
    });
  });

  describe('deleteCancellable', () => {
    it('should make a successful DELETE request', async () => {
      const url = '/api/test/123';
      const responseData = { success: true };

      mockAxios.onDelete(url).reply(200, responseData);

      const { promise } = deleteCancellable(url);
      const result = await promise;

      expect(result).toEqual(responseData);
    });

    it('should provide a cancel function for DELETE request', () => {
      const url = '/api/test/123';

      mockAxios.onDelete(url).reply(200, { success: true });

      const { cancel } = deleteCancellable(url);

      expect(cancel).toBeDefined();
      expect(typeof cancel).toBe('function');
      expect(() => cancel()).not.toThrow();
    });
  });

  describe('patchCancellable', () => {
    it('should make a successful PATCH request', async () => {
      const url = '/api/test/123';
      const requestData = { name: 'patched' };
      const responseData = { id: '123', ...requestData };

      mockAxios.onPatch(url, requestData).reply(200, responseData);

      const { promise } = patchCancellable(url, requestData);
      const result = await promise;

      expect(result).toEqual(responseData);
    });

    it('should provide a cancel function for PATCH request', () => {
      const url = '/api/test/123';
      const requestData = { name: 'patched' };

      mockAxios.onPatch(url).reply(200, { id: '123' });

      const { cancel } = patchCancellable(url, requestData);

      expect(cancel).toBeDefined();
      expect(typeof cancel).toBe('function');
      expect(() => cancel()).not.toThrow();
    });
  });

  describe('isAbortError', () => {
    it('should return true for AbortError', () => {
      const error = new Error('Request aborted');
      error.name = 'AbortError';

      expect(isAbortError(error)).toBe(true);
    });

    it('should return true for CanceledError', () => {
      const error = new Error('Request canceled');
      error.name = 'CanceledError';

      expect(isAbortError(error)).toBe(true);
    });

    it('should return false for other errors', () => {
      const error = new Error('Network error');

      expect(isAbortError(error)).toBe(false);
    });

    it('should return false for non-Error objects', () => {
      expect(isAbortError('error string')).toBe(false);
      expect(isAbortError({ message: 'error' })).toBe(false);
      expect(isAbortError(null)).toBe(false);
      expect(isAbortError(undefined)).toBe(false);
    });
  });
});
