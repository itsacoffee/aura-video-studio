import {
  makeStyles,
  tokens,
  Text,
  Button,
  Card,
  Badge,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
} from '@fluentui/react-components';
import { Open20Regular } from '@fluentui/react-icons';
import { useState } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  stepsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalS,
  },
  step: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'flex-start',
  },
  stepNumber: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    minWidth: '28px',
    height: '28px',
    borderRadius: '50%',
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase200,
  },
  stepContent: {
    flex: 1,
    paddingTop: '4px',
  },
  pricingCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  calculator: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  calculatorRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  buttonRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
  },
});

export interface ProviderHelpPanelProps {
  providerName: string;
  signupUrl: string;
  steps: string[];
  usedFor: string;
  pricingInfo: {
    freeTier?: string;
    costEstimate: string;
  };
  keyFormat?: string;
}

export function ProviderHelpPanel({
  providerName,
  signupUrl,
  steps,
  usedFor,
  pricingInfo,
  keyFormat,
}: ProviderHelpPanelProps) {
  const styles = useStyles();
  const [videosPerMonth, setVideosPerMonth] = useState(10);

  const calculateCost = () => {
    // Extract numeric value from cost estimate (e.g., "$0.15" -> 0.15)
    const match = pricingInfo.costEstimate.match(/\$?(\d+\.?\d*)/);
    if (match) {
      const costPerVideo = parseFloat(match[1]);
      return (costPerVideo * videosPerMonth).toFixed(2);
    }
    return '0.00';
  };

  return (
    <div className={styles.container}>
      <Accordion collapsible>
        <AccordionItem value="usage">
          <AccordionHeader>What it&apos;s used for</AccordionHeader>
          <AccordionPanel>
            <Text>{usedFor}</Text>
          </AccordionPanel>
        </AccordionItem>

        <AccordionItem value="pricing">
          <AccordionHeader>Pricing</AccordionHeader>
          <AccordionPanel>
            <Card className={styles.pricingCard}>
              {pricingInfo.freeTier && (
                <div style={{ marginBottom: tokens.spacingVerticalM }}>
                  <Badge appearance="filled" color="success">
                    Free Tier Available
                  </Badge>
                  <Text
                    style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}
                    size={200}
                  >
                    {pricingInfo.freeTier}
                  </Text>
                </div>
              )}
              <Text weight="semibold">Cost per video:</Text>
              <Text size={200}>{pricingInfo.costEstimate}</Text>

              <div className={styles.calculator}>
                <Text weight="semibold" size={200}>
                  Monthly Cost Calculator
                </Text>
                <div className={styles.calculatorRow}>
                  <Text size={200}>Videos per month:</Text>
                  <input
                    type="number"
                    value={videosPerMonth}
                    onChange={(e) => setVideosPerMonth(parseInt(e.target.value) || 0)}
                    min="0"
                    max="1000"
                    style={{
                      width: '80px',
                      padding: '4px 8px',
                      borderRadius: '4px',
                      border: `1px solid ${tokens.colorNeutralStroke1}`,
                    }}
                  />
                </div>
                <Text weight="semibold">Estimated monthly cost: ${calculateCost()}</Text>
              </div>
            </Card>
          </AccordionPanel>
        </AccordionItem>

        <AccordionItem value="howto">
          <AccordionHeader>How to get your API key</AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <div className={styles.stepsList}>
                {steps.map((step, index) => (
                  <div key={index} className={styles.step}>
                    <div className={styles.stepNumber}>{index + 1}</div>
                    <div className={styles.stepContent}>
                      <Text>{step}</Text>
                    </div>
                  </div>
                ))}
              </div>

              {keyFormat && (
                <Card
                  style={{ marginTop: tokens.spacingVerticalM, padding: tokens.spacingVerticalS }}
                >
                  <Text size={200} style={{ fontStyle: 'italic' }}>
                    💡 Tip: {providerName} API keys {keyFormat}
                  </Text>
                </Card>
              )}

              <div className={styles.buttonRow} style={{ marginTop: tokens.spacingVerticalM }}>
                <Button
                  appearance="primary"
                  icon={<Open20Regular />}
                  onClick={() => window.open(signupUrl, '_blank')}
                >
                  Get API Key
                </Button>
              </div>
            </div>
          </AccordionPanel>
        </AccordionItem>
      </Accordion>
    </div>
  );
}
