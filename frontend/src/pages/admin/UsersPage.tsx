import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Chip,
  FormControlLabel,
  IconButton,
  Paper,
  Switch,
  Tooltip,
  Typography,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  LockOpen as UnlockIcon,
  VpnKey as PasswordIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { userService, UserDetail } from '../../services/userService';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { LdapStatusCard } from '../../components/admin/LdapStatusCard';

export const UsersPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [includeInactive, setIncludeInactive] = useState(false);
  const [resetPasswordDialog, setResetPasswordDialog] = useState<UserDetail | null>(null);
  const [newPassword, setNewPassword] = useState('');
  const [deleteDialog, setDeleteDialog] = useState<UserDetail | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['users', includeInactive],
    queryFn: () => userService.getUsers(includeInactive),
  });

  const unlockMutation = useMutation({
    mutationFn: (id: number) => userService.unlockUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });

  const resetPasswordMutation = useMutation({
    mutationFn: ({ id, password }: { id: number; password: string }) =>
      userService.resetPassword(id, password),
    onSuccess: () => {
      setResetPasswordDialog(null);
      setNewPassword('');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => userService.deleteUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setDeleteDialog(null);
    },
  });

  const columns: GridColDef[] = [
    {
      field: 'actions',
      headerName: '',
      width: 160,
      sortable: false,
      renderCell: (params) => (
        <Box onClick={(e) => e.stopPropagation()}>
          <Tooltip title="Edit">
            <IconButton
              size="small"
              onClick={() => navigate(`/admin/users/${params.row.id}/edit`)}
            >
              <EditIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          {params.row.isLocked && (
            <Tooltip title="Unlock Account">
              <IconButton
                size="small"
                color="warning"
                onClick={() => unlockMutation.mutate(params.row.id)}
              >
                <UnlockIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          )}
          <Tooltip title="Reset Password">
            <IconButton
              size="small"
              onClick={() => setResetPasswordDialog(params.row)}
            >
              <PasswordIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          {params.row.username !== 'admin' && (
            <Tooltip title="Deactivate">
              <IconButton
                size="small"
                color="error"
                onClick={() => setDeleteDialog(params.row)}
              >
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          )}
        </Box>
      ),
    },
    { field: 'username', headerName: 'Username', width: 130 },
    { field: 'fullName', headerName: 'Name', width: 180 },
    { field: 'email', headerName: 'Email', width: 220 },
    { field: 'phone', headerName: 'Phone', width: 130 },
    {
      field: 'roles',
      headerName: 'Roles',
      width: 200,
      renderCell: (params) => (
        <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
          {params.value?.map((role: string) => (
            <Chip key={role} label={role} size="small" variant="outlined" />
          ))}
        </Box>
      ),
    },
    {
      field: 'isActive',
      headerName: 'Status',
      width: 100,
      renderCell: (params) => (
        <Chip
          label={params.row.isLocked ? 'Locked' : params.value ? 'Active' : 'Inactive'}
          color={params.row.isLocked ? 'error' : params.value ? 'success' : 'default'}
          size="small"
        />
      ),
    },
    {
      field: 'lastLoginAt',
      headerName: 'Last Login',
      width: 130,
      valueFormatter: (params: any) =>
        params.value ? new Date(params.value).toLocaleDateString() : 'Never',
    },
  ];

  if (isLoading) {
    return <LoadingSpinner message="Loading users..." />;
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">User Management</Typography>
        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          <FormControlLabel
            control={
              <Switch
                checked={includeInactive}
                onChange={(e) => setIncludeInactive(e.target.checked)}
              />
            }
            label="Show Inactive"
          />
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => navigate('/admin/users/new')}
          >
            Add User
          </Button>
        </Box>
      </Box>

      {/* LDAP Status Card */}
      <LdapStatusCard />

      <Paper sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
        <DataGrid
          rows={data?.data || []}
          columns={columns}
          pageSizeOptions={[10, 25, 50]}
          initialState={{
            pagination: { paginationModel: { pageSize: 25 } },
          }}
          disableRowSelectionOnClick
          onRowClick={(params) => navigate(`/admin/users/${params.row.id}/edit`)}
          sx={{ flex: 1, '& .MuiDataGrid-row': { cursor: 'pointer' } }}
        />
      </Paper>

      {/* Reset Password Dialog */}
      <Dialog open={!!resetPasswordDialog} onClose={() => setResetPasswordDialog(null)}>
        <DialogTitle>Reset Password</DialogTitle>
        <DialogContent>
          <Typography variant="body2" sx={{ mb: 2 }}>
            Reset password for user: <strong>{resetPasswordDialog?.username}</strong>
          </Typography>
          <TextField
            fullWidth
            type="password"
            label="New Password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            autoFocus
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setResetPasswordDialog(null)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() =>
              resetPasswordDialog &&
              resetPasswordMutation.mutate({ id: resetPasswordDialog.id, password: newPassword })
            }
            disabled={!newPassword || newPassword.length < 6}
          >
            Reset Password
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deleteDialog} onClose={() => setDeleteDialog(null)}>
        <DialogTitle>Deactivate User</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to deactivate user <strong>{deleteDialog?.username}</strong>?
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            The user will no longer be able to log in.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialog(null)}>Cancel</Button>
          <Button
            variant="contained"
            color="error"
            onClick={() => deleteDialog && deleteMutation.mutate(deleteDialog.id)}
          >
            Deactivate
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
