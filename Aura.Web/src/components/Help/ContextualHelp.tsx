import React from 'react';
import { HelpCircle, Info, AlertCircle, CheckCircle } from 'lucide-react';

type HelpType = 'info' | 'tip' | 'warning' | 'success';

interface ContextualHelpProps {
  type?: HelpType;
  title?: string;
  children: React.ReactNode;
  className?: string;
  collapsible?: boolean;
}

const typeConfig = {
  info: {
    icon: Info,
    bgColor: 'bg-blue-50 dark:bg-blue-900/20',
    borderColor: 'border-blue-200 dark:border-blue-800',
    iconColor: 'text-blue-600 dark:text-blue-400',
    textColor: 'text-blue-900 dark:text-blue-100'
  },
  tip: {
    icon: HelpCircle,
    bgColor: 'bg-green-50 dark:bg-green-900/20',
    borderColor: 'border-green-200 dark:border-green-800',
    iconColor: 'text-green-600 dark:text-green-400',
    textColor: 'text-green-900 dark:text-green-100'
  },
  warning: {
    icon: AlertCircle,
    bgColor: 'bg-yellow-50 dark:bg-yellow-900/20',
    borderColor: 'border-yellow-200 dark:border-yellow-800',
    iconColor: 'text-yellow-600 dark:text-yellow-400',
    textColor: 'text-yellow-900 dark:text-yellow-100'
  },
  success: {
    icon: CheckCircle,
    bgColor: 'bg-emerald-50 dark:bg-emerald-900/20',
    borderColor: 'border-emerald-200 dark:border-emerald-800',
    iconColor: 'text-emerald-600 dark:text-emerald-400',
    textColor: 'text-emerald-900 dark:text-emerald-100'
  }
};

export const ContextualHelp: React.FC<ContextualHelpProps> = ({
  type = 'info',
  title,
  children,
  className = '',
  collapsible = false
}) => {
  const [isCollapsed, setIsCollapsed] = React.useState(false);
  const config = typeConfig[type];
  const Icon = config.icon;

  return (
    <div
      className={`${config.bgColor} ${config.borderColor} border rounded-lg p-4 ${className}`}
      role="note"
      aria-label={title || 'Helpful information'}
    >
      <div className="flex gap-3">
        <Icon className={`w-5 h-5 ${config.iconColor} flex-shrink-0 mt-0.5`} />
        <div className="flex-1 min-w-0">
          {title && (
            <div className="flex items-center justify-between mb-2">
              <h4 className={`font-semibold ${config.textColor}`}>
                {title}
              </h4>
              {collapsible && (
                <button
                  onClick={() => setIsCollapsed(!isCollapsed)}
                  className={`text-sm ${config.textColor} hover:underline`}
                  aria-expanded={!isCollapsed}
                >
                  {isCollapsed ? 'Show' : 'Hide'}
                </button>
              )}
            </div>
          )}
          {(!collapsible || !isCollapsed) && (
            <div className={`text-sm ${config.textColor} space-y-2`}>
              {children}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

interface InlineHelpProps {
  children: React.ReactNode;
  icon?: React.ReactNode;
}

export const InlineHelp: React.FC<InlineHelpProps> = ({ children, icon }) => {
  return (
    <div className="flex items-start gap-2 text-sm text-gray-600 dark:text-gray-400 mt-1">
      {icon || <Info className="w-4 h-4 mt-0.5 flex-shrink-0" />}
      <span>{children}</span>
    </div>
  );
};

interface FeatureExplanationProps {
  feature: string;
  description: string;
  example?: string;
  learnMoreLink?: string;
}

export const FeatureExplanation: React.FC<FeatureExplanationProps> = ({
  feature,
  description,
  example,
  learnMoreLink
}) => {
  return (
    <ContextualHelp type="tip" title={`What is ${feature}?`} collapsible>
      <p>{description}</p>
      {example && (
        <div className="mt-2 pl-3 border-l-2 border-green-300 dark:border-green-700">
          <p className="text-xs font-semibold mb-1">Example:</p>
          <p className="text-xs">{example}</p>
        </div>
      )}
      {learnMoreLink && (
        <a
          href={learnMoreLink}
          target="_blank"
          rel="noopener noreferrer"
          className="text-sm underline hover:no-underline mt-2 inline-block"
        >
          Learn more →
        </a>
      )}
    </ContextualHelp>
  );
};
