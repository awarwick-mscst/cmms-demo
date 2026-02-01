import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Grid,
  Typography,
  ToggleButton,
  ToggleButtonGroup,
  Alert,
} from '@mui/material';
import {
  PlayArrow as StartIcon,
  Timer as TimerIcon,
  Visibility as ViewIcon,
  Stop as StopIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { workOrderService } from '../../services/workOrderService';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { useAuth } from '../../hooks/useAuth';
import { WorkOrderSummary } from '../../types';

const statusColors: Record<string, 'success' | 'warning' | 'error' | 'default' | 'info' | 'primary'> = {
  Draft: 'default',
  Open: 'info',
  InProgress: 'primary',
  OnHold: 'warning',
  Completed: 'success',
  Cancelled: 'error',
};

const priorityColors: Record<string, 'error' | 'warning' | 'info' | 'default'> = {
  Emergency: 'error',
  Critical: 'error',
  High: 'warning',
  Medium: 'info',
  Low: 'default',
};

export const MyWorkOrdersPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const [statusFilter, setStatusFilter] = useState<string>('active');
  const [elapsedTime, setElapsedTime] = useState(0);

  // Get my active session
  const { data: sessionData, refetch: refetchSession } = useQuery({
    queryKey: ['myActiveSession'],
    queryFn: () => workOrderService.getMyActiveSession(),
    refetchInterval: 30000,
  });

  const activeSession = sessionData?.data;

  // Update elapsed time for active session
  useEffect(() => {
    if (activeSession?.isActive) {
      const startTime = new Date(activeSession.startedAt).getTime();
      const updateTimer = () => {
        const now = Date.now();
        setElapsedTime(Math.floor((now - startTime) / 1000));
      };
      updateTimer();
      const interval = setInterval(updateTimer, 1000);
      return () => clearInterval(interval);
    } else {
      setElapsedTime(0);
    }
  }, [activeSession]);

  const formatElapsedTime = (seconds: number) => {
    const hrs = Math.floor(seconds / 3600);
    const mins = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    return `${hrs.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  // Get work orders assigned to me
  const { data: workOrdersData, isLoading } = useQuery({
    queryKey: ['myWorkOrders', user?.id, statusFilter],
    queryFn: () => workOrderService.getWorkOrders({
      assignedToId: user?.id,
      status: statusFilter === 'active' ? undefined : statusFilter,
      pageSize: 50,
    }),
    enabled: !!user?.id,
  });

  // Filter for active statuses if needed
  const workOrders = statusFilter === 'active'
    ? workOrdersData?.items?.filter(wo => ['Open', 'InProgress', 'OnHold'].includes(wo.status)) || []
    : workOrdersData?.items || [];

  // Mutations
  const startWorkMutation = useMutation({
    mutationFn: (id: number) => workOrderService.startWorkOrder(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['myWorkOrders'] });
    },
  });

  const startSessionMutation = useMutation({
    mutationFn: (workOrderId: number) => workOrderService.startSession(workOrderId),
    onSuccess: () => {
      refetchSession();
      queryClient.invalidateQueries({ queryKey: ['myActiveSession'] });
    },
  });

  const stopSessionMutation = useMutation({
    mutationFn: (workOrderId: number) => workOrderService.stopSession(workOrderId),
    onSuccess: () => {
      refetchSession();
      queryClient.invalidateQueries({ queryKey: ['myActiveSession'] });
      queryClient.invalidateQueries({ queryKey: ['myWorkOrders'] });
    },
  });

  const handleStartWork = async (workOrder: WorkOrderSummary) => {
    if (workOrder.status === 'Open') {
      // First transition to InProgress
      await startWorkMutation.mutateAsync(workOrder.id);
    }
    // Then start the session
    await startSessionMutation.mutateAsync(workOrder.id);
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading your work orders..." />;
  }

  return (
    <Box>
      <Typography variant="h5" gutterBottom>
        My Work Orders
      </Typography>

      {/* Active Session Banner */}
      {activeSession?.isActive && (
        <Alert
          severity="success"
          sx={{ mb: 3 }}
          action={
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Typography variant="h6" sx={{ fontFamily: 'monospace', mr: 2 }}>
                {formatElapsedTime(elapsedTime)}
              </Typography>
              <Button
                color="inherit"
                size="small"
                onClick={() => navigate(`/maintenance/work-orders/${activeSession.workOrderId}`)}
              >
                View
              </Button>
              <Button
                color="error"
                variant="contained"
                size="small"
                startIcon={<StopIcon />}
                onClick={() => stopSessionMutation.mutate(activeSession.workOrderId)}
              >
                Stop
              </Button>
            </Box>
          }
        >
          <Typography variant="subtitle1">
            Currently working on: <strong>{activeSession.workOrderNumber}</strong> - {activeSession.workOrderTitle}
          </Typography>
        </Alert>
      )}

      {/* Status Filter */}
      <Box sx={{ mb: 3 }}>
        <ToggleButtonGroup
          value={statusFilter}
          exclusive
          onChange={(_, value) => value && setStatusFilter(value)}
          size="small"
        >
          <ToggleButton value="active">Active</ToggleButton>
          <ToggleButton value="Open">Open</ToggleButton>
          <ToggleButton value="InProgress">In Progress</ToggleButton>
          <ToggleButton value="OnHold">On Hold</ToggleButton>
          <ToggleButton value="Completed">Completed</ToggleButton>
        </ToggleButtonGroup>
      </Box>

      {/* Work Order Cards */}
      {workOrders.length === 0 ? (
        <Card>
          <CardContent>
            <Typography color="text.secondary" align="center">
              No work orders found
            </Typography>
          </CardContent>
        </Card>
      ) : (
        <Grid container spacing={2}>
          {workOrders.map((wo) => (
            <Grid item xs={12} md={6} lg={4} key={wo.id}>
              <Card
                sx={{
                  height: '100%',
                  border: activeSession?.workOrderId === wo.id ? '2px solid' : undefined,
                  borderColor: 'success.main',
                }}
              >
                <CardContent>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                    <Typography variant="subtitle2" color="text.secondary">
                      {wo.workOrderNumber}
                    </Typography>
                    <Box sx={{ display: 'flex', gap: 0.5 }}>
                      <Chip
                        label={wo.priority}
                        size="small"
                        color={priorityColors[wo.priority] || 'default'}
                        variant="outlined"
                      />
                      <Chip
                        label={wo.status}
                        size="small"
                        color={statusColors[wo.status] || 'default'}
                      />
                    </Box>
                  </Box>

                  <Typography variant="h6" sx={{ mb: 1, fontSize: '1rem' }}>
                    {wo.title}
                  </Typography>

                  <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                    {wo.assetName || 'No asset'} {wo.locationName && `@ ${wo.locationName}`}
                  </Typography>

                  {wo.scheduledStartDate && (
                    <Typography variant="caption" display="block" color="text.secondary">
                      Scheduled: {new Date(wo.scheduledStartDate).toLocaleDateString()}
                    </Typography>
                  )}

                  <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
                    <Button
                      size="small"
                      variant="outlined"
                      startIcon={<ViewIcon />}
                      onClick={() => navigate(`/maintenance/work-orders/${wo.id}`)}
                    >
                      View
                    </Button>

                    {/* Show Start Working button for Open or InProgress work orders */}
                    {(wo.status === 'Open' || wo.status === 'InProgress') && !activeSession?.isActive && (
                      <Button
                        size="small"
                        variant="contained"
                        color="success"
                        startIcon={wo.status === 'Open' ? <StartIcon /> : <TimerIcon />}
                        onClick={() => handleStartWork(wo)}
                        disabled={startWorkMutation.isPending || startSessionMutation.isPending}
                      >
                        {wo.status === 'Open' ? 'Start Work' : 'Clock In'}
                      </Button>
                    )}

                    {/* Show Stop button if this is the active session */}
                    {activeSession?.workOrderId === wo.id && (
                      <Button
                        size="small"
                        variant="contained"
                        color="error"
                        startIcon={<StopIcon />}
                        onClick={() => stopSessionMutation.mutate(wo.id)}
                        disabled={stopSessionMutation.isPending}
                      >
                        Clock Out
                      </Button>
                    )}
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
    </Box>
  );
};
