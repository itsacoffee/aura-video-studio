/**
 * UI components index
 * Exports all UI components for easy importing
 */

// Canonical Aura components (preferred)
export {
  AuraButton,
  type AuraButtonProps,
  type AuraButtonVariant,
  type AuraButtonSize,
} from './AuraButton';
export { AuraFormField, type AuraFormFieldProps } from './AuraFormField';

// Legacy/base components
export { Button } from './Button';
export { AnimatedButton } from './AnimatedButton';
export { AnimatedInput } from './AnimatedInput';
export {
  AnimatedCard,
  AnimatedCardHeader,
  AnimatedCardBody,
  AnimatedCardFooter,
} from './AnimatedCard';
export { AnimatedModal, AnimatedModalFooter } from './AnimatedModal';
export { AnimatedTooltip } from './AnimatedTooltip';
