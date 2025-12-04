import type { QueryRequest, QueryResponse } from './types';

export async function sendQuery(question: string): Promise<QueryResponse> {
  const response = await fetch('/api/v1/query', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ question } as QueryRequest),
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error || 'Failed to process query');
  }

  return response.json();
}
