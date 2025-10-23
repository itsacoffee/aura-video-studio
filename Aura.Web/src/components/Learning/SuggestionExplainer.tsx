import React from 'react';
import type { SuggestionPrediction } from '../../types/learning';
import { ConfidenceIndicator } from './ConfidenceIndicator';

interface SuggestionExplainerProps {
  prediction: SuggestionPrediction;
}

export const SuggestionExplainer: React.FC<SuggestionExplainerProps> = ({ prediction }) => {
  const getPredictionLabel = (): string => {
    if (prediction.acceptanceProbability >= 0.7) {
      return 'Likely to accept';
    }
    if (prediction.rejectionProbability >= 0.7) {
      return 'Likely to reject';
    }
    if (prediction.modificationProbability >= 0.5) {
      return 'Likely to modify';
    }
    return 'Uncertain';
  };

  const getPredictionColor = (): string => {
    if (prediction.acceptanceProbability >= 0.7) {
      return 'text-green-600';
    }
    if (prediction.rejectionProbability >= 0.7) {
      return 'text-red-600';
    }
    if (prediction.modificationProbability >= 0.5) {
      return 'text-yellow-600';
    }
    return 'text-gray-600';
  };

  return (
    <div className="bg-white border border-gray-200 rounded-lg p-4">
      <div className="flex items-start gap-3 mb-4">
        <div className="text-2xl">🤖</div>
        <div className="flex-1">
          <h3 className="font-semibold text-gray-900">Why this suggestion?</h3>
          <p className={`text-sm mt-1 ${getPredictionColor()}`}>{getPredictionLabel()}</p>
        </div>
      </div>

      <div className="space-y-3">
        {/* Probabilities */}
        <div className="space-y-2">
          <ConfidenceIndicator
            confidence={prediction.acceptanceProbability}
            label="Acceptance probability"
            size="sm"
          />
          <ConfidenceIndicator
            confidence={prediction.rejectionProbability}
            label="Rejection probability"
            size="sm"
          />
          <ConfidenceIndicator
            confidence={prediction.modificationProbability}
            label="Modification probability"
            size="sm"
          />
        </div>

        {/* Overall Confidence */}
        <div className="pt-3 border-t border-gray-100">
          <ConfidenceIndicator
            confidence={prediction.confidence}
            label="Overall confidence"
            size="md"
          />
        </div>

        {/* Reasoning Factors */}
        {prediction.reasoningFactors.length > 0 && (
          <div className="pt-3 border-t border-gray-100">
            <p className="text-sm font-medium text-gray-700 mb-2">Why we think this:</p>
            <ul className="space-y-1">
              {prediction.reasoningFactors.map((factor, index) => (
                <li key={index} className="text-sm text-gray-600 flex gap-2">
                  <span>•</span>
                  <span>{factor}</span>
                </li>
              ))}
            </ul>
          </div>
        )}

        {/* Similar Past Decisions */}
        {prediction.similarPastDecisions && prediction.similarPastDecisions.length > 0 && (
          <div className="pt-3 border-t border-gray-100">
            <p className="text-xs text-gray-500">
              Based on {prediction.similarPastDecisions.length} similar past decision
              {prediction.similarPastDecisions.length !== 1 ? 's' : ''}
            </p>
          </div>
        )}
      </div>
    </div>
  );
};
