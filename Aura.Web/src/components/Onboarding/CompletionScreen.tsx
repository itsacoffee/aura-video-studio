import {
  makeStyles,
  tokens,
  Title1,
  Title3,
  Text,
  Card,
  Button,
  Checkbox,
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  VideoClip24Regular,
  Apps24Regular,
  BookOpen24Regular,
  Lightbulb24Regular,
  QuestionCircle24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXL,
    alignItems: 'center',
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
  },
  successIcon: {
    fontSize: '120px',
    color: tokens.colorPaletteGreenForeground1,
    marginBottom: tokens.spacingVerticalL,
    animation: 'successAnimation 0.8s ease-in-out',
  },
  '@keyframes successAnimation': {
    '0%': { opacity: 0, transform: 'scale(0) rotate(-180deg)' },
    '50%': { transform: 'scale(1.2) rotate(10deg)' },
    '100%': { opacity: 1, transform: 'scale(1) rotate(0deg)' },
  },
  title: {
    marginBottom: tokens.spacingVerticalM,
    fontSize: '42px',
  },
  subtitle: {
    maxWidth: '700px',
    marginBottom: tokens.spacingVerticalL,
    fontSize: '18px',
    lineHeight: '1.6',
    color: tokens.colorNeutralForeground2,
  },
  summaryCard: {
    padding: tokens.spacingVerticalXXL,
    width: '100%',
    maxWidth: '700px',
    textAlign: 'left',
    marginTop: tokens.spacingVerticalL,
    background: `linear-gradient(135deg, ${tokens.colorNeutralBackground1} 0%, ${tokens.colorNeutralBackground2} 100%)`,
  },
  summaryHeader: {
    marginBottom: tokens.spacingVerticalL,
    textAlign: 'center',
  },
  summaryList: {
    listStyleType: 'none',
    padding: 0,
    margin: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  summaryItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
  },
  summaryIcon: {
    fontSize: '24px',
    color: tokens.colorPaletteGreenForeground1,
  },
  summaryContent: {
    flex: 1,
  },
  resourcesContainer: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
    gap: tokens.spacingHorizontalL,
    width: '100%',
    marginTop: tokens.spacingVerticalXL,
  },
  resourceCard: {
    padding: tokens.spacingVerticalL,
    textAlign: 'center',
    cursor: 'pointer',
    transition: 'all 0.3s ease-in-out',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: tokens.shadow16,
    },
  },
  resourceIcon: {
    fontSize: '48px',
    marginBottom: tokens.spacingVerticalM,
    display: 'block',
  },
  resourceTitle: {
    marginBottom: tokens.spacingVerticalS,
  },
  resourceDescription: {
    color: tokens.colorNeutralForeground3,
  },
  ctaContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    marginTop: tokens.spacingVerticalXXL,
    alignItems: 'center',
  },
  buttonGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    flexWrap: 'wrap',
    justifyContent: 'center',
  },
  primaryButton: {
    fontSize: '18px',
    padding: '14px 36px',
    height: 'auto',
  },
  secondaryButton: {
    fontSize: '16px',
    padding: '12px 28px',
    height: 'auto',
  },
  neverShowAgain: {
    marginTop: tokens.spacingVerticalM,
  },
});

export interface CompletionSummary {
  tier: 'free' | 'pro';
  apiKeysConfigured: string[];
  hardwareDetected: boolean;
  componentsInstalled: string[];
  workspaceConfigured: boolean;
  tutorialCompleted?: boolean;
  templateSelected?: string;
}

export interface CompletionScreenProps {
  summary: CompletionSummary;
  onCreateFirstVideo: () => void;
  onExploreApp: () => void;
  onNeverShowAgain?: (checked: boolean) => void;
  showNeverShowAgain?: boolean;
}

export function CompletionScreen({
  summary,
  onCreateFirstVideo,
  onExploreApp,
  onNeverShowAgain,
  showNeverShowAgain = true,
}: CompletionScreenProps) {
  const styles = useStyles();

  const resources = [
    {
      icon: <BookOpen24Regular className={styles.resourceIcon} />,
      title: 'Documentation',
      description: 'Learn the ins and outs of Aura Video Studio',
      link: '/docs',
    },
    {
      icon: <VideoClip24Regular className={styles.resourceIcon} />,
      title: 'Video Tutorials',
      description: 'Watch step-by-step guides and tips',
      link: '/tutorials',
    },
    {
      icon: <QuestionCircle24Regular className={styles.resourceIcon} />,
      title: 'Community & Support',
      description: 'Get help and connect with other creators',
      link: '/community',
    },
  ];

  return (
    <div className={styles.container}>
      {/* Success Animation */}
      <Checkmark24Regular className={styles.successIcon} />

      {/* Title & Subtitle */}
      <div>
        <Title1 className={styles.title}>You&apos;re All Set! 🎉</Title1>
        <Text className={styles.subtitle} size={500}>
          Congratulations! Your workspace is configured and ready to go. Let&apos;s create something
          amazing together.
        </Text>
      </div>

      {/* Configuration Summary */}
      <Card className={styles.summaryCard}>
        <div className={styles.summaryHeader}>
          <Title3>Your Configuration</Title3>
        </div>
        <ul className={styles.summaryList}>
          <li className={styles.summaryItem}>
            <Checkmark24Regular className={styles.summaryIcon} />
            <div className={styles.summaryContent}>
              <Text weight="semibold" size={400}>
                Tier: {summary.tier === 'free' ? 'Free (Stock)' : 'Pro (AI-Powered)'}
              </Text>
              <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                {summary.tier === 'free'
                  ? 'Using built-in tools and stock content'
                  : 'AI features enabled for professional content creation'}
              </Text>
            </div>
          </li>

          {summary.apiKeysConfigured.length > 0 && (
            <li className={styles.summaryItem}>
              <Checkmark24Regular className={styles.summaryIcon} />
              <div className={styles.summaryContent}>
                <Text weight="semibold" size={400}>
                  API Keys Configured: {summary.apiKeysConfigured.length}
                </Text>
                <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                  {summary.apiKeysConfigured.join(', ')}
                </Text>
              </div>
            </li>
          )}

          {summary.hardwareDetected && (
            <li className={styles.summaryItem}>
              <Checkmark24Regular className={styles.summaryIcon} />
              <div className={styles.summaryContent}>
                <Text weight="semibold" size={400}>
                  Hardware Optimized
                </Text>
                <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                  System detected and performance settings optimized
                </Text>
              </div>
            </li>
          )}

          {summary.componentsInstalled.length > 0 && (
            <li className={styles.summaryItem}>
              <Checkmark24Regular className={styles.summaryIcon} />
              <div className={styles.summaryContent}>
                <Text weight="semibold" size={400}>
                  Components Installed: {summary.componentsInstalled.length}
                </Text>
                <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                  {summary.componentsInstalled.join(', ')}
                </Text>
              </div>
            </li>
          )}

          {summary.workspaceConfigured && (
            <li className={styles.summaryItem}>
              <Checkmark24Regular className={styles.summaryIcon} />
              <div className={styles.summaryContent}>
                <Text weight="semibold" size={400}>
                  Workspace Configured
                </Text>
                <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                  Save location, cache, and preferences set
                </Text>
              </div>
            </li>
          )}

          {summary.templateSelected && (
            <li className={styles.summaryItem}>
              <Checkmark24Regular className={styles.summaryIcon} />
              <div className={styles.summaryContent}>
                <Text weight="semibold" size={400}>
                  Template Selected
                </Text>
                <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                  {summary.templateSelected} template ready to use
                </Text>
              </div>
            </li>
          )}
        </ul>
      </Card>

      {/* Quick Start Resources */}
      <div style={{ width: '100%' }}>
        <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>
          <Lightbulb24Regular
            style={{ verticalAlign: 'middle', marginRight: tokens.spacingHorizontalS }}
          />
          Learn More
        </Title3>
        <div className={styles.resourcesContainer}>
          {resources.map((resource, index) => (
            <Card
              key={index}
              className={styles.resourceCard}
              onClick={() => window.open(resource.link, '_blank')}
            >
              {resource.icon}
              <Text className={styles.resourceTitle} weight="semibold" size={400}>
                {resource.title}
              </Text>
              <Text className={styles.resourceDescription} size={200}>
                {resource.description}
              </Text>
            </Card>
          ))}
        </div>
      </div>

      {/* Call to Action */}
      <div className={styles.ctaContainer}>
        <div className={styles.buttonGroup}>
          <Button
            appearance="primary"
            size="large"
            className={styles.primaryButton}
            icon={<VideoClip24Regular />}
            onClick={onCreateFirstVideo}
          >
            Create Your First Video
          </Button>
          <Button
            appearance="secondary"
            size="large"
            className={styles.secondaryButton}
            icon={<Apps24Regular />}
            onClick={onExploreApp}
          >
            Explore the App
          </Button>
        </div>

        {showNeverShowAgain && onNeverShowAgain && (
          <div className={styles.neverShowAgain}>
            <Checkbox
              label="Don't show this wizard again (you can reset it in Settings)"
              onChange={(_, data) => onNeverShowAgain(data.checked === true)}
            />
          </div>
        )}
      </div>
    </div>
  );
}
