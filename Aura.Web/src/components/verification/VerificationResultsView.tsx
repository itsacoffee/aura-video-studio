import React from 'react';
import { CheckCircle, XCircle, AlertTriangle, Info, ExternalLink } from 'lucide-react';
import { ConfidenceMeter } from './ConfidenceMeter';

interface Source {
  sourceId: string;
  name: string;
  url: string;
  type: string;
  credibilityScore: number;
  publishedDate?: string;
  author?: string;
}

interface MisinformationFlag {
  flagId: string;
  pattern: string;
  category: string;
  severity: number;
  description: string;
  suggestedCorrections: string[];
}

interface VerificationResult {
  contentId: string;
  overallStatus: string;
  overallConfidence: number;
  warnings: string[];
  sources: Source[];
  misinformation?: {
    riskLevel: string;
    riskScore: number;
    flags: MisinformationFlag[];
    recommendations: string[];
  };
  verifiedAt: string;
}

interface VerificationResultsViewProps {
  result: VerificationResult;
}

export const VerificationResultsView: React.FC<VerificationResultsViewProps> = ({ result }) => {
  const getStatusBadge = (status: string) => {
    const statusLower = status.toLowerCase();
    const styles = {
      verified: 'bg-green-100 text-green-800 border-green-200',
      partiallyverified: 'bg-blue-100 text-blue-800 border-blue-200',
      disputed: 'bg-red-100 text-red-800 border-red-200',
      false: 'bg-red-100 text-red-800 border-red-200',
      unverified: 'bg-yellow-100 text-yellow-800 border-yellow-200',
      unknown: 'bg-gray-100 text-gray-800 border-gray-200',
    };

    return (
      <span className={`px-2 py-1 rounded text-xs font-semibold border ${styles[statusLower] || styles.unknown}`}>
        {status}
      </span>
    );
  };

  const getRiskLevelBadge = (level: string) => {
    const styles = {
      low: 'bg-green-100 text-green-800',
      medium: 'bg-yellow-100 text-yellow-800',
      high: 'bg-orange-100 text-orange-800',
      critical: 'bg-red-100 text-red-800',
    };

    return (
      <span className={`px-2 py-1 rounded text-xs font-semibold ${styles[level.toLowerCase()] || styles.low}`}>
        {level} Risk
      </span>
    );
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="border-b pb-4">
        <div className="flex items-center justify-between mb-2">
          <h2 className="text-2xl font-bold">Verification Results</h2>
          {getStatusBadge(result.overallStatus)}
        </div>
        <p className="text-sm text-gray-600">
          Verified at: {new Date(result.verifiedAt).toLocaleString()}
        </p>
      </div>

      {/* Overall Confidence */}
      <div className="bg-white p-4 rounded-lg border">
        <ConfidenceMeter 
          confidence={result.overallConfidence} 
          label="Overall Confidence"
          size="lg"
        />
      </div>

      {/* Warnings */}
      {result.warnings && result.warnings.length > 0 && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <div className="flex items-center gap-2 mb-3">
            <AlertTriangle className="h-5 w-5 text-yellow-600" />
            <h3 className="font-semibold text-yellow-800">Warnings</h3>
          </div>
          <ul className="space-y-2">
            {result.warnings.map((warning, idx) => (
              <li key={idx} className="flex items-start gap-2 text-sm text-yellow-700">
                <span className="font-medium">•</span>
                <span>{warning}</span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Misinformation Detection */}
      {result.misinformation && (
        <div className="bg-white border rounded-lg p-4">
          <div className="flex items-center justify-between mb-4">
            <h3 className="font-semibold">Misinformation Analysis</h3>
            {getRiskLevelBadge(result.misinformation.riskLevel)}
          </div>

          {result.misinformation.flags.length > 0 && (
            <div className="space-y-3 mb-4">
              <h4 className="text-sm font-medium text-gray-700">
                Detected Issues ({result.misinformation.flags.length})
              </h4>
              {result.misinformation.flags.map((flag) => (
                <div key={flag.flagId} className="bg-red-50 border border-red-200 rounded p-3">
                  <div className="flex items-start gap-2 mb-2">
                    <XCircle className="h-4 w-4 text-red-500 mt-0.5 flex-shrink-0" />
                    <div className="flex-1">
                      <p className="text-sm font-medium text-red-800">{flag.pattern}</p>
                      <p className="text-xs text-red-600 mt-1">{flag.description}</p>
                      <div className="mt-2 flex items-center gap-2">
                        <span className="text-xs text-red-700">Severity:</span>
                        <div className="flex-1 bg-red-200 rounded-full h-1.5">
                          <div 
                            className="bg-red-600 h-1.5 rounded-full"
                            style={{ width: `${flag.severity * 100}%` }}
                          />
                        </div>
                      </div>
                      {flag.suggestedCorrections.length > 0 && (
                        <div className="mt-2">
                          <p className="text-xs font-medium text-red-700 mb-1">Suggestions:</p>
                          <ul className="text-xs text-red-600 space-y-1">
                            {flag.suggestedCorrections.map((correction, idx) => (
                              <li key={idx}>• {correction}</li>
                            ))}
                          </ul>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}

          {result.misinformation.recommendations.length > 0 && (
            <div className="bg-blue-50 border border-blue-200 rounded p-3">
              <h4 className="text-sm font-medium text-blue-800 mb-2">Recommendations</h4>
              <ul className="text-sm text-blue-700 space-y-1">
                {result.misinformation.recommendations.map((rec, idx) => (
                  <li key={idx} className="flex items-start gap-2">
                    <Info className="h-4 w-4 mt-0.5 flex-shrink-0" />
                    <span>{rec}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}

      {/* Sources */}
      {result.sources && result.sources.length > 0 && (
        <div className="bg-white border rounded-lg p-4">
          <h3 className="font-semibold mb-3">Sources ({result.sources.length})</h3>
          <div className="space-y-2">
            {result.sources.map((source) => (
              <div key={source.sourceId} className="bg-gray-50 border rounded p-3">
                <div className="flex items-start justify-between gap-2">
                  <div className="flex-1">
                    <h4 className="text-sm font-medium">{source.name}</h4>
                    <p className="text-xs text-gray-600 mt-1">
                      Type: {source.type}
                      {source.author && ` • Author: ${source.author}`}
                    </p>
                    {source.publishedDate && (
                      <p className="text-xs text-gray-500 mt-1">
                        Published: {new Date(source.publishedDate).toLocaleDateString()}
                      </p>
                    )}
                    <div className="mt-2">
                      <ConfidenceMeter 
                        confidence={source.credibilityScore}
                        label="Credibility"
                        size="sm"
                      />
                    </div>
                  </div>
                  <a
                    href={source.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-blue-600 hover:text-blue-800"
                  >
                    <ExternalLink className="h-4 w-4" />
                  </a>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
