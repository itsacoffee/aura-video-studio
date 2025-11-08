import { Title2, Text } from '@fluentui/react-components';
import { useEffect } from 'react';
import type { FC } from 'react';
import type { ExportData, WizardData, StepValidation } from '../types';

interface FinalExportProps {
  data: ExportData;
  wizardData: WizardData;
  advancedMode: boolean;
  onChange: (data: ExportData) => void;
  onValidationChange: (validation: StepValidation) => void;
}

export const FinalExport: FC<FinalExportProps> = ({ onValidationChange }) => {
  useEffect(() => {
    onValidationChange({ isValid: true, errors: [] });
  }, [onValidationChange]);

  return (
    <div>
      <Title2>Final Export</Title2>
      <Text>Configure export quality and format settings.</Text>
    </div>
  );
};
