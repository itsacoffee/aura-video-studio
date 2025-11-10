import { Tooltip, Text, tokens } from '@fluentui/react-components';
import { Info24Regular } from '@fluentui/react-icons';
import type { FC, ReactNode } from 'react';

interface TooltipHelperProps {
  content: string | ReactNode;
  title?: string;
  placement?: 'top' | 'bottom' | 'left' | 'right';
}

export const TooltipHelper: FC<TooltipHelperProps> = ({
  content,
  title,
  placement = 'top',
}) => {
  const tooltipContent = (
    <div style={{ maxWidth: '300px', padding: tokens.spacingVerticalS }}>
      {title && (
        <Text
          weight="semibold"
          size={300}
          style={{ display: 'block', marginBottom: tokens.spacingVerticalXS }}
        >
          {title}
        </Text>
      )}
      <Text size={200}>{content}</Text>
    </div>
  );

  return (
    <Tooltip content={tooltipContent} relationship="description" positioning={placement}>
      <Info24Regular
        style={{
          fontSize: '16px',
          color: tokens.colorNeutralForeground3,
          cursor: 'help',
          marginLeft: tokens.spacingHorizontalXS,
        }}
      />
    </Tooltip>
  );
};
