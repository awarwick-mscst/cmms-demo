import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Chip,
  IconButton,
  Paper,
  TextField,
  Typography,
  InputAdornment,
  FormControlLabel,
  Switch,
} from '@mui/material';
import {
  Add as AddIcon,
  Search as SearchIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { taskTemplateService } from '../../services/taskTemplateService';
import { WorkOrderTaskTemplateSummary } from '../../types';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';

export const TaskTemplatesPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [showInactive, setShowInactive] = useState(false);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [deleteId, setDeleteId] = useState<number | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['taskTemplates', { search, showInactive, page, pageSize }],
    queryFn: () =>
      taskTemplateService.getTemplates({
        search: search || undefined,
        isActive: showInactive ? undefined : true,
        page: page + 1,
        pageSize,
      }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => taskTemplateService.deleteTemplate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['taskTemplates'] });
      setDeleteId(null);
    },
  });

  const columns: GridColDef<WorkOrderTaskTemplateSummary>[] = [
    {
      field: 'name',
      headerName: 'Name',
      flex: 1,
      minWidth: 200,
    },
    {
      field: 'description',
      headerName: 'Description',
      flex: 1.5,
      minWidth: 250,
    },
    {
      field: 'itemCount',
      headerName: 'Tasks',
      width: 100,
      align: 'center',
      headerAlign: 'center',
    },
    {
      field: 'isActive',
      headerName: 'Status',
      width: 100,
      renderCell: (params: GridRenderCellParams<WorkOrderTaskTemplateSummary>) => (
        <Chip
          label={params.value ? 'Active' : 'Inactive'}
          color={params.value ? 'success' : 'default'}
          size="small"
        />
      ),
    },
    {
      field: 'createdAt',
      headerName: 'Created',
      width: 120,
      valueFormatter: (params: any) => params.value ? new Date(params.value).toLocaleDateString() : '',
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 120,
      sortable: false,
      renderCell: (params: GridRenderCellParams<WorkOrderTaskTemplateSummary>) => (
        <Box>
          <IconButton
            size="small"
            onClick={(e) => {
              e.stopPropagation();
              navigate(`/admin/task-templates/${params.row.id}/edit`);
            }}
          >
            <EditIcon fontSize="small" />
          </IconButton>
          <IconButton
            size="small"
            color="error"
            onClick={(e) => {
              e.stopPropagation();
              setDeleteId(params.row.id);
            }}
          >
            <DeleteIcon fontSize="small" />
          </IconButton>
        </Box>
      ),
    },
  ];

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">Task Templates</Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => navigate('/admin/task-templates/new')}
        >
          New Template
        </Button>
      </Box>

      <Paper sx={{ p: 2, mb: 2 }}>
        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          <TextField
            size="small"
            placeholder="Search templates..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon />
                </InputAdornment>
              ),
            }}
            sx={{ width: 300 }}
          />
          <FormControlLabel
            control={
              <Switch
                checked={showInactive}
                onChange={(e) => setShowInactive(e.target.checked)}
              />
            }
            label="Show Inactive"
          />
        </Box>
      </Paper>

      <Paper sx={{ height: 600 }}>
        <DataGrid
          rows={data?.items || []}
          columns={columns}
          loading={isLoading}
          paginationMode="server"
          rowCount={data?.totalCount || 0}
          pageSizeOptions={[10, 20, 50]}
          paginationModel={{ page, pageSize }}
          onPaginationModelChange={(model) => {
            setPage(model.page);
            setPageSize(model.pageSize);
          }}
          onRowClick={(params) => navigate(`/admin/task-templates/${params.row.id}/edit`)}
          sx={{
            '& .MuiDataGrid-row': { cursor: 'pointer' },
          }}
        />
      </Paper>

      <ConfirmDialog
        open={deleteId !== null}
        title="Delete Template"
        message="Are you sure you want to delete this template? This action cannot be undone."
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
        onCancel={() => setDeleteId(null)}
        confirmText="Delete"
        confirmColor="error"
      />
    </Box>
  );
};
