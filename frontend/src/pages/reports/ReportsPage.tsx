import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Grid,
  Card,
  CardContent,
  CardActionArea,
  Typography,
  Divider,
} from '@mui/material';
import {
  ShoppingCart as ReorderIcon,
  Inventory as InventoryIcon,
  SwapHoriz as MovementIcon,
  Warning as OverdueIcon,
  Build as MaintenanceIcon,
  CheckCircle as ComplianceIcon,
  Assessment as SummaryIcon,
} from '@mui/icons-material';

interface ReportCardProps {
  title: string;
  description: string;
  icon: React.ReactNode;
  path: string;
}

const ReportCard: React.FC<ReportCardProps> = ({ title, description, icon, path }) => {
  const navigate = useNavigate();

  return (
    <Card sx={{ height: '100%' }}>
      <CardActionArea onClick={() => navigate(path)} sx={{ height: '100%' }}>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
            <Box sx={{ color: 'primary.main', mr: 2 }}>{icon}</Box>
            <Typography variant="h6" component="div">
              {title}
            </Typography>
          </Box>
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        </CardContent>
      </CardActionArea>
    </Card>
  );
};

export const ReportsPage: React.FC = () => {
  const inventoryReports = [
    {
      title: 'Reorder Report',
      description: 'Parts at or below reorder point that need ordering. View low stock, critical, and out of stock items.',
      icon: <ReorderIcon fontSize="large" />,
      path: '/reports/reorder',
    },
    {
      title: 'Inventory Valuation',
      description: 'Total value of inventory broken down by category and location. Track your inventory investment.',
      icon: <InventoryIcon fontSize="large" />,
      path: '/reports/inventory-valuation',
    },
    {
      title: 'Stock Movement',
      description: 'Transaction history showing all inventory movements over a date range.',
      icon: <MovementIcon fontSize="large" />,
      path: '/reports/stock-movement',
    },
  ];

  const maintenanceReports = [
    {
      title: 'Overdue Maintenance',
      description: 'PM schedules and work orders that are past their due date and require immediate attention.',
      icon: <OverdueIcon fontSize="large" />,
      path: '/reports/overdue-maintenance',
    },
    {
      title: 'Maintenance Performed',
      description: 'Completed work orders with labor hours, parts used, and total costs for a date range.',
      icon: <MaintenanceIcon fontSize="large" />,
      path: '/reports/maintenance-performed',
    },
    {
      title: 'PM Compliance',
      description: 'Scheduled vs completed preventive maintenance. Track compliance rate by schedule.',
      icon: <ComplianceIcon fontSize="large" />,
      path: '/reports/pm-compliance',
    },
    {
      title: 'Work Order Summary',
      description: 'Work orders grouped by status, type, and priority. Overview of maintenance activities.',
      icon: <SummaryIcon fontSize="large" />,
      path: '/reports/work-order-summary',
    },
  ];

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Reports
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        Generate reports to analyze inventory levels, maintenance activities, and operational performance.
      </Typography>

      <Typography variant="h5" gutterBottom sx={{ mt: 4 }}>
        Inventory Reports
      </Typography>
      <Divider sx={{ mb: 3 }} />
      <Grid container spacing={3} sx={{ mb: 4 }}>
        {inventoryReports.map((report) => (
          <Grid item xs={12} sm={6} md={4} key={report.title}>
            <ReportCard {...report} />
          </Grid>
        ))}
      </Grid>

      <Typography variant="h5" gutterBottom sx={{ mt: 4 }}>
        Maintenance Reports
      </Typography>
      <Divider sx={{ mb: 3 }} />
      <Grid container spacing={3}>
        {maintenanceReports.map((report) => (
          <Grid item xs={12} sm={6} md={4} key={report.title}>
            <ReportCard {...report} />
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};

export default ReportsPage;
