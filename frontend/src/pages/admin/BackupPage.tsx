import React, { useState, useRef } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  Alert,
  CircularProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Divider,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  FormControlLabel,
  Checkbox,
  Card,
  CardContent,
  Grid,
  Chip,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
import {
  Download as DownloadIcon,
  Upload as UploadIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Storage as StorageIcon,
  TableChart as TableIcon,
} from '@mui/icons-material';
import { useQuery, useMutation } from '@tanstack/react-query';
import { backupService, BackupValidation, BackupImportResult } from '../../services/backupService';

export const BackupPage: React.FC = () => {
  const [exportError, setExportError] = useState<string | null>(null);
  const [exportSuccess, setExportSuccess] = useState(false);
  const [importDialogOpen, setImportDialogOpen] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [validationResult, setValidationResult] = useState<BackupValidation | null>(null);
  const [clearExisting, setClearExisting] = useState(false);
  const [importResult, setImportResult] = useState<BackupImportResult | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Fetch backup info
  const { data: backupInfo, isLoading: infoLoading } = useQuery({
    queryKey: ['backupInfo'],
    queryFn: async () => {
      const response = await backupService.getInfo();
      return response.data;
    },
  });

  // Export mutation
  const exportMutation = useMutation({
    mutationFn: () => backupService.export(),
    onSuccess: (blob) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `cmms-backup-${new Date().toISOString().slice(0, 19).replace(/[:-]/g, '')}.json`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      setExportSuccess(true);
      setExportError(null);
      setTimeout(() => setExportSuccess(false), 5000);
    },
    onError: (error: Error) => {
      setExportError(error.message);
      setExportSuccess(false);
    },
  });

  // Validate mutation
  const validateMutation = useMutation({
    mutationFn: (file: File) => backupService.validate(file),
    onSuccess: (response) => {
      if (response.data) {
        setValidationResult(response.data);
      }
    },
  });

  // Import mutation
  const importMutation = useMutation({
    mutationFn: ({ file, clearExisting }: { file: File; clearExisting: boolean }) =>
      backupService.import(file, clearExisting),
    onSuccess: (response) => {
      if (response.data) {
        setImportResult(response.data);
      }
    },
  });

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setSelectedFile(file);
      setValidationResult(null);
      setImportResult(null);
      validateMutation.mutate(file);
    }
  };

  const handleImport = () => {
    if (selectedFile) {
      importMutation.mutate({ file: selectedFile, clearExisting });
    }
  };

  const handleCloseDialog = () => {
    setImportDialogOpen(false);
    setSelectedFile(null);
    setValidationResult(null);
    setImportResult(null);
    setClearExisting(false);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <Box>
      <Typography variant="h4" sx={{ mb: 3 }}>
        Backup & Restore
      </Typography>

      {exportError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setExportError(null)}>
          {exportError}
        </Alert>
      )}

      {exportSuccess && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setExportSuccess(false)}>
          Backup downloaded successfully!
        </Alert>
      )}

      <Grid container spacing={3}>
        {/* Export Section */}
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3, height: '100%' }}>
            <Typography variant="h6" gutterBottom>
              <DownloadIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
              Export Backup
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              Download a complete backup of all data in a portable JSON format. This backup can be
              imported into any supported database (SQL Server, PostgreSQL, MySQL).
            </Typography>

            {infoLoading ? (
              <CircularProgress size={24} />
            ) : backupInfo ? (
              <Box sx={{ mb: 3 }}>
                <Card variant="outlined">
                  <CardContent>
                    <Grid container spacing={2}>
                      <Grid item xs={6}>
                        <Typography variant="body2" color="text.secondary">
                          Tables
                        </Typography>
                        <Typography variant="h6">{backupInfo.tables.length}</Typography>
                      </Grid>
                      <Grid item xs={6}>
                        <Typography variant="body2" color="text.secondary">
                          Total Records
                        </Typography>
                        <Typography variant="h6">{backupInfo.totalRecords.toLocaleString()}</Typography>
                      </Grid>
                      <Grid item xs={12}>
                        <Typography variant="body2" color="text.secondary">
                          Estimated Size
                        </Typography>
                        <Typography variant="h6">{formatBytes(backupInfo.estimatedSizeBytes)}</Typography>
                      </Grid>
                    </Grid>
                  </CardContent>
                </Card>
              </Box>
            ) : null}

            <Button
              variant="contained"
              startIcon={exportMutation.isPending ? <CircularProgress size={20} color="inherit" /> : <DownloadIcon />}
              onClick={() => exportMutation.mutate()}
              disabled={exportMutation.isPending}
              size="large"
              fullWidth
            >
              {exportMutation.isPending ? 'Creating Backup...' : 'Download Backup'}
            </Button>
          </Paper>
        </Grid>

        {/* Import Section */}
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3, height: '100%' }}>
            <Typography variant="h6" gutterBottom>
              <UploadIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
              Import Backup
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              Restore data from a previously exported backup file. You can validate the backup before
              importing to ensure data integrity.
            </Typography>

            <Button
              variant="outlined"
              startIcon={<UploadIcon />}
              onClick={() => setImportDialogOpen(true)}
              size="large"
              fullWidth
            >
              Import from Backup
            </Button>
          </Paper>
        </Grid>

        {/* Data Overview */}
        {backupInfo && (
          <Grid item xs={12}>
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>
                <StorageIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                Data Overview
              </Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Table</TableCell>
                      <TableCell align="right">Records</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {Object.entries(backupInfo.recordCounts)
                      .filter(([_, count]) => count > 0)
                      .sort((a, b) => b[1] - a[1])
                      .map(([table, count]) => (
                        <TableRow key={table}>
                          <TableCell>
                            <TableIcon sx={{ mr: 1, verticalAlign: 'middle', fontSize: 16 }} />
                            {table}
                          </TableCell>
                          <TableCell align="right">{count.toLocaleString()}</TableCell>
                        </TableRow>
                      ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Paper>
          </Grid>
        )}
      </Grid>

      {/* Import Dialog */}
      <Dialog open={importDialogOpen} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
        <DialogTitle>Import Backup</DialogTitle>
        <DialogContent>
          {!importResult ? (
            <>
              <DialogContentText sx={{ mb: 2 }}>
                Select a backup file to import. The file will be validated before import.
              </DialogContentText>

              <input
                type="file"
                accept=".json"
                onChange={handleFileSelect}
                ref={fileInputRef}
                style={{ display: 'none' }}
                id="backup-file-input"
              />
              <label htmlFor="backup-file-input">
                <Button variant="outlined" component="span" fullWidth>
                  {selectedFile ? selectedFile.name : 'Select Backup File'}
                </Button>
              </label>

              {validateMutation.isPending && (
                <Box sx={{ display: 'flex', alignItems: 'center', mt: 2 }}>
                  <CircularProgress size={20} sx={{ mr: 2 }} />
                  <Typography>Validating backup...</Typography>
                </Box>
              )}

              {validationResult && (
                <Box sx={{ mt: 2 }}>
                  <Alert severity={validationResult.isValid ? 'success' : 'error'} sx={{ mb: 2 }}>
                    {validationResult.isValid ? 'Backup file is valid' : 'Backup file has errors'}
                  </Alert>

                  {validationResult.version && (
                    <Typography variant="body2">
                      <strong>Version:</strong> {validationResult.version}
                    </Typography>
                  )}
                  {validationResult.exportedAt && (
                    <Typography variant="body2">
                      <strong>Exported:</strong> {new Date(validationResult.exportedAt).toLocaleString()}
                    </Typography>
                  )}
                  <Typography variant="body2">
                    <strong>Tables:</strong> {validationResult.tables.length}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Records:</strong> {validationResult.totalRecords.toLocaleString()}
                  </Typography>

                  {validationResult.errors.length > 0 && (
                    <List dense>
                      {validationResult.errors.map((error, i) => (
                        <ListItem key={i}>
                          <ListItemIcon>
                            <ErrorIcon color="error" />
                          </ListItemIcon>
                          <ListItemText primary={error} />
                        </ListItem>
                      ))}
                    </List>
                  )}

                  {validationResult.warnings.length > 0 && (
                    <List dense>
                      {validationResult.warnings.map((warning, i) => (
                        <ListItem key={i}>
                          <ListItemIcon>
                            <WarningIcon color="warning" />
                          </ListItemIcon>
                          <ListItemText primary={warning} />
                        </ListItem>
                      ))}
                    </List>
                  )}

                  {validationResult.isValid && (
                    <>
                      <Divider sx={{ my: 2 }} />
                      <FormControlLabel
                        control={
                          <Checkbox
                            checked={clearExisting}
                            onChange={(e) => setClearExisting(e.target.checked)}
                            color="warning"
                          />
                        }
                        label="Clear existing data before import (dangerous!)"
                      />
                      {clearExisting && (
                        <Alert severity="warning" sx={{ mt: 1 }}>
                          This will delete all existing data before importing. This action cannot be
                          undone!
                        </Alert>
                      )}
                    </>
                  )}
                </Box>
              )}
            </>
          ) : (
            <Box>
              <Alert severity={importResult.success ? 'success' : 'error'} sx={{ mb: 2 }}>
                {importResult.success ? 'Import completed successfully!' : 'Import failed'}
              </Alert>

              {importResult.success && (
                <>
                  <Typography variant="body2">
                    <strong>Tables Imported:</strong> {importResult.tablesImported}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Records Imported:</strong> {importResult.recordsImported.toLocaleString()}
                  </Typography>
                </>
              )}

              {importResult.errors.length > 0 && (
                <List dense>
                  {importResult.errors.map((error, i) => (
                    <ListItem key={i}>
                      <ListItemIcon>
                        <ErrorIcon color="error" />
                      </ListItemIcon>
                      <ListItemText primary={error} />
                    </ListItem>
                  ))}
                </List>
              )}

              {importResult.warnings.length > 0 && (
                <List dense>
                  {importResult.warnings.map((warning, i) => (
                    <ListItem key={i}>
                      <ListItemIcon>
                        <WarningIcon color="warning" />
                      </ListItemIcon>
                      <ListItemText primary={warning} />
                    </ListItem>
                  ))}
                </List>
              )}
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>
            {importResult ? 'Close' : 'Cancel'}
          </Button>
          {!importResult && validationResult?.isValid && (
            <Button
              onClick={handleImport}
              variant="contained"
              color={clearExisting ? 'warning' : 'primary'}
              disabled={importMutation.isPending}
              startIcon={importMutation.isPending ? <CircularProgress size={20} color="inherit" /> : <UploadIcon />}
            >
              {importMutation.isPending ? 'Importing...' : 'Import'}
            </Button>
          )}
        </DialogActions>
      </Dialog>
    </Box>
  );
};
