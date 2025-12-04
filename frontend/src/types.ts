export interface QueryRequest {
  question: string;
}

export interface QueryResponse {
  resultType: 'ranking' | 'trend' | 'comparison' | 'table';
  data: any; // API returns raw JSON response - structure determined by resultType
}

export interface ChatMessage {
  id: string;
  type: 'user' | 'assistant';
  content: string;
  result?: QueryResponse;
  isLoading?: boolean;
  error?: string;
}
