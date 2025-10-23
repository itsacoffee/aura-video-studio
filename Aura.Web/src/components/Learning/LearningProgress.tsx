import React from 'react';
import type { LearningMaturity } from '../../types/learning';

interface LearningProgressProps {
  maturity: LearningMaturity;
}

export const LearningProgress: React.FC<LearningProgressProps> = ({ maturity }) => {
  const getMaturityInfo = (
    level: string
  ): { label: string; color: string; description: string } => {
    switch (level) {
      case 'expert':
        return {
          label: 'Expert',
          color: 'text-purple-600 bg-purple-100',
          description: 'AI has deep understanding of your preferences',
        };
      case 'mature':
        return {
          label: 'Mature',
          color: 'text-blue-600 bg-blue-100',
          description: 'AI has good understanding of your preferences',
        };
      case 'developing':
        return {
          label: 'Developing',
          color: 'text-yellow-600 bg-yellow-100',
          description: 'AI is learning your preferences',
        };
      case 'nascent':
      default:
        return {
          label: 'Nascent',
          color: 'text-gray-600 bg-gray-100',
          description: 'AI needs more data to learn your preferences',
        };
    }
  };

  const maturityInfo = getMaturityInfo(maturity.maturityLevel);

  const getProgressPercentage = (): number => {
    if (maturity.totalDecisions >= 100) return 100;
    if (maturity.totalDecisions >= 50) return 75;
    if (maturity.totalDecisions >= 20) return 50;
    return (maturity.totalDecisions / 20) * 25;
  };

  return (
    <div className="bg-white border border-gray-200 rounded-lg p-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold">Learning Progress</h3>
        <span className={`px-3 py-1 rounded-full text-sm font-medium ${maturityInfo.color}`}>
          {maturityInfo.label}
        </span>
      </div>

      <p className="text-sm text-gray-600 mb-4">{maturityInfo.description}</p>

      <div className="space-y-4">
        {/* Overall Progress */}
        <div>
          <div className="flex justify-between text-sm mb-2">
            <span className="text-gray-700">Overall Learning</span>
            <span className="font-medium">
              {maturity.totalDecisions} decision{maturity.totalDecisions !== 1 ? 's' : ''}
            </span>
          </div>
          <div className="w-full bg-gray-200 rounded-full h-2 overflow-hidden">
            <div
              className="bg-blue-500 h-full rounded-full transition-all duration-300"
              style={{ width: `${getProgressPercentage()}%` }}
            />
          </div>
        </div>

        {/* Confidence Score */}
        <div>
          <div className="flex justify-between text-sm mb-2">
            <span className="text-gray-700">AI Confidence</span>
            <span className="font-medium">{Math.round(maturity.overallConfidence * 100)}%</span>
          </div>
          <div className="w-full bg-gray-200 rounded-full h-2 overflow-hidden">
            <div
              className="bg-green-500 h-full rounded-full transition-all duration-300"
              style={{ width: `${maturity.overallConfidence * 100}%` }}
            />
          </div>
        </div>

        {/* Categories */}
        {Object.keys(maturity.decisionsByCategory).length > 0 && (
          <div className="pt-4 border-t border-gray-100">
            <p className="text-sm font-medium text-gray-700 mb-3">Decisions by Category</p>
            <div className="grid grid-cols-2 gap-2">
              {Object.entries(maturity.decisionsByCategory).map(([category, count]) => (
                <div
                  key={category}
                  className="flex items-center justify-between text-xs bg-gray-50 rounded px-2 py-1"
                >
                  <span className="capitalize text-gray-600">{category}</span>
                  <span className="font-medium">{count}</span>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Strength Areas */}
        {maturity.strengthCategories.length > 0 && (
          <div className="pt-4 border-t border-gray-100">
            <p className="text-sm font-medium text-gray-700 mb-2">Strong Categories</p>
            <div className="flex flex-wrap gap-2">
              {maturity.strengthCategories.map((cat) => (
                <span
                  key={cat}
                  className="px-2 py-1 bg-green-100 text-green-700 rounded text-xs capitalize"
                >
                  {cat}
                </span>
              ))}
            </div>
          </div>
        )}

        {/* Weak Areas */}
        {maturity.weakCategories.length > 0 && (
          <div className="pt-4 border-t border-gray-100">
            <p className="text-sm font-medium text-gray-700 mb-2">Needs More Data</p>
            <div className="flex flex-wrap gap-2">
              {maturity.weakCategories.map((cat) => (
                <span
                  key={cat}
                  className="px-2 py-1 bg-gray-100 text-gray-600 rounded text-xs capitalize"
                >
                  {cat}
                </span>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
