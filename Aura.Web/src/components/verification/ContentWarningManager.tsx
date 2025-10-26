import {
  Warning20Regular as AlertTriangle,
  Dismiss20Regular as X,
  Info20Regular as Info,
  ErrorCircle20Regular as AlertCircle,
} from '@fluentui/react-icons';
import { useState } from 'react';

interface Warning {
  id: string;
  type: 'error' | 'warning' | 'info';
  message: string;
  timestamp: Date;
  dismissible: boolean;
}

interface ContentWarningManagerProps {
  warnings: string[];
  riskLevel?: string;
  onDismiss?: (warningId: string) => void;
  showTimestamp?: boolean;
}

export const ContentWarningManager: React.FC<ContentWarningManagerProps> = ({
  warnings: initialWarnings,
  riskLevel,
  onDismiss,
  showTimestamp = false,
}) => {
  const [warnings, setWarnings] = useState<Warning[]>(
    initialWarnings.map((msg, idx) => ({
      id: `warning-${idx}`,
      type: determineWarningType(msg, riskLevel),
      message: msg,
      timestamp: new Date(),
      dismissible: true,
    }))
  );

  const handleDismiss = (warningId: string) => {
    setWarnings(warnings.filter((w) => w.id !== warningId));
    onDismiss?.(warningId);
  };

  const getWarningIcon = (type: Warning['type']) => {
    switch (type) {
      case 'error':
        return <AlertCircle className="h-5 w-5 flex-shrink-0" />;
      case 'warning':
        return <AlertTriangle className="h-5 w-5 flex-shrink-0" />;
      case 'info':
        return <Info className="h-5 w-5 flex-shrink-0" />;
    }
  };

  const getWarningStyles = (type: Warning['type']) => {
    switch (type) {
      case 'error':
        return {
          container: 'bg-red-50 border-red-200 text-red-800',
          icon: 'text-red-600',
          dismissBtn: 'text-red-400 hover:text-red-600 hover:bg-red-100',
        };
      case 'warning':
        return {
          container: 'bg-yellow-50 border-yellow-200 text-yellow-800',
          icon: 'text-yellow-600',
          dismissBtn: 'text-yellow-400 hover:text-yellow-600 hover:bg-yellow-100',
        };
      case 'info':
        return {
          container: 'bg-blue-50 border-blue-200 text-blue-800',
          icon: 'text-blue-600',
          dismissBtn: 'text-blue-400 hover:text-blue-600 hover:bg-blue-100',
        };
    }
  };

  if (warnings.length === 0) {
    return (
      <div className="bg-green-50 border border-green-200 rounded-lg p-4">
        <div className="flex items-center gap-2 text-green-800">
          <Info className="h-5 w-5 text-green-600" />
          <p className="text-sm font-medium">No warnings detected</p>
        </div>
        <p className="text-xs text-green-700 mt-1 ml-7">
          Content appears to be free of verification issues.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold">Content Warnings</h3>
        {riskLevel && (
          <span
            className={`px-2 py-1 rounded text-xs font-semibold ${getRiskLevelColor(riskLevel)}`}
          >
            {riskLevel} Risk
          </span>
        )}
      </div>

      <div className="space-y-2">
        {warnings.map((warning) => {
          const styles = getWarningStyles(warning.type);

          return (
            <div key={warning.id} className={`border rounded-lg p-3 ${styles.container}`}>
              <div className="flex items-start gap-3">
                <div className={styles.icon}>{getWarningIcon(warning.type)}</div>

                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium">{warning.message}</p>
                  {showTimestamp && (
                    <p className="text-xs opacity-75 mt-1">
                      {warning.timestamp.toLocaleTimeString()}
                    </p>
                  )}
                </div>

                {warning.dismissible && (
                  <button
                    onClick={() => handleDismiss(warning.id)}
                    className={`p-1 rounded ${styles.dismissBtn}`}
                    title="Dismiss warning"
                  >
                    <X className="h-4 w-4" />
                  </button>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {/* Summary */}
      <div className="text-sm text-gray-600">
        <p>
          {warnings.filter((w) => w.type === 'error').length > 0 && (
            <span className="text-red-600 font-medium">
              {warnings.filter((w) => w.type === 'error').length} critical issue(s)
            </span>
          )}
          {warnings.filter((w) => w.type === 'error').length > 0 &&
            warnings.filter((w) => w.type === 'warning').length > 0 &&
            ', '}
          {warnings.filter((w) => w.type === 'warning').length > 0 && (
            <span className="text-yellow-600 font-medium">
              {warnings.filter((w) => w.type === 'warning').length} warning(s)
            </span>
          )}
          {(warnings.filter((w) => w.type === 'error').length > 0 ||
            warnings.filter((w) => w.type === 'warning').length > 0) &&
            warnings.filter((w) => w.type === 'info').length > 0 &&
            ', '}
          {warnings.filter((w) => w.type === 'info').length > 0 && (
            <span className="text-blue-600 font-medium">
              {warnings.filter((w) => w.type === 'info').length} info message(s)
            </span>
          )}
        </p>
      </div>
    </div>
  );
};

// Configuration for warning type patterns
const WARNING_TYPE_PATTERNS = {
  error: [/\bfalse\b/i, /\bdisputed\b/i, /\bcritical\b/i, /\bverified as false\b/i],
  warning: [
    /\blow confidence\b/i,
    /\bbelow threshold\b/i,
    /\brisk detected\b/i,
    /\bissue\(s\)\b/i,
    /\bunverified\b/i,
  ],
};

function determineWarningType(message: string, riskLevel?: string): Warning['type'] {
  // Check risk level first for explicit categorization
  const normalizedRiskLevel = riskLevel?.toLowerCase();
  if (normalizedRiskLevel === 'critical') {
    return 'error';
  }
  if (normalizedRiskLevel === 'high' || normalizedRiskLevel === 'medium') {
    return 'warning';
  }

  // Check message patterns
  if (WARNING_TYPE_PATTERNS.error.some((pattern) => pattern.test(message))) {
    return 'error';
  }
  if (WARNING_TYPE_PATTERNS.warning.some((pattern) => pattern.test(message))) {
    return 'warning';
  }

  // Default to info
  return 'info';
}

function getRiskLevelColor(level: string): string {
  switch (level.toLowerCase()) {
    case 'critical':
      return 'bg-red-100 text-red-800';
    case 'high':
      return 'bg-orange-100 text-orange-800';
    case 'medium':
      return 'bg-yellow-100 text-yellow-800';
    case 'low':
      return 'bg-green-100 text-green-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
}
