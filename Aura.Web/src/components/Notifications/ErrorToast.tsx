import { Toast, ToastTitle, ToastBody, ToastFooter, Button } from '@fluentui/react-components';
import { Dismiss24Regular, ArrowClockwise24Regular, Info24Regular } from '@fluentui/react-icons';
import type { ReactElement } from 'react';
import type { ErrorSeverity } from '../../services/errorReportingService';
import { severityToIntent } from './errorToastUtils';

export interface ErrorToastAction {
  label: string;
  handler: () => void | Promise<void>;
  icon?: ReactElement;
}

export interface ErrorToastProps {
  title: string;
  message: string;
  severity: ErrorSeverity;
  canRetry?: boolean;
  onRetry?: () => void | Promise<void>;
  actions?: ErrorToastAction[];
  onDismiss?: () => void;
  correlationId?: string;
}

export function ErrorToast({
  title,
  message,
  severity,
  canRetry = false,
  onRetry,
  actions = [],
  onDismiss,
  correlationId,
}: ErrorToastProps) {
  return (
    <Toast>
      <ToastTitle
        action={
          onDismiss ? (
            <Button
              appearance="transparent"
              icon={<Dismiss24Regular />}
              onClick={onDismiss}
              size="small"
            />
          ) : undefined
        }
      >
        {title}
      </ToastTitle>
      <ToastBody>
        {message}
        {correlationId && (
          <div style={{ marginTop: '8px', fontSize: '12px', opacity: 0.7 }}>
            ID: {correlationId}
          </div>
        )}
      </ToastBody>
      {(canRetry || actions.length > 0) && (
        <ToastFooter>
          <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
            {canRetry && onRetry && (
              <Button
                appearance="primary"
                size="small"
                icon={<ArrowClockwise24Regular />}
                onClick={onRetry}
              >
                Retry
              </Button>
            )}
            {actions.map((action, index) => (
              <Button
                key={index}
                appearance="subtle"
                size="small"
                icon={action.icon}
                onClick={action.handler}
              >
                {action.label}
              </Button>
            ))}
          </div>
        </ToastFooter>
      )}
    </Toast>
  );
}

export interface ErrorToastOptions {
  title: string;
  message: string;
  severity?: ErrorSeverity;
  canRetry?: boolean;
  onRetry?: () => void | Promise<void>;
  actions?: ErrorToastAction[];
  duration?: number;
  correlationId?: string;
}

export function createErrorToast(options: ErrorToastOptions) {
  return {
    intent: severityToIntent(options.severity || 'error'),
    timeout: options.duration,
    content: (
      <ErrorToast
        title={options.title}
        message={options.message}
        severity={options.severity || 'error'}
        canRetry={options.canRetry}
        onRetry={options.onRetry}
        actions={options.actions}
        correlationId={options.correlationId}
      />
    ),
  };
}

export interface ErrorToastWithDetailsProps extends ErrorToastProps {
  showDetailsLink?: boolean;
  onShowDetails?: () => void;
}

export function ErrorToastWithDetails({
  showDetailsLink = true,
  onShowDetails,
  ...props
}: ErrorToastWithDetailsProps) {
  const detailsAction: ErrorToastAction = {
    label: 'View Details',
    icon: <Info24Regular />,
    handler: onShowDetails || (() => {}),
  };

  const actionsWithDetails =
    showDetailsLink && onShowDetails ? [...(props.actions || []), detailsAction] : props.actions;

  return <ErrorToast {...props} actions={actionsWithDetails} />;
}
