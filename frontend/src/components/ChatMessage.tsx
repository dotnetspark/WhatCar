import type { ChatMessage } from '../types';
import { ResultRenderer } from './ResultRenderer';

interface ChatMessageProps {
  message: ChatMessage;
}

export function ChatMessageView({ message }: ChatMessageProps) {
  return (
    <div className={`flex ${message.type === 'user' ? 'justify-end' : 'justify-start'} mb-4`}>
      <div className={`max-w-2xl ${message.type === 'user' ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-900'} rounded-lg px-4 py-3 shadow-md`}>
        <p className="text-sm font-medium mb-1">{message.type === 'user' ? 'You' : 'Assistant'}</p>
        <p className="whitespace-pre-wrap">{message.content}</p>
        
        {message.isLoading && (
          <div className="mt-3 flex items-center space-x-2">
            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-900"></div>
            <span className="text-sm text-gray-600">Analyzing...</span>
          </div>
        )}
        
        {message.error && (
          <div className="mt-3 p-3 bg-red-100 border border-red-400 text-red-700 rounded">
            <p className="text-sm">{message.error}</p>
          </div>
        )}
        
        {message.result && !message.isLoading && (
          <div className="mt-4">
            <ResultRenderer result={message.result} />
          </div>
        )}
      </div>
    </div>
  );
}
