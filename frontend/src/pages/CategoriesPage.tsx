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
import { Category as CategoryIcon, ExpandLess, ExpandMore } from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { assetService } from '../services/assetService';
import { AssetCategory } from '../types';
import { LoadingSpinner } from '../components/common/LoadingSpinner';

const CategoryItem: React.FC<{ category: AssetCategory; level: number }> = ({ category, level }) => {
  const [open, setOpen] = React.useState(true);
  const hasChildren = category.children && category.children.length > 0;

  return (
    <>
      <ListItem
        sx={{ pl: 2 + level * 3, cursor: hasChildren ? 'pointer' : 'default' }}
        onClick={() => hasChildren && setOpen(!open)}
      >
        <ListItemIcon>
          <CategoryIcon />
        </ListItemIcon>
        <ListItemText
          primary={category.name}
          secondary={category.description}
        />
        <Chip label={category.code} size="small" variant="outlined" sx={{ mr: 1 }} />
        {hasChildren && (open ? <ExpandLess /> : <ExpandMore />)}
      </ListItem>
      {hasChildren && (
        <Collapse in={open} timeout="auto" unmountOnExit>
          <List component="div" disablePadding>
            {category.children.map((child) => (
              <CategoryItem key={child.id} category={child} level={level + 1} />
            ))}
          </List>
        </Collapse>
      )}
    </>
  );
};

export const CategoriesPage: React.FC = () => {
  const { data, isLoading } = useQuery({
    queryKey: ['categories'],
    queryFn: () => assetService.getCategories(),
  });

  if (isLoading) {
    return <LoadingSpinner message="Loading categories..." />;
  }

  return (
    <Box>
      <Typography variant="h5" gutterBottom>
        Asset Categories
      </Typography>

      <Paper sx={{ mt: 2 }}>
        <List>
          {data?.data?.map((category) => (
            <CategoryItem key={category.id} category={category} level={0} />
          ))}
        </List>
      </Paper>
    </Box>
  );
};
