import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  Alert,
  CircularProgress,
  Card,
  CardContent,
  CardActions,
  Grid,
  Divider,
  Chip,
  IconButton,
  InputAdornment,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material';
import {
  Visibility,
  VisibilityOff,
  CheckCircle,
  Error as ErrorIcon,
  Send as SendIcon,
  CalendarMonth as CalendarIcon,
  Refresh as RefreshIcon,
  Chat as TeamsIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { integrationAdminService } from '../../services/integrationAdminService';
import { MicrosoftGraphSettings } from '../../types';

export const IntegrationSettingsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [showSecret, setShowSecret] = useState(false);
  const [testEmailDialogOpen, setTestEmailDialogOpen] = useState(false);
  const [testEmail, setTestEmail] = useState('');
  const [formData, setFormData] = useState<MicrosoftGraphSettings>({
    tenantId: '',
    clientId: '',
    clientSecret: '',
    sharedMailbox: '',
    sharedCalendarId: '',
    teamsWebhookUrl: '',
  });
  const [testTeamsResult, setTestTeamsResult] = useState<{ success: boolean; message: string } | null>(null);

  // Fetch integrations status
  const { data: integrations, isLoading: integrationsLoading } = useQuery({
    queryKey: ['integrations'],
    queryFn: async () => {
      const response = await integrationAdminService.getIntegrations();
      return response.data;
    },
  });

  // Fetch Microsoft Graph settings
  const { data: graphSettings, isLoading: settingsLoading } = useQuery({
    queryKey: ['microsoftGraphSettings'],
    queryFn: async () => {
      const response = await integrationAdminService.getMicrosoftGraphSettings();
      if (response.data) {
        setFormData(response.data);
      }
      return response.data;
    },
  });

  // Save settings mutation
  const saveMutation = useMutation({
    mutationFn: () => integrationAdminService.updateMicrosoftGraphSettings(formData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['integrations'] });
      queryClient.invalidateQueries({ queryKey: ['microsoftGraphSettings'] });
    },
  });

  // Validate mutation
  const validateMutation = useMutation({
    mutationFn: () => integrationAdminService.validateMicrosoftGraphSettings(),
  });

  // Test email mutation
  const testEmailMutation = useMutation({
    mutationFn: (toEmail: string) => integrationAdminService.sendTestEmail({ toEmail }),
    onSuccess: () => {
      setTestEmailDialogOpen(false);
      setTestEmail('');
    },
  });

  // Test calendar mutation
  const testCalendarMutation = useMutation({
    mutationFn: () => integrationAdminService.createTestCalendarEvent({ title: 'CMMS Test Event' }),
  });

  // Test Teams mutation
  const testTeamsMutation = useMutation({
    mutationFn: () => integrationAdminService.sendTestTeamsNotification(),
    onSuccess: (response) => {
      setTestTeamsResult({ success: response.success, message: response.message || 'Test notification sent' });
    },
    onError: () => {
      setTestTeamsResult({ success: false, message: 'Failed to send test notification' });
    },
  });

  const handleChange = (field: keyof MicrosoftGraphSettings) => (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    setFormData((prev) => ({ ...prev, [field]: e.target.value }));
  };

  const handleSave = () => {
    saveMutation.mutate();
  };

  const handleValidate = () => {
    validateMutation.mutate();
  };

  const handleSendTestEmail = () => {
    if (testEmail) {
      testEmailMutation.mutate(testEmail);
    }
  };

  const handleCreateTestEvent = () => {
    testCalendarMutation.mutate();
  };

  const microsoftGraphIntegration = integrations?.find((i) => i.providerType === 'MicrosoftGraph');

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Integration Settings
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Configure email and calendar integrations for notifications.
      </Typography>

      <Grid container spacing={3}>
        {/* Status Card */}
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Microsoft Graph
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                {integrationsLoading ? (
                  <CircularProgress size={20} />
                ) : microsoftGraphIntegration?.isConfigured ? (
                  <>
                    {microsoftGraphIntegration.isValid ? (
                      <Chip
                        icon={<CheckCircle />}
                        label="Connected"
                        color="success"
                        size="small"
                      />
                    ) : (
                      <Chip
                        icon={<ErrorIcon />}
                        label="Configuration Error"
                        color="error"
                        size="small"
                      />
                    )}
                  </>
                ) : (
                  <Chip label="Not Configured" color="default" size="small" />
                )}
              </Box>
              <Typography variant="body2" color="text.secondary">
                Send emails and create calendar events via Microsoft 365.
              </Typography>
            </CardContent>
            <CardActions>
              <Button
                size="small"
                startIcon={<RefreshIcon />}
                onClick={handleValidate}
                disabled={validateMutation.isPending}
              >
                {validateMutation.isPending ? 'Validating...' : 'Validate'}
              </Button>
            </CardActions>
          </Card>

          {validateMutation.isSuccess && (
            <Alert severity={validateMutation.data?.data ? 'success' : 'error'} sx={{ mt: 2 }}>
              {validateMutation.data?.message}
            </Alert>
          )}
        </Grid>

        {/* Settings Form */}
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Microsoft Graph Configuration
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              Enter your Azure AD App Registration details. Required permissions: Mail.Send,
              Calendars.ReadWrite, User.Read.All
            </Typography>

            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Tenant ID"
                  value={formData.tenantId}
                  onChange={handleChange('tenantId')}
                  placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
                  helperText="Azure AD Directory (tenant) ID"
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Client ID"
                  value={formData.clientId}
                  onChange={handleChange('clientId')}
                  placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
                  helperText="Application (client) ID"
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Client Secret"
                  type={showSecret ? 'text' : 'password'}
                  value={formData.clientSecret}
                  onChange={handleChange('clientSecret')}
                  helperText="Leave unchanged if not updating"
                  InputProps={{
                    endAdornment: (
                      <InputAdornment position="end">
                        <IconButton onClick={() => setShowSecret(!showSecret)} edge="end">
                          {showSecret ? <VisibilityOff /> : <Visibility />}
                        </IconButton>
                      </InputAdornment>
                    ),
                  }}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Shared Mailbox"
                  value={formData.sharedMailbox}
                  onChange={handleChange('sharedMailbox')}
                  placeholder="cmms@yourdomain.com"
                  helperText="Email address to send from"
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Shared Calendar ID (Optional)"
                  value={formData.sharedCalendarId}
                  onChange={handleChange('sharedCalendarId')}
                  helperText="Leave empty to use default calendar"
                />
              </Grid>
              <Grid item xs={12}>
                <Divider sx={{ my: 1 }} />
                <Typography variant="subtitle1" sx={{ mb: 1, mt: 2 }}>
                  Microsoft Teams Integration
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                  Configure a Teams incoming webhook to send notifications to a channel.
                </Typography>
              </Grid>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Teams Webhook URL"
                  value={formData.teamsWebhookUrl || ''}
                  onChange={handleChange('teamsWebhookUrl')}
                  placeholder="https://outlook.office.com/webhook/..."
                  helperText="Create an incoming webhook in your Teams channel settings"
                />
              </Grid>
            </Grid>

            <Divider sx={{ my: 3 }} />

            <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
              <Button
                variant="contained"
                onClick={handleSave}
                disabled={saveMutation.isPending}
              >
                {saveMutation.isPending ? 'Saving...' : 'Save Settings'}
              </Button>
              <Button
                variant="outlined"
                startIcon={<SendIcon />}
                onClick={() => setTestEmailDialogOpen(true)}
              >
                Send Test Email
              </Button>
              <Button
                variant="outlined"
                startIcon={<CalendarIcon />}
                onClick={handleCreateTestEvent}
                disabled={testCalendarMutation.isPending}
              >
                {testCalendarMutation.isPending ? 'Creating...' : 'Create Test Event'}
              </Button>
              <Button
                variant="outlined"
                startIcon={<TeamsIcon />}
                onClick={() => testTeamsMutation.mutate()}
                disabled={testTeamsMutation.isPending || !formData.teamsWebhookUrl}
              >
                {testTeamsMutation.isPending ? 'Sending...' : 'Test Teams'}
              </Button>
            </Box>

            {saveMutation.isSuccess && (
              <Alert severity="success" sx={{ mt: 2 }}>
                Settings saved successfully
              </Alert>
            )}
            {saveMutation.isError && (
              <Alert severity="error" sx={{ mt: 2 }}>
                Failed to save settings
              </Alert>
            )}
            {testEmailMutation.isSuccess && (
              <Alert severity="success" sx={{ mt: 2 }}>
                Test email sent successfully
              </Alert>
            )}
            {testEmailMutation.isError && (
              <Alert severity="error" sx={{ mt: 2 }}>
                Failed to send test email
              </Alert>
            )}
            {testCalendarMutation.isSuccess && (
              <Alert severity="success" sx={{ mt: 2 }}>
                Test calendar event created successfully
              </Alert>
            )}
            {testCalendarMutation.isError && (
              <Alert severity="error" sx={{ mt: 2 }}>
                Failed to create test calendar event
              </Alert>
            )}
            {testTeamsResult && (
              <Alert severity={testTeamsResult.success ? 'success' : 'error'} sx={{ mt: 2 }} onClose={() => setTestTeamsResult(null)}>
                {testTeamsResult.message}
              </Alert>
            )}
          </Paper>
        </Grid>
      </Grid>

      {/* Test Email Dialog */}
      <Dialog open={testEmailDialogOpen} onClose={() => setTestEmailDialogOpen(false)}>
        <DialogTitle>Send Test Email</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Email Address"
            type="email"
            fullWidth
            value={testEmail}
            onChange={(e) => setTestEmail(e.target.value)}
            placeholder="test@example.com"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setTestEmailDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleSendTestEmail}
            disabled={!testEmail || testEmailMutation.isPending}
            variant="contained"
          >
            {testEmailMutation.isPending ? 'Sending...' : 'Send'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default IntegrationSettingsPage;
