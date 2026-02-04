import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  MenuItem,
  Grid,
  Alert,
  Typography,
  Paper,
  CircularProgress,
  Box,
} from '@mui/material';
import { useQuery, useMutation } from '@tanstack/react-query';
import { labelService } from '../../services/labelService';
import { LabelTemplate, LabelPrinter, PrintLabelRequest } from '../../types';

interface PrintLabelDialogProps {
  open: boolean;
  onClose: () => void;
  partId: number;
  partNumber: string;
  partName: string;
}

export const PrintLabelDialog: React.FC<PrintLabelDialogProps> = ({
  open,
  onClose,
  partId,
  partNumber,
  partName,
}) => {
  const [templateId, setTemplateId] = useState<number | ''>('');
  const [printerId, setPrinterId] = useState<number | ''>('');
  const [quantity, setQuantity] = useState(1);
  const [showPreview, setShowPreview] = useState(false);

  const { data: templates, isLoading: loadingTemplates } = useQuery({
    queryKey: ['label-templates'],
    queryFn: () => labelService.getTemplates(),
    enabled: open,
  });

  const { data: printers, isLoading: loadingPrinters } = useQuery({
    queryKey: ['printers', { activeOnly: true }],
    queryFn: () => labelService.getPrinters(true),
    enabled: open,
  });

  const { data: preview, isLoading: loadingPreview } = useQuery({
    queryKey: ['print-preview', partId, templateId, printerId],
    queryFn: () =>
      labelService.getPreview({
        partId,
        templateId: templateId || undefined,
        printerId: printerId || undefined,
      }),
    enabled: open && showPreview && !!partId,
  });

  // Set defaults when data loads
  useEffect(() => {
    if (templates?.data) {
      const defaultTemplate = templates.data.find((t) => t.isDefault);
      if (defaultTemplate && templateId === '') {
        setTemplateId(defaultTemplate.id);
      }
    }
  }, [templates]);

  useEffect(() => {
    if (printers?.data) {
      const defaultPrinter = printers.data.find((p) => p.isDefault && p.isActive);
      if (defaultPrinter && printerId === '') {
        setPrinterId(defaultPrinter.id);
      }
    }
  }, [printers]);

  const printMutation = useMutation({
    mutationFn: labelService.printPartLabel,
    onSuccess: (response) => {
      if (response.data?.success) {
        onClose();
      }
    },
  });

  const handlePrint = () => {
    const request: PrintLabelRequest = {
      partId,
      templateId: templateId || undefined,
      printerId: printerId || undefined,
      quantity,
    };
    printMutation.mutate(request);
  };

  const handleClose = () => {
    setShowPreview(false);
    printMutation.reset();
    onClose();
  };

  const isLoading = loadingTemplates || loadingPrinters;
  const templateList = templates?.data || [];
  const printerList = printers?.data || [];
  const hasTemplates = templateList.length > 0;
  const hasPrinters = printerList.length > 0;

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Print Label</DialogTitle>
      <DialogContent>
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress />
          </Box>
        ) : (
          <>
            <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
              Part: {partNumber} - {partName}
            </Typography>

            {!hasTemplates && (
              <Alert severity="warning" sx={{ mb: 2 }}>
                No label templates configured. Please create a template first.
              </Alert>
            )}

            {!hasPrinters && (
              <Alert severity="warning" sx={{ mb: 2 }}>
                No active printers configured. Please add a printer first.
              </Alert>
            )}

            {printMutation.isError && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {(printMutation.error as any)?.response?.data?.message ||
                  'Failed to print label'}
              </Alert>
            )}

            {printMutation.isSuccess && printMutation.data?.data?.success && (
              <Alert severity="success" sx={{ mb: 2 }}>
                {printMutation.data.data.message}
              </Alert>
            )}

            {printMutation.isSuccess && !printMutation.data?.data?.success && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {printMutation.data?.data?.message || 'Print failed'}
              </Alert>
            )}

            <Grid container spacing={2}>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  select
                  label="Template"
                  value={templateId}
                  onChange={(e) => setTemplateId(Number(e.target.value))}
                  disabled={!hasTemplates}
                >
                  {templateList.map((template) => (
                    <MenuItem key={template.id} value={template.id}>
                      {template.name}
                      {template.isDefault && ' (Default)'}
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  select
                  label="Printer"
                  value={printerId}
                  onChange={(e) => setPrinterId(Number(e.target.value))}
                  disabled={!hasPrinters}
                >
                  {printerList.map((printer) => (
                    <MenuItem key={printer.id} value={printer.id}>
                      {printer.name}
                      {printer.location && ` (${printer.location})`}
                      {printer.isDefault && ' - Default'}
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Quantity"
                  type="number"
                  inputProps={{ min: 1, max: 100 }}
                  value={quantity}
                  onChange={(e) => setQuantity(Math.max(1, Math.min(100, parseInt(e.target.value) || 1)))}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <Button
                  fullWidth
                  variant="outlined"
                  onClick={() => setShowPreview(!showPreview)}
                  sx={{ height: '100%' }}
                >
                  {showPreview ? 'Hide Preview' : 'Show Commands Preview'}
                </Button>
              </Grid>
            </Grid>

            {showPreview && (
              <Paper
                variant="outlined"
                sx={{
                  mt: 2,
                  p: 2,
                  maxHeight: 200,
                  overflow: 'auto',
                  bgcolor: 'grey.100',
                }}
              >
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 1 }}>
                  {preview?.data?.language || 'ZPL'} Preview:
                </Typography>
                {loadingPreview ? (
                  <CircularProgress size={20} />
                ) : preview?.data?.zpl ? (
                  <Typography
                    component="pre"
                    sx={{
                      fontFamily: 'monospace',
                      fontSize: '0.75rem',
                      whiteSpace: 'pre-wrap',
                      wordBreak: 'break-all',
                      m: 0,
                    }}
                  >
                    {preview.data.zpl}
                  </Typography>
                ) : (
                  <Typography color="textSecondary">Unable to generate preview</Typography>
                )}
              </Paper>
            )}
          </>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>Cancel</Button>
        <Button
          variant="contained"
          onClick={handlePrint}
          disabled={
            isLoading ||
            printMutation.isPending ||
            !hasTemplates ||
            !hasPrinters ||
            !templateId ||
            !printerId
          }
        >
          {printMutation.isPending ? 'Printing...' : `Print ${quantity} Label${quantity > 1 ? 's' : ''}`}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
