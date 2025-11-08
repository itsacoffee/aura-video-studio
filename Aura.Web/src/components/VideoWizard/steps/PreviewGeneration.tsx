import { Title2, Text } from '@fluentui/react-components';
import { useEffect } from 'react';
import type { FC } from 'react';
import type { PreviewData, ScriptData, StyleData, StepValidation } from '../types';

interface PreviewGenerationProps {
  data: PreviewData;
  scriptData: ScriptData;
  styleData: StyleData;
  advancedMode: boolean;
  onChange: (data: PreviewData) => void;
  onValidationChange: (validation: StepValidation) => void;
}

export const PreviewGeneration: FC<PreviewGenerationProps> = ({ onValidationChange }) => {
  useEffect(() => {
    onValidationChange({ isValid: true, errors: [] });
  }, [onValidationChange]);

  return (
    <div>
      <Title2>Preview Generation</Title2>
      <Text>Generate preview with thumbnails and audio samples.</Text>
    </div>
  );
};
