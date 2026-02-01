import React, { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  FormControlLabel,
  Grid,
  ListItemText,
  MenuItem,
  TextField,
  Typography,
} from '@mui/material';
import { ArrowBack as BackIcon, Save as SaveIcon } from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { userService, CreateUserRequest, UpdateUserRequest } from '../../services/userService';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';

interface UserFormData {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone: string;
  isActive: boolean;
  roleIds: number[];
}

export const UserFormPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const isEdit = !!id && id !== 'new';

  const { control, handleSubmit, reset, formState: { errors } } = useForm<UserFormData>({
    defaultValues: {
      username: '',
      email: '',
      password: '',
      firstName: '',
      lastName: '',
      phone: '',
      isActive: true,
      roleIds: [],
    },
  });

  const { data: userData, isLoading: isLoadingUser } = useQuery({
    queryKey: ['user', id],
    queryFn: () => userService.getUser(Number(id)),
    enabled: isEdit,
  });

  const { data: rolesData } = useQuery({
    queryKey: ['roles'],
    queryFn: () => userService.getRoles(),
  });

  useEffect(() => {
    if (userData?.data) {
      const user = userData.data;
      const roleIds = rolesData?.data
        ?.filter((r) => user.roles.includes(r.name))
        .map((r) => r.id) || [];

      reset({
        username: user.username,
        email: user.email,
        password: '',
        firstName: user.firstName,
        lastName: user.lastName,
        phone: user.phone || '',
        isActive: user.isActive,
        roleIds,
      });
    }
  }, [userData, rolesData, reset]);

  const createMutation = useMutation({
    mutationFn: (data: CreateUserRequest) => userService.createUser(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      navigate('/admin/users');
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateUserRequest) => userService.updateUser(Number(id), data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      queryClient.invalidateQueries({ queryKey: ['user', id] });
      navigate('/admin/users');
    },
  });

  const onSubmit = (data: UserFormData) => {
    if (isEdit) {
      const updateData: UpdateUserRequest = {
        email: data.email,
        password: data.password || undefined,
        firstName: data.firstName,
        lastName: data.lastName,
        phone: data.phone || undefined,
        isActive: data.isActive,
        roleIds: data.roleIds,
      };
      updateMutation.mutate(updateData);
    } else {
      const createData: CreateUserRequest = {
        username: data.username,
        email: data.email,
        password: data.password,
        firstName: data.firstName,
        lastName: data.lastName,
        phone: data.phone || undefined,
        isActive: data.isActive,
        roleIds: data.roleIds,
      };
      createMutation.mutate(createData);
    }
  };

  if (isEdit && isLoadingUser) {
    return <LoadingSpinner message="Loading user..." />;
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <Button startIcon={<BackIcon />} onClick={() => navigate('/admin/users')}>
          Back
        </Button>
        <Typography variant="h5">
          {isEdit ? 'Edit User' : 'Create User'}
        </Typography>
      </Box>

      <form onSubmit={handleSubmit(onSubmit)}>
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  User Information
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="username"
                      control={control}
                      rules={{ required: 'Username is required' }}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          label="Username"
                          error={!!errors.username}
                          helperText={errors.username?.message}
                          disabled={isEdit}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="email"
                      control={control}
                      rules={{
                        required: 'Email is required',
                        pattern: {
                          value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                          message: 'Invalid email address',
                        },
                      }}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="email"
                          label="Email"
                          error={!!errors.email}
                          helperText={errors.email?.message}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="firstName"
                      control={control}
                      rules={{ required: 'First name is required' }}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          label="First Name"
                          error={!!errors.firstName}
                          helperText={errors.firstName?.message}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="lastName"
                      control={control}
                      rules={{ required: 'Last name is required' }}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          label="Last Name"
                          error={!!errors.lastName}
                          helperText={errors.lastName?.message}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="phone"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} fullWidth label="Phone" />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="password"
                      control={control}
                      rules={{
                        required: isEdit ? false : 'Password is required',
                        minLength: {
                          value: 6,
                          message: 'Password must be at least 6 characters',
                        },
                      }}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="password"
                          label={isEdit ? 'New Password (leave blank to keep current)' : 'Password'}
                          error={!!errors.password}
                          helperText={errors.password?.message}
                        />
                      )}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Roles & Status
                </Typography>
                <Controller
                  name="roleIds"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      select
                      fullWidth
                      label="Roles"
                      SelectProps={{
                        multiple: true,
                        renderValue: (selected) => {
                          const selectedRoles = rolesData?.data?.filter((r) =>
                            (selected as number[]).includes(r.id)
                          );
                          return selectedRoles?.map((r) => r.name).join(', ') || '';
                        },
                      }}
                      sx={{ mb: 2 }}
                    >
                      {rolesData?.data?.map((role) => (
                        <MenuItem key={role.id} value={role.id}>
                          <Checkbox checked={field.value.includes(role.id)} />
                          <ListItemText primary={role.name} secondary={role.description} />
                        </MenuItem>
                      ))}
                    </TextField>
                  )}
                />
                <Controller
                  name="isActive"
                  control={control}
                  render={({ field }) => (
                    <FormControlLabel
                      control={
                        <Checkbox
                          checked={field.value}
                          onChange={(e) => field.onChange(e.target.checked)}
                        />
                      }
                      label="Active"
                    />
                  )}
                />
              </CardContent>
            </Card>

            <Box sx={{ mt: 2, display: 'flex', gap: 2 }}>
              <Button
                fullWidth
                variant="outlined"
                onClick={() => navigate('/admin/users')}
              >
                Cancel
              </Button>
              <Button
                fullWidth
                type="submit"
                variant="contained"
                startIcon={<SaveIcon />}
                disabled={createMutation.isPending || updateMutation.isPending}
              >
                {isEdit ? 'Save Changes' : 'Create User'}
              </Button>
            </Box>
          </Grid>
        </Grid>
      </form>
    </Box>
  );
};
