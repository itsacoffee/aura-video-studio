import {
  makeStyles,
  tokens,
  Title1,
  Title3,
  Text,
  Card,
} from '@fluentui/react-components';
import { 
  VideoClip24Regular, 
  Sparkle24Regular, 
  Clock24Regular 
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXL,
    alignItems: 'center',
    textAlign: 'center',
  },
  logoContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  logo: {
    fontSize: '64px',
    animation: 'fadeIn 1s ease-in-out',
  },
  '@keyframes fadeIn': {
    from: { opacity: 0, transform: 'scale(0.8)' },
    to: { opacity: 1, transform: 'scale(1)' },
  },
  title: {
    marginBottom: tokens.spacingVerticalM,
  },
  subtitle: {
    maxWidth: '600px',
    marginBottom: tokens.spacingVerticalL,
  },
  cardsContainer: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
    gap: tokens.spacingHorizontalL,
    width: '100%',
    marginTop: tokens.spacingVerticalL,
  },
  valueCard: {
    padding: tokens.spacingVerticalXL,
    textAlign: 'center',
    transition: 'transform 0.3s ease-in-out',
    ':hover': {
      transform: 'translateY(-4px)',
    },
  },
  icon: {
    fontSize: '48px',
    marginBottom: tokens.spacingVerticalM,
    display: 'block',
  },
  timeEstimate: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalXL,
  },
});

export function WelcomeStep() {
  const styles = useStyles();

  const valuePropositions = [
    {
      icon: <VideoClip24Regular className={styles.icon} />,
      title: 'Create Amazing Videos',
      description: 'Transform your ideas into professional videos in minutes',
    },
    {
      icon: <Sparkle24Regular className={styles.icon} />,
      title: 'AI-Powered Tools',
      description: 'Leverage cutting-edge AI for scripts, voices, and visuals',
    },
    {
      icon: <Clock24Regular className={styles.icon} />,
      title: 'Save Time',
      description: 'Automate the tedious parts and focus on creativity',
    },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.logoContainer}>
        <div className={styles.logo}>🎬</div>
      </div>

      <div>
        <Title1 className={styles.title}>Welcome to Aura Video Studio!</Title1>
        <Text className={styles.subtitle} size={400}>
          Your all-in-one platform for creating professional videos with the power of AI. 
          Let&apos;s get you set up in just a few steps.
        </Text>
      </div>

      <div className={styles.cardsContainer}>
        {valuePropositions.map((prop, index) => (
          <Card key={index} className={styles.valueCard}>
            {prop.icon}
            <Title3 style={{ marginBottom: tokens.spacingVerticalS }}>{prop.title}</Title3>
            <Text size={200}>{prop.description}</Text>
          </Card>
        ))}
      </div>

      <div className={styles.timeEstimate}>
        <Text weight="semibold">⏱️ Setup takes 3-5 minutes</Text>
        <Text size={200} style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
          You can pause and resume at any time
        </Text>
      </div>
    </div>
  );
}
