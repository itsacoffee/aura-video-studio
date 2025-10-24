import { useState } from 'react';
import {
  CheckmarkCircle20Regular as CheckCircle,
  DismissCircle20Regular as XCircle,
  Warning20Regular as AlertTriangle,
  Info20Regular as Info
} from '@fluentui/react-icons';

interface Claim {
  claimId: string;
  text: string;
  type: string;
  extractionConfidence: number;
}

interface FactCheck {
  claimId: string;
  claim: string;
  status: string;
  confidenceScore: number;
  explanation: string;
  verifiedAt: string;
}

interface VerificationResult {
  contentId: string;
  claims: Claim[];
  factChecks: FactCheck[];
  overallStatus: string;
  overallConfidence: number;
  warnings: string[];
}

interface FactCheckPanelProps {
  content: string;
  onVerify?: (result: VerificationResult) => void;
}

export const FactCheckPanel: React.FC<FactCheckPanelProps> = ({ content, onVerify }) => {
  const [isVerifying, setIsVerifying] = useState(false);
  const [result, setResult] = useState<VerificationResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleVerify = async () => {
    if (!content.trim()) {
      setError('Please enter content to verify');
      return;
    }

    setIsVerifying(true);
    setError(null);

    try {
      const response = await fetch('/api/verification/verify', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          content,
          options: {
            checkFacts: true,
            detectMisinformation: true,
            analyzeConfidence: true,
            attributeSources: true,
          },
        }),
      });

      if (!response.ok) {
        throw new Error('Verification failed');
      }

      const data = await response.json();
      setResult(data.details);
      onVerify?.(data.details);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setIsVerifying(false);
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'verified':
        return <CheckCircle className="h-5 w-5 text-green-500" />;
      case 'partiallyverified':
        return <Info className="h-5 w-5 text-blue-500" />;
      case 'disputed':
      case 'false':
        return <XCircle className="h-5 w-5 text-red-500" />;
      default:
        return <AlertTriangle className="h-5 w-5 text-yellow-500" />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'verified':
        return 'bg-green-50 border-green-200 text-green-800';
      case 'partiallyverified':
        return 'bg-blue-50 border-blue-200 text-blue-800';
      case 'disputed':
      case 'false':
        return 'bg-red-50 border-red-200 text-red-800';
      default:
        return 'bg-yellow-50 border-yellow-200 text-yellow-800';
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold">Fact Checking</h3>
        <button
          onClick={handleVerify}
          disabled={isVerifying || !content.trim()}
          className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isVerifying ? 'Verifying...' : 'Verify Content'}
        </button>
      </div>

      {error && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-md text-red-800">
          {error}
        </div>
      )}

      {result && (
        <div className="space-y-4">
          {/* Overall Status */}
          <div
            className={`p-4 rounded-md border ${getStatusColor(result.overallStatus)}`}
          >
            <div className="flex items-center gap-2 mb-2">
              {getStatusIcon(result.overallStatus)}
              <h4 className="font-semibold">
                Overall Status: {result.overallStatus}
              </h4>
            </div>
            <p className="text-sm">
              Confidence: {(result.overallConfidence * 100).toFixed(1)}%
            </p>
            <div className="mt-2 w-full bg-gray-200 rounded-full h-2">
              <div
                className="bg-blue-600 h-2 rounded-full transition-all"
                style={{ width: `${result.overallConfidence * 100}%` }}
              />
            </div>
          </div>

          {/* Warnings */}
          {result.warnings && result.warnings.length > 0 && (
            <div className="p-4 bg-yellow-50 border border-yellow-200 rounded-md">
              <h4 className="font-semibold text-yellow-800 mb-2">Warnings</h4>
              <ul className="space-y-1 text-sm text-yellow-700">
                {result.warnings.map((warning, idx) => (
                  <li key={idx} className="flex items-start gap-2">
                    <AlertTriangle className="h-4 w-4 mt-0.5 flex-shrink-0" />
                    <span>{warning}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {/* Fact Checks */}
          <div>
            <h4 className="font-semibold mb-2">
              Fact Checks ({result.factChecks.length})
            </h4>
            <div className="space-y-2 max-h-96 overflow-y-auto">
              {result.factChecks.map((factCheck) => (
                <div
                  key={factCheck.claimId}
                  className={`p-3 rounded-md border ${getStatusColor(factCheck.status)}`}
                >
                  <div className="flex items-start gap-2 mb-1">
                    {getStatusIcon(factCheck.status)}
                    <div className="flex-1">
                      <p className="text-sm font-medium">{factCheck.claim}</p>
                      <p className="text-xs mt-1 opacity-75">
                        Confidence: {(factCheck.confidenceScore * 100).toFixed(1)}%
                      </p>
                      {factCheck.explanation && (
                        <p className="text-xs mt-2">{factCheck.explanation}</p>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Summary */}
          <div className="p-4 bg-gray-50 border border-gray-200 rounded-md text-sm">
            <h4 className="font-semibold mb-2">Summary</h4>
            <ul className="space-y-1 text-gray-700">
              <li>Total Claims: {result.claims.length}</li>
              <li>Verified: {result.factChecks.filter(fc => fc.status === 'Verified').length}</li>
              <li>Disputed: {result.factChecks.filter(fc => fc.status === 'Disputed' || fc.status === 'False').length}</li>
              <li>Unverified: {result.factChecks.filter(fc => fc.status === 'Unverified' || fc.status === 'Unknown').length}</li>
            </ul>
          </div>
        </div>
      )}
    </div>
  );
};
