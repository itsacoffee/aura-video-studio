/**
 * Diagnostics Page
 * System diagnostics and repair tools
 */

import { Title1, Button } from '@fluentui/react-components';
import { ArrowLeft24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import type { FC } from 'react';
import { useNavigate } from 'react-router-dom';
import { DiagnosticsPanel, RepairWizard } from '../../components/SafeMode';
import './DiagnosticsPage.css';

export const DiagnosticsPage: FC = () => {
  const navigate = useNavigate();
  const [showRepairWizard, setShowRepairWizard] = useState(false);

  return (
    <div className="diagnostics-page">
      <div className="diagnostics-page-header">
        <Button appearance="subtle" icon={<ArrowLeft24Regular />} onClick={() => navigate('/')}>
          Back
        </Button>
        <Title1>System Diagnostics</Title1>
        <Button appearance="primary" onClick={() => setShowRepairWizard(true)}>
          Run Repair Wizard
        </Button>
      </div>

      <DiagnosticsPanel />

      <RepairWizard open={showRepairWizard} onClose={() => setShowRepairWizard(false)} />
    </div>
  );
};

export default DiagnosticsPage;
