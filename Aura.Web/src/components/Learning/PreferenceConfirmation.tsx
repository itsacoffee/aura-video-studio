import React, { useState } from 'react';
import type { InferredPreference } from '../../types/learning';
import { ConfidenceIndicator } from './ConfidenceIndicator';

interface PreferenceConfirmationProps {
  preference: InferredPreference;
  onConfirm: (preferenceId: string, isCorrect: boolean, correctedValue?: unknown) => void;
  onDismiss: () => void;
}

export const PreferenceConfirmation: React.FC<PreferenceConfirmationProps> = ({
  preference,
  onConfirm,
  onDismiss,
}) => {
  const [showCorrection, setShowCorrection] = useState(false);
  const [correctedValue, setCorrectedValue] = useState<string>('');

  const handleConfirm = () => {
    onConfirm(preference.preferenceId, true);
  };

  const handleReject = () => {
    if (showCorrection && correctedValue) {
      onConfirm(preference.preferenceId, false, correctedValue);
    } else {
      setShowCorrection(true);
    }
  };

  const formatValue = (value: unknown): string => {
    if (Array.isArray(value)) {
      return value.join(', ');
    }
    return String(value);
  };

  return (
    <div className="border border-blue-200 bg-blue-50 rounded-lg p-4 mb-4">
      <div className="flex items-start gap-3">
        <div className="text-2xl">🤔</div>
        <div className="flex-1">
          <h4 className="font-medium text-gray-900 mb-2">We&apos;ve noticed a preference</h4>
          <p className="text-sm text-gray-700 mb-3">
            Based on your decisions, it seems you prefer:
          </p>
          <div className="bg-white rounded p-3 mb-3">
            <p className="text-sm">
              <span className="font-medium capitalize">{preference.preferenceName}:</span>{' '}
              <span className="text-gray-700">{formatValue(preference.preferenceValue)}</span>
            </p>
            <div className="mt-2">
              <ConfidenceIndicator
                confidence={preference.confidence}
                label="Confidence"
                size="sm"
              />
            </div>
            <p className="text-xs text-gray-500 mt-2">
              Based on {preference.basedOnDecisions} decision
              {preference.basedOnDecisions !== 1 ? 's' : ''}
            </p>
          </div>

          {preference.conflictsWith && (
            <div className="bg-yellow-50 border border-yellow-200 rounded p-2 mb-3">
              <p className="text-xs text-yellow-800">
                ⚠️ Conflicts with: {preference.conflictsWith}
              </p>
            </div>
          )}

          {showCorrection && (
            <div className="mb-3">
              <label
                htmlFor="correction-value"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                What should the value be?
              </label>
              <input
                id="correction-value"
                type="text"
                value={correctedValue}
                onChange={(e) => setCorrectedValue(e.target.value)}
                placeholder="Enter correct value"
                className="w-full border border-gray-300 rounded px-3 py-2 text-sm"
              />
            </div>
          )}

          <div className="flex gap-2">
            <button
              onClick={handleConfirm}
              className="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 text-sm font-medium"
            >
              ✓ That&apos;s right
            </button>
            <button
              onClick={handleReject}
              className="px-4 py-2 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 text-sm font-medium"
            >
              {showCorrection ? '✓ Update' : '✗ Not quite'}
            </button>
            <button
              onClick={onDismiss}
              className="px-4 py-2 text-gray-600 hover:text-gray-700 text-sm"
            >
              Dismiss
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
