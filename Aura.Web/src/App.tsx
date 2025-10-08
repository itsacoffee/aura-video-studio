import { useState, useEffect } from 'react'
import { FluentProvider, webLightTheme, Text, Title1, Card, CardHeader, Button } from '@fluentui/react-components'
import './App.css'

interface HealthStatus {
  status: string;
  timestamp: string;
}

function App() {
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('/api/healthz')
      .then(res => res.json())
      .then(data => {
        setHealth(data);
        setLoading(false);
      })
      .catch(err => {
        console.error('Health check failed:', err);
        setLoading(false);
      });
  }, []);

  return (
    <FluentProvider theme={webLightTheme}>
      <div className="App">
        <Title1>Aura Video Studio</Title1>
        <Card>
          <CardHeader
            header={<Text weight="semibold">API Status</Text>}
            description={
              loading ? (
                <Text>Checking...</Text>
              ) : health ? (
                <Text>✓ {health.status} - {new Date(health.timestamp).toLocaleString()}</Text>
              ) : (
                <Text>✗ API unavailable</Text>
              )
            }
          />
        </Card>
        <div style={{ marginTop: '2rem' }}>
          <Text>Welcome to Aura Video Studio - Your AI-powered video creation tool</Text>
        </div>
        <div style={{ marginTop: '1rem' }}>
          <Button appearance="primary">Get Started</Button>
        </div>
      </div>
    </FluentProvider>
  )
}

export default App
