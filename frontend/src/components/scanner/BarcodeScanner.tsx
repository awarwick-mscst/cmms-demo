import React, { useEffect, useRef, useState, useCallback } from 'react';
import { Box, Button, Alert, CircularProgress, FormControl, InputLabel, Select, MenuItem } from '@mui/material';
import { Html5Qrcode, Html5QrcodeSupportedFormats } from 'html5-qrcode';
import CameraAltIcon from '@mui/icons-material/CameraAlt';
import StopIcon from '@mui/icons-material/Stop';

// Supported barcode formats for inventory/asset scanning
// Code128 first for priority
const SUPPORTED_FORMATS = [
  Html5QrcodeSupportedFormats.CODE_128,
  Html5QrcodeSupportedFormats.CODE_39,
  Html5QrcodeSupportedFormats.CODE_93,
  Html5QrcodeSupportedFormats.EAN_13,
  Html5QrcodeSupportedFormats.EAN_8,
  Html5QrcodeSupportedFormats.UPC_A,
  Html5QrcodeSupportedFormats.UPC_E,
  Html5QrcodeSupportedFormats.ITF,
  Html5QrcodeSupportedFormats.CODABAR,
  Html5QrcodeSupportedFormats.QR_CODE,
  Html5QrcodeSupportedFormats.DATA_MATRIX,
];

// Use experimentalFeatures for better 1D barcode scanning
const SCANNER_CONFIG = {
  experimentalFeatures: {
    useBarCodeDetectorIfSupported: true, // Uses native BarcodeDetector API if available
  },
};

interface BarcodeScannerProps {
  onScan: (barcode: string) => void;
  onError?: (error: string) => void;
}

interface CameraDevice {
  id: string;
  label: string;
}

export const BarcodeScanner: React.FC<BarcodeScannerProps> = ({ onScan, onError }) => {
  const [isScanning, setIsScanning] = useState(false);
  const [showContainer, setShowContainer] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [cameras, setCameras] = useState<CameraDevice[]>([]);
  const [selectedCamera, setSelectedCamera] = useState<string>('');
  const [loading, setLoading] = useState(false);
  const [lastDetected, setLastDetected] = useState<string | null>(null);
  const scannerRef = useRef<Html5Qrcode | null>(null);
  const lastScanRef = useRef<string>('');

  useEffect(() => {
    // Get available cameras on component mount
    Html5Qrcode.getCameras()
      .then((devices) => {
        if (devices && devices.length > 0) {
          const cameraList = devices.map((device) => ({
            id: device.id,
            label: device.label || `Camera ${device.id.slice(0, 8)}`,
          }));
          setCameras(cameraList);
          // Prefer back camera on mobile devices
          const backCamera = cameraList.find(
            (c) => c.label.toLowerCase().includes('back') || c.label.toLowerCase().includes('rear')
          );
          setSelectedCamera(backCamera?.id || cameraList[0].id);
        }
      })
      .catch((err) => {
        console.error('Failed to get cameras:', err);
        setError('Could not access camera list. Please ensure camera permissions are granted.');
      });

    return () => {
      // Cleanup on unmount
      if (scannerRef.current) {
        scannerRef.current.stop().catch(() => {});
      }
    };
  }, []);

  const stopScanning = useCallback(async () => {
    if (scannerRef.current) {
      try {
        await scannerRef.current.stop();
        scannerRef.current.clear();
      } catch (err) {
        console.error('Error stopping scanner:', err);
      }
      scannerRef.current = null;
    }
    setIsScanning(false);
    setShowContainer(false);
    setLastDetected(null);
    lastScanRef.current = '';
  }, []);

  const startScanning = async () => {
    if (!selectedCamera) {
      setError('No camera selected');
      return;
    }

    setLoading(true);
    setError(null);

    // Show container first, then start scanner after a brief delay
    setShowContainer(true);

    // Wait for container to be visible and have dimensions
    await new Promise(resolve => setTimeout(resolve, 100));

    try {
      const scannerId = 'barcode-scanner-container';
      const container = document.getElementById(scannerId);

      if (!container) {
        setError('Scanner container not found');
        setLoading(false);
        setShowContainer(false);
        return;
      }

      // Create new scanner instance with supported barcode formats
      scannerRef.current = new Html5Qrcode(scannerId, {
        formatsToSupport: SUPPORTED_FORMATS,
        verbose: false,
        ...SCANNER_CONFIG,
      });

      // Get container width for responsive scanning area
      // Make scanning area wider for 1D barcodes
      const containerWidth = container.clientWidth || 500;
      const qrboxWidth = Math.min(400, containerWidth - 20);
      const qrboxHeight = Math.min(200, qrboxWidth * 0.5);

      await scannerRef.current.start(
        selectedCamera,
        {
          fps: 30,
          // Scan 95% of the frame for maximum detection area
          qrbox: (viewfinderWidth: number, viewfinderHeight: number) => ({
            width: Math.floor(viewfinderWidth * 0.95),
            height: Math.floor(viewfinderHeight * 0.95),
          }),
          aspectRatio: 16 / 9,
          disableFlip: false,
          videoConstraints: {
            deviceId: { exact: selectedCamera },
            // Lower resolution = faster processing
            width: { ideal: 1280, min: 640 },
            height: { ideal: 720, min: 480 },
          },
        },
        (decodedText) => {
          // Prevent duplicate rapid scans of the same barcode
          if (decodedText !== lastScanRef.current) {
            lastScanRef.current = decodedText;
            setLastDetected(decodedText);
            onScan(decodedText);
            // Reset after 1 second to allow re-scanning same code
            setTimeout(() => {
              lastScanRef.current = '';
            }, 1000);
          }
        },
        () => {
          // Ignore scan errors (no barcode found in frame)
        }
      );

      setIsScanning(true);
    } catch (err) {
      console.error('Scanner start error:', err);
      const errorMessage = err instanceof Error ? err.message : 'Failed to start camera';
      setError(errorMessage);
      onError?.(errorMessage);
      setShowContainer(false);

      // Clean up on error
      if (scannerRef.current) {
        try {
          scannerRef.current.clear();
        } catch {}
        scannerRef.current = null;
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {cameras.length > 1 && !isScanning && (
        <FormControl fullWidth sx={{ mb: 2 }}>
          <InputLabel id="camera-select-label">Camera</InputLabel>
          <Select
            labelId="camera-select-label"
            value={selectedCamera}
            label="Camera"
            onChange={(e) => setSelectedCamera(e.target.value)}
          >
            {cameras.map((camera) => (
              <MenuItem key={camera.id} value={camera.id}>
                {camera.label}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      )}

      <Box
        id="barcode-scanner-container"
        sx={{
          width: '100%',
          minHeight: showContainer ? 300 : 0,
          height: showContainer ? 'auto' : 0,
          bgcolor: 'black',
          borderRadius: 1,
          overflow: 'hidden',
          mb: showContainer ? 2 : 0,
          visibility: showContainer ? 'visible' : 'hidden',
          position: showContainer ? 'relative' : 'absolute',
          '& video': {
            width: '100% !important',
            height: 'auto !important',
            objectFit: 'cover',
          },
        }}
      />

      {lastDetected && isScanning && (
        <Alert severity="success" sx={{ mb: 2 }}>
          Detected: <strong>{lastDetected}</strong>
        </Alert>
      )}

      <Box sx={{ display: 'flex', justifyContent: 'center' }}>
        {!isScanning ? (
          <Button
            variant="contained"
            startIcon={loading ? <CircularProgress size={20} color="inherit" /> : <CameraAltIcon />}
            onClick={startScanning}
            disabled={loading || cameras.length === 0}
            size="large"
          >
            {loading ? 'Starting Camera...' : 'Start Camera Scanner'}
          </Button>
        ) : (
          <Button
            variant="outlined"
            color="error"
            startIcon={<StopIcon />}
            onClick={stopScanning}
            size="large"
          >
            Stop Scanner
          </Button>
        )}
      </Box>

      {cameras.length === 0 && !loading && (
        <Alert severity="info" sx={{ mt: 2 }}>
          No cameras detected. You can use manual entry or a USB barcode scanner.
        </Alert>
      )}
    </Box>
  );
};
