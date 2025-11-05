/**
 * TypeScript types for RunTelemetry v1 schema
 */

export type RunStage =
  | 'brief'
  | 'plan'
  | 'script'
  | 'ssml'
  | 'tts'
  | 'visuals'
  | 'render'
  | 'post';

export type SelectionSource = 'default' | 'pinned' | 'cli' | 'fallback';

export type ResultStatus = 'ok' | 'warn' | 'error';

export interface RunTelemetryRecord {
  version: string;
  job_id: string;
  correlation_id: string;
  project_id?: string | null;
  stage: RunStage;
  scene_index?: number | null;
  model_id?: string | null;
  provider?: string | null;
  selection_source?: SelectionSource | null;
  fallback_reason?: string | null;
  tokens_in?: number | null;
  tokens_out?: number | null;
  cache_hit?: boolean | null;
  retries: number;
  latency_ms: number;
  cost_estimate?: number | null;
  currency: string;
  pricing_version?: string | null;
  result_status: ResultStatus;
  error_code?: string | null;
  message?: string | null;
  started_at: string;
  ended_at: string;
  metadata?: Record<string, unknown> | null;
}

export interface RunTelemetrySummary {
  total_operations: number;
  successful_operations: number;
  failed_operations: number;
  total_cost: number;
  currency: string;
  total_latency_ms: number;
  total_tokens_in: number;
  total_tokens_out: number;
  cache_hits: number;
  total_retries: number;
  cost_by_stage?: Record<string, number> | null;
  operations_by_provider?: Record<string, number> | null;
}

export interface RunTelemetryCollection {
  version: string;
  job_id: string;
  correlation_id: string;
  collection_started_at: string;
  collection_ended_at?: string | null;
  records: RunTelemetryRecord[];
  summary?: RunTelemetrySummary | null;
}

export interface TelemetrySchemaInfo {
  version: string;
  schemaUrl: string;
  description: string;
  stages: RunStage[];
  resultStatuses: ResultStatus[];
  selectionSources: SelectionSource[];
  documentation: string;
}
