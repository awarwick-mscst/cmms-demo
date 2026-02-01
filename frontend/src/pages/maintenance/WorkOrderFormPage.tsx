import React, { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import {
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  MenuItem,
  TextField,
  Typography,
  Alert,
} from '@mui/material';
import { Save as SaveIcon, ArrowBack as BackIcon } from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { workOrderService } from '../../services/workOrderService';
import { assetService } from '../../services/assetService';
import { userService } from '../../services/userService';
import {
  CreateWorkOrderRequest,
  WorkOrderTypes,
  WorkOrderPriorities,
} from '../../types';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';

const typeLabels: Record<string, string> = {
  Repair: 'Repair',
  ScheduledJob: 'Scheduled Job',
  SafetyInspection: 'Safety Inspection',
  PreventiveMaintenance: 'Preventive Maintenance',
};

export const WorkOrderFormPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const isEdit = !!id;

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreateWorkOrderRequest>({
    defaultValues: {
      type: 'Repair',
      priority: 'Medium',
    },
  });

  const { data: workOrderData, isLoading: isLoadingWorkOrder } = useQuery({
    queryKey: ['workOrder', id],
    queryFn: () => workOrderService.getWorkOrder(Number(id)),
    enabled: isEdit,
  });

  const { data: assetsData } = useQuery({
    queryKey: ['assets', { pageSize: 100 }],
    queryFn: () => assetService.getAssets({ pageSize: 100 }),
  });

  const { data: locationsData } = useQuery({
    queryKey: ['locations'],
    queryFn: () => assetService.getLocations(),
  });

  const { data: usersData } = useQuery({
    queryKey: ['users'],
    queryFn: () => userService.getUsers(),
  });

  useEffect(() => {
    if (workOrderData?.data) {
      const wo = workOrderData.data;
      reset({
        type: wo.type,
        priority: wo.priority,
        title: wo.title,
        description: wo.description,
        assetId: wo.assetId,
        locationId: wo.locationId,
        requestedBy: wo.requestedBy,
        requestedDate: wo.requestedDate?.split('T')[0],
        assignedToId: wo.assignedToId,
        scheduledStartDate: wo.scheduledStartDate?.split('T')[0],
        scheduledEndDate: wo.scheduledEndDate?.split('T')[0],
        estimatedHours: wo.estimatedHours,
      });
    }
  }, [workOrderData, reset]);

  const createMutation = useMutation({
    mutationFn: workOrderService.createWorkOrder,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['workOrders'] });
      queryClient.invalidateQueries({ queryKey: ['workOrderDashboard'] });
      navigate(`/maintenance/work-orders/${data.data?.id}`);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: any }) => workOrderService.updateWorkOrder(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workOrders'] });
      queryClient.invalidateQueries({ queryKey: ['workOrder', id] });
      navigate(`/maintenance/work-orders/${id}`);
    },
  });

  const onSubmit = (data: CreateWorkOrderRequest) => {
    if (isEdit) {
      updateMutation.mutate({ id: Number(id), data });
    } else {
      createMutation.mutate(data);
    }
  };

  if (isEdit && isLoadingWorkOrder) {
    return <LoadingSpinner message="Loading work order..." />;
  }

  const error = createMutation.error || updateMutation.error;
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <Button startIcon={<BackIcon />} onClick={() => navigate('/maintenance/work-orders')}>
          Back
        </Button>
        <Typography variant="h5">{isEdit ? 'Edit Work Order' : 'New Work Order'}</Typography>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {(error as any)?.response?.data?.errors?.[0] || 'An error occurred'}
        </Alert>
      )}

      <form onSubmit={handleSubmit(onSubmit)}>
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Work Order Details
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12}>
                    <Controller
                      name="title"
                      control={control}
                      rules={{ required: 'Title is required' }}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          label="Title"
                          error={!!errors.title}
                          helperText={errors.title?.message}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="type"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} select fullWidth label="Type">
                          {WorkOrderTypes.map((type) => (
                            <MenuItem key={type} value={type}>
                              {typeLabels[type] || type}
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="priority"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} select fullWidth label="Priority">
                          {WorkOrderPriorities.map((priority) => (
                            <MenuItem key={priority} value={priority}>
                              {priority}
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="assetId"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          select
                          fullWidth
                          label="Asset"
                          value={field.value || ''}
                        >
                          <MenuItem value="">None</MenuItem>
                          {assetsData?.items?.map((asset) => (
                            <MenuItem key={asset.id} value={asset.id}>
                              {asset.assetTag} - {asset.name}
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="locationId"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          select
                          fullWidth
                          label="Location"
                          value={field.value || ''}
                        >
                          <MenuItem value="">None</MenuItem>
                          {locationsData?.data?.map((loc) => (
                            <MenuItem key={loc.id} value={loc.id}>
                              {loc.fullPath || loc.name}
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <Controller
                      name="assignedToId"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          select
                          fullWidth
                          label="Assigned To"
                          value={field.value || ''}
                        >
                          <MenuItem value="">Unassigned</MenuItem>
                          {usersData?.data?.map((user) => (
                            <MenuItem key={user.id} value={user.id}>
                              {user.fullName}
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="description"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          multiline
                          rows={4}
                          label="Description"
                          placeholder="Describe the work to be performed..."
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
                  Schedule
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12}>
                    <Controller
                      name="scheduledStartDate"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="date"
                          label="Scheduled Start"
                          InputLabelProps={{ shrink: true }}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="scheduledEndDate"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="date"
                          label="Scheduled End"
                          InputLabelProps={{ shrink: true }}
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="estimatedHours"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="number"
                          label="Estimated Hours"
                          inputProps={{ step: 0.5, min: 0 }}
                        />
                      )}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>

            <Card sx={{ mt: 3 }}>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Request Information
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12}>
                    <Controller
                      name="requestedBy"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} fullWidth label="Requested By" />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="requestedDate"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="date"
                          label="Requested Date"
                          InputLabelProps={{ shrink: true }}
                        />
                      )}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12}>
            <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 2 }}>
              <Button variant="outlined" onClick={() => navigate('/maintenance/work-orders')}>
                Cancel
              </Button>
              <Button
                type="submit"
                variant="contained"
                startIcon={<SaveIcon />}
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Saving...' : 'Save Work Order'}
              </Button>
            </Box>
          </Grid>
        </Grid>
      </form>
    </Box>
  );
};
