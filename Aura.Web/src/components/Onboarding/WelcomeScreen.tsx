import { Button, Card, makeStyles, Text, Title1, Title3, tokens } from '@fluentui/react-components';
import {
  Clock24Regular,
  FolderOpen24Regular,
  Play24Regular,
  Sparkle24Regular,
  VideoClip24Regular,
} from '@fluentui/react-icons';
import { Logo } from '../Logo';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXL,
    alignItems: 'center',
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
  },
  heroContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalXXL,
    marginTop: tokens.spacingVerticalL,
    position: 'relative',
  },
  heroGraphic: {
    width: '150px',
    height: '150px',
    animation: 'heroAnimation 2s ease-in-out infinite',
    filter: `drop-shadow(0 0 40px ${tokens.colorBrandBackground2})`,
  },
  '@keyframes heroAnimation': {
    '0%, 100%': { transform: 'scale(1) rotate(0deg)' },
    '50%': { transform: 'scale(1.05) rotate(2deg)' },
  },
  brandContainer: {
    marginBottom: tokens.spacingVerticalXL,
  },
  title: {
    marginBottom: tokens.spacingVerticalM,
    fontSize: '42px',
    fontWeight: tokens.fontWeightBold,
  },
  subtitle: {
    maxWidth: '700px',
    marginBottom: tokens.spacingVerticalL,
    fontSize: '18px',
    lineHeight: '1.6',
    color: tokens.colorNeutralForeground2,
  },
  valuePropsContainer: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
    gap: tokens.spacingHorizontalL,
    width: '100%',
    marginTop: tokens.spacingVerticalXL,
  },
  valueCard: {
    padding: tokens.spacingVerticalXL,
    textAlign: 'center',
    transition: 'all 0.3s ease-in-out',
    background: `linear-gradient(135deg, ${tokens.colorNeutralBackground1} 0%, ${tokens.colorNeutralBackground2} 100%)`,
    ':hover': {
      transform: 'translateY(-8px)',
      boxShadow: tokens.shadow28,
    },
  },
  icon: {
    fontSize: '56px',
    marginBottom: tokens.spacingVerticalM,
    display: 'block',
    color: tokens.colorBrandBackground,
  },
  cardTitle: {
    marginBottom: tokens.spacingVerticalS,
    fontWeight: tokens.fontWeightSemibold,
  },
  cardDescription: {
    color: tokens.colorNeutralForeground3,
    lineHeight: '1.5',
  },
  ctaContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    marginTop: tokens.spacingVerticalXXL,
    justifyContent: 'center',
    flexWrap: 'wrap',
  },
  primaryButton: {
    fontSize: '16px',
    padding: '12px 32px',
    height: 'auto',
  },
  secondaryButton: {
    fontSize: '16px',
    padding: '12px 32px',
    height: 'auto',
  },
  timeEstimate: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalXL,
    maxWidth: '500px',
  },
  estimateIcon: {
    fontSize: '24px',
    marginRight: tokens.spacingHorizontalS,
    verticalAlign: 'middle',
  },
});

export interface WelcomeScreenProps {
  onGetStarted: () => void;
  onImportProject?: () => void;
}

export function WelcomeScreen({ onGetStarted, onImportProject }: WelcomeScreenProps) {
  const styles = useStyles();

  const valuePropositions = [
    {
      icon: <VideoClip24Regular className={styles.icon} />,
      title: 'Essential Setup Required',
      description:
        'Configure FFmpeg, AI providers, and your workspace to unlock the full power of video generation',
    },
    {
      icon: <Sparkle24Regular className={styles.icon} />,
      title: 'AI-Powered Automation',
      description:
        'Set up script generation, voice synthesis, and custom image creation with your preferred providers',
    },
    {
      icon: <Clock24Regular className={styles.icon} />,
      title: 'Quick Configuration',
      description:
        'Complete the Setup Wizard in just 3-5 minutes to start creating professional videos',
    },
  ];

  return (
    <div className={styles.container}>
      {/* Hero Section */}
      <div className={styles.heroContainer}>
        <Logo size={150} className={styles.heroGraphic} />
      </div>

      {/* Brand & Value Prop */}
      <div className={styles.brandContainer}>
        <Title1 className={styles.title}>Welcome to Aura Video Studio!</Title1>
        <Text className={styles.subtitle} size={500} block>
          Complete your setup to start generating videos. Run the Setup Wizard now to configure AI
          providers, FFmpeg, and your workspace. You can update these settings later in Settings.
        </Text>
      </div>

      {/* Value Propositions Grid */}
      <div className={styles.valuePropsContainer}>
        {valuePropositions.map((prop, index) => (
          <Card key={index} className={styles.valueCard}>
            {prop.icon}
            <Title3 className={styles.cardTitle}>{prop.title}</Title3>
            <Text className={styles.cardDescription} size={300}>
              {prop.description}
            </Text>
          </Card>
        ))}
      </div>

      {/* Call to Action */}
      <div className={styles.ctaContainer}>
        <Button
          appearance="primary"
          size="large"
          className={styles.primaryButton}
          icon={<Play24Regular />}
          onClick={onGetStarted}
        >
          Start Setup Wizard
        </Button>
        {onImportProject && (
          <Button
            appearance="secondary"
            size="large"
            className={styles.secondaryButton}
            icon={<FolderOpen24Regular />}
            onClick={onImportProject}
          >
            Import Existing Project
          </Button>
        )}
      </div>

      {/* Time Estimate */}
      <div className={styles.timeEstimate}>
        <Text weight="semibold" size={400}>
          <Clock24Regular className={styles.estimateIcon} />
          Quick Setup: 3-5 minutes
        </Text>
        <Text size={300} style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
          You can pause and resume at any time. Your progress is automatically saved.
        </Text>
      </div>
    </div>
  );
}
