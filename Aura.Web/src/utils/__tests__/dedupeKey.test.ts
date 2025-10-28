import { describe, it, expect } from 'vitest';
import { createDedupeKey } from '../dedupeKey';

describe('createDedupeKey', () => {
  it('should create key from method and URL', () => {
    const key = createDedupeKey('POST', '/api/jobs');
    expect(key).toBe('POST:/api/jobs');
  });

  it('should normalize method to uppercase', () => {
    const key = createDedupeKey('post', '/api/jobs');
    expect(key).toBe('POST:/api/jobs');
  });

  it('should include data hash when data is provided', () => {
    const data = { topic: 'test', audience: 'developers' };
    const key = createDedupeKey('POST', '/api/jobs', data);
    
    expect(key).toContain('POST:/api/jobs:');
    expect(key.split(':').length).toBe(3);
  });

  it('should create same hash for identical objects', () => {
    const data1 = { topic: 'test', audience: 'developers' };
    const data2 = { topic: 'test', audience: 'developers' };
    
    const key1 = createDedupeKey('POST', '/api/jobs', data1);
    const key2 = createDedupeKey('POST', '/api/jobs', data2);
    
    expect(key1).toBe(key2);
  });

  it('should create same hash for objects with different key order', () => {
    const data1 = { topic: 'test', audience: 'developers' };
    const data2 = { audience: 'developers', topic: 'test' };
    
    const key1 = createDedupeKey('POST', '/api/jobs', data1);
    const key2 = createDedupeKey('POST', '/api/jobs', data2);
    
    expect(key1).toBe(key2);
  });

  it('should create different hash for different objects', () => {
    const data1 = { topic: 'test1', audience: 'developers' };
    const data2 = { topic: 'test2', audience: 'developers' };
    
    const key1 = createDedupeKey('POST', '/api/jobs', data1);
    const key2 = createDedupeKey('POST', '/api/jobs', data2);
    
    expect(key1).not.toBe(key2);
  });

  it('should handle nested objects', () => {
    const data1 = { 
      topic: 'test',
      settings: { pacing: 'fast', density: 'high' }
    };
    const data2 = { 
      topic: 'test',
      settings: { pacing: 'fast', density: 'high' }
    };
    
    const key1 = createDedupeKey('POST', '/api/jobs', data1);
    const key2 = createDedupeKey('POST', '/api/jobs', data2);
    
    expect(key1).toBe(key2);
  });

  it('should handle arrays', () => {
    const data1 = { items: [1, 2, 3] };
    const data2 = { items: [1, 2, 3] };
    
    const key1 = createDedupeKey('POST', '/api/jobs', data1);
    const key2 = createDedupeKey('POST', '/api/jobs', data2);
    
    expect(key1).toBe(key2);
  });

  it('should create different keys for different methods', () => {
    const data = { topic: 'test' };
    
    const key1 = createDedupeKey('POST', '/api/jobs', data);
    const key2 = createDedupeKey('PUT', '/api/jobs', data);
    
    expect(key1).not.toBe(key2);
  });

  it('should create different keys for different URLs', () => {
    const data = { topic: 'test' };
    
    const key1 = createDedupeKey('POST', '/api/jobs', data);
    const key2 = createDedupeKey('POST', '/api/scripts', data);
    
    expect(key1).not.toBe(key2);
  });

  it('should handle null and undefined data', () => {
    const key1 = createDedupeKey('GET', '/api/jobs', null);
    const key2 = createDedupeKey('GET', '/api/jobs', undefined);
    const key3 = createDedupeKey('GET', '/api/jobs');
    
    expect(key1).toBe('GET:/api/jobs');
    expect(key2).toBe('GET:/api/jobs');
    expect(key3).toBe('GET:/api/jobs');
  });

  it('should handle primitive data types', () => {
    const key1 = createDedupeKey('POST', '/api/jobs', 'string-data');
    const key2 = createDedupeKey('POST', '/api/jobs', 123);
    const key3 = createDedupeKey('POST', '/api/jobs', true);
    
    expect(key1).toContain('POST:/api/jobs:');
    expect(key2).toContain('POST:/api/jobs:');
    expect(key3).toContain('POST:/api/jobs:');
    
    // Different primitive values should create different keys
    expect(key1).not.toBe(key2);
    expect(key2).not.toBe(key3);
  });
});
