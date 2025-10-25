import { ReactElement } from 'react';
import { Tooltip as FluentTooltip, TooltipProps } from '@fluentui/react-components';

interface EnhancedTooltipProps extends Omit<TooltipProps, 'content'> {
  content: string;
  children: ReactElement;
  showDelay?: number;
  // Optional keyboard shortcut to display
  shortcut?: string;
}

/**
 * Enhanced Tooltip wrapper that provides consistent tooltip styling
 * and behavior across the application, with optional keyboard shortcut display.
 */
export function Tooltip({
  content,
  children,
  showDelay = 500,
  relationship = 'description',
  shortcut,
  ...props
}: EnhancedTooltipProps) {
  // Build tooltip content with shortcut if provided
  const tooltipContent = shortcut ? (
    <div>
      {content}
      <div style={{ marginTop: '4px', opacity: 0.8, fontSize: '0.85em' }}>
        <kbd style={{ 
          padding: '2px 6px', 
          borderRadius: '3px', 
          border: '1px solid rgba(255,255,255,0.3)',
          backgroundColor: 'rgba(0,0,0,0.2)',
          fontFamily: 'monospace',
        }}>
          {shortcut}
        </kbd>
      </div>
    </div>
  ) : content;

  return (
    <FluentTooltip
      content={tooltipContent}
      relationship={relationship}
      positioning="above"
      showDelay={showDelay}
      {...props}
    >
      {children}
    </FluentTooltip>
  );
}
