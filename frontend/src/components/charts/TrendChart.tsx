import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';

interface TrendChartProps {
  data: any[];
}

export function TrendChart({ data }: TrendChartProps) {
  // Group data by time period (Year, Quarter, etc.)
  const timeKey = Object.keys(data[0] || {}).find(key => 
    key.toLowerCase().includes('year') || key.toLowerCase().includes('quarter') || key.toLowerCase().includes('date')
  ) || 'Year';
  
  const valueKey = Object.keys(data[0] || {}).find(key => 
    key !== timeKey && typeof data[0][key] === 'number' && 
    (key.toLowerCase().includes('total') || key.toLowerCase().includes('units') || 
     key.toLowerCase().includes('sold') || key.toLowerCase().includes('count'))
  ) || Object.keys(data[0] || {}).find(key => 
    key !== timeKey && typeof data[0][key] === 'number'
  ) || 'value';

  // Sort by time
  const sortedData = [...data].sort((a, b) => {
    const aTime = a[timeKey];
    const bTime = b[timeKey];
    return aTime < bTime ? -1 : aTime > bTime ? 1 : 0;
  });

  // Group by categories (Fuel types, Makes, etc.)
  const categoryKey = Object.keys(data[0] || {}).find(key => 
    key !== timeKey && key !== valueKey && (typeof data[0][key] === 'string' || data[0][key]?.Fuel)
  );

  let chartData;
  let categories: string[] = [];

  if (categoryKey) {
    // Multi-line chart (grouped by category)
    const grouped = sortedData.reduce((acc: any, item: any) => {
      const time = item[timeKey];
      const category = item[categoryKey]?.Fuel || item[categoryKey] || 'Unknown';
      const value = item[valueKey];
      
      if (!acc[time]) acc[time] = { [timeKey]: time };
      acc[time][category] = value;
      
      if (!categories.includes(category)) categories.push(category);
      
      return acc;
    }, {});
    
    chartData = Object.values(grouped);
  } else {
    // Single line chart
    chartData = sortedData.map(item => ({
      [timeKey]: item[timeKey],
      value: item[valueKey]
    }));
    categories = ['value'];
  }

  const colors = ['#0969da', '#cf222e', '#1a7f37', '#fb8500', '#8250df', '#bf3989'];

  // Calculate Y-axis domain for better visualization
  const allValues = chartData.flatMap(item => 
    categories.map(cat => item[cat]).filter(val => typeof val === 'number')
  );
  const minValue = Math.min(...allValues);
  const maxValue = Math.max(...allValues);
  const padding = (maxValue - minValue) * 0.1 || maxValue * 0.1 || 100;
  const yDomain = [
    Math.max(0, Math.floor(minValue - padding)),
    Math.ceil(maxValue + padding)
  ];

  return (
    <div className="w-full" style={{ height: '450px', minHeight: '350px' }}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
          <XAxis 
            dataKey={timeKey}
            tick={{ fill: '#6b7280', fontSize: 12 }}
            tickLine={false}
          />
          <YAxis 
            domain={yDomain}
            tick={{ fill: '#6b7280', fontSize: 12 }}
            tickLine={false}
            allowDataOverflow={false}
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
          {categories.map((category, index) => (
            <Line
              key={category}
              type="monotone"
              dataKey={category}
              stroke={colors[index % colors.length]}
              strokeWidth={2.5}
              dot={{ r: 4, strokeWidth: 2 }}
              activeDot={{ r: 6 }}
            />
          ))}
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
