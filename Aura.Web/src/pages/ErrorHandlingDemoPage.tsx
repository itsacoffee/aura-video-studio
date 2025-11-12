/**
 * Error Handling Demo Page
 * Demonstrates all error handling and recovery mechanisms
 */

import {
  Button,
  Card,
  makeStyles,
  shorthands,
  Tab,
  TabList,
  Text,
  tokens,
} from '@fluentui/react-components';
import {
  ArrowClockwise24Regular,
  ErrorCircle24Regular,
  Shield24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { ComponentErrorBoundary } from '../components/ErrorBoundary/ComponentErrorBoundary';
import {
  ErrorDisplay,
  createNetworkErrorDisplay,
  createAuthErrorDisplay,
  createValidationErrorDisplay,
} from '../components/ErrorBoundary/ErrorDisplay';
import { RouteErrorBoundary } from '../components/ErrorBoundary/RouteErrorBoundary';
import { ExampleValidatedForm } from '../components/forms/ExampleValidatedForm';

const useStyles = makeStyles({
  container: {
    ...shorthands.padding(tokens.spacingVerticalXXL),
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
  },
  title: {
    marginBottom: tokens.spacingVerticalM,
  },
  section: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  sectionTitle: {
    marginBottom: tokens.spacingVerticalL,
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap(tokens.spacingHorizontalS),
  },
  demoGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    ...shorthands.gap(tokens.spacingHorizontalL),
  },
  demoCard: {
    ...shorthands.padding(tokens.spacingVerticalL),
  },
  buttonGrid: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalM),
  },
  errorContainer: {
    marginTop: tokens.spacingVerticalL,
  },
});

// Component that throws an error on demand
function ErrorThrowingComponent({ shouldError }: { shouldError: boolean }) {
  if (shouldError) {
    throw new Error('This is a simulated render error!');
  }
  return <Text>Component rendered successfully ✓</Text>;
}

export function ErrorHandlingDemoPage() {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<string>('display');
  const [showNetworkError, setShowNetworkError] = useState(false);
  const [showAuthError, setShowAuthError] = useState(false);
  const [showValidationError, setShowValidationError] = useState(false);
  const [triggerRenderError, setTriggerRenderError] = useState(false);
  const [triggerComponentError, setTriggerComponentError] = useState(false);

  const handleRetry = () => {
    setShowNetworkError(false);
    setShowAuthError(false);
    setShowValidationError(false);
  };

  const handleResetRenderError = () => {
    setTriggerRenderError(false);
  };

  const handleResetComponentError = () => {
    setTriggerComponentError(false);
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text size={900} weight="bold" block className={styles.title}>
          Error Handling & Recovery Demo
        </Text>
        <Text size={400} block>
          Comprehensive demonstration of error handling mechanisms, error boundaries, and form
          validation
        </Text>
      </div>

      <TabList
        selectedValue={selectedTab}
        onTabSelect={(_, data) => setSelectedTab(data.value as string)}
      >
        <Tab value="display">Error Display</Tab>
        <Tab value="boundaries">Error Boundaries</Tab>
        <Tab value="forms">Form Validation</Tab>
      </TabList>

      {selectedTab === 'display' && (
        <div className={styles.section}>
          <div className={styles.sectionTitle}>
            <ErrorCircle24Regular />
            <Text size={700} weight="semibold">
              Error Display Components
            </Text>
          </div>

          <div className={styles.demoGrid}>
            <Card className={styles.demoCard}>
              <Text
                size={500}
                weight="semibold"
                block
                style={{ marginBottom: tokens.spacingVerticalM }}
              >
                Network Error
              </Text>
              <div className={styles.buttonGrid}>
                <Button
                  appearance="primary"
                  onClick={() => setShowNetworkError(true)}
                  disabled={showNetworkError}
                >
                  Trigger Network Error
                </Button>
                {showNetworkError && (
                  <div className={styles.errorContainer}>
                    <ErrorDisplay {...createNetworkErrorDisplay(handleRetry)} />
                  </div>
                )}
              </div>
            </Card>

            <Card className={styles.demoCard}>
              <Text
                size={500}
                weight="semibold"
                block
                style={{ marginBottom: tokens.spacingVerticalM }}
              >
                Authentication Error
              </Text>
              <div className={styles.buttonGrid}>
                <Button
                  appearance="primary"
                  onClick={() => setShowAuthError(true)}
                  disabled={showAuthError}
                >
                  Trigger Auth Error
                </Button>
                {showAuthError && (
                  <div className={styles.errorContainer}>
                    <ErrorDisplay {...createAuthErrorDisplay(handleRetry)} />
                  </div>
                )}
              </div>
            </Card>

            <Card className={styles.demoCard}>
              <Text
                size={500}
                weight="semibold"
                block
                style={{ marginBottom: tokens.spacingVerticalM }}
              >
                Validation Error
              </Text>
              <div className={styles.buttonGrid}>
                <Button
                  appearance="primary"
                  onClick={() => setShowValidationError(true)}
                  disabled={showValidationError}
                >
                  Trigger Validation Error
                </Button>
                {showValidationError && (
                  <div className={styles.errorContainer}>
                    <ErrorDisplay
                      {...createValidationErrorDisplay([
                        'Title must be at least 3 characters',
                        'Email format is invalid',
                        'Password must contain at least one number',
                      ])}
                      onDismiss={() => setShowValidationError(false)}
                    />
                  </div>
                )}
              </div>
            </Card>
          </div>
        </div>
      )}

      {selectedTab === 'boundaries' && (
        <div className={styles.section}>
          <div className={styles.sectionTitle}>
            <Shield24Regular />
            <Text size={700} weight="semibold">
              Error Boundaries
            </Text>
          </div>

          <div className={styles.demoGrid}>
            <Card className={styles.demoCard}>
              <Text
                size={500}
                weight="semibold"
                block
                style={{ marginBottom: tokens.spacingVerticalM }}
              >
                Route-Level Error Boundary
              </Text>
              <Text size={300} block style={{ marginBottom: tokens.spacingVerticalM }}>
                Catches errors in page components and shows recovery options
              </Text>
              <div className={styles.buttonGrid}>
                <Button
                  appearance="primary"
                  onClick={() => setTriggerRenderError(true)}
                  disabled={triggerRenderError}
                >
                  Trigger Render Error
                </Button>
                <RouteErrorBoundary onRetry={handleResetRenderError}>
                  <ErrorThrowingComponent shouldError={triggerRenderError} />
                </RouteErrorBoundary>
              </div>
            </Card>

            <Card className={styles.demoCard}>
              <Text
                size={500}
                weight="semibold"
                block
                style={{ marginBottom: tokens.spacingVerticalM }}
              >
                Component-Level Error Boundary
              </Text>
              <Text size={300} block style={{ marginBottom: tokens.spacingVerticalM }}>
                Isolates errors to specific components without crashing the page
              </Text>
              <div className={styles.buttonGrid}>
                <Button
                  appearance="primary"
                  onClick={() => setTriggerComponentError(true)}
                  disabled={triggerComponentError}
                >
                  Trigger Component Error
                </Button>
                <ComponentErrorBoundary
                  componentName="DemoComponent"
                  onRetry={handleResetComponentError}
                >
                  <ErrorThrowingComponent shouldError={triggerComponentError} />
                </ComponentErrorBoundary>
              </div>
            </Card>

            <Card className={styles.demoCard}>
              <Text
                size={500}
                weight="semibold"
                block
                style={{ marginBottom: tokens.spacingVerticalM }}
              >
                Recovery Features
              </Text>
              <Text size={300} block style={{ marginBottom: tokens.spacingVerticalS }}>
                ✓ Automatic error catching
              </Text>
              <Text size={300} block style={{ marginBottom: tokens.spacingVerticalS }}>
                ✓ User-friendly error messages
              </Text>
              <Text size={300} block style={{ marginBottom: tokens.spacingVerticalS }}>
                ✓ Retry mechanism
              </Text>
              <Text size={300} block style={{ marginBottom: tokens.spacingVerticalS }}>
                ✓ Crash recovery service
              </Text>
              <Text size={300} block>
                ✓ Detailed error logging
              </Text>
            </Card>
          </div>
        </div>
      )}

      {selectedTab === 'forms' && (
        <div className={styles.section}>
          <div className={styles.sectionTitle}>
            <ArrowClockwise24Regular />
            <Text size={700} weight="semibold">
              Form Validation with react-hook-form + zod
            </Text>
          </div>

          <Card className={styles.demoCard}>
            <Text size={300} block style={{ marginBottom: tokens.spacingVerticalL }}>
              This form demonstrates comprehensive validation including required fields, length
              constraints, format validation, and async submission with loading states.
            </Text>
            <ExampleValidatedForm
              onSubmit={async (data) => {
                console.info('Form submitted:', data);
                await new Promise((resolve) => setTimeout(resolve, 1000));
              }}
            />
          </Card>

          <Card className={styles.demoCard} style={{ marginTop: tokens.spacingVerticalL }}>
            <Text
              size={500}
              weight="semibold"
              block
              style={{ marginBottom: tokens.spacingVerticalM }}
            >
              Validation Features
            </Text>
            <ul style={{ marginLeft: tokens.spacingHorizontalL }}>
              <li>
                <Text>Required field validation</Text>
              </li>
              <li>
                <Text>Min/max length constraints</Text>
              </li>
              <li>
                <Text>Format validation (regex patterns)</Text>
              </li>
              <li>
                <Text>Range validation for numbers</Text>
              </li>
              <li>
                <Text>Inline error display</Text>
              </li>
              <li>
                <Text>Disabled state during submission</Text>
              </li>
              <li>
                <Text>Success feedback</Text>
              </li>
              <li>
                <Text>Form reset functionality</Text>
              </li>
            </ul>
          </Card>
        </div>
      )}
    </div>
  );
}

export default ErrorHandlingDemoPage;
