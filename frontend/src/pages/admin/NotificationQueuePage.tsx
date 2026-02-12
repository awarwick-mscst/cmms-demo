import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Chip,
  IconButton,
  Tooltip,
  Alert,
  Card,
  CardContent,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Tabs,
  Tab,
  Skeleton,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  Delete as DeleteIcon,
  CheckCircle,
  Error as ErrorIcon,
  Schedule as ScheduleIcon,
  Pending as PendingIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { integrationAdminService } from '../../services/integrationAdminService';
import { NotificationQueueItem, NotificationLogItem, NotificationTypes, NotificationStatuses } from '../../types';
import { format } from 'date-fns';

export const NotificationQueuePage: React.FC = () => {
  const queryClient = useQueryClient();
  const [tabValue, setTabValue] = useState(0);
  const [queuePage, setQueuePage] = useState(0);
  const [queueRowsPerPage, setQueueRowsPerPage] = useState(20);
  const [logPage, setLogPage] = useState(0);
  const [logRowsPerPage, setLogRowsPerPage] = useState(20);
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [typeFilter, setTypeFilter] = useState<string>('');

  // Fetch stats
  const { data: stats, isLoading: statsLoading } = useQuery({
    queryKey: ['notificationStats'],
    queryFn: async () => {
      const response = await integrationAdminService.getNotificationStats();
      return response.data;
    },
    refetchInterval: 30000,
  });

  // Fetch queue
  const { data: queueData, isLoading: queueLoading } = useQuery({
    queryKey: ['notificationQueue', queuePage, queueRowsPerPage, statusFilter, typeFilter],
    queryFn: async () => {
      const response = await integrationAdminService.getNotificationQueue({
        page: queuePage + 1,
        pageSize: queueRowsPerPage,
        status: statusFilter || undefined,
        type: typeFilter || undefined,
      });
      return response.data;
    },
    enabled: tabValue === 0,
  });

  // Fetch logs
  const { data: logData, isLoading: logLoading } = useQuery({
    queryKey: ['notificationLogs', logPage, logRowsPerPage],
    queryFn: async () => {
      const response = await integrationAdminService.getNotificationLogs({
        page: logPage + 1,
        pageSize: logRowsPerPage,
      });
      return response.data;
    },
    enabled: tabValue === 1,
  });

  // Retry mutation
  const retryMutation = useMutation({
    mutationFn: (id: number) => integrationAdminService.retryNotification(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationQueue'] });
      queryClient.invalidateQueries({ queryKey: ['notificationStats'] });
    },
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: (id: number) => integrationAdminService.deleteNotification(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationQueue'] });
      queryClient.invalidateQueries({ queryKey: ['notificationStats'] });
    },
  });

  const getStatusChip = (status: string) => {
    switch (status) {
      case 'Pending':
        return <Chip icon={<PendingIcon />} label="Pending" color="warning" size="small" />;
      case 'Processing':
        return <Chip icon={<ScheduleIcon />} label="Processing" color="info" size="small" />;
      case 'Sent':
        return <Chip icon={<CheckCircle />} label="Sent" color="success" size="small" />;
      case 'Failed':
        return <Chip icon={<ErrorIcon />} label="Failed" color="error" size="small" />;
      default:
        return <Chip label={status} size="small" />;
    }
  };

  const formatDate = (dateString: string) => {
    return format(new Date(dateString), 'MMM d, yyyy h:mm a');
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Notification Queue
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Monitor and manage notification delivery.
      </Typography>

      {/* Stats Cards */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={6} sm={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <Typography variant="h4" color="warning.main">
                {statsLoading ? <Skeleton width={40} sx={{ mx: 'auto' }} /> : stats?.pendingCount || 0}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Pending
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} sm={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <Typography variant="h4" color="info.main">
                {statsLoading ? <Skeleton width={40} sx={{ mx: 'auto' }} /> : stats?.processingCount || 0}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Processing
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} sm={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <Typography variant="h4" color="success.main">
                {statsLoading ? <Skeleton width={40} sx={{ mx: 'auto' }} /> : stats?.sentToday || 0}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Sent Today
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} sm={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 2 }}>
              <Typography variant="h4" color="error.main">
                {statsLoading ? <Skeleton width={40} sx={{ mx: 'auto' }} /> : stats?.failedToday || 0}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Failed Today
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Tabs */}
      <Paper>
        <Tabs value={tabValue} onChange={(_, v) => setTabValue(v)} sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tab label="Queue" />
          <Tab label="Sent Log" />
        </Tabs>

        {/* Queue Tab */}
        {tabValue === 0 && (
          <>
            <Box sx={{ p: 2, display: 'flex', gap: 2 }}>
              <FormControl size="small" sx={{ minWidth: 150 }}>
                <InputLabel>Status</InputLabel>
                <Select
                  value={statusFilter}
                  label="Status"
                  onChange={(e) => setStatusFilter(e.target.value)}
                >
                  <MenuItem value="">All</MenuItem>
                  {NotificationStatuses.map((status) => (
                    <MenuItem key={status} value={status}>{status}</MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl size="small" sx={{ minWidth: 200 }}>
                <InputLabel>Type</InputLabel>
                <Select
                  value={typeFilter}
                  label="Type"
                  onChange={(e) => setTypeFilter(e.target.value)}
                >
                  <MenuItem value="">All</MenuItem>
                  {NotificationTypes.map((type) => (
                    <MenuItem key={type} value={type}>{type}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Box>

            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Type</TableCell>
                    <TableCell>Recipient</TableCell>
                    <TableCell>Subject</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Scheduled</TableCell>
                    <TableCell>Retries</TableCell>
                    <TableCell align="right">Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {queueLoading ? (
                    Array.from({ length: 5 }).map((_, i) => (
                      <TableRow key={i}>
                        <TableCell colSpan={7}><Skeleton /></TableCell>
                      </TableRow>
                    ))
                  ) : queueData?.items?.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={7} align="center">
                        <Typography color="text.secondary">No notifications in queue</Typography>
                      </TableCell>
                    </TableRow>
                  ) : (
                    queueData?.items?.map((item: NotificationQueueItem) => (
                      <TableRow key={item.id} hover>
                        <TableCell>
                          <Typography variant="body2">{item.type}</Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{item.recipientEmail}</Typography>
                          {item.recipientName && (
                            <Typography variant="caption" color="text.secondary">
                              {item.recipientName}
                            </Typography>
                          )}
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" noWrap sx={{ maxWidth: 300 }}>
                            {item.subject}
                          </Typography>
                        </TableCell>
                        <TableCell>{getStatusChip(item.status)}</TableCell>
                        <TableCell>
                          <Typography variant="body2">{formatDate(item.scheduledFor)}</Typography>
                        </TableCell>
                        <TableCell>{item.retryCount}</TableCell>
                        <TableCell align="right">
                          {item.status === 'Failed' && (
                            <Tooltip title="Retry">
                              <IconButton
                                size="small"
                                onClick={() => retryMutation.mutate(item.id)}
                                disabled={retryMutation.isPending}
                              >
                                <RefreshIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          )}
                          <Tooltip title="Delete">
                            <IconButton
                              size="small"
                              onClick={() => deleteMutation.mutate(item.id)}
                              disabled={deleteMutation.isPending}
                            >
                              <DeleteIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
            <TablePagination
              component="div"
              count={queueData?.totalCount || 0}
              page={queuePage}
              onPageChange={(_, p) => setQueuePage(p)}
              rowsPerPage={queueRowsPerPage}
              onRowsPerPageChange={(e) => {
                setQueueRowsPerPage(parseInt(e.target.value, 10));
                setQueuePage(0);
              }}
            />
          </>
        )}

        {/* Log Tab */}
        {tabValue === 1 && (
          <>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Type</TableCell>
                    <TableCell>Recipient</TableCell>
                    <TableCell>Subject</TableCell>
                    <TableCell>Channel</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Sent At</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {logLoading ? (
                    Array.from({ length: 5 }).map((_, i) => (
                      <TableRow key={i}>
                        <TableCell colSpan={6}><Skeleton /></TableCell>
                      </TableRow>
                    ))
                  ) : logData?.items?.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} align="center">
                        <Typography color="text.secondary">No notification logs</Typography>
                      </TableCell>
                    </TableRow>
                  ) : (
                    logData?.items?.map((item: NotificationLogItem) => (
                      <TableRow key={item.id} hover>
                        <TableCell>
                          <Typography variant="body2">{item.type}</Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{item.recipientEmail}</Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" noWrap sx={{ maxWidth: 300 }}>
                            {item.subject}
                          </Typography>
                        </TableCell>
                        <TableCell>{item.channel}</TableCell>
                        <TableCell>
                          {item.success ? (
                            <Chip icon={<CheckCircle />} label="Success" color="success" size="small" />
                          ) : (
                            <Tooltip title={item.errorMessage || 'Unknown error'}>
                              <Chip icon={<ErrorIcon />} label="Failed" color="error" size="small" />
                            </Tooltip>
                          )}
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{formatDate(item.sentAt)}</Typography>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
            <TablePagination
              component="div"
              count={logData?.totalCount || 0}
              page={logPage}
              onPageChange={(_, p) => setLogPage(p)}
              rowsPerPage={logRowsPerPage}
              onRowsPerPageChange={(e) => {
                setLogRowsPerPage(parseInt(e.target.value, 10));
                setLogPage(0);
              }}
            />
          </>
        )}
      </Paper>
    </Box>
  );
};

export default NotificationQueuePage;
