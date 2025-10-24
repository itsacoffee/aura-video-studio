import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Card,
  Button,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
} from '@fluentui/react-components';
import { Checkmark20Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    textAlign: 'center',
  },
  cardsContainer: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingHorizontalL,
  },
  tierCard: {
    padding: tokens.spacingVerticalXL,
    cursor: 'pointer',
    transition: 'all 0.3s ease-in-out',
    position: 'relative',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: tokens.shadow16,
    },
  },
  selectedCard: {
    outline: `2px solid ${tokens.colorBrandBackground}`,
    outlineOffset: '-2px',
  },
  featuresList: {
    listStyleType: 'none',
    padding: 0,
    margin: `${tokens.spacingVerticalM} 0`,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  featureItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  badge: {
    position: 'absolute',
    top: tokens.spacingVerticalM,
    right: tokens.spacingVerticalM,
    padding: '4px 12px',
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
  },
  comparisonSection: {
    marginTop: tokens.spacingVerticalXXL,
  },
  table: {
    width: '100%',
  },
  checkIcon: {
    color: tokens.colorPaletteGreenForeground1,
  },
  dismissIcon: {
    color: tokens.colorNeutralForeground3,
  },
  costEstimate: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
});

export interface ChooseTierStepProps {
  selectedTier: 'free' | 'pro' | null;
  onSelectTier: (tier: 'free' | 'pro') => void;
}

export function ChooseTierStep({ selectedTier, onSelectTier }: ChooseTierStepProps) {
  const styles = useStyles();

  const freeFeatures = [
    'Windows built-in text-to-speech',
    'Rule-based script generation',
    'Stock images from Pexels/Unsplash',
    'Basic video effects',
    'No setup required',
  ];

  const proFeatures = [
    'GPT-4 powered script generation',
    'Professional AI voices (ElevenLabs)',
    'Custom image generation (Stable Diffusion)',
    'Advanced editing capabilities',
    'Priority support',
  ];

  const comparisonData: Array<{ feature: string; free: string; pro: string }> = [
    { feature: 'Script Generation', free: 'Rule-based', pro: 'GPT-4 / Claude / Gemini' },
    { feature: 'Text-to-Speech', free: 'Windows TTS', pro: 'ElevenLabs / PlayHT' },
    { feature: 'Images', free: 'Stock photos', pro: 'AI-generated custom images' },
    { feature: 'Setup Time', free: '< 1 minute', pro: '3-5 minutes' },
    { feature: 'Cost', free: 'Free forever', pro: '~$1-5 per video' },
    { feature: 'Quality', free: 'Good', pro: 'Professional' },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Choose Your Experience</Title2>
        <Text>Select the tier that best fits your needs. You can change this later.</Text>
      </div>

      <div className={styles.cardsContainer}>
        {/* Free Tier Card */}
        <Card
          className={`${styles.tierCard} ${selectedTier === 'free' ? styles.selectedCard : ''}`}
          onClick={() => onSelectTier('free')}
        >
          <Title3>🆓 Start Free</Title3>
          <Text style={{ marginTop: tokens.spacingVerticalS, display: 'block' }}>
            Perfect for getting started quickly with zero setup
          </Text>

          <ul className={styles.featuresList}>
            {freeFeatures.map((feature, index) => (
              <li key={index} className={styles.featureItem}>
                <Checkmark20Regular className={styles.checkIcon} />
                <Text size={200}>{feature}</Text>
              </li>
            ))}
          </ul>

          <Button
            appearance={selectedTier === 'free' ? 'primary' : 'secondary'}
            style={{ width: '100%', marginTop: tokens.spacingVerticalM }}
            onClick={(e) => {
              e.stopPropagation();
              onSelectTier('free');
            }}
          >
            {selectedTier === 'free' ? 'Selected' : 'Select Free'}
          </Button>
        </Card>

        {/* Pro Tier Card */}
        <Card
          className={`${styles.tierCard} ${selectedTier === 'pro' ? styles.selectedCard : ''}`}
          onClick={() => onSelectTier('pro')}
        >
          <div className={styles.badge}>RECOMMENDED</div>
          <Title3>⭐ Unlock Pro Features</Title3>
          <Text style={{ marginTop: tokens.spacingVerticalS, display: 'block' }}>
            Professional quality AI-powered video creation
          </Text>

          <ul className={styles.featuresList}>
            {proFeatures.map((feature, index) => (
              <li key={index} className={styles.featureItem}>
                <Checkmark20Regular className={styles.checkIcon} />
                <Text size={200}>{feature}</Text>
              </li>
            ))}
          </ul>

          <div className={styles.costEstimate}>
            <Text weight="semibold" size={200}>
              Estimated cost per video:
            </Text>
            <Text size={200} style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
              $1-5 depending on length and features used
            </Text>
          </div>

          <Button
            appearance={selectedTier === 'pro' ? 'primary' : 'secondary'}
            style={{ width: '100%', marginTop: tokens.spacingVerticalM }}
            onClick={(e) => {
              e.stopPropagation();
              onSelectTier('pro');
            }}
          >
            {selectedTier === 'pro' ? 'Selected' : 'Select Pro'}
          </Button>
        </Card>
      </div>

      {/* Comparison Table */}
      <div className={styles.comparisonSection}>
        <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>Feature Comparison</Title3>
        <Table className={styles.table}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Feature</TableHeaderCell>
              <TableHeaderCell>Free</TableHeaderCell>
              <TableHeaderCell>Pro</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {comparisonData.map((row, index) => (
              <TableRow key={index}>
                <TableCell>
                  <Text weight="semibold">{row.feature}</Text>
                </TableCell>
                <TableCell>{row.free}</TableCell>
                <TableCell>{row.pro}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
