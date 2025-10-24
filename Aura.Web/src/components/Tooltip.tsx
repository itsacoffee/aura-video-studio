import { ReactElement } from 'react';
import { Tooltip as FluentTooltip, TooltipProps } from '@fluentui/react-components';

interface EnhancedTooltipProps extends Omit<TooltipProps, 'content'> {
  content: string;
  children: ReactElement;
  showDelay?: number;
}

/**
 * Enhanced Tooltip wrapper that provides consistent tooltip styling
 * and behavior across the application.
 */
export function Tooltip({
  content,
  children,
  showDelay = 500,
  relationship = 'description',
  ...props
}: EnhancedTooltipProps) {
  return (
    <FluentTooltip
      content={content}
      relationship={relationship}
      positioning="above"
      showDelay={showDelay}
      {...props}
    >
      {children}
    </FluentTooltip>
  );
}
