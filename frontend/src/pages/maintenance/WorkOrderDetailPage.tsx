import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Divider,
  Grid,
  IconButton,
  List,
  ListItem,
  ListItemText,
  MenuItem,
  Paper,
  Tab,
  Tabs,
  TextField,
  Typography,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControlLabel,
  Checkbox,
  Alert,
} from '@mui/material';
import {
  ArrowBack as BackIcon,
  Edit as EditIcon,
  PlayArrow as StartIcon,
  CheckCircle as CompleteIcon,
  Pause as HoldIcon,
  Cancel as CancelIcon,
  Refresh as ResumeIcon,
  Send as SubmitIcon,
  Add as AddIcon,
  Delete as DeleteIcon,
  Timer as TimerIcon,
  Stop as StopIcon,
  NoteAdd as NoteAddIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { workOrderService } from '../../services/workOrderService';
import { partService } from '../../services/partService';
import { userService } from '../../services/userService';
import { assetService } from '../../services/assetService';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { useAuth } from '../../hooks/useAuth';
import { LaborTypes } from '../../types';
import { WorkOrderTaskList } from '../../components/workorder/WorkOrderTaskList';

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

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;
  return (
    <div hidden={value !== index} {...other}>
      {value === index && <Box sx={{ py: 2 }}>{children}</Box>}
    </div>
  );
}

export const WorkOrderDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { hasPermission, user } = useAuth();
  const [tabValue, setTabValue] = useState(0);
  const [commentDialog, setCommentDialog] = useState(false);
  const [newComment, setNewComment] = useState('');
  const [isInternal, setIsInternal] = useState(false);
  const [completeDialog, setCompleteDialog] = useState(false);
  const [completionNotes, setCompletionNotes] = useState('');

  // Labor entry state
  const [laborDialog, setLaborDialog] = useState(false);
  const [laborUserId, setLaborUserId] = useState<number | ''>('');
  const [laborDate, setLaborDate] = useState(new Date().toISOString().split('T')[0]);
  const [laborHours, setLaborHours] = useState<number | ''>('');
  const [laborType, setLaborType] = useState('Regular');
  const [laborRate, setLaborRate] = useState<number | ''>('');
  const [laborNotes, setLaborNotes] = useState('');

  // Parts usage state
  const [partsDialog, setPartsDialog] = useState(false);
  const [selectedPartId, setSelectedPartId] = useState<number | ''>('');
  const [partQuantity, setPartQuantity] = useState<number | ''>(1);
  const [partLocationId, setPartLocationId] = useState<number | ''>('');
  const [partNotes, setPartNotes] = useState('');

  // Work session state
  const [sessionNoteDialog, setSessionNoteDialog] = useState(false);
  const [sessionNote, setSessionNote] = useState('');
  const [elapsedTime, setElapsedTime] = useState(0);

  const { data, isLoading, error } = useQuery({
    queryKey: ['workOrder', id],
    queryFn: () => workOrderService.getWorkOrder(Number(id)),
  });

  const { data: historyData } = useQuery({
    queryKey: ['workOrderHistory', id],
    queryFn: () => workOrderService.getWorkOrderHistory(Number(id)),
  });

  const { data: commentsData, refetch: refetchComments } = useQuery({
    queryKey: ['workOrderComments', id],
    queryFn: () => workOrderService.getWorkOrderComments(Number(id), true),
  });

  const { data: laborData, refetch: refetchLabor } = useQuery({
    queryKey: ['workOrderLabor', id],
    queryFn: () => workOrderService.getWorkOrderLabor(Number(id)),
  });

  const { data: partsData, refetch: refetchParts } = useQuery({
    queryKey: ['workOrderParts', id],
    queryFn: () => workOrderService.getWorkOrderParts(Number(id)),
  });

  // Fetch active work session
  const { data: sessionData, refetch: refetchSession } = useQuery({
    queryKey: ['activeSession', id],
    queryFn: () => workOrderService.getActiveSession(Number(id)),
    refetchInterval: 30000, // Refresh every 30 seconds to keep timer updated
  });

  const activeSession = sessionData?.data;

  // Update elapsed time for active session
  React.useEffect(() => {
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

  // Fetch users for labor entry
  const { data: usersData } = useQuery({
    queryKey: ['users'],
    queryFn: () => userService.getUsers(),
  });

  // Fetch parts for part usage
  const { data: availablePartsData } = useQuery({
    queryKey: ['parts', { pageSize: 200 }],
    queryFn: () => partService.getParts({ pageSize: 200 }),
  });

  // Fetch storage locations for part usage
  const { data: storageLocationsData } = useQuery({
    queryKey: ['storageLocations'],
    queryFn: async () => {
      const response = await fetch('/api/v1/storage-locations');
      return response.json();
    },
  });

  const invalidateQueries = () => {
    queryClient.invalidateQueries({ queryKey: ['workOrder', id] });
    queryClient.invalidateQueries({ queryKey: ['workOrderHistory', id] });
    queryClient.invalidateQueries({ queryKey: ['workOrders'] });
    queryClient.invalidateQueries({ queryKey: ['workOrderDashboard'] });
  };

  const submitMutation = useMutation({
    mutationFn: () => workOrderService.submitWorkOrder(Number(id)),
    onSuccess: invalidateQueries,
  });

  const startMutation = useMutation({
    mutationFn: () => workOrderService.startWorkOrder(Number(id)),
    onSuccess: invalidateQueries,
  });

  const completeMutation = useMutation({
    mutationFn: () => workOrderService.completeWorkOrder(Number(id), { completionNotes }),
    onSuccess: () => {
      invalidateQueries();
      setCompleteDialog(false);
      setCompletionNotes('');
    },
  });

  const holdMutation = useMutation({
    mutationFn: () => workOrderService.holdWorkOrder(Number(id)),
    onSuccess: invalidateQueries,
  });

  const resumeMutation = useMutation({
    mutationFn: () => workOrderService.resumeWorkOrder(Number(id)),
    onSuccess: invalidateQueries,
  });

  const cancelMutation = useMutation({
    mutationFn: () => workOrderService.cancelWorkOrder(Number(id)),
    onSuccess: invalidateQueries,
  });

  const addCommentMutation = useMutation({
    mutationFn: () => workOrderService.addComment(Number(id), { comment: newComment, isInternal }),
    onSuccess: () => {
      refetchComments();
      setCommentDialog(false);
      setNewComment('');
      setIsInternal(false);
    },
  });

  const addLaborMutation = useMutation({
    mutationFn: () => workOrderService.addLaborEntry(Number(id), {
      userId: laborUserId as number,
      workDate: laborDate,
      hoursWorked: laborHours as number,
      laborType: laborType,
      hourlyRate: laborRate || undefined,
      notes: laborNotes || undefined,
    }),
    onSuccess: () => {
      refetchLabor();
      invalidateQueries();
      setLaborDialog(false);
      resetLaborForm();
    },
  });

  const deleteLaborMutation = useMutation({
    mutationFn: (laborId: number) => workOrderService.deleteLaborEntry(Number(id), laborId),
    onSuccess: () => {
      refetchLabor();
      invalidateQueries();
    },
  });

  const addPartMutation = useMutation({
    mutationFn: async () => {
      const workOrder = data?.data;
      if (!workOrder || !selectedPartId || !partLocationId) return;

      return partService.usePartOnAsset(selectedPartId as number, {
        assetId: workOrder.assetId || 0,
        partId: selectedPartId as number,
        locationId: partLocationId as number,
        quantityUsed: partQuantity as number,
        workOrderId: Number(id),
        notes: partNotes || undefined,
      });
    },
    onSuccess: () => {
      refetchParts();
      setPartsDialog(false);
      resetPartsForm();
    },
  });

  // Work session mutations
  const startSessionMutation = useMutation({
    mutationFn: () => workOrderService.startSession(Number(id)),
    onSuccess: () => {
      refetchSession();
      queryClient.invalidateQueries({ queryKey: ['myActiveSession'] });
    },
  });

  const stopSessionMutation = useMutation({
    mutationFn: (notes?: string) => workOrderService.stopSession(Number(id), { notes }),
    onSuccess: () => {
      refetchSession();
      refetchLabor();
      invalidateQueries();
      queryClient.invalidateQueries({ queryKey: ['myActiveSession'] });
    },
  });

  const addSessionNoteMutation = useMutation({
    mutationFn: (note: string) => workOrderService.addSessionNote(Number(id), { note }),
    onSuccess: () => {
      refetchSession();
      setSessionNoteDialog(false);
      setSessionNote('');
    },
  });

  const reopenMutation = useMutation({
    mutationFn: () => workOrderService.reopenWorkOrder(Number(id)),
    onSuccess: invalidateQueries,
  });

  const resetLaborForm = () => {
    setLaborUserId(user?.id || '');
    setLaborDate(new Date().toISOString().split('T')[0]);
    setLaborHours('');
    setLaborType('Regular');
    setLaborRate('');
    setLaborNotes('');
  };

  const resetPartsForm = () => {
    setSelectedPartId('');
    setPartQuantity(1);
    setPartLocationId('');
    setPartNotes('');
  };

  if (isLoading) {
    return <LoadingSpinner message="Loading work order..." />;
  }

  if (error || !data?.data) {
    return (
      <Box>
        <Alert severity="error">Work order not found</Alert>
        <Button startIcon={<BackIcon />} onClick={() => navigate('/maintenance/work-orders')} sx={{ mt: 2 }}>
          Back to Work Orders
        </Button>
      </Box>
    );
  }

  const workOrder = data.data;
  const canEdit = hasPermission('work-orders.edit');
  const isWorkOrderActive = workOrder.status === 'Open' || workOrder.status === 'InProgress' || workOrder.status === 'OnHold';

  const getAvailableActions = () => {
    const actions: React.ReactNode[] = [];

    if (!canEdit) return actions;

    switch (workOrder.status) {
      case 'Draft':
        actions.push(
          <Button key="submit" variant="contained" startIcon={<SubmitIcon />} onClick={() => submitMutation.mutate()}>
            Submit
          </Button>
        );
        break;
      case 'Open':
        actions.push(
          <Button key="start" variant="contained" color="primary" startIcon={<StartIcon />} onClick={() => startMutation.mutate()}>
            Start Work
          </Button>,
          <Button key="cancel" variant="outlined" color="error" startIcon={<CancelIcon />} onClick={() => cancelMutation.mutate()}>
            Cancel
          </Button>
        );
        break;
      case 'InProgress':
        actions.push(
          <Button key="complete" variant="contained" color="success" startIcon={<CompleteIcon />} onClick={() => setCompleteDialog(true)}>
            Complete
          </Button>,
          <Button key="hold" variant="outlined" color="warning" startIcon={<HoldIcon />} onClick={() => holdMutation.mutate()}>
            Put On Hold
          </Button>
        );
        break;
      case 'OnHold':
        actions.push(
          <Button key="resume" variant="contained" color="primary" startIcon={<ResumeIcon />} onClick={() => resumeMutation.mutate()}>
            Resume
          </Button>,
          <Button key="cancel" variant="outlined" color="error" startIcon={<CancelIcon />} onClick={() => cancelMutation.mutate()}>
            Cancel
          </Button>
        );
        break;
      case 'Cancelled':
      case 'Completed':
        actions.push(
          <Button key="reopen" variant="contained" color="primary" startIcon={<ResumeIcon />} onClick={() => reopenMutation.mutate()}>
            Reopen
          </Button>
        );
        break;
    }

    return actions;
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Button startIcon={<BackIcon />} onClick={() => navigate('/maintenance/work-orders')}>
            Back
          </Button>
          <Box>
            <Typography variant="h5">{workOrder.workOrderNumber}</Typography>
            <Typography variant="body2" color="text.secondary">
              {workOrder.title}
            </Typography>
          </Box>
        </Box>
        <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
          {canEdit && workOrder.status !== 'Completed' && workOrder.status !== 'Cancelled' && (
            <Button
              variant="outlined"
              startIcon={<EditIcon />}
              onClick={() => navigate(`/maintenance/work-orders/${id}/edit`)}
            >
              Edit
            </Button>
          )}
          {getAvailableActions()}
        </Box>
      </Box>

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
                <Chip label={workOrder.status} color={statusColors[workOrder.status] || 'default'} />
                <Chip label={workOrder.priority} color={priorityColors[workOrder.priority] || 'default'} variant="outlined" />
                <Chip label={workOrder.type} variant="outlined" />
              </Box>

              <Typography variant="h6" gutterBottom>
                {workOrder.title}
              </Typography>

              {workOrder.description && (
                <Typography variant="body1" color="text.secondary" sx={{ whiteSpace: 'pre-wrap', mb: 2 }}>
                  {workOrder.description}
                </Typography>
              )}

              <Divider sx={{ my: 2 }} />

              <Grid container spacing={2}>
                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary">Asset</Typography>
                  <Typography variant="body2">{workOrder.assetName || '-'}</Typography>
                </Grid>
                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary">Location</Typography>
                  <Typography variant="body2">{workOrder.locationName || '-'}</Typography>
                </Grid>
                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary">Assigned To</Typography>
                  <Typography variant="body2">{workOrder.assignedToName || 'Unassigned'}</Typography>
                </Grid>
                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary">Requested By</Typography>
                  <Typography variant="body2">{workOrder.requestedBy || '-'}</Typography>
                </Grid>
                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary">Scheduled Start</Typography>
                  <Typography variant="body2">
                    {workOrder.scheduledStartDate ? new Date(workOrder.scheduledStartDate).toLocaleDateString() : '-'}
                  </Typography>
                </Grid>
                <Grid item xs={6} sm={4}>
                  <Typography variant="caption" color="text.secondary">Scheduled End</Typography>
                  <Typography variant="body2">
                    {workOrder.scheduledEndDate ? new Date(workOrder.scheduledEndDate).toLocaleDateString() : '-'}
                  </Typography>
                </Grid>
              </Grid>

              {workOrder.status === 'Completed' && (
                <>
                  <Divider sx={{ my: 2 }} />
                  <Typography variant="subtitle2" gutterBottom>Completion Details</Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={6} sm={4}>
                      <Typography variant="caption" color="text.secondary">Actual Start</Typography>
                      <Typography variant="body2">
                        {workOrder.actualStartDate ? new Date(workOrder.actualStartDate).toLocaleDateString() : '-'}
                      </Typography>
                    </Grid>
                    <Grid item xs={6} sm={4}>
                      <Typography variant="caption" color="text.secondary">Actual End</Typography>
                      <Typography variant="body2">
                        {workOrder.actualEndDate ? new Date(workOrder.actualEndDate).toLocaleDateString() : '-'}
                      </Typography>
                    </Grid>
                    <Grid item xs={6} sm={4}>
                      <Typography variant="caption" color="text.secondary">Actual Hours</Typography>
                      <Typography variant="body2">{workOrder.actualHours || 0} hrs</Typography>
                    </Grid>
                    {workOrder.completionNotes && (
                      <Grid item xs={12}>
                        <Typography variant="caption" color="text.secondary">Completion Notes</Typography>
                        <Typography variant="body2">{workOrder.completionNotes}</Typography>
                      </Grid>
                    )}
                  </Grid>
                </>
              )}
            </CardContent>
          </Card>

          <Paper sx={{ mt: 3 }}>
            <Tabs value={tabValue} onChange={(_, v) => setTabValue(v)}>
              <Tab label="Tasks" />
              <Tab label="Comments" />
              <Tab label="Labor" />
              <Tab label="Parts" />
              <Tab label="History" />
            </Tabs>

            <TabPanel value={tabValue} index={0}>
              <Box sx={{ px: 2 }}>
                <WorkOrderTaskList
                  workOrderId={Number(id)}
                  disabled={workOrder.status === 'Completed' || workOrder.status === 'Cancelled'}
                />
              </Box>
            </TabPanel>

            <TabPanel value={tabValue} index={1}>
              <Box sx={{ px: 2 }}>
                {canEdit && (
                  <Button startIcon={<AddIcon />} onClick={() => setCommentDialog(true)} sx={{ mb: 2 }}>
                    Add Comment
                  </Button>
                )}
                <List>
                  {commentsData?.data?.map((comment) => (
                    <ListItem key={comment.id} divider>
                      <ListItemText
                        primary={
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <Typography variant="body2">{comment.createdByName}</Typography>
                            {comment.isInternal && <Chip label="Internal" size="small" color="warning" />}
                          </Box>
                        }
                        secondary={
                          <>
                            <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                              {comment.comment}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {new Date(comment.createdAt).toLocaleString()}
                            </Typography>
                          </>
                        }
                      />
                    </ListItem>
                  ))}
                  {(!commentsData?.data || commentsData.data.length === 0) && (
                    <Typography color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                      No comments yet
                    </Typography>
                  )}
                </List>
              </Box>
            </TabPanel>

            <TabPanel value={tabValue} index={2}>
              <Box sx={{ px: 2 }}>
                {canEdit && isWorkOrderActive && (
                  <Button startIcon={<AddIcon />} onClick={() => { resetLaborForm(); setLaborDialog(true); }} sx={{ mb: 2 }}>
                    Add Labor Entry
                  </Button>
                )}
                <List>
                  {laborData?.data?.map((labor) => (
                    <ListItem
                      key={labor.id}
                      divider
                      secondaryAction={
                        canEdit && isWorkOrderActive && (
                          <IconButton edge="end" onClick={() => deleteLaborMutation.mutate(labor.id)} color="error">
                            <DeleteIcon />
                          </IconButton>
                        )
                      }
                    >
                      <ListItemText
                        primary={`${labor.userName} - ${labor.hoursWorked} hrs (${labor.laborType})`}
                        secondary={
                          <>
                            <Typography variant="body2">
                              Date: {new Date(labor.workDate).toLocaleDateString()}
                              {labor.hourlyRate && ` | Rate: $${labor.hourlyRate}/hr`}
                              {labor.totalCost && ` | Cost: $${labor.totalCost.toFixed(2)}`}
                            </Typography>
                            {labor.notes && <Typography variant="body2">{labor.notes}</Typography>}
                          </>
                        }
                      />
                    </ListItem>
                  ))}
                  {(!laborData?.data || laborData.data.length === 0) && (
                    <Typography color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                      No labor entries
                    </Typography>
                  )}
                </List>
              </Box>
            </TabPanel>

            <TabPanel value={tabValue} index={3}>
              <Box sx={{ px: 2 }}>
                {canEdit && isWorkOrderActive && workOrder.assetId && (
                  <Button startIcon={<AddIcon />} onClick={() => { resetPartsForm(); setPartsDialog(true); }} sx={{ mb: 2 }}>
                    Add Part Usage
                  </Button>
                )}
                {!workOrder.assetId && (
                  <Alert severity="info" sx={{ mb: 2 }}>
                    Assign an asset to this work order to track parts usage.
                  </Alert>
                )}
                <List>
                  {partsData?.data?.map((part) => (
                    <ListItem key={part.id} divider>
                      <ListItemText
                        primary={`${part.partNumber} - ${part.partName}`}
                        secondary={
                          <>
                            Qty: {part.quantityUsed} @ ${part.unitCostAtTime.toFixed(2)} = ${part.totalCost.toFixed(2)}
                            <br />
                            Used: {new Date(part.usedDate).toLocaleDateString()}
                          </>
                        }
                      />
                    </ListItem>
                  ))}
                  {(!partsData?.data || partsData.data.length === 0) && (
                    <Typography color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                      No parts used
                    </Typography>
                  )}
                </List>
              </Box>
            </TabPanel>

            <TabPanel value={tabValue} index={4}>
              <Box sx={{ px: 2 }}>
                <List>
                  {historyData?.data?.map((history) => (
                    <ListItem key={history.id} divider>
                      <ListItemText
                        primary={
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            {history.fromStatus && (
                              <>
                                <Chip label={history.fromStatus} size="small" />
                                <Typography>→</Typography>
                              </>
                            )}
                            <Chip label={history.toStatus} size="small" color={statusColors[history.toStatus] || 'default'} />
                          </Box>
                        }
                        secondary={
                          <>
                            <Typography variant="body2">
                              By {history.changedByName} on {new Date(history.changedAt).toLocaleString()}
                            </Typography>
                            {history.notes && <Typography variant="body2">{history.notes}</Typography>}
                          </>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              </Box>
            </TabPanel>
          </Paper>
        </Grid>

        <Grid item xs={12} md={4}>
          {/* Work Session Card - shown when work order is InProgress */}
          {workOrder.status === 'InProgress' && (
            <Card sx={{ mb: 2, border: activeSession?.isActive ? '2px solid' : undefined, borderColor: 'success.main' }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                  <TimerIcon color={activeSession?.isActive ? 'success' : 'action'} />
                  <Typography variant="h6">Work Session</Typography>
                </Box>

                {activeSession?.isActive ? (
                  <Box>
                    <Box sx={{ textAlign: 'center', mb: 2 }}>
                      <Typography variant="h3" sx={{ fontFamily: 'monospace', color: 'success.main' }}>
                        {formatElapsedTime(elapsedTime)}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Started at {new Date(activeSession.startedAt).toLocaleTimeString()}
                      </Typography>
                    </Box>

                    {activeSession.notes && (
                      <Box sx={{ mb: 2, p: 1, bgcolor: 'grey.100', borderRadius: 1 }}>
                        <Typography variant="caption" color="text.secondary">Session Notes:</Typography>
                        <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', fontSize: '0.85rem' }}>
                          {activeSession.notes}
                        </Typography>
                      </Box>
                    )}

                    <Box sx={{ display: 'flex', gap: 1, flexDirection: 'column' }}>
                      <Button
                        variant="outlined"
                        startIcon={<NoteAddIcon />}
                        onClick={() => setSessionNoteDialog(true)}
                        fullWidth
                      >
                        Add Note
                      </Button>
                      <Button
                        variant="contained"
                        color="error"
                        startIcon={<StopIcon />}
                        onClick={() => stopSessionMutation.mutate(undefined)}
                        disabled={stopSessionMutation.isPending}
                        fullWidth
                      >
                        Stop Working
                      </Button>
                    </Box>
                  </Box>
                ) : (
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      Start a work session to automatically track your time on this work order.
                    </Typography>
                    <Button
                      variant="contained"
                      color="success"
                      startIcon={<TimerIcon />}
                      onClick={() => startSessionMutation.mutate()}
                      disabled={startSessionMutation.isPending}
                      fullWidth
                    >
                      Start Working
                    </Button>
                  </Box>
                )}
              </CardContent>
            </Card>
          )}

          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Summary</Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Typography variant="body2" color="text.secondary">Estimated Hours</Typography>
                  <Typography variant="body2">{workOrder.estimatedHours || 0} hrs</Typography>
                </Box>
                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Typography variant="body2" color="text.secondary">Actual Hours</Typography>
                  <Typography variant="body2">{workOrder.actualHours || 0} hrs</Typography>
                </Box>
                <Divider />
                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Typography variant="body2" color="text.secondary">Created</Typography>
                  <Typography variant="body2">{new Date(workOrder.createdAt).toLocaleDateString()}</Typography>
                </Box>
                {workOrder.updatedAt && (
                  <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Typography variant="body2" color="text.secondary">Updated</Typography>
                    <Typography variant="body2">{new Date(workOrder.updatedAt).toLocaleDateString()}</Typography>
                  </Box>
                )}
              </Box>
            </CardContent>
          </Card>

          {workOrder.preventiveMaintenanceScheduleId && (
            <Card sx={{ mt: 2 }}>
              <CardContent>
                <Typography variant="h6" gutterBottom>PM Schedule</Typography>
                <Typography variant="body2">
                  This work order was generated from preventive maintenance schedule:
                </Typography>
                <Typography variant="body2" color="primary" sx={{ mt: 1 }}>
                  {workOrder.preventiveMaintenanceScheduleName}
                </Typography>
              </CardContent>
            </Card>
          )}
        </Grid>
      </Grid>

      {/* Add Comment Dialog */}
      <Dialog open={commentDialog} onClose={() => setCommentDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Comment</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            multiline
            rows={4}
            label="Comment"
            value={newComment}
            onChange={(e) => setNewComment(e.target.value)}
            sx={{ mt: 1 }}
          />
          <FormControlLabel
            control={<Checkbox checked={isInternal} onChange={(e) => setIsInternal(e.target.checked)} />}
            label="Internal comment (only visible to technicians)"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCommentDialog(false)}>Cancel</Button>
          <Button variant="contained" onClick={() => addCommentMutation.mutate()} disabled={!newComment.trim()}>
            Add Comment
          </Button>
        </DialogActions>
      </Dialog>

      {/* Complete Work Order Dialog */}
      <Dialog open={completeDialog} onClose={() => setCompleteDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Complete Work Order</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            multiline
            rows={4}
            label="Completion Notes"
            value={completionNotes}
            onChange={(e) => setCompletionNotes(e.target.value)}
            sx={{ mt: 1 }}
            placeholder="Describe the work completed..."
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCompleteDialog(false)}>Cancel</Button>
          <Button variant="contained" color="success" onClick={() => completeMutation.mutate()}>
            Complete Work Order
          </Button>
        </DialogActions>
      </Dialog>

      {/* Add Labor Entry Dialog */}
      <Dialog open={laborDialog} onClose={() => setLaborDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Labor Entry</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 0.5 }}>
            <Grid item xs={12}>
              <TextField
                select
                fullWidth
                label="Technician"
                value={laborUserId}
                onChange={(e) => setLaborUserId(Number(e.target.value))}
              >
                {usersData?.data?.map((u) => (
                  <MenuItem key={u.id} value={u.id}>
                    {u.fullName}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                type="date"
                label="Work Date"
                value={laborDate}
                onChange={(e) => setLaborDate(e.target.value)}
                InputLabelProps={{ shrink: true }}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                type="number"
                label="Hours Worked"
                value={laborHours}
                onChange={(e) => setLaborHours(e.target.value ? Number(e.target.value) : '')}
                inputProps={{ step: 0.25, min: 0 }}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                select
                fullWidth
                label="Labor Type"
                value={laborType}
                onChange={(e) => setLaborType(e.target.value)}
              >
                {LaborTypes.map((type) => (
                  <MenuItem key={type} value={type}>
                    {type}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                type="number"
                label="Hourly Rate (optional)"
                value={laborRate}
                onChange={(e) => setLaborRate(e.target.value ? Number(e.target.value) : '')}
                inputProps={{ step: 0.01, min: 0 }}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                multiline
                rows={2}
                label="Notes (optional)"
                value={laborNotes}
                onChange={(e) => setLaborNotes(e.target.value)}
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setLaborDialog(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => addLaborMutation.mutate()}
            disabled={!laborUserId || !laborHours || addLaborMutation.isPending}
          >
            Add Labor
          </Button>
        </DialogActions>
      </Dialog>

      {/* Add Part Usage Dialog */}
      <Dialog open={partsDialog} onClose={() => setPartsDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Part Usage</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 0.5 }}>
            <Grid item xs={12}>
              <TextField
                select
                fullWidth
                label="Part"
                value={selectedPartId}
                onChange={(e) => setSelectedPartId(Number(e.target.value))}
              >
                {availablePartsData?.items?.map((part) => (
                  <MenuItem key={part.id} value={part.id}>
                    {part.partNumber} - {part.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                type="number"
                label="Quantity"
                value={partQuantity}
                onChange={(e) => setPartQuantity(e.target.value ? Number(e.target.value) : '')}
                inputProps={{ min: 1 }}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                select
                fullWidth
                label="Pull From Location"
                value={partLocationId}
                onChange={(e) => setPartLocationId(Number(e.target.value))}
              >
                {storageLocationsData?.data?.map((loc: any) => (
                  <MenuItem key={loc.id} value={loc.id}>
                    {loc.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                multiline
                rows={2}
                label="Notes (optional)"
                value={partNotes}
                onChange={(e) => setPartNotes(e.target.value)}
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPartsDialog(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => addPartMutation.mutate()}
            disabled={!selectedPartId || !partQuantity || !partLocationId || addPartMutation.isPending}
          >
            Add Part
          </Button>
        </DialogActions>
      </Dialog>

      {/* Add Session Note Dialog */}
      <Dialog open={sessionNoteDialog} onClose={() => setSessionNoteDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Session Note</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            multiline
            rows={3}
            label="Note"
            value={sessionNote}
            onChange={(e) => setSessionNote(e.target.value)}
            sx={{ mt: 1 }}
            placeholder="What are you working on? Any issues or observations?"
            autoFocus
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSessionNoteDialog(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => addSessionNoteMutation.mutate(sessionNote)}
            disabled={!sessionNote.trim() || addSessionNoteMutation.isPending}
          >
            Add Note
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
