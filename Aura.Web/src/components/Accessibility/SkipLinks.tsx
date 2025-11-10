/**
 * Skip Links Component
 * 
 * Provides keyboard-accessible skip navigation links for screen reader users
 * and keyboard navigation users to quickly jump to main content areas.
 */

import { makeStyles, tokens } from '@fluentui/react-components';
import { useCallback } from 'react';

const useStyles = makeStyles({
  skipLinks: {
    position: 'fixed',
    top: '0',
    left: '0',
    zIndex: 9999,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalS,
  },
  skipLink: {
    position: 'absolute',
    left: '-9999px',
    width: '1px',
    height: '1px',
    overflow: 'hidden',
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderRadius: tokens.borderRadiusMedium,
    textDecoration: 'none',
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    boxShadow: tokens.shadow28,
    transition: 'all 0.2s ease-out',
    ':focus': {
      position: 'static',
      width: 'auto',
      height: 'auto',
      left: '0',
      overflow: 'visible',
    },
    ':hover': {
      backgroundColor: tokens.colorBrandBackgroundHover,
    },
  },
});

interface SkipLink {
  id: string;
  label: string;
  target: string;
}

const defaultSkipLinks: SkipLink[] = [
  { id: 'skip-to-main', label: 'Skip to main content', target: '#main-content' },
  { id: 'skip-to-nav', label: 'Skip to navigation', target: '#main-navigation' },
  { id: 'skip-to-footer', label: 'Skip to footer', target: '#global-footer' },
];

export interface SkipLinksProps {
  links?: SkipLink[];
}

export function SkipLinks({ links = defaultSkipLinks }: SkipLinksProps) {
  const styles = useStyles();

  const handleSkipLinkClick = useCallback((e: React.MouseEvent<HTMLAnchorElement>, target: string) => {
    e.preventDefault();
    
    const targetElement = document.querySelector(target);
    if (targetElement) {
      // Ensure the element is focusable
      const originalTabIndex = targetElement.getAttribute('tabindex');
      if (!originalTabIndex) {
        targetElement.setAttribute('tabindex', '-1');
      }
      
      // Focus the element
      (targetElement as HTMLElement).focus();
      
      // Scroll into view
      targetElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
      
      // Remove temporary tabindex after a short delay
      if (!originalTabIndex) {
        setTimeout(() => {
          targetElement.removeAttribute('tabindex');
        }, 1000);
      }
    }
  }, []);

  return (
    <div className={styles.skipLinks} role="navigation" aria-label="Skip links">
      {links.map((link) => (
        <a
          key={link.id}
          href={link.target}
          className={styles.skipLink}
          onClick={(e) => handleSkipLinkClick(e, link.target)}
        >
          {link.label}
        </a>
      ))}
    </div>
  );
}
