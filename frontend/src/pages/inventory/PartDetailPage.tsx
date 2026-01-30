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
  Paper,
  Tab,
  Tabs,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  MenuItem,
  Alert,
} from '@mui/material';
import {
  ArrowBack as BackIcon,
  Edit as EditIcon,
  Add as AddIcon,
  SwapHoriz as TransferIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { partService } from '../../services/partService';
import { storageLocationService } from '../../services/storageLocationService';
import { PartDetail, StockAdjustmentRequest, StockTransferRequest, TransactionTypes } from '../../types';
import { useAuth } from '../../hooks/useAuth';

interface TabPanelProps {
  children?: React.ReactNode;
  value: number;
  index: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => (
  <div hidden={value !== index}>
    {value === index && <Box sx={{ pt: 2 }}>{children}</Box>}
  </div>
);

const statusColors: Record<string, 'success' | 'warning' | 'error' | 'default'> = {
  Active: 'success',
  Inactive: 'default',
  Obsolete: 'warning',
  Discontinued: 'error',
};

const reorderStatusColors: Record<string, 'success' | 'warning' | 'error' | 'default'> = {
  Ok: 'success',
  Low: 'warning',
  Critical: 'error',
  OutOfStock: 'error',
};

export const PartDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { hasPermission } = useAuth();

  const [tabValue, setTabValue] = useState(0);
  const [adjustDialogOpen, setAdjustDialogOpen] = useState(false);
  const [transferDialogOpen, setTransferDialogOpen] = useState(false);
  const [adjustmentData, setAdjustmentData] = useState<StockAdjustmentRequest>({
    locationId: 0,
    transactionType: 'Receive',
    quantity: 0,
    notes: '',
  });
  const [transferData, setTransferData] = useState<StockTransferRequest>({
    fromLocationId: 0,
    toLocationId: 0,
    quantity: 0,
    notes: '',
  });

  const { data: partData, isLoading } = useQuery({
    queryKey: ['part', id],
    queryFn: () => partService.getPart(Number(id)),
    enabled: !!id,
  });

  const { data: transactionsData } = useQuery({
    queryKey: ['partTransactions', id],
    queryFn: () => partService.getPartTransactions(Number(id), { pageSize: 50 }),
    enabled: !!id,
  });

  const { data: locationsData } = useQuery({
    queryKey: ['storageLocationsFlat'],
    queryFn: () => storageLocationService.getLocationsFlat(),
  });

  const adjustMutation = useMutation({
    mutationFn: (data: StockAdjustmentRequest) => partService.adjustStock(Number(id), data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['part', id] });
      queryClient.invalidateQueries({ queryKey: ['partTransactions', id] });
      setAdjustDialogOpen(false);
      setAdjustmentData({ locationId: 0, transactionType: 'Receive', quantity: 0, notes: '' });
    },
  });

  const transferMutation = useMutation({
    mutationFn: (data: StockTransferRequest) => partService.transferStock(Number(id), data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['part', id] });
      queryClient.invalidateQueries({ queryKey: ['partTransactions', id] });
      setTransferDialogOpen(false);
      setTransferData({ fromLocationId: 0, toLocationId: 0, quantity: 0, notes: '' });
    },
  });

  if (isLoading) {
    return <Typography>Loading...</Typography>;
  }

  const part = partData?.data;

  if (!part) {
    return <Typography>Part not found</Typography>;
  }

  const handleAdjustSubmit = () => {
    adjustMutation.mutate(adjustmentData);
  };

  const handleTransferSubmit = () => {
    transferMutation.mutate(transferData);
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Button startIcon={<BackIcon />} onClick={() => navigate('/inventory/parts')}>
            Back
          </Button>
          <Typography variant="h5">{part.name}</Typography>
          <Chip label={part.partNumber} variant="outlined" />
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          {hasPermission('inventory.manage') && (
            <>
              <Button
                variant="outlined"
                startIcon={<AddIcon />}
                onClick={() => setAdjustDialogOpen(true)}
              >
                Adjust Stock
              </Button>
              <Button
                variant="outlined"
                startIcon={<TransferIcon />}
                onClick={() => setTransferDialogOpen(true)}
              >
                Transfer
              </Button>
              <Button
                variant="contained"
                startIcon={<EditIcon />}
                onClick={() => navigate(`/inventory/parts/${id}/edit`)}
              >
                Edit
              </Button>
            </>
          )}
        </Box>
      </Box>

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <Card>
            <CardContent>
              <Grid container spacing={2}>
                <Grid item xs={6} sm={3}>
                  <Typography variant="body2" color="text.secondary">Status</Typography>
                  <Chip
                    label={part.status}
                    size="small"
                    color={statusColors[part.status] || 'default'}
                    sx={{ mt: 0.5 }}
                  />
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Typography variant="body2" color="text.secondary">Stock Status</Typography>
                  <Chip
                    label={part.reorderStatus}
                    size="small"
                    color={reorderStatusColors[part.reorderStatus] || 'default'}
                    variant="outlined"
                    sx={{ mt: 0.5 }}
                  />
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Typography variant="body2" color="text.secondary">Category</Typography>
                  <Typography variant="body1">{part.categoryName || 'None'}</Typography>
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Typography variant="body2" color="text.secondary">Supplier</Typography>
                  <Typography variant="body1">{part.supplierName || 'None'}</Typography>
                </Grid>
                <Grid item xs={12}>
                  <Divider sx={{ my: 1 }} />
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Typography variant="body2" color="text.secondary">Unit Cost</Typography>
                  <Typography variant="h6">${part.unitCost.toFixed(2)}</Typography>
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Typography variant="body2" color="text.secondary">Unit</Typography>
                  <Typography variant="body1">{part.unitOfMeasure}</Typography>
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Typography variant="body2" color="text.secondary">Reorder Point</Typography>
                  <Typography variant="body1">{part.reorderPoint}</Typography>
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Typography variant="body2" color="text.secondary">Lead Time</Typography>
                  <Typography variant="body1">{part.leadTimeDays} days</Typography>
                </Grid>
                {part.description && (
                  <Grid item xs={12}>
                    <Divider sx={{ my: 1 }} />
                    <Typography variant="body2" color="text.secondary">Description</Typography>
                    <Typography variant="body1">{part.description}</Typography>
                  </Grid>
                )}
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Stock Summary</Typography>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                <Typography color="text.secondary">On Hand:</Typography>
                <Typography fontWeight="bold">{part.totalQuantityOnHand}</Typography>
              </Box>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                <Typography color="text.secondary">Reserved:</Typography>
                <Typography>{part.totalQuantityReserved}</Typography>
              </Box>
              <Divider sx={{ my: 1 }} />
              <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                <Typography color="text.secondary">Available:</Typography>
                <Typography fontWeight="bold" color="primary">
                  {part.totalQuantityAvailable}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12}>
          <Paper sx={{ p: 2 }}>
            <Tabs value={tabValue} onChange={(_, v) => setTabValue(v)}>
              <Tab label="Stock by Location" />
              <Tab label="Transaction History" />
            </Tabs>

            <TabPanel value={tabValue} index={0}>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Location</TableCell>
                      <TableCell align="right">On Hand</TableCell>
                      <TableCell align="right">Reserved</TableCell>
                      <TableCell align="right">Available</TableCell>
                      <TableCell>Bin/Shelf</TableCell>
                      <TableCell>Last Count</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {part.stocks?.length > 0 ? (
                      part.stocks.map((stock) => (
                        <TableRow key={stock.id}>
                          <TableCell>{stock.locationFullPath || stock.locationName}</TableCell>
                          <TableCell align="right">{stock.quantityOnHand}</TableCell>
                          <TableCell align="right">{stock.quantityReserved}</TableCell>
                          <TableCell align="right">{stock.quantityAvailable}</TableCell>
                          <TableCell>{[stock.binNumber, stock.shelfLocation].filter(Boolean).join(' / ') || '-'}</TableCell>
                          <TableCell>
                            {stock.lastCountDate
                              ? new Date(stock.lastCountDate).toLocaleDateString()
                              : '-'}
                          </TableCell>
                        </TableRow>
                      ))
                    ) : (
                      <TableRow>
                        <TableCell colSpan={6} align="center">
                          No stock records
                        </TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            </TabPanel>

            <TabPanel value={tabValue} index={1}>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Date</TableCell>
                      <TableCell>Type</TableCell>
                      <TableCell>Location</TableCell>
                      <TableCell align="right">Quantity</TableCell>
                      <TableCell align="right">Cost</TableCell>
                      <TableCell>Reference</TableCell>
                      <TableCell>By</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {transactionsData?.items?.length ? (
                      transactionsData.items.map((tx) => (
                        <TableRow key={tx.id}>
                          <TableCell>{new Date(tx.transactionDate).toLocaleString()}</TableCell>
                          <TableCell>
                            <Chip label={tx.transactionType} size="small" variant="outlined" />
                          </TableCell>
                          <TableCell>
                            {tx.transactionType === 'Transfer'
                              ? `${tx.locationName} → ${tx.toLocationName}`
                              : tx.locationName}
                          </TableCell>
                          <TableCell align="right" sx={{ color: tx.quantity >= 0 ? 'success.main' : 'error.main' }}>
                            {tx.quantity >= 0 ? '+' : ''}{tx.quantity}
                          </TableCell>
                          <TableCell align="right">${tx.totalCost.toFixed(2)}</TableCell>
                          <TableCell>{tx.referenceType ? `${tx.referenceType} #${tx.referenceId}` : '-'}</TableCell>
                          <TableCell>{tx.createdByName || '-'}</TableCell>
                        </TableRow>
                      ))
                    ) : (
                      <TableRow>
                        <TableCell colSpan={7} align="center">
                          No transactions
                        </TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            </TabPanel>
          </Paper>
        </Grid>
      </Grid>

      {/* Adjust Stock Dialog */}
      <Dialog open={adjustDialogOpen} onClose={() => setAdjustDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Adjust Stock</DialogTitle>
        <DialogContent>
          {adjustMutation.error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {(adjustMutation.error as any)?.response?.data?.errors?.[0] || 'Error adjusting stock'}
            </Alert>
          )}
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                select
                fullWidth
                label="Location"
                value={adjustmentData.locationId || ''}
                onChange={(e) => setAdjustmentData({ ...adjustmentData, locationId: Number(e.target.value) })}
              >
                {locationsData?.data?.map((loc) => (
                  <MenuItem key={loc.id} value={loc.id}>
                    {loc.fullPath || loc.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={6}>
              <TextField
                select
                fullWidth
                label="Type"
                value={adjustmentData.transactionType}
                onChange={(e) => setAdjustmentData({ ...adjustmentData, transactionType: e.target.value })}
              >
                <MenuItem value="Receive">Receive</MenuItem>
                <MenuItem value="Issue">Issue</MenuItem>
                <MenuItem value="Adjust">Adjust</MenuItem>
              </TextField>
            </Grid>
            <Grid item xs={6}>
              <TextField
                fullWidth
                type="number"
                label="Quantity"
                value={adjustmentData.quantity}
                onChange={(e) => setAdjustmentData({ ...adjustmentData, quantity: Number(e.target.value) })}
                inputProps={{ min: 1 }}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                multiline
                rows={2}
                label="Notes"
                value={adjustmentData.notes || ''}
                onChange={(e) => setAdjustmentData({ ...adjustmentData, notes: e.target.value })}
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAdjustDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleAdjustSubmit}
            variant="contained"
            disabled={adjustMutation.isPending || !adjustmentData.locationId || !adjustmentData.quantity}
          >
            {adjustMutation.isPending ? 'Saving...' : 'Adjust'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Transfer Stock Dialog */}
      <Dialog open={transferDialogOpen} onClose={() => setTransferDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Transfer Stock</DialogTitle>
        <DialogContent>
          {transferMutation.error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {(transferMutation.error as any)?.response?.data?.errors?.[0] || 'Error transferring stock'}
            </Alert>
          )}
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                select
                fullWidth
                label="From Location"
                value={transferData.fromLocationId || ''}
                onChange={(e) => setTransferData({ ...transferData, fromLocationId: Number(e.target.value) })}
              >
                {locationsData?.data?.map((loc) => (
                  <MenuItem key={loc.id} value={loc.id}>
                    {loc.fullPath || loc.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12}>
              <TextField
                select
                fullWidth
                label="To Location"
                value={transferData.toLocationId || ''}
                onChange={(e) => setTransferData({ ...transferData, toLocationId: Number(e.target.value) })}
              >
                {locationsData?.data?.map((loc) => (
                  <MenuItem key={loc.id} value={loc.id}>
                    {loc.fullPath || loc.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                type="number"
                label="Quantity"
                value={transferData.quantity}
                onChange={(e) => setTransferData({ ...transferData, quantity: Number(e.target.value) })}
                inputProps={{ min: 1 }}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                multiline
                rows={2}
                label="Notes"
                value={transferData.notes || ''}
                onChange={(e) => setTransferData({ ...transferData, notes: e.target.value })}
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setTransferDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleTransferSubmit}
            variant="contained"
            disabled={transferMutation.isPending || !transferData.fromLocationId || !transferData.toLocationId || !transferData.quantity}
          >
            {transferMutation.isPending ? 'Transferring...' : 'Transfer'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
