import React from 'react';
import { Box, Grid, Paper, Typography } from '@mui/material';
import {
  Inventory as InventoryIcon,
  Category as CategoryIcon,
  LocationOn as LocationIcon,
  Build as BuildIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { assetService } from '../services/assetService';

interface StatCardProps {
  title: string;
  value: number | string;
  icon: React.ReactNode;
  color: string;
}

const StatCard: React.FC<StatCardProps> = ({ title, value, icon, color }) => (
  <Paper sx={{ p: 3, display: 'flex', alignItems: 'center', gap: 2 }}>
    <Box
      sx={{
        p: 2,
        borderRadius: 2,
        bgcolor: color,
        color: 'white',
        display: 'flex',
      }}
    >
      {icon}
    </Box>
    <Box>
      <Typography variant="h4" component="div">
        {value}
      </Typography>
      <Typography variant="body2" color="text.secondary">
        {title}
      </Typography>
    </Box>
  </Paper>
);

export const DashboardPage: React.FC = () => {
  const { data: assetsData } = useQuery({
    queryKey: ['assets', 'count'],
    queryFn: () => assetService.getAssets({ pageSize: 1 }),
  });

  const { data: categoriesData } = useQuery({
    queryKey: ['categories'],
    queryFn: () => assetService.getCategories(),
  });

  const { data: locationsData } = useQuery({
    queryKey: ['locations'],
    queryFn: () => assetService.getLocations(),
  });

  const countCategories = (categories: any[]): number => {
    return categories.reduce((count, cat) => {
      return count + 1 + (cat.children ? countCategories(cat.children) : 0);
    }, 0);
  };

  return (
    <Box>
      <Typography variant="h5" gutterBottom>
        Dashboard
      </Typography>

      <Grid container spacing={3} sx={{ mt: 1 }}>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Total Assets"
            value={assetsData?.totalCount || 0}
            icon={<InventoryIcon />}
            color="#1976d2"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Categories"
            value={categoriesData?.data ? countCategories(categoriesData.data) : 0}
            icon={<CategoryIcon />}
            color="#388e3c"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Locations"
            value={locationsData?.data?.length || 0}
            icon={<LocationIcon />}
            color="#f57c00"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Maintenance Due"
            value={0}
            icon={<BuildIcon />}
            color="#d32f2f"
          />
        </Grid>
      </Grid>

      <Paper sx={{ p: 3, mt: 4 }}>
        <Typography variant="h6" gutterBottom>
          Welcome to CMMS
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Use the sidebar to navigate to different sections of the system. You can manage assets,
          categories, and locations from their respective pages.
        </Typography>
      </Paper>
    </Box>
  );
};
