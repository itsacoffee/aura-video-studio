/**
 * Button Demo Page - Showcases AuraButton variants for UI review
 */

import { makeStyles, tokens, Text, Title1, Card, CardHeader } from '@fluentui/react-components';
import {
  Play24Regular,
  Save24Regular,
  Delete24Regular,
  Settings24Regular,
  ArrowRight24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { AuraButton } from '../../components/ui';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1000px',
    margin: '0 auto',
  },
  section: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  sectionTitle: {
    marginBottom: tokens.spacingVerticalL,
    display: 'block',
  },
  buttonRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  card: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalM,
  },
  label: {
    display: 'block',
    marginBottom: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
});

export function ButtonDemoPage() {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);

  const handleLoadingDemo = () => {
    setLoading(true);
    setTimeout(() => setLoading(false), 2000);
  };

  return (
    <div className={styles.container}>
      <Title1>AuraButton Component Demo</Title1>
      <Text size={400} style={{ display: 'block', marginBottom: tokens.spacingVerticalXL }}>
        Premium button variants for Aura Video Studio
      </Text>

      {/* Variants Section */}
      <Card className={styles.card}>
        <CardHeader
          header={
            <Text weight="semibold" size={500}>
              Button Variants
            </Text>
          }
        />
        <div className={styles.section}>
          <Text className={styles.label}>Primary - Main actions</Text>
          <div className={styles.buttonRow}>
            <AuraButton variant="primary">Generate Video</AuraButton>
            <AuraButton variant="primary" iconStart={<Play24Regular />}>
              Start
            </AuraButton>
            <AuraButton variant="primary" iconEnd={<ArrowRight24Regular />}>
              Next
            </AuraButton>
          </div>

          <Text className={styles.label}>Secondary - Alternative actions</Text>
          <div className={styles.buttonRow}>
            <AuraButton variant="secondary">Cancel</AuraButton>
            <AuraButton variant="secondary" iconStart={<Settings24Regular />}>
              Settings
            </AuraButton>
            <AuraButton variant="secondary">Back</AuraButton>
          </div>

          <Text className={styles.label}>Tertiary - Minor actions</Text>
          <div className={styles.buttonRow}>
            <AuraButton variant="tertiary">Learn more</AuraButton>
            <AuraButton variant="tertiary">Advanced settings</AuraButton>
            <AuraButton variant="tertiary">Skip</AuraButton>
          </div>

          <Text className={styles.label}>Destructive - Dangerous actions</Text>
          <div className={styles.buttonRow}>
            <AuraButton variant="destructive">Delete</AuraButton>
            <AuraButton variant="destructive" iconStart={<Delete24Regular />}>
              Delete Project
            </AuraButton>
          </div>
        </div>
      </Card>

      {/* Sizes Section */}
      <Card className={styles.card}>
        <CardHeader
          header={
            <Text weight="semibold" size={500}>
              Button Sizes
            </Text>
          }
        />
        <div className={styles.section}>
          <div className={styles.buttonRow}>
            <AuraButton variant="primary" size="small">
              Small
            </AuraButton>
            <AuraButton variant="primary" size="medium">
              Medium
            </AuraButton>
            <AuraButton variant="primary" size="large">
              Large
            </AuraButton>
          </div>
        </div>
      </Card>

      {/* States Section */}
      <Card className={styles.card}>
        <CardHeader
          header={
            <Text weight="semibold" size={500}>
              Button States
            </Text>
          }
        />
        <div className={styles.section}>
          <Text className={styles.label}>Loading State</Text>
          <div className={styles.buttonRow}>
            <AuraButton variant="primary" loading={loading} onClick={handleLoadingDemo}>
              {loading ? 'Generating...' : 'Click to Load'}
            </AuraButton>
            <AuraButton variant="primary" loading loadingText="Saving...">
              Save
            </AuraButton>
          </div>

          <Text className={styles.label}>Disabled State</Text>
          <div className={styles.buttonRow}>
            <AuraButton variant="primary" disabled>
              Disabled Primary
            </AuraButton>
            <AuraButton variant="secondary" disabled>
              Disabled Secondary
            </AuraButton>
            <AuraButton variant="tertiary" disabled>
              Disabled Tertiary
            </AuraButton>
            <AuraButton variant="destructive" disabled>
              Disabled Destructive
            </AuraButton>
          </div>
        </div>
      </Card>

      {/* Full Width Section */}
      <Card className={styles.card}>
        <CardHeader
          header={
            <Text weight="semibold" size={500}>
              Full Width
            </Text>
          }
        />
        <div className={styles.section}>
          <AuraButton variant="primary" fullWidth iconStart={<Save24Regular />}>
            Save Changes
          </AuraButton>
        </div>
      </Card>

      {/* Icon Only Section */}
      <Card className={styles.card}>
        <CardHeader
          header={
            <Text weight="semibold" size={500}>
              With Icons
            </Text>
          }
        />
        <div className={styles.section}>
          <div className={styles.buttonRow}>
            <AuraButton variant="primary" iconStart={<Play24Regular />}>
              Play Video
            </AuraButton>
            <AuraButton variant="secondary" iconStart={<Save24Regular />}>
              Save Draft
            </AuraButton>
            <AuraButton variant="primary" iconEnd={<ArrowRight24Regular />}>
              Continue
            </AuraButton>
          </div>
        </div>
      </Card>
    </div>
  );
}

export default ButtonDemoPage;
