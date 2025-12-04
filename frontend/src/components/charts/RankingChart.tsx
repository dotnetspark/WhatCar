import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';

interface RankingChartProps {
  data: any[];
}

export function RankingChart({ data }: RankingChartProps) {
  // Extract the first 10 items for ranking
  const items = data.slice(0, 10);
  
  // Transform data for the chart
  const chartData = items.map((item, index) => {
    // Find numeric value for ranking
    const valueKey = Object.keys(item).find(key => 
      typeof item[key] === 'number' && key.toLowerCase().includes('sold' || 'units' || 'count')
    ) || Object.keys(item).find(key => typeof item[key] === 'number');
    
    // Find label (Make, Model, or first string field)
    const labelKey = Object.keys(item).find(key => 
      typeof item[key] === 'string' || (typeof item[key] === 'object' && item[key]?.Make)
    );
    
    const label = item[labelKey]?.Make || item[labelKey]?.Model || item[labelKey] || `Item ${index + 1}`;
    const value = valueKey ? item[valueKey] : 0;
    
    return {
      name: label,
      value: value,
      rank: index + 1
    };
  });

  return (
    <div className="w-full" style={{ height: '500px', minHeight: '400px' }}>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={chartData} layout="vertical" margin={{ top: 5, right: 30, left: 150, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
          <XAxis type="number" tick={{ fill: '#6b7280', fontSize: 12 }} />
          <YAxis 
            dataKey="name" 
            type="category" 
            width={140} 
            tick={{ fill: '#374151', fontSize: 12 }}
            tickLine={false}
          />
          <Tooltip 
            contentStyle={{ 
              backgroundColor: '#ffffff',
              border: '1px solid #d1d5db',
              borderRadius: '6px',
              fontSize: '13px'
            }}
          />
          <Legend 
            wrapperStyle={{ fontSize: '13px', paddingTop: '10px' }}
          />
          <Bar 
            dataKey="value" 
            fill="#0969da" 
            name="Units Sold"
            radius={[0, 4, 4, 0]}
          />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
