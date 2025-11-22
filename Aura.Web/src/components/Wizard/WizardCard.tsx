import type { FC, ReactNode } from 'react';
import './WizardCard.css';

interface WizardCardProps {
  children: ReactNode;
  title?: string;
  subtitle?: string;
  className?: string;
  elevated?: boolean;
  glow?: boolean;
}

/**
 * Premium Card Component for Wizard Steps
 * Features:
 * - Elevation with shadow
 * - Optional glow effect
 * - Generous padding
 * - Smooth hover transitions
 */
export const WizardCard: FC<WizardCardProps> = ({
  children,
  title,
  subtitle,
  className = '',
  elevated = true,
  glow = false,
}) => {
  const classes = [
    'wizard-card',
    elevated && 'wizard-card--elevated',
    glow && 'wizard-card--glow',
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={classes}>
      {(title || subtitle) && (
        <div className="wizard-card__header">
          {title && <h3 className="wizard-card__title">{title}</h3>}
          {subtitle && <p className="wizard-card__subtitle">{subtitle}</p>}
        </div>
      )}
      <div className="wizard-card__content">{children}</div>
    </div>
  );
};
