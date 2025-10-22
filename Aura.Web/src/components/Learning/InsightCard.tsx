import React from 'react';
import type { LearningInsight } from '../../types/learning';

interface InsightCardProps {
  insight: LearningInsight;
  onAction?: (insight: LearningInsight) => void;
}

export const InsightCard: React.FC<InsightCardProps> = ({ insight, onAction }) => {
  const getInsightIcon = (type: string): string => {
    switch (type) {
      case 'preference':
        return '💡';
      case 'tendency':
        return '📊';
      case 'anti-pattern':
        return '⚠️';
      case 'proactive':
        return '🚀';
      case 'alternative':
        return '🔄';
      default:
        return 'ℹ️';
    }
  };

  const getConfidenceBadgeColor = (confidence: number): string => {
    if (confidence >= 0.7) return 'bg-green-100 text-green-800';
    if (confidence >= 0.4) return 'bg-yellow-100 text-yellow-800';
    return 'bg-gray-100 text-gray-800';
  };

  return (
    <div className="border border-gray-200 rounded-lg p-4 bg-white shadow-sm hover:shadow-md transition-shadow">
      <div className="flex items-start gap-3">
        <div className="text-2xl">{getInsightIcon(insight.insightType)}</div>
        <div className="flex-1">
          <div className="flex items-start justify-between gap-2">
            <div>
              <p className="text-sm font-medium text-gray-900">{insight.description}</p>
              <div className="flex items-center gap-2 mt-2">
                <span className="text-xs text-gray-500 capitalize">{insight.category}</span>
                <span
                  className={`text-xs px-2 py-0.5 rounded-full ${getConfidenceBadgeColor(
                    insight.confidence
                  )}`}
                >
                  {Math.round(insight.confidence * 100)}% confidence
                </span>
              </div>
            </div>
          </div>
          {insight.isActionable && insight.suggestedAction && (
            <div className="mt-3 pt-3 border-t border-gray-100">
              <button
                onClick={() => onAction?.(insight)}
                className="text-sm text-blue-600 hover:text-blue-700 font-medium"
              >
                {insight.suggestedAction} →
              </button>
            </div>
          )}
        </div>
      </div>
      <div className="text-xs text-gray-400 mt-2">
        {new Date(insight.discoveredAt).toLocaleString()}
      </div>
    </div>
  );
};
