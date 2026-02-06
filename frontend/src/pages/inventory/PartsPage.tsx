import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Paper,
  Typography,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  IconButton,
  Chip,
  InputAdornment,
  FormControlLabel,
  Switch,
} from '@mui/material';
import {
  Add as AddIcon,
  Search as SearchIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as ViewIcon,
  Warning as WarningIcon,
} from '@mui/icons-material';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { partService } from '../../services/partService';
import { partCategoryService } from '../../services/partCategoryService';
import { supplierService } from '../../services/supplierService';
import { Part, PartStatuses, PartFilter } from '../../types';
import { useAuth } from '../../hooks/useAuth';

const statusColors: Record<string, 'success' | 'warning' | 'error' | 'default'> = {
  Active: 'success',
  Inactive: 'default',
  Obsolete: 'warning',
  Discontinued: 'error',
};

const reorderStatusColors: Record<string, 'success' | 'warning' | 'error' | 'default'> = {
  Ok: 'success',
  Low: 'warning',
  Critical: 'error',
  OutOfStock: 'error',
};

export const PartsPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { hasPermission } = useAuth();

  const [filter, setFilter] = useState<PartFilter>({
    page: 1,
    pageSize: 20,
  });

  const [searchInput, setSearchInput] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['parts', filter],
    queryFn: () => partService.getParts(filter),
  });

  const { data: categoriesData } = useQuery({
    queryKey: ['partCategories'],
    queryFn: () => partCategoryService.getCategories(),
  });

  const { data: suppliersData } = useQuery({
    queryKey: ['suppliers'],
    queryFn: () => supplierService.getSuppliers({ pageSize: 100 }),
  });

  const deleteMutation = useMutation({
    mutationFn: partService.deletePart,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['parts'] });
    },
  });

  const handleSearch = () => {
    setFilter({ ...filter, search: searchInput, page: 1 });
  };

  const handlePaginationChange = (model: GridPaginationModel) => {
    setFilter({
      ...filter,
      page: model.page + 1,
      pageSize: model.pageSize,
    });
  };

  const handleDelete = (id: number) => {
    if (window.confirm('Are you sure you want to delete this part?')) {
      deleteMutation.mutate(id);
    }
  };

  const flattenCategories = (categories: any[], level = 0): any[] => {
    return categories.reduce((acc, cat) => {
      acc.push({ ...cat, indent: level });
      if (cat.children?.length) {
        acc.push(...flattenCategories(cat.children, level + 1));
      }
      return acc;
    }, []);
  };

  const flatCategories = flattenCategories(categoriesData?.data || []);

  const columns: GridColDef[] = [
    {
      field: 'actions',
      headerName: '',
      width: 120,
      sortable: false,
      renderCell: (params) => (
        <Box onClick={(e) => e.stopPropagation()}>
          <IconButton size="small" onClick={() => navigate(`/inventory/parts/${params.row.id}`)}>
            <ViewIcon fontSize="small" />
          </IconButton>
          {hasPermission('inventory.manage') && (
            <>
              <IconButton size="small" onClick={() => navigate(`/inventory/parts/${params.row.id}/edit`)}>
                <EditIcon fontSize="small" />
              </IconButton>
              <IconButton size="small" onClick={() => handleDelete(params.row.id)} color="error">
                <DeleteIcon fontSize="small" />
              </IconButton>
            </>
          )}
        </Box>
      ),
    },
    { field: 'partNumber', headerName: 'Part #', width: 130 },
    { field: 'name', headerName: 'Name', width: 200, flex: 1 },
    { field: 'categoryName', headerName: 'Category', width: 150 },
    { field: 'supplierName', headerName: 'Supplier', width: 150 },
    {
      field: 'totalQuantityAvailable',
      headerName: 'Available',
      width: 100,
      align: 'right',
      headerAlign: 'right',
    },
    {
      field: 'unitCost',
      headerName: 'Unit Cost',
      width: 100,
      align: 'right',
      headerAlign: 'right',
      valueFormatter: (params) => `$${params.value?.toFixed(2) || '0.00'}`,
    },
    {
      field: 'status',
      headerName: 'Status',
      width: 120,
      renderCell: (params) => (
        <Chip
          label={params.value}
          size="small"
          color={statusColors[params.value as string] || 'default'}
        />
      ),
    },
    {
      field: 'reorderStatus',
      headerName: 'Stock',
      width: 110,
      renderCell: (params) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
          {params.value !== 'Ok' && (
            <WarningIcon fontSize="small" color="warning" />
          )}
          <Chip
            label={params.value}
            size="small"
            color={reorderStatusColors[params.value as string] || 'default'}
            variant="outlined"
          />
        </Box>
      ),
    },
  ];

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">Parts Inventory</Typography>
        {hasPermission('inventory.manage') && (
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => navigate('/inventory/parts/new')}
          >
            Add Part
          </Button>
        )}
      </Box>

      <Paper sx={{ p: 2, mb: 2 }}>
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'center' }}>
          <TextField
            size="small"
            placeholder="Search parts..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton size="small" onClick={handleSearch}>
                    <SearchIcon />
                  </IconButton>
                </InputAdornment>
              ),
            }}
            sx={{ minWidth: 250 }}
          />

          <FormControl size="small" sx={{ minWidth: 150 }}>
            <InputLabel>Category</InputLabel>
            <Select
              value={filter.categoryId || ''}
              label="Category"
              onChange={(e) => setFilter({ ...filter, categoryId: e.target.value as number || undefined, page: 1 })}
            >
              <MenuItem value="">All</MenuItem>
              {flatCategories.map((cat) => (
                <MenuItem key={cat.id} value={cat.id}>
                  {'  '.repeat(cat.indent)}{cat.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 150 }}>
            <InputLabel>Supplier</InputLabel>
            <Select
              value={filter.supplierId || ''}
              label="Supplier"
              onChange={(e) => setFilter({ ...filter, supplierId: e.target.value as number || undefined, page: 1 })}
            >
              <MenuItem value="">All</MenuItem>
              {suppliersData?.items?.map((supplier) => (
                <MenuItem key={supplier.id} value={supplier.id}>
                  {supplier.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 120 }}>
            <InputLabel>Status</InputLabel>
            <Select
              value={filter.status || ''}
              label="Status"
              onChange={(e) => setFilter({ ...filter, status: e.target.value || undefined, page: 1 })}
            >
              <MenuItem value="">All</MenuItem>
              {PartStatuses.map((status) => (
                <MenuItem key={status} value={status}>
                  {status}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControlLabel
            control={
              <Switch
                checked={filter.lowStock || false}
                onChange={(e) => setFilter({ ...filter, lowStock: e.target.checked || undefined, page: 1 })}
                color="warning"
              />
            }
            label="Low Stock Only"
          />
        </Box>
      </Paper>

      <Paper sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
        <DataGrid
          rows={data?.items || []}
          columns={columns}
          loading={isLoading}
          paginationMode="server"
          rowCount={data?.totalCount || 0}
          paginationModel={{
            page: (filter.page || 1) - 1,
            pageSize: filter.pageSize || 20,
          }}
          onPaginationModelChange={handlePaginationChange}
          pageSizeOptions={[10, 20, 50]}
          disableRowSelectionOnClick
          onRowClick={(params) => navigate(`/inventory/parts/${params.row.id}`)}
          sx={{ flex: 1, '& .MuiDataGrid-row': { cursor: 'pointer' } }}
        />
      </Paper>
    </Box>
  );
};
