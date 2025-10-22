import React from 'react';

interface ConfidenceIndicatorProps {
  confidence: number;
  label?: string;
  showPercentage?: boolean;
  size?: 'sm' | 'md' | 'lg';
}

export const ConfidenceIndicator: React.FC<ConfidenceIndicatorProps> = ({
  confidence,
  label,
  showPercentage = true,
  size = 'md',
}) => {
  const getConfidenceLevel = (conf: number): string => {
    if (conf >= 0.7) return 'High';
    if (conf >= 0.4) return 'Medium';
    return 'Low';
  };

  const getConfidenceColor = (conf: number): string => {
    if (conf >= 0.7) return 'bg-green-500';
    if (conf >= 0.4) return 'bg-yellow-500';
    return 'bg-gray-400';
  };

  const getTextColor = (conf: number): string => {
    if (conf >= 0.7) return 'text-green-600';
    if (conf >= 0.4) return 'text-yellow-600';
    return 'text-gray-600';
  };

  const sizeClasses = {
    sm: { bar: 'h-1', text: 'text-xs' },
    md: { bar: 'h-2', text: 'text-sm' },
    lg: { bar: 'h-3', text: 'text-base' },
  };

  const percentage = Math.round(confidence * 100);

  return (
    <div className="w-full">
      {label && (
        <div className="flex justify-between items-center mb-1">
          <span className={`font-medium ${sizeClasses[size].text}`}>
            {label}
          </span>
          {showPercentage && (
            <span className={`${getTextColor(confidence)} ${sizeClasses[size].text}`}>
              {percentage}% {getConfidenceLevel(confidence)}
            </span>
          )}
        </div>
      )}
      <div className="w-full bg-gray-200 rounded-full overflow-hidden">
        <div
          className={`${getConfidenceColor(confidence)} ${sizeClasses[size].bar} rounded-full transition-all duration-300`}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
};
