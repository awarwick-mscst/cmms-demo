import React from 'react';
import {
  Box,
  Paper,
  Typography,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Collapse,
  Chip,
} from '@mui/material';
import { LocationOn as LocationIcon, ExpandLess, ExpandMore } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { assetService } from '../services/assetService';
import { AssetLocation } from '../types';
import { LoadingSpinner } from '../components/common/LoadingSpinner';

const LocationItem: React.FC<{ location: AssetLocation; level: number }> = ({ location, level }) => {
  const [open, setOpen] = React.useState(true);
  const hasChildren = location.children && location.children.length > 0;

  return (
    <>
      <ListItem
        sx={{ pl: 2 + level * 3, cursor: hasChildren ? 'pointer' : 'default' }}
        onClick={() => hasChildren && setOpen(!open)}
      >
        <ListItemIcon>
          <LocationIcon />
        </ListItemIcon>
        <ListItemText
          primary={location.name}
          secondary={
            <React.Fragment>
              {location.description}
              {location.building && ` | Building: ${location.building}`}
              {location.floor && ` | Floor: ${location.floor}`}
            </React.Fragment>
          }
        />
        <Chip label={location.code} size="small" variant="outlined" sx={{ mr: 1 }} />
        {hasChildren && (open ? <ExpandLess /> : <ExpandMore />)}
      </ListItem>
      {hasChildren && (
        <Collapse in={open} timeout="auto" unmountOnExit>
          <List component="div" disablePadding>
            {location.children.map((child) => (
              <LocationItem key={child.id} location={child} level={level + 1} />
            ))}
          </List>
        </Collapse>
      )}
    </>
  );
};

export const LocationsPage: React.FC = () => {
  const { data, isLoading } = useQuery({
    queryKey: ['locationTree'],
    queryFn: () => assetService.getLocationTree(),
  });

  if (isLoading) {
    return <LoadingSpinner message="Loading locations..." />;
  }

  return (
    <Box>
      <Typography variant="h5" gutterBottom>
        Asset Locations
      </Typography>

      <Paper sx={{ mt: 2 }}>
        <List>
          {data?.data?.map((location) => (
            <LocationItem key={location.id} location={location} level={0} />
          ))}
        </List>
      </Paper>
    </Box>
  );
};
