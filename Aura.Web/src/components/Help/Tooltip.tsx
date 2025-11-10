import React, { useState, useRef, useEffect } from 'react';
import { HelpCircle } from 'lucide-react';

interface TooltipProps {
  content: string;
  children?: React.ReactNode;
  position?: 'top' | 'bottom' | 'left' | 'right';
  showIcon?: boolean;
  maxWidth?: string;
  delay?: number;
}

export const Tooltip: React.FC<TooltipProps> = ({
  content,
  children,
  position = 'top',
  showIcon = false,
  maxWidth = '250px',
  delay = 300
}) => {
  const [isVisible, setIsVisible] = useState(false);
  const [actualPosition, setActualPosition] = useState(position);
  const timeoutRef = useRef<NodeJS.Timeout>();
  const triggerRef = useRef<HTMLDivElement>(null);
  const tooltipRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  const handleMouseEnter = () => {
    timeoutRef.current = setTimeout(() => {
      setIsVisible(true);
      adjustPosition();
    }, delay);
  };

  const handleMouseLeave = () => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
    setIsVisible(false);
  };

  const adjustPosition = () => {
    if (!triggerRef.current || !tooltipRef.current) return;

    const triggerRect = triggerRef.current.getBoundingClientRect();
    const tooltipRect = tooltipRef.current.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    let newPosition = position;

    if (position === 'top' && triggerRect.top < tooltipRect.height + 10) {
      newPosition = 'bottom';
    } else if (position === 'bottom' && viewportHeight - triggerRect.bottom < tooltipRect.height + 10) {
      newPosition = 'top';
    } else if (position === 'left' && triggerRect.left < tooltipRect.width + 10) {
      newPosition = 'right';
    } else if (position === 'right' && viewportWidth - triggerRect.right < tooltipRect.width + 10) {
      newPosition = 'left';
    }

    setActualPosition(newPosition);
  };

  const getPositionClasses = () => {
    const baseClasses = 'absolute z-50 px-3 py-2 text-sm text-white bg-gray-900 dark:bg-gray-700 rounded-lg shadow-lg pointer-events-none';
    
    switch (actualPosition) {
      case 'top':
        return `${baseClasses} bottom-full left-1/2 transform -translate-x-1/2 -translate-y-2`;
      case 'bottom':
        return `${baseClasses} top-full left-1/2 transform -translate-x-1/2 translate-y-2`;
      case 'left':
        return `${baseClasses} right-full top-1/2 transform -translate-y-1/2 -translate-x-2`;
      case 'right':
        return `${baseClasses} left-full top-1/2 transform -translate-y-1/2 translate-x-2`;
      default:
        return baseClasses;
    }
  };

  const getArrowClasses = () => {
    const baseClasses = 'absolute w-2 h-2 bg-gray-900 dark:bg-gray-700 transform rotate-45';
    
    switch (actualPosition) {
      case 'top':
        return `${baseClasses} bottom-[-4px] left-1/2 transform -translate-x-1/2 rotate-45`;
      case 'bottom':
        return `${baseClasses} top-[-4px] left-1/2 transform -translate-x-1/2 rotate-45`;
      case 'left':
        return `${baseClasses} right-[-4px] top-1/2 transform -translate-y-1/2 rotate-45`;
      case 'right':
        return `${baseClasses} left-[-4px] top-1/2 transform -translate-y-1/2 rotate-45`;
      default:
        return baseClasses;
    }
  };

  return (
    <div className="relative inline-block">
      <div
        ref={triggerRef}
        onMouseEnter={handleMouseEnter}
        onMouseLeave={handleMouseLeave}
        className="inline-flex items-center gap-1 cursor-help"
      >
        {children}
        {showIcon && (
          <HelpCircle className="w-4 h-4 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors" />
        )}
      </div>
      
      {isVisible && (
        <div
          ref={tooltipRef}
          className={getPositionClasses()}
          style={{ maxWidth }}
          role="tooltip"
        >
          <div className={getArrowClasses()} />
          <div className="relative z-10 whitespace-normal">
            {content}
          </div>
        </div>
      )}
    </div>
  );
};

interface HelpTextProps {
  text: string;
  position?: 'top' | 'bottom' | 'left' | 'right';
}

export const HelpText: React.FC<HelpTextProps> = ({ text, position = 'top' }) => {
  return (
    <Tooltip content={text} position={position} showIcon />
  );
};
