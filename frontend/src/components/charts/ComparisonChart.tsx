import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';

interface ComparisonChartProps {
  data: any[];
}

export function ComparisonChart({ data }: ComparisonChartProps) {
  // Group data by category for comparison
  const categoryKey = Object.keys(data[0] || {}).find(key => 
    typeof data[0][key] === 'string' || data[0][key]?.Make || data[0][key]?.Fuel
  );
  
  const valueKey = Object.keys(data[0] || {}).find(key => 
    typeof data[0][key] === 'number'
  );

  if (!categoryKey || !valueKey) {
    return <div className="p-4 text-gray-600">Unable to render comparison chart</div>;
  }

  // Aggregate by category
  const aggregated = data.reduce((acc: any, item: any) => {
    const category = item[categoryKey]?.Make || item[categoryKey]?.Fuel || item[categoryKey] || 'Unknown';
    const value = item[valueKey];
    
    if (!acc[category]) {
      acc[category] = { name: category, value: 0, count: 0 };
    }
    acc[category].value += value;
    acc[category].count += 1;
    
    return acc;
  }, {});

  const chartData = Object.values(aggregated).sort((a: any, b: any) => b.value - a.value);

  return (
    <div className="w-full" style={{ height: '450px', minHeight: '350px' }}>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 60 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
          <XAxis 
            dataKey="name" 
            angle={-45}
            textAnchor="end"
            height={80}
            tick={{ fill: '#6b7280', fontSize: 12 }}
            tickLine={false}
          />
          <YAxis 
            tick={{ fill: '#6b7280', fontSize: 12 }}
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
            fill="#1a7f37" 
            name="Total"
            radius={[4, 4, 0, 0]}
          />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
