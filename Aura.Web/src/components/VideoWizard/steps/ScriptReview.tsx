import { Title2, Text } from '@fluentui/react-components';
import { useEffect } from 'react';
import type { FC } from 'react';
import type { ScriptData, BriefData, StyleData, StepValidation } from '../types';

interface ScriptReviewProps {
  data: ScriptData;
  briefData: BriefData;
  styleData: StyleData;
  advancedMode: boolean;
  onChange: (data: ScriptData) => void;
  onValidationChange: (validation: StepValidation) => void;
}

export const ScriptReview: FC<ScriptReviewProps> = ({ onValidationChange }) => {
  useEffect(() => {
    onValidationChange({ isValid: true, errors: [] });
  }, [onValidationChange]);

  return (
    <div>
      <Title2>Script Review</Title2>
      <Text>Review and edit the AI-generated script.</Text>
    </div>
  );
};
