import { Title2, Text } from '@fluentui/react-components';
import { useEffect } from 'react';
import type { FC } from 'react';
import type { StyleData, BriefData, StepValidation } from '../types';

interface StyleSelectionProps {
  data: StyleData;
  briefData: BriefData;
  advancedMode: boolean;
  onChange: (data: StyleData) => void;
  onValidationChange: (validation: StepValidation) => void;
}

export const StyleSelection: FC<StyleSelectionProps> = ({ onValidationChange }) => {
  useEffect(() => {
    onValidationChange({ isValid: true, errors: [] });
  }, [onValidationChange]);

  return (
    <div>
      <Title2>Style Selection</Title2>
      <Text>Configure voice, visual style, and music preferences.</Text>
    </div>
  );
};
