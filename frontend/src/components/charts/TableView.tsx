interface TableViewProps {
  data: any[];
}

export function TableView({ data }: TableViewProps) {
  if (!data || data.length === 0) {
    return (
      <div className="p-4 text-gray-600">No data to display</div>
    );
  }

  const columns = Object.keys(data[0]);

  return (
    <div className="overflow-x-auto rounded-lg" style={{ border: '1px solid #d1d5db' }}>
      <table className="min-w-full divide-y" style={{ borderColor: '#e5e7eb' }}>
        <thead style={{ backgroundColor: '#f9fafb' }}>
          <tr>
            {columns.map((key) => (
              <th
                key={key}
                className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider"
                style={{ color: '#374151' }}
              >
                {key}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y" style={{ backgroundColor: '#ffffff', borderColor: '#e5e7eb' }}>
          {data.map((row: Record<string, any>, index: number) => (
            <tr 
              key={index}
              onMouseEnter={(e) => {
                e.currentTarget.style.backgroundColor = '#f9fafb';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.backgroundColor = '#ffffff';
              }}
            >
              {columns.map((key: string) => {
                const value = row[key];
                return (
                  <td 
                    key={key} 
                    className="px-4 py-3 text-sm whitespace-nowrap"
                    style={{ color: '#1f2328' }}
                  >
                    {typeof value === 'object' && value !== null
                      ? JSON.stringify(value)
                      : String(value)}
                  </td>
                );
              })}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
