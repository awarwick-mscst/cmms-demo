import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  Alert,
  CircularProgress,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  Stepper,
  Step,
  StepLabel,
  Card,
  CardContent,
  FormControlLabel,
  Radio,
  RadioGroup,
  InputAdornment,
  IconButton,
  Chip,
  Divider,
} from '@mui/material';
import {
  Storage as StorageIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Visibility,
  VisibilityOff,
  Speed as SpeedIcon,
  CloudQueue as CloudIcon,
  Dns as DnsIcon,
  FolderOpen as FolderIcon,
} from '@mui/icons-material';
import { useQuery, useMutation } from '@tanstack/react-query';
import {
  databaseConfigService,
  DatabaseSettings,
  DatabaseProviderInfo,
  DatabaseTestResult,
} from '../../services/databaseConfigService';

const steps = ['Select Provider', 'Connection Details', 'Test & Save'];

const tierDescriptions: Record<string, { title: string; description: string; icon: React.ReactNode }> = {
  Tiny: {
    title: 'Tiny',
    description: 'Single workstation with SQLite. Best for very small businesses or personal use.',
    icon: <FolderIcon sx={{ fontSize: 40 }} />,
  },
  Small: {
    title: 'Small Business',
    description: 'Single server with SQL Server Express. Suitable for small teams.',
    icon: <DnsIcon sx={{ fontSize: 40 }} />,
  },
  Enterprise: {
    title: 'Enterprise',
    description: 'Separate database and application servers. SQL Server Standard/Enterprise.',
    icon: <StorageIcon sx={{ fontSize: 40 }} />,
  },
  Cloud: {
    title: 'Cloud SaaS',
    description: 'Azure hosted with Azure SQL. Supports Entra ID authentication.',
    icon: <CloudIcon sx={{ fontSize: 40 }} />,
  },
};

export const DatabaseConfigPage: React.FC = () => {
  const [activeStep, setActiveStep] = useState(0);
  const [showPassword, setShowPassword] = useState(false);
  const [testResult, setTestResult] = useState<DatabaseTestResult | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  const [settings, setSettings] = useState<DatabaseSettings>({
    provider: 'SqlServer',
    server: 'localhost',
    port: undefined,
    database: 'CMMS',
    authType: 'Windows',
    username: '',
    password: '',
    additionalOptions: '',
    filePath: '',
    isConfigured: false,
    tier: 'Small',
  });

  // Fetch providers
  const { data: providers, isLoading: providersLoading } = useQuery({
    queryKey: ['databaseProviders'],
    queryFn: () => databaseConfigService.getProviders(),
  });

  // Fetch current settings
  const { data: currentSettings, isLoading: settingsLoading } = useQuery({
    queryKey: ['databaseSettings'],
    queryFn: () => databaseConfigService.getSettings(),
    retry: false,
  });

  // Load current settings if they exist
  useEffect(() => {
    if (currentSettings?.isConfigured) {
      setSettings(currentSettings);
    }
  }, [currentSettings]);

  // Test connection mutation
  const testMutation = useMutation({
    mutationFn: () => databaseConfigService.testConnection({
      provider: settings.provider,
      server: settings.server,
      port: settings.port,
      database: settings.database,
      authType: settings.authType,
      username: settings.username,
      password: settings.password,
      additionalOptions: settings.additionalOptions,
      filePath: settings.filePath,
    }),
    onSuccess: (result) => {
      setTestResult(result);
    },
  });

  // Save settings mutation
  const saveMutation = useMutation({
    mutationFn: () => databaseConfigService.saveSettings(settings),
    onSuccess: () => {
      setSaveSuccess(true);
      setSaveError(null);
    },
    onError: (error: Error) => {
      setSaveError(error.message);
      setSaveSuccess(false);
    },
  });

  const handleNext = () => {
    setActiveStep((prev) => prev + 1);
    setTestResult(null);
  };

  const handleBack = () => {
    setActiveStep((prev) => prev - 1);
    setTestResult(null);
  };

  const handleProviderChange = (provider: string) => {
    const providerInfo = providers?.find((p) => p.name === provider);
    setSettings((prev) => ({
      ...prev,
      provider,
      port: providerInfo?.defaultPort || undefined,
      authType: providerInfo?.supportsWindowsAuth ? 'Windows' : 'SqlAuth',
      filePath: provider === 'Sqlite' ? 'CMMS.db' : '',
    }));
  };

  const handleTierChange = (tier: string) => {
    let provider = settings.provider;
    if (tier === 'Tiny') {
      provider = 'Sqlite';
    } else if (tier !== 'Tiny' && settings.provider === 'Sqlite') {
      provider = 'SqlServer';
    }
    setSettings((prev) => ({
      ...prev,
      tier,
      provider,
    }));
  };

  const selectedProvider = providers?.find((p) => p.name === settings.provider);

  const renderTierSelection = () => (
    <Box>
      <Typography variant="h6" gutterBottom>
        Select Deployment Tier
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Choose the deployment model that best fits your organization.
      </Typography>

      <Grid container spacing={2}>
        {Object.entries(tierDescriptions).map(([key, tier]) => (
          <Grid item xs={12} sm={6} key={key}>
            <Card
              variant={settings.tier === key ? 'elevation' : 'outlined'}
              sx={{
                cursor: 'pointer',
                border: settings.tier === key ? 2 : 1,
                borderColor: settings.tier === key ? 'primary.main' : 'divider',
                '&:hover': { borderColor: 'primary.main' },
              }}
              onClick={() => handleTierChange(key)}
            >
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                  <Box sx={{ color: settings.tier === key ? 'primary.main' : 'text.secondary', mr: 2 }}>
                    {tier.icon}
                  </Box>
                  <Box>
                    <Typography variant="h6">{tier.title}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {tier.description}
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Divider sx={{ my: 3 }} />

      <Typography variant="h6" gutterBottom>
        Database Provider
      </Typography>

      {providersLoading ? (
        <CircularProgress />
      ) : (
        <Grid container spacing={2}>
          {providers?.map((provider) => (
            <Grid item xs={12} sm={6} md={3} key={provider.name}>
              <Card
                variant={settings.provider === provider.name ? 'elevation' : 'outlined'}
                sx={{
                  cursor: provider.isSupported ? 'pointer' : 'not-allowed',
                  opacity: provider.isSupported ? 1 : 0.5,
                  border: settings.provider === provider.name ? 2 : 1,
                  borderColor: settings.provider === provider.name ? 'primary.main' : 'divider',
                  '&:hover': provider.isSupported ? { borderColor: 'primary.main' } : {},
                }}
                onClick={() => provider.isSupported && handleProviderChange(provider.name)}
              >
                <CardContent sx={{ textAlign: 'center' }}>
                  <StorageIcon sx={{ fontSize: 40, mb: 1 }} />
                  <Typography variant="subtitle1">{provider.displayName}</Typography>
                  {!provider.isSupported && (
                    <Chip
                      label="Coming Soon"
                      size="small"
                      color="warning"
                      sx={{ mt: 1 }}
                    />
                  )}
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
    </Box>
  );

  const renderConnectionDetails = () => (
    <Box>
      <Typography variant="h6" gutterBottom>
        Connection Details
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Enter the connection details for your {selectedProvider?.displayName} database.
      </Typography>

      <Grid container spacing={3}>
        {settings.provider !== 'Sqlite' ? (
          <>
            <Grid item xs={12} md={8}>
              <TextField
                fullWidth
                label="Server"
                value={settings.server}
                onChange={(e) => setSettings((prev) => ({ ...prev, server: e.target.value }))}
                placeholder="localhost or server.domain.com"
                helperText="Hostname, IP address, or named instance (e.g., SERVER\SQLEXPRESS)"
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                label="Port"
                type="number"
                value={settings.port || ''}
                onChange={(e) => setSettings((prev) => ({ ...prev, port: parseInt(e.target.value) || undefined }))}
                placeholder={selectedProvider?.defaultPort.toString()}
                helperText={`Default: ${selectedProvider?.defaultPort}`}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Database Name"
                value={settings.database}
                onChange={(e) => setSettings((prev) => ({ ...prev, database: e.target.value }))}
              />
            </Grid>

            {selectedProvider?.supportsWindowsAuth && (
              <Grid item xs={12}>
                <FormControl component="fieldset">
                  <Typography variant="subtitle2" gutterBottom>
                    Authentication Type
                  </Typography>
                  <RadioGroup
                    row
                    value={settings.authType}
                    onChange={(e) => setSettings((prev) => ({ ...prev, authType: e.target.value }))}
                  >
                    <FormControlLabel
                      value="Windows"
                      control={<Radio />}
                      label="Windows Authentication"
                    />
                    <FormControlLabel
                      value="SqlAuth"
                      control={<Radio />}
                      label="SQL Server Authentication"
                    />
                  </RadioGroup>
                </FormControl>
              </Grid>
            )}

            {(settings.authType === 'SqlAuth' || !selectedProvider?.supportsWindowsAuth) && (
              <>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Username"
                    value={settings.username || ''}
                    onChange={(e) => setSettings((prev) => ({ ...prev, username: e.target.value }))}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Password"
                    type={showPassword ? 'text' : 'password'}
                    value={settings.password || ''}
                    onChange={(e) => setSettings((prev) => ({ ...prev, password: e.target.value }))}
                    InputProps={{
                      endAdornment: (
                        <InputAdornment position="end">
                          <IconButton
                            onClick={() => setShowPassword(!showPassword)}
                            edge="end"
                          >
                            {showPassword ? <VisibilityOff /> : <Visibility />}
                          </IconButton>
                        </InputAdornment>
                      ),
                    }}
                  />
                </Grid>
              </>
            )}

            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Additional Options"
                value={settings.additionalOptions || ''}
                onChange={(e) => setSettings((prev) => ({ ...prev, additionalOptions: e.target.value }))}
                placeholder="TrustServerCertificate=true;Encrypt=false"
                helperText="Additional connection string parameters (optional)"
              />
            </Grid>
          </>
        ) : (
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Database File Path"
              value={settings.filePath || ''}
              onChange={(e) => setSettings((prev) => ({ ...prev, filePath: e.target.value }))}
              placeholder="CMMS.db"
              helperText="Path to the SQLite database file. Relative paths are from the application directory."
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <FolderIcon />
                  </InputAdornment>
                ),
              }}
            />
          </Grid>
        )}
      </Grid>
    </Box>
  );

  const renderTestAndSave = () => (
    <Box>
      <Typography variant="h6" gutterBottom>
        Test Connection
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Test your connection settings before saving.
      </Typography>

      {/* Connection Summary */}
      <Card variant="outlined" sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="subtitle2" gutterBottom>
            Connection Summary
          </Typography>
          <Grid container spacing={1}>
            <Grid item xs={4}>
              <Typography variant="body2" color="text.secondary">Provider</Typography>
            </Grid>
            <Grid item xs={8}>
              <Typography variant="body2">{selectedProvider?.displayName}</Typography>
            </Grid>
            {settings.provider !== 'Sqlite' ? (
              <>
                <Grid item xs={4}>
                  <Typography variant="body2" color="text.secondary">Server</Typography>
                </Grid>
                <Grid item xs={8}>
                  <Typography variant="body2">
                    {settings.server}{settings.port ? `:${settings.port}` : ''}
                  </Typography>
                </Grid>
                <Grid item xs={4}>
                  <Typography variant="body2" color="text.secondary">Database</Typography>
                </Grid>
                <Grid item xs={8}>
                  <Typography variant="body2">{settings.database}</Typography>
                </Grid>
                <Grid item xs={4}>
                  <Typography variant="body2" color="text.secondary">Authentication</Typography>
                </Grid>
                <Grid item xs={8}>
                  <Typography variant="body2">
                    {settings.authType === 'Windows' ? 'Windows Authentication' : `SQL Auth (${settings.username})`}
                  </Typography>
                </Grid>
              </>
            ) : (
              <>
                <Grid item xs={4}>
                  <Typography variant="body2" color="text.secondary">File Path</Typography>
                </Grid>
                <Grid item xs={8}>
                  <Typography variant="body2">{settings.filePath}</Typography>
                </Grid>
              </>
            )}
          </Grid>
        </CardContent>
      </Card>

      {/* Test Button */}
      <Button
        variant="outlined"
        onClick={() => testMutation.mutate()}
        disabled={testMutation.isPending}
        startIcon={testMutation.isPending ? <CircularProgress size={20} /> : <SpeedIcon />}
        sx={{ mb: 2 }}
      >
        {testMutation.isPending ? 'Testing...' : 'Test Connection'}
      </Button>

      {/* Test Result */}
      {testResult && (
        <Alert
          severity={testResult.success ? 'success' : 'error'}
          sx={{ mb: 2 }}
          icon={testResult.success ? <CheckCircleIcon /> : <ErrorIcon />}
        >
          <Typography variant="subtitle2">{testResult.message}</Typography>
          {testResult.serverVersion && (
            <Typography variant="body2">Server: {testResult.serverVersion}</Typography>
          )}
          {testResult.latencyMs && (
            <Typography variant="body2">Latency: {testResult.latencyMs}ms</Typography>
          )}
          {testResult.errorDetails && (
            <Typography variant="body2" sx={{ mt: 1 }}>
              Error: {testResult.errorDetails}
            </Typography>
          )}
        </Alert>
      )}

      {/* Save Status */}
      {saveSuccess && (
        <Alert severity="success" sx={{ mb: 2 }}>
          Database configuration saved successfully! The application will use these settings on restart.
        </Alert>
      )}

      {saveError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {saveError}
        </Alert>
      )}
    </Box>
  );

  const getStepContent = (step: number) => {
    switch (step) {
      case 0:
        return renderTierSelection();
      case 1:
        return renderConnectionDetails();
      case 2:
        return renderTestAndSave();
      default:
        return 'Unknown step';
    }
  };

  if (settingsLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" sx={{ mb: 3 }}>
        Database Configuration
      </Typography>

      {currentSettings?.isConfigured && (
        <Alert severity="info" sx={{ mb: 3 }}>
          Database is currently configured. Changes will take effect after restarting the application.
        </Alert>
      )}

      <Paper sx={{ p: 3 }}>
        <Stepper activeStep={activeStep} sx={{ mb: 4 }}>
          {steps.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>

        {getStepContent(activeStep)}

        <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 4 }}>
          <Button
            disabled={activeStep === 0}
            onClick={handleBack}
          >
            Back
          </Button>
          <Box>
            {activeStep === steps.length - 1 ? (
              <Button
                variant="contained"
                onClick={() => saveMutation.mutate()}
                disabled={saveMutation.isPending || !testResult?.success}
                startIcon={saveMutation.isPending ? <CircularProgress size={20} color="inherit" /> : <CheckCircleIcon />}
              >
                {saveMutation.isPending ? 'Saving...' : 'Save Configuration'}
              </Button>
            ) : (
              <Button variant="contained" onClick={handleNext}>
                Next
              </Button>
            )}
          </Box>
        </Box>
      </Paper>
    </Box>
  );
};
