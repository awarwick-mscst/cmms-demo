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
  FormControlLabel,
  Switch,
} from '@mui/material';
import { Save as SaveIcon, ArrowBack as BackIcon } from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { preventiveMaintenanceService } from '../../services/preventiveMaintenanceService';
import { assetService } from '../../services/assetService';
import {
  CreatePreventiveMaintenanceScheduleRequest,
  FrequencyTypes,
  WorkOrderPriorities,
} from '../../types';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';

const frequencyLabels: Record<string, string> = {
  Daily: 'Daily',
  Weekly: 'Weekly',
  BiWeekly: 'Bi-Weekly',
  Monthly: 'Monthly',
  Quarterly: 'Quarterly',
  SemiAnnually: 'Semi-Annually',
  Annually: 'Annually',
  Custom: 'Custom (Days)',
};

const daysOfWeek = [
  { value: 0, label: 'Sunday' },
  { value: 1, label: 'Monday' },
  { value: 2, label: 'Tuesday' },
  { value: 3, label: 'Wednesday' },
  { value: 4, label: 'Thursday' },
  { value: 5, label: 'Friday' },
  { value: 6, label: 'Saturday' },
];

export const PMScheduleFormPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const isEdit = !!id;

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<CreatePreventiveMaintenanceScheduleRequest>({
    defaultValues: {
      frequencyType: 'Monthly',
      frequencyValue: 1,
      priority: 'Medium',
      leadTimeDays: 7,
      isActive: true,
    },
  });

  const frequencyType = watch('frequencyType');

  const { data: scheduleData, isLoading: isLoadingSchedule } = useQuery({
    queryKey: ['pmSchedule', id],
    queryFn: () => preventiveMaintenanceService.getSchedule(Number(id)),
    enabled: isEdit,
  });

  const { data: assetsData } = useQuery({
    queryKey: ['assets', { pageSize: 100 }],
    queryFn: () => assetService.getAssets({ pageSize: 100 }),
  });

  useEffect(() => {
    if (scheduleData?.data) {
      const schedule = scheduleData.data;
      reset({
        name: schedule.name,
        description: schedule.description,
        assetId: schedule.assetId,
        frequencyType: schedule.frequencyType,
        frequencyValue: schedule.frequencyValue,
        dayOfWeek: schedule.dayOfWeek,
        dayOfMonth: schedule.dayOfMonth,
        nextDueDate: schedule.nextDueDate?.split('T')[0],
        leadTimeDays: schedule.leadTimeDays,
        workOrderTitle: schedule.workOrderTitle,
        workOrderDescription: schedule.workOrderDescription,
        priority: schedule.priority,
        estimatedHours: schedule.estimatedHours,
        isActive: schedule.isActive,
      });
    }
  }, [scheduleData, reset]);

  const createMutation = useMutation({
    mutationFn: preventiveMaintenanceService.createSchedule,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pmSchedules'] });
      navigate('/maintenance/pm-schedules');
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: any }) =>
      preventiveMaintenanceService.updateSchedule(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pmSchedules'] });
      queryClient.invalidateQueries({ queryKey: ['pmSchedule', id] });
      navigate('/maintenance/pm-schedules');
    },
  });

  const onSubmit = (data: CreatePreventiveMaintenanceScheduleRequest) => {
    if (isEdit) {
      updateMutation.mutate({ id: Number(id), data });
    } else {
      createMutation.mutate(data);
    }
  };

  if (isEdit && isLoadingSchedule) {
    return <LoadingSpinner message="Loading schedule..." />;
  }

  const error = createMutation.error || updateMutation.error;
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <Button startIcon={<BackIcon />} onClick={() => navigate('/maintenance/pm-schedules')}>
          Back
        </Button>
        <Typography variant="h5">{isEdit ? 'Edit PM Schedule' : 'New PM Schedule'}</Typography>
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
                  Schedule Information
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12}>
                    <Controller
                      name="name"
                      control={control}
                      rules={{ required: 'Name is required' }}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          label="Schedule Name"
                          error={!!errors.name}
                          helperText={errors.name?.message}
                        />
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
                          <MenuItem value="">No specific asset</MenuItem>
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
                      name="isActive"
                      control={control}
                      render={({ field }) => (
                        <FormControlLabel
                          control={<Switch checked={field.value} onChange={field.onChange} />}
                          label="Active"
                        />
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
                          rows={3}
                          label="Description"
                          placeholder="Describe the maintenance activities..."
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
                  Work Order Template
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12}>
                    <Controller
                      name="workOrderTitle"
                      control={control}
                      rules={{ required: 'Work order title is required' }}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          label="Work Order Title"
                          error={!!errors.workOrderTitle}
                          helperText={errors.workOrderTitle?.message || 'Title for generated work orders'}
                        />
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
                  <Grid item xs={12}>
                    <Controller
                      name="workOrderDescription"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          multiline
                          rows={4}
                          label="Work Order Description"
                          placeholder="Instructions for the generated work orders..."
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
                  Frequency
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12}>
                    <Controller
                      name="frequencyType"
                      control={control}
                      render={({ field }) => (
                        <TextField {...field} select fullWidth label="Frequency Type">
                          {FrequencyTypes.map((type) => (
                            <MenuItem key={type} value={type}>
                              {frequencyLabels[type] || type}
                            </MenuItem>
                          ))}
                        </TextField>
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="frequencyValue"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="number"
                          label={frequencyType === 'Custom' ? 'Days' : 'Every N periods'}
                          inputProps={{ min: 1 }}
                          helperText={
                            frequencyType === 'Custom'
                              ? 'Number of days between maintenance'
                              : `Every ${field.value || 1} ${frequencyType?.toLowerCase() || 'period'}(s)`
                          }
                        />
                      )}
                    />
                  </Grid>

                  {frequencyType === 'Weekly' && (
                    <Grid item xs={12}>
                      <Controller
                        name="dayOfWeek"
                        control={control}
                        render={({ field }) => (
                          <TextField
                            {...field}
                            select
                            fullWidth
                            label="Day of Week"
                            value={field.value ?? ''}
                          >
                            <MenuItem value="">Any day</MenuItem>
                            {daysOfWeek.map((day) => (
                              <MenuItem key={day.value} value={day.value}>
                                {day.label}
                              </MenuItem>
                            ))}
                          </TextField>
                        )}
                      />
                    </Grid>
                  )}

                  {(frequencyType === 'Monthly' ||
                    frequencyType === 'Quarterly' ||
                    frequencyType === 'SemiAnnually' ||
                    frequencyType === 'Annually') && (
                    <Grid item xs={12}>
                      <Controller
                        name="dayOfMonth"
                        control={control}
                        render={({ field }) => (
                          <TextField
                            {...field}
                            fullWidth
                            type="number"
                            label="Day of Month"
                            inputProps={{ min: 1, max: 31 }}
                            helperText="1-31 (will adjust for shorter months)"
                          />
                        )}
                      />
                    </Grid>
                  )}
                </Grid>
              </CardContent>
            </Card>

            <Card sx={{ mt: 3 }}>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Schedule Settings
                </Typography>
                <Grid container spacing={2}>
                  <Grid item xs={12}>
                    <Controller
                      name="nextDueDate"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="date"
                          label="Next Due Date"
                          InputLabelProps={{ shrink: true }}
                          helperText="When to generate the first/next work order"
                        />
                      )}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <Controller
                      name="leadTimeDays"
                      control={control}
                      render={({ field }) => (
                        <TextField
                          {...field}
                          fullWidth
                          type="number"
                          label="Lead Time (Days)"
                          inputProps={{ min: 0 }}
                          helperText="Days before due date to generate work order"
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
              <Button variant="outlined" onClick={() => navigate('/maintenance/pm-schedules')}>
                Cancel
              </Button>
              <Button
                type="submit"
                variant="contained"
                startIcon={<SaveIcon />}
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Saving...' : 'Save Schedule'}
              </Button>
            </Box>
          </Grid>
        </Grid>
      </form>
    </Box>
  );
};
