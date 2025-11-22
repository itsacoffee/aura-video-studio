/**
 * Error Test Page
 * Used to test the ErrorBoundary functionality
 */

import { Button, Card, Text, Title1 } from '@fluentui/react-components';
import { useState } from 'react';

export function ErrorTestPage() {
  const [shouldThrow, setShouldThrow] = useState(false);

  if (shouldThrow) {
    // This will trigger the ErrorBoundary
    throw new Error('Test error from ErrorTestPage - This is intentional for testing');
  }

  const handleThrowError = () => {
    setShouldThrow(true);
  };

  const handleThrowAsyncError = () => {
    // Simulate async error
    setTimeout(() => {
      throw new Error('Test async error - This is intentional for testing');
    }, 100);
  };

  const handleThrowPromiseRejection = () => {
    // Simulate unhandled promise rejection
    Promise.reject(new Error('Test promise rejection - This is intentional for testing'));
  };

  return (
    <div style={{ padding: '40px', maxWidth: '800px', margin: '0 auto' }}>
      <Card>
        <div style={{ padding: '24px' }}>
          <Title1 style={{ marginBottom: '16px' }}>Error Boundary Testing</Title1>

          <Text block style={{ marginBottom: '24px' }}>
            Use these buttons to test different error scenarios and verify the ErrorBoundary catches
            them correctly.
          </Text>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            <Button appearance="primary" onClick={handleThrowError}>
              Throw Synchronous Error (Caught by ErrorBoundary)
            </Button>

            <Button appearance="secondary" onClick={handleThrowAsyncError}>
              Throw Async Error (Caught by window.onerror)
            </Button>

            <Button appearance="secondary" onClick={handleThrowPromiseRejection}>
              Throw Promise Rejection (Caught by window.onunhandledrejection)
            </Button>
          </div>

          <Text
            block
            size={300}
            style={{ marginTop: '24px', color: 'var(--colorNeutralForeground3)' }}
          >
            Note: After triggering an error, check the browser console and network tab to verify the
            error was logged to /api/logs/error endpoint.
          </Text>
        </div>
      </Card>
    </div>
  );
}

export default ErrorTestPage;
