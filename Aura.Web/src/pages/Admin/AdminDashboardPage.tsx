import React from 'react';
import { Card, Text, Title1 } from '@fluentui/react-components';

/**
 * Placeholder for Admin Dashboard
 * Full implementation requires @tremor/react
 */
export default function AdminDashboardPage() {
  return (
    <div style={{ padding: '20px' }}>
      <Card>
        <Title1>Admin Dashboard</Title1>
        <Text>Admin features require additional dependencies to be installed.</Text>
        <Text block style={{ marginTop: '10px', color: '#666' }}>
          Install @tremor/react to enable full admin functionality.
        </Text>
      </Card>
    </div>
  );
}
