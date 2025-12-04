import { useState } from 'react';
import type { ChatMessage } from '../types';
import { ChatMessageView } from './ChatMessage';
import { ResultRenderer } from './ResultRenderer';
import { sendQuery } from '../api';

export function ChatInterface() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [isProcessing, setIsProcessing] = useState(false);
  const [selectedResult, setSelectedResult] = useState<ChatMessage | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!input.trim() || isProcessing) return;

    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      type: 'user',
      content: input.trim(),
    };

    const assistantMessage: ChatMessage = {
      id: (Date.now() + 1).toString(),
      type: 'assistant',
      content: 'Processing your question...',
      isLoading: true,
    };

    setMessages((prev) => [...prev, userMessage, assistantMessage]);
    setInput('');
    setIsProcessing(true);

    try {
      const result = await sendQuery(userMessage.content);
      
      setMessages((prev) =>
        prev.map((msg) =>
          msg.id === assistantMessage.id
            ? {
                ...msg,
                content: 'Here are the results:',
                result,
                isLoading: false,
              }
            : msg
        )
      );
      
      // Auto-open the result panel
      const updatedMessage = {
        ...assistantMessage,
        content: 'Here are the results:',
        result,
        isLoading: false,
      };
      setSelectedResult(updatedMessage);
    } catch (error) {
      setMessages((prev) =>
        prev.map((msg) =>
          msg.id === assistantMessage.id
            ? {
                ...msg,
                content: 'Failed to process your question.',
                error: error instanceof Error ? error.message : 'Unknown error occurred',
                isLoading: false,
              }
            : msg
        )
      );
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <div className="flex h-screen" style={{ backgroundColor: '#f0f6ff' }}>
      {/* Main Chat Area - Centered */}
      <div className={`flex flex-col transition-all duration-300 ${selectedResult ? 'w-2/5' : 'w-full'}`}>
        {/* Header */}
        <header className="px-6 py-3" style={{ backgroundColor: '#f0f6ff' }}>
          <div className="max-w-3xl mx-auto">
            <h1 className="font-semibold" style={{ color: '#1f2328', fontSize: '16px' }}>
              Vehicle Sales Analytics
            </h1>
            <p style={{ color: '#656d76', fontSize: '13px' }}>Ask questions about UK vehicle sales data</p>
          </div>
        </header>

        {/* Messages */}
        <div className="flex-1 overflow-y-auto">
          <div className="max-w-3xl mx-auto px-6 py-6">
            {messages.length === 0 ? (
              <div className="flex flex-col items-center justify-center min-h-[60vh]">
                <div className="text-center space-y-8 max-w-xl">
                  <div className="space-y-2">
                    <p className="text-sm font-medium text-left" style={{ color: '#656d76' }}>Example questions:</p>
                    {[
                      'What are the top 10 electric cars in 2024?',
                      'Show me diesel and petrol sales since 2020',
                      'Compare Tesla vs BMW sales in 2024',
                    ].map((example, idx) => (
                      <button
                        key={idx}
                        onClick={() => setInput(example)}
                        className="w-full text-left px-4 py-3 rounded-lg transition-colors text-sm"
                        style={{ 
                          backgroundColor: '#ffffff', 
                          border: '1px solid #d0d7de',
                          color: '#1f2328'
                        }}
                        onMouseEnter={(e) => {
                          e.currentTarget.style.backgroundColor = '#f6f8fa';
                          e.currentTarget.style.borderColor = '#1f2328';
                        }}
                        onMouseLeave={(e) => {
                          e.currentTarget.style.backgroundColor = '#ffffff';
                          e.currentTarget.style.borderColor = '#d0d7de';
                        }}
                      >
                        {example}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            ) : (
              <div className="space-y-4">
                {messages.map((message) => (
                  <div key={message.id}>
                    {message.type === 'user' ? (
                      <div className="flex justify-end">
                        <div className="rounded-lg px-4 py-2 max-w-[80%]" style={{ backgroundColor: '#0969da', color: '#ffffff' }}>
                          {message.content}
                        </div>
                      </div>
                    ) : (
                      <div className="flex justify-start">
                        <div className="rounded-lg px-4 py-3 max-w-[80%]" style={{ backgroundColor: '#ffffff', border: '1px solid #d0d7de' }}>
                          {message.isLoading ? (
                            <div className="flex items-center gap-2" style={{ color: '#656d76' }}>
                              <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                              </svg>
                              Processing...
                            </div>
                          ) : (
                            <div>
                              <p className="mb-2" style={{ color: '#1f2328' }}>{message.content}</p>
                              {message.result && (
                                <button
                                  onClick={() => setSelectedResult(message)}
                                  className="text-sm font-medium px-3 py-1.5 rounded-md transition-colors"
                                  style={{ 
                                    backgroundColor: '#0969da',
                                    color: '#ffffff'
                                  }}
                                  onMouseEnter={(e) => {
                                    e.currentTarget.style.backgroundColor = '#0860ca';
                                  }}
                                  onMouseLeave={(e) => {
                                    e.currentTarget.style.backgroundColor = '#0969da';
                                  }}
                                >
                                  View results →
                                </button>
                              )}
                              {message.error && (
                                <p className="text-sm mt-2" style={{ color: '#d1242f' }}>{message.error}</p>
                              )}
                            </div>
                          )}
                        </div>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Input */}
        <div className="px-6 py-4 pb-6" style={{ backgroundColor: '#f0f6ff' }}>
          <div className="max-w-3xl mx-auto mb-4">
            <form onSubmit={handleSubmit}>
              <div className="relative">
                <input
                  type="text"
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  placeholder="Ask a question about vehicle sales..."
                  disabled={isProcessing}
                  className="w-full rounded-lg px-4 py-3 pr-24 focus:outline-none disabled:cursor-not-allowed"
                  style={{
                    border: '1px solid #d0d7de',
                    backgroundColor: '#ffffff',
                    color: '#1f2328'
                  }}
                  onFocus={(e) => {
                    e.currentTarget.style.borderColor = '#0969da';
                    e.currentTarget.style.boxShadow = '0 0 0 3px rgba(9, 105, 218, 0.1)';
                  }}
                  onBlur={(e) => {
                    e.currentTarget.style.borderColor = '#d0d7de';
                    e.currentTarget.style.boxShadow = 'none';
                  }}
                />
                <button
                  type="submit"
                  disabled={!input.trim() || isProcessing}
                  className="absolute right-2 top-1/2 -translate-y-1/2 px-4 py-1.5 text-sm font-medium rounded-md focus:outline-none disabled:cursor-not-allowed transition-colors"
                  style={{
                    backgroundColor: !input.trim() || isProcessing ? '#d0d7de' : '#0969da',
                    color: '#ffffff'
                  }}
                  onMouseEnter={(e) => {
                    if (!isProcessing && input.trim()) {
                      e.currentTarget.style.backgroundColor = '#0860ca';
                    }
                  }}
                  onMouseLeave={(e) => {
                    if (!isProcessing && input.trim()) {
                      e.currentTarget.style.backgroundColor = '#0969da';
                    }
                  }}
                >
                  {isProcessing ? 'Thinking...' : 'Send'}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      {/* Results Panel - Expandable from Right */}
      {selectedResult && (
        <div className="w-3/5 flex flex-col" style={{ backgroundColor: '#ffffff', borderLeft: '1px solid #d0d7de' }}>
          {/* Panel Header */}
          <div className="flex items-center justify-between px-6 py-4" style={{ borderBottom: '1px solid #d0d7de' }}>
            <h2 className="text-lg font-semibold" style={{ color: '#1f2328' }}>Results</h2>
            <button
              onClick={() => setSelectedResult(null)}
              className="rounded-md p-1.5 transition-colors"
              style={{ color: '#656d76' }}
              onMouseEnter={(e) => {
                e.currentTarget.style.color = '#1f2328';
                e.currentTarget.style.backgroundColor = '#f6f8fa';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.color = '#656d76';
                e.currentTarget.style.backgroundColor = 'transparent';
              }}
              aria-label="Close results panel"
            >
              <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          {/* Panel Content */}
          <div className="flex-1 overflow-y-auto p-6">
            {selectedResult.result && <ResultRenderer result={selectedResult.result} />}
            {selectedResult.error && (
              <div className="p-4 rounded-lg" style={{ backgroundColor: '#fff1f2', border: '1px solid #fecdd3', color: '#be123c' }}>
                <p className="text-sm font-medium">Error</p>
                <p className="text-sm mt-1">{selectedResult.error}</p>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
