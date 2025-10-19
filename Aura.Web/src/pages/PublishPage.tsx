import {
  makeStyles,
  tokens,
  Title1,
  Text,
  Button,
  Input,
  Card,
  Field,
} from '@fluentui/react-components';
import { Share24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import { useNotifications } from '../components/Notifications/Toasts';

const useStyles = makeStyles({
  container: {
    maxWidth: '800px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    padding: tokens.spacingVerticalXL,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalXL,
  },
});

export function PublishPage() {
  const styles = useStyles();
  const { showFailureToast } = useNotifications();
  const [metadata, setMetadata] = useState({
    title: '',
    description: '',
    tags: '',
  });

  const handlePublish = () => {
    showFailureToast({
      title: 'OAuth Setup Required',
      message: 'YouTube publishing requires OAuth setup. Configure OAuth credentials in Settings to enable direct publishing.',
    });
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Publish to YouTube</Title1>
        <Text className={styles.subtitle}>
          Add metadata and publish your rendered video to YouTube
        </Text>
      </div>

      <Card className={styles.section}>
        <div className={styles.form}>
          <Field label="Title" required hint="Your video title (max 100 characters)">
            <Input
              value={metadata.title}
              onChange={(_, data) => setMetadata({ ...metadata, title: data.value })}
              placeholder="Enter video title"
            />
          </Field>

          <Field label="Description" hint="Describe your video (max 5000 characters)">
            <Input
              value={metadata.description}
              onChange={(_, data) => setMetadata({ ...metadata, description: data.value })}
              placeholder="Enter video description"
            />
          </Field>

          <Field label="Tags" hint="Comma-separated tags to help viewers find your video">
            <Input
              value={metadata.tags}
              onChange={(_, data) => setMetadata({ ...metadata, tags: data.value })}
              placeholder="e.g. tutorial, howto, education"
            />
          </Field>

          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            Note: YouTube publishing requires OAuth authentication setup
          </Text>

          <div className={styles.actions}>
            <Button appearance="primary" icon={<Share24Regular />} onClick={handlePublish}>
              Publish to YouTube
            </Button>
          </div>
        </div>
      </Card>
    </div>
  );
}
