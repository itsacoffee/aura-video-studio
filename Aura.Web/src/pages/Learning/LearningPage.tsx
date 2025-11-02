import { Button, Field, Input, tokens } from '@fluentui/react-components';
import React, { useState } from 'react';
import { LearningDashboard } from '../../components/Learning/LearningDashboard';

export const LearningPage: React.FC = () => {
  const [profileId, setProfileId] = useState('default-profile');
  const [currentProfileId, setCurrentProfileId] = useState('default-profile');

  const handleLoadProfile = () => {
    setCurrentProfileId(profileId);
  };

  return (
    <div style={{ padding: tokens.spacingVerticalXL }}>
      <div style={{ marginBottom: tokens.spacingVerticalXL }}>
        <h1>AI Learning & Patterns</h1>
        <p style={{ color: tokens.colorNeutralForeground3 }}>
          View how AI learns from your decisions and provides personalized suggestions.
        </p>
      </div>

      <div
        style={{
          display: 'flex',
          gap: tokens.spacingHorizontalM,
          marginBottom: tokens.spacingVerticalL,
          alignItems: 'flex-end',
        }}
      >
        <Field label="Profile ID" style={{ flexGrow: 1, maxWidth: '400px' }}>
          <Input
            value={profileId}
            onChange={(e) => setProfileId(e.target.value)}
            placeholder="Enter profile ID..."
          />
        </Field>
        <Button appearance="primary" onClick={handleLoadProfile}>
          Load Profile
        </Button>
      </div>

      <LearningDashboard profileId={currentProfileId} />
    </div>
  );
};

export default LearningPage;
