export interface CsvColumn<T> {
  key: keyof T | string;
  header: string;
  formatter?: (value: unknown, row: T) => string;
}

export const downloadCsv = (blob: Blob, filename: string): void => {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
};

export const exportToCsv = <T extends Record<string, unknown>>(
  data: T[],
  filename: string,
  columns: CsvColumn<T>[]
): void => {
  const csvContent = generateCsvContent(data, columns);
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  downloadCsv(blob, filename);
};

export const generateCsvContent = <T extends Record<string, unknown>>(
  data: T[],
  columns: CsvColumn<T>[]
): string => {
  const headers = columns.map((col) => escapeCsvValue(col.header));
  const headerRow = headers.join(',');

  const dataRows = data.map((row) => {
    return columns
      .map((col) => {
        const value = getNestedValue(row, col.key as string);
        const formattedValue = col.formatter ? col.formatter(value, row) : value;
        return escapeCsvValue(String(formattedValue ?? ''));
      })
      .join(',');
  });

  return [headerRow, ...dataRows].join('\n');
};

const escapeCsvValue = (value: string): string => {
  if (value.includes(',') || value.includes('"') || value.includes('\n') || value.includes('\r')) {
    return `"${value.replace(/"/g, '""')}"`;
  }
  return value;
};

const getNestedValue = (obj: Record<string, unknown>, path: string): unknown => {
  return path.split('.').reduce((acc: unknown, part: string) => {
    if (acc && typeof acc === 'object' && part in acc) {
      return (acc as Record<string, unknown>)[part];
    }
    return undefined;
  }, obj);
};

export const formatCurrency = (value: unknown): string => {
  if (typeof value === 'number') {
    return `$${value.toFixed(2)}`;
  }
  return String(value ?? '');
};

export const formatDate = (value: unknown): string => {
  if (!value) return '';
  try {
    const date = new Date(String(value));
    return date.toLocaleDateString();
  } catch {
    return String(value);
  }
};

export const formatDateTime = (value: unknown): string => {
  if (!value) return '';
  try {
    const date = new Date(String(value));
    return date.toLocaleString();
  } catch {
    return String(value);
  }
};

export const formatNumber = (value: unknown, decimals = 2): string => {
  if (typeof value === 'number') {
    return value.toFixed(decimals);
  }
  return String(value ?? '');
};

export const formatPercent = (value: unknown): string => {
  if (typeof value === 'number') {
    return `${value.toFixed(1)}%`;
  }
  return String(value ?? '');
};
