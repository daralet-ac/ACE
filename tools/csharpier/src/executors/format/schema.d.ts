export interface FormatExecutorSchema {
  check: boolean;
  logLevel: 'debug' | 'information' | 'warning' | 'error' | 'none';
  fast: boolean;
  includeGenerated: boolean;
}
