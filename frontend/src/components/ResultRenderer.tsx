import type { QueryResponse } from '../types';
import { RankingChart } from './charts/RankingChart';
import { TrendChart } from './charts/TrendChart';
import { ComparisonChart } from './charts/ComparisonChart';
import { TableView } from './charts/TableView';

interface ResultRendererProps {
  result: QueryResponse;
}

export function ResultRenderer({ result }: ResultRendererProps) {
  const { resultType, data } = result;

  // Extract the actual data array from the API response
  // The API returns the raw response which may have a 'value' property (from OData backend)
  const items = Array.isArray(data) ? data : (data?.value || []);

  if (!items || items.length === 0) {
    return (
      <div className="p-4 rounded-lg" style={{ backgroundColor: '#fff8e1', border: '1px solid #ffd54f' }}>
        <p className="text-sm font-medium" style={{ color: '#f57c00' }}>No results found</p>
        <p className="text-sm mt-1" style={{ color: '#e65100' }}>Try rephrasing your question or using different keywords.</p>
      </div>
    );
  }

  // Smart rendering based on result type
  const renderVisualization = () => {
    switch (resultType?.toLowerCase()) {
      case 'ranking':
        return <RankingChart data={items} />;
      case 'trend':
        return <TrendChart data={items} />;
      case 'comparison':
        return <ComparisonChart data={items} />;
      case 'table':
      default:
        return <TableView data={items} />;
    }
  };
  
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between pb-3" style={{ borderBottom: '1px solid #e5e7eb' }}>
        <div>
          <span className="text-sm font-semibold uppercase tracking-wide" style={{ color: '#0969da' }}>
            {resultType}
          </span>
          <p className="text-xs mt-0.5" style={{ color: '#6b7280' }}>
            Visualization of your query results
          </p>
        </div>
        <span className="text-xs px-2 py-1 rounded" style={{ backgroundColor: '#f0f6ff', color: '#0969da' }}>
          {items.length} result{items.length !== 1 ? 's' : ''}
        </span>
      </div>
      
      <div className="rounded-lg p-4" style={{ backgroundColor: '#fafbfc', border: '1px solid #e5e7eb' }}>
        {renderVisualization()}
      </div>
    </div>
  );
}
