import React from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  Paper,
  Box,
  Button,
  Typography,
  Skeleton,
} from '@mui/material';
import { Download as DownloadIcon } from '@mui/icons-material';

export interface ReportColumn<T> {
  id: keyof T | string;
  label: string;
  minWidth?: number;
  align?: 'left' | 'center' | 'right';
  format?: (value: unknown, row: T) => React.ReactNode;
  sortable?: boolean;
}

interface ReportTableProps<T> {
  columns: ReportColumn<T>[];
  data: T[];
  loading?: boolean;
  title?: string;
  onExport?: () => void;
  exportLoading?: boolean;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  onSort?: (columnId: string) => void;
  emptyMessage?: string;
  getRowKey?: (row: T, index: number) => string | number;
}

function ReportTable<T>({
  columns,
  data,
  loading = false,
  title,
  onExport,
  exportLoading = false,
  sortBy,
  sortDirection = 'asc',
  onSort,
  emptyMessage = 'No data available',
  getRowKey,
}: ReportTableProps<T>): React.ReactElement {
  const handleSort = (columnId: string) => {
    if (onSort) {
      onSort(columnId);
    }
  };

  const getValue = (row: T, columnId: string): unknown => {
    const parts = columnId.split('.');
    let value: unknown = row;
    for (const part of parts) {
      if (value && typeof value === 'object' && value !== null && part in value) {
        value = (value as Record<string, unknown>)[part];
      } else {
        return undefined;
      }
    }
    return value;
  };

  return (
    <Paper sx={{ width: '100%', overflow: 'hidden' }}>
      {(title || onExport) && (
        <Box sx={{ p: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          {title && (
            <Typography variant="h6" component="div">
              {title}
            </Typography>
          )}
          {onExport && (
            <Button
              variant="outlined"
              startIcon={<DownloadIcon />}
              onClick={onExport}
              disabled={exportLoading || loading || data.length === 0}
            >
              {exportLoading ? 'Exporting...' : 'Export CSV'}
            </Button>
          )}
        </Box>
      )}
      <TableContainer sx={{ maxHeight: 600 }}>
        <Table stickyHeader size="small">
          <TableHead>
            <TableRow>
              {columns.map((column) => (
                <TableCell
                  key={String(column.id)}
                  align={column.align || 'left'}
                  style={{ minWidth: column.minWidth }}
                  sx={{ fontWeight: 'bold', backgroundColor: 'grey.100' }}
                >
                  {column.sortable !== false && onSort ? (
                    <TableSortLabel
                      active={sortBy === column.id}
                      direction={sortBy === column.id ? sortDirection : 'asc'}
                      onClick={() => handleSort(String(column.id))}
                    >
                      {column.label}
                    </TableSortLabel>
                  ) : (
                    column.label
                  )}
                </TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              // Loading skeleton
              [...Array(5)].map((_, index) => (
                <TableRow key={index}>
                  {columns.map((column) => (
                    <TableCell key={String(column.id)}>
                      <Skeleton variant="text" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : data.length === 0 ? (
              // Empty state
              <TableRow>
                <TableCell colSpan={columns.length} align="center" sx={{ py: 4 }}>
                  <Typography color="text.secondary">{emptyMessage}</Typography>
                </TableCell>
              </TableRow>
            ) : (
              // Data rows
              data.map((row, index) => {
                const key = getRowKey ? getRowKey(row, index) : index;
                return (
                  <TableRow hover key={key}>
                    {columns.map((column) => {
                      const value = getValue(row, String(column.id));
                      return (
                        <TableCell key={String(column.id)} align={column.align || 'left'}>
                          {column.format ? column.format(value, row) : String(value ?? '')}
                        </TableCell>
                      );
                    })}
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Paper>
  );
}

export default ReportTable;
