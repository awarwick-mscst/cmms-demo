import React, { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  Card,
  CardContent,
  CardActions,
  Grid,
  Alert,
  Divider,
  Chip,
  CircularProgress,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import InventoryIcon from '@mui/icons-material/Inventory';
import BuildIcon from '@mui/icons-material/Build';
import KeyboardIcon from '@mui/icons-material/Keyboard';
import { useMutation } from '@tanstack/react-query';
import { BarcodeScanner } from '../components/scanner';
import { useKeyboardScanner } from '../hooks/useKeyboardScanner';
import { barcodeService } from '../services/barcodeService';
import { BarcodeLookupResult } from '../types';

export const ScannerPage: React.FC = () => {
  const navigate = useNavigate();
  const [manualBarcode, setManualBarcode] = useState('');
  const [lastScanned, setLastScanned] = useState<string | null>(null);
  const [result, setResult] = useState<BarcodeLookupResult | null>(null);
  const [notFound, setNotFound] = useState(false);

  const lookupMutation = useMutation({
    mutationFn: (barcode: string) => barcodeService.lookup(barcode),
    onSuccess: (data) => {
      if (data.success && data.data) {
        setResult(data.data);
        setNotFound(false);
      } else {
        setResult(null);
        setNotFound(true);
      }
    },
    onError: () => {
      setResult(null);
      setNotFound(true);
    },
  });

  const handleScan = useCallback(
    (barcode: string) => {
      if (barcode && barcode !== lastScanned) {
        setLastScanned(barcode);
        setNotFound(false);
        setResult(null);
        lookupMutation.mutate(barcode);
      }
    },
    [lastScanned, lookupMutation]
  );

  // Enable USB barcode scanner detection
  useKeyboardScanner({
    onScan: handleScan,
    minLength: 4,
    maxDelayMs: 50,
    enabled: true,
  });

  const handleManualSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (manualBarcode.trim()) {
      handleScan(manualBarcode.trim());
    }
  };

  const handleViewDetails = () => {
    if (result) {
      if (result.type === 'Part') {
        navigate(`/inventory/parts/${result.id}`);
      } else {
        navigate(`/assets/${result.id}`);
      }
    }
  };

  const handleClear = () => {
    setResult(null);
    setNotFound(false);
    setLastScanned(null);
    setManualBarcode('');
  };

  return (
    <Box sx={{ maxWidth: 800, mx: 'auto' }}>
      <Typography variant="h4" sx={{ mb: 3 }}>
        Barcode Scanner
      </Typography>

      <Grid container spacing={3}>
        {/* Camera Scanner Section */}
        <Grid item xs={12}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Camera Scanner
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Point your camera at a barcode to scan
            </Typography>
            <BarcodeScanner onScan={handleScan} />
          </Paper>
        </Grid>

        {/* Manual Entry Section */}
        <Grid item xs={12}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Manual Entry
            </Typography>
            <Box component="form" onSubmit={handleManualSearch} sx={{ display: 'flex', gap: 2 }}>
              <TextField
                fullWidth
                label="Enter barcode"
                value={manualBarcode}
                onChange={(e) => setManualBarcode(e.target.value)}
                placeholder="Type or scan barcode..."
                autoComplete="off"
              />
              <Button
                type="submit"
                variant="contained"
                startIcon={<SearchIcon />}
                disabled={!manualBarcode.trim() || lookupMutation.isPending}
              >
                Search
              </Button>
            </Box>
          </Paper>
        </Grid>

        {/* USB Scanner Hint */}
        <Grid item xs={12}>
          <Alert icon={<KeyboardIcon />} severity="info">
            USB barcode scanners are automatically detected. Just scan a barcode and the result will appear below.
          </Alert>
        </Grid>

        {/* Results Section */}
        {(lookupMutation.isPending || result || notFound) && (
          <Grid item xs={12}>
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>
                Result
              </Typography>

              {lookupMutation.isPending && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, py: 3 }}>
                  <CircularProgress size={24} />
                  <Typography>Searching for barcode: {lastScanned}</Typography>
                </Box>
              )}

              {notFound && !lookupMutation.isPending && (
                <Alert severity="warning" action={<Button onClick={handleClear}>Clear</Button>}>
                  No part or asset found with barcode: <strong>{lastScanned}</strong>
                </Alert>
              )}

              {result && !lookupMutation.isPending && (
                <Card variant="outlined">
                  <CardContent>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                      {result.type === 'Part' ? (
                        <BuildIcon color="primary" />
                      ) : (
                        <InventoryIcon color="secondary" />
                      )}
                      <Chip
                        label={result.type}
                        color={result.type === 'Part' ? 'primary' : 'secondary'}
                        size="small"
                      />
                    </Box>

                    <Typography variant="h5" gutterBottom>
                      {result.name}
                    </Typography>

                    <Divider sx={{ my: 2 }} />

                    <Grid container spacing={2}>
                      <Grid item xs={6}>
                        <Typography variant="body2" color="text.secondary">
                          {result.type === 'Part' ? 'Part Number' : 'Asset Tag'}
                        </Typography>
                        <Typography variant="body1" fontWeight="medium">
                          {result.code}
                        </Typography>
                      </Grid>
                      <Grid item xs={6}>
                        <Typography variant="body2" color="text.secondary">
                          Barcode
                        </Typography>
                        <Typography variant="body1" fontWeight="medium" fontFamily="monospace">
                          {result.barcode}
                        </Typography>
                      </Grid>
                      {result.description && (
                        <Grid item xs={12}>
                          <Typography variant="body2" color="text.secondary">
                            Description
                          </Typography>
                          <Typography variant="body1">{result.description}</Typography>
                        </Grid>
                      )}
                    </Grid>
                  </CardContent>
                  <CardActions sx={{ justifyContent: 'flex-end', px: 2, pb: 2 }}>
                    <Button onClick={handleClear}>Clear</Button>
                    <Button variant="contained" onClick={handleViewDetails}>
                      View Details
                    </Button>
                  </CardActions>
                </Card>
              )}
            </Paper>
          </Grid>
        )}
      </Grid>
    </Box>
  );
};
