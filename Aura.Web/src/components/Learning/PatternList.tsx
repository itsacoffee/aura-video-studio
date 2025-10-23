import React from 'react';
import type { DecisionPattern } from '../../types/learning';

interface PatternListProps {
  patterns: DecisionPattern[];
  onPatternSelect?: (pattern: DecisionPattern) => void;
}

export const PatternList: React.FC<PatternListProps> = ({ patterns, onPatternSelect }) => {
  const getPatternColor = (strength: number): string => {
    if (strength >= 0.7) return 'text-green-600';
    if (strength >= 0.4) return 'text-yellow-600';
    return 'text-gray-600';
  };

  const getPatternIcon = (patternType: string): string => {
    switch (patternType) {
      case 'acceptance':
        return '✓';
      case 'rejection':
        return '✗';
      case 'modification':
        return '✎';
      default:
        return '•';
    }
  };

  if (patterns.length === 0) {
    return (
      <div className="text-center py-8 text-gray-500">
        <p>No patterns identified yet.</p>
        <p className="text-sm mt-2">Make more decisions to help the AI learn your preferences.</p>
      </div>
    );
  }

  return (
    <div className="space-y-2">
      {patterns.map((pattern) => (
        <div
          key={pattern.patternId}
          className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50 cursor-pointer transition-colors"
          onClick={() => onPatternSelect?.(pattern)}
        >
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <div className="flex items-center gap-2">
                <span className="text-xl">{getPatternIcon(pattern.patternType)}</span>
                <span className="font-medium capitalize">{pattern.suggestionType}</span>
                <span className="text-sm text-gray-500 capitalize">{pattern.patternType}</span>
              </div>
              <div className="mt-2 text-sm text-gray-600">
                <p>
                  Observed <strong>{pattern.occurrences}</strong> time
                  {pattern.occurrences !== 1 ? 's' : ''}
                </p>
                <p className="text-xs mt-1">
                  Last seen: {new Date(pattern.lastObserved).toLocaleDateString()}
                </p>
              </div>
            </div>
            <div className="flex flex-col items-end">
              <span className={`font-semibold ${getPatternColor(pattern.strength)}`}>
                {Math.round(pattern.strength * 100)}%
              </span>
              <span className="text-xs text-gray-500">strength</span>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
};
