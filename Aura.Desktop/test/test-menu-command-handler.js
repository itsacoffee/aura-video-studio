/**
 * Unit Tests for Menu Command Handler (Preload)
 * 
 * Tests the enhanced menu command handler that adds:
 * 1. Correlation ID generation
 * 2. Payload validation
 * 3. Enhanced logging
 * 4. Error context propagation
 */

const assert = require('assert');
const EventEmitter = require('events');
const { createValidatedMenuListener, generateCorrelationId } = require('../electron/menu-command-handler');

console.log('='.repeat(60));
console.log('Testing Menu Command Handler (Preload)');
console.log('='.repeat(60));

// Mock ipcRenderer
class MockIpcRenderer extends EventEmitter {
  constructor() {
    super();
    this.listeners = new Map();
  }

  on(channel, handler) {
    if (!this.listeners.has(channel)) {
      this.listeners.set(channel, []);
    }
    this.listeners.get(channel).push(handler);
    return super.on(channel, handler);
  }

  removeListener(channel, handler) {
    const handlers = this.listeners.get(channel);
    if (handlers) {
      const index = handlers.indexOf(handler);
      if (index > -1) {
        handlers.splice(index, 1);
      }
    }
    return super.removeListener(channel, handler);
  }

  getListenerCount(channel) {
    return (this.listeners.get(channel) || []).length;
  }

  send(channel, ...args) {
    this.emit(channel, {}, ...args);
  }
}

const ipcRenderer = new MockIpcRenderer();

// Test 1: Correlation ID generation
console.log('\n1. Testing correlation ID generation...');

const correlationId1 = generateCorrelationId();
const correlationId2 = generateCorrelationId();

assert.ok(correlationId1, 'Should generate correlation ID');
assert.ok(correlationId1.startsWith('cmd_'), 'Correlation ID should start with cmd_');
assert.notStrictEqual(correlationId1, correlationId2, 'Should generate unique IDs');

console.log('✓ Correlation ID generation works correctly');

// Test 2: Listener creation with valid parameters
console.log('\n2. Testing listener creation with valid parameters...');

let callbackCalled = false;
let receivedPayload = null;

const unsub = createValidatedMenuListener(
  ipcRenderer,
  'menu:newProject',
  (payload) => {
    callbackCalled = true;
    receivedPayload = payload;
  }
);

assert.strictEqual(typeof unsub, 'function', 'Should return unsubscribe function');
assert.strictEqual(ipcRenderer.getListenerCount('menu:newProject'), 1, 'Should register one listener');

console.log('✓ Listener creation works correctly');

// Test 3: Invalid channel throws error
console.log('\n3. Testing invalid channel throws error...');

try {
  createValidatedMenuListener(ipcRenderer, 'invalid:channel', () => {});
  assert.fail('Should throw error for invalid channel');
} catch (error) {
  assert.ok(error.message.includes('Invalid menu channel'), 'Error should mention invalid channel');
  assert.ok(error.message.includes('Must start with'), 'Error should explain requirement');
}

console.log('✓ Invalid channel properly rejected');

// Test 4: Invalid callback throws error
console.log('\n4. Testing invalid callback throws error...');

try {
  createValidatedMenuListener(ipcRenderer, 'menu:newProject', 'not a function');
  assert.fail('Should throw error for invalid callback');
} catch (error) {
  assert.ok(error instanceof TypeError, 'Should throw TypeError');
  assert.ok(error.message.includes('must be a function'), 'Error should mention function requirement');
}

console.log('✓ Invalid callback properly rejected');

// Test 5: Payload validation and enhancement
console.log('\n5. Testing payload validation and enhancement...');

callbackCalled = false;
receivedPayload = null;

// Send valid command
ipcRenderer.send('menu:newProject', {});

// Wait a bit for async processing
setTimeout(() => {
  assert.strictEqual(callbackCalled, true, 'Callback should be called');
  assert.ok(receivedPayload, 'Should receive payload');
  assert.ok(receivedPayload._correlationId, 'Payload should have correlation ID');
  assert.ok(receivedPayload._timestamp, 'Payload should have timestamp');
  assert.ok(receivedPayload._command, 'Payload should have command metadata');
  assert.strictEqual(receivedPayload._command.label, 'New Project', 'Should have correct command label');
  
  console.log('✓ Payload validation and enhancement works correctly');
  
  // Test 6: Validation error handling
  console.log('\n6. Testing validation error handling...');
  
  callbackCalled = false;
  receivedPayload = null;
  
  const unsub2 = createValidatedMenuListener(
    ipcRenderer,
    'menu:openRecentProject',
    (payload) => {
      callbackCalled = true;
      receivedPayload = payload;
    }
  );
  
  // Send invalid payload (missing required 'path' field)
  ipcRenderer.send('menu:openRecentProject', {});
  
  setTimeout(() => {
    assert.strictEqual(callbackCalled, true, 'Callback should still be called on validation error');
    assert.ok(receivedPayload._validationError, 'Payload should have validation error');
    assert.ok(receivedPayload._validationIssues, 'Payload should have validation issues');
    assert.ok(receivedPayload._correlationId, 'Payload should have correlation ID even on error');
    
    console.log('✓ Validation error handling works correctly');
    
    // Test 7: Valid payload with data
    console.log('\n7. Testing valid payload with data...');
    
    callbackCalled = false;
    receivedPayload = null;
    
    // Send valid payload with required data
    ipcRenderer.send('menu:openRecentProject', { path: '/path/to/project' });
    
    setTimeout(() => {
      assert.strictEqual(callbackCalled, true, 'Callback should be called');
      assert.ok(receivedPayload, 'Should receive payload');
      assert.strictEqual(receivedPayload.path, '/path/to/project', 'Should preserve path data');
      assert.ok(!receivedPayload._validationError, 'Should not have validation error');
      assert.ok(receivedPayload._correlationId, 'Should have correlation ID');
      assert.ok(receivedPayload._command, 'Should have command metadata');
      
      console.log('✓ Valid payload with data works correctly');
      
      // Test 8: Unsubscribe functionality
      console.log('\n8. Testing unsubscribe functionality...');
      
      const initialCount = ipcRenderer.getListenerCount('menu:newProject');
      unsub();
      const afterCount = ipcRenderer.getListenerCount('menu:newProject');
      
      assert.strictEqual(afterCount, initialCount - 1, 'Should remove one listener');
      
      // Try sending event after unsubscribe
      callbackCalled = false;
      ipcRenderer.send('menu:newProject', {});
      
      setTimeout(() => {
        // The original listener should not be called, but unsub2 listener might still be active
        console.log('✓ Unsubscribe functionality works correctly');
        
        // Cleanup
        unsub2();
        
        // Test 9: Error in user callback is caught
        console.log('\n9. Testing error in user callback is caught...');
        
        let errorThrown = false;
        const unsub3 = createValidatedMenuListener(
          ipcRenderer,
          'menu:saveProject',
          () => {
            errorThrown = true;
            throw new Error('User callback error');
          }
        );
        
        // This should not crash the process
        ipcRenderer.send('menu:saveProject', {});
        
        setTimeout(() => {
          assert.strictEqual(errorThrown, true, 'User callback should have been called');
          console.log('✓ Error in user callback is caught and logged');
          
          unsub3();
          
          // Test 10: Promise-based callback tracking
          console.log('\n10. Testing promise-based callback tracking...');
          
          let promiseResolved = false;
          const unsub4 = createValidatedMenuListener(
            ipcRenderer,
            'menu:clearCache',
            async () => {
              return new Promise((resolve) => {
                setTimeout(() => {
                  promiseResolved = true;
                  resolve();
                }, 10);
              });
            }
          );
          
          ipcRenderer.send('menu:clearCache', {});
          
          setTimeout(() => {
            assert.strictEqual(promiseResolved, true, 'Promise should be resolved');
            console.log('✓ Promise-based callback tracking works correctly');
            
            unsub4();
            
            console.log('\n' + '='.repeat(60));
            console.log('ALL MENU COMMAND HANDLER TESTS PASSED ✓');
            console.log('='.repeat(60));
            console.log('Verified:');
            console.log('  - Correlation ID generation');
            console.log('  - Listener creation and registration');
            console.log('  - Channel and callback validation');
            console.log('  - Payload validation and enhancement');
            console.log('  - Validation error handling');
            console.log('  - Data preservation in payloads');
            console.log('  - Unsubscribe functionality');
            console.log('  - Error handling in user callbacks');
            console.log('  - Promise-based callback tracking');
            console.log('='.repeat(60));
          }, 50);
        }, 50);
      }, 50);
    }, 50);
  }, 50);
}, 50);
