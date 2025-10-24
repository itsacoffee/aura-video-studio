import { 
  ArrowTrendingLines20Regular as TrendingUp,
  ArrowTrendingDown20Regular as TrendingDown,
  Subtract20Regular as Minus
} from '@fluentui/react-icons';

interface ConfidenceMeterProps {
  confidence: number;
  label?: string;
  showIcon?: boolean;
  size?: 'sm' | 'md' | 'lg';
}

export const ConfidenceMeter: React.FC<ConfidenceMeterProps> = ({
  confidence,
  label = 'Confidence',
  showIcon = true,
  size = 'md',
}) => {
  const getColor = (conf: number) => {
    if (conf >= 0.8) return 'bg-green-500';
    if (conf >= 0.6) return 'bg-blue-500';
    if (conf >= 0.4) return 'bg-yellow-500';
    return 'bg-red-500';
  };

  const getTextColor = (conf: number) => {
    if (conf >= 0.8) return 'text-green-700';
    if (conf >= 0.6) return 'text-blue-700';
    if (conf >= 0.4) return 'text-yellow-700';
    return 'text-red-700';
  };

  const getIcon = (conf: number) => {
    if (conf >= 0.8) return <TrendingUp className="h-4 w-4" />;
    if (conf >= 0.4) return <Minus className="h-4 w-4" />;
    return <TrendingDown className="h-4 w-4" />;
  };

  const getLabel = (conf: number) => {
    if (conf >= 0.8) return 'High Confidence';
    if (conf >= 0.6) return 'Moderate Confidence';
    if (conf >= 0.4) return 'Low Confidence';
    return 'Very Low Confidence';
  };

  const heightClass = {
    sm: 'h-1',
    md: 'h-2',
    lg: 'h-3',
  }[size];

  const textSizeClass = {
    sm: 'text-xs',
    md: 'text-sm',
    lg: 'text-base',
  }[size];

  return (
    <div className="space-y-1">
      <div className="flex items-center justify-between">
        <div className={`flex items-center gap-2 ${textSizeClass}`}>
          {showIcon && <span className={getTextColor(confidence)}>{getIcon(confidence)}</span>}
          <span className="font-medium">{label}</span>
        </div>
        <span className={`font-semibold ${getTextColor(confidence)} ${textSizeClass}`}>
          {(confidence * 100).toFixed(1)}%
        </span>
      </div>
      
      <div className="w-full bg-gray-200 rounded-full overflow-hidden">
        <div
          className={`${heightClass} ${getColor(confidence)} rounded-full transition-all duration-500`}
          style={{ width: `${confidence * 100}%` }}
        />
      </div>
      
      <p className={`${textSizeClass} ${getTextColor(confidence)}`}>
        {getLabel(confidence)}
      </p>
    </div>
  );
};
