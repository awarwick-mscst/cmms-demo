import React, { useState } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import {
  AppBar,
  Box,
  CssBaseline,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  Menu,
  MenuItem,
  Avatar,
} from '@mui/material';
import {
  Menu as MenuIcon,
  Dashboard as DashboardIcon,
  Inventory as InventoryIcon,
  Category as CategoryIcon,
  LocationOn as LocationIcon,
  AccountCircle,
  Logout as LogoutIcon,
  Build as PartsIcon,
  LocalShipping as SupplierIcon,
  Warehouse as WarehouseIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
} from '@mui/icons-material';
import { Collapse } from '@mui/material';
import { useAuth } from '../../hooks/useAuth';

const drawerWidth = 240;

interface MenuItem {
  text: string;
  icon: React.ReactNode;
  path?: string;
  children?: MenuItem[];
}

const menuItems: MenuItem[] = [
  { text: 'Dashboard', icon: <DashboardIcon />, path: '/' },
  { text: 'Assets', icon: <InventoryIcon />, path: '/assets' },
  { text: 'Categories', icon: <CategoryIcon />, path: '/categories' },
  { text: 'Locations', icon: <LocationIcon />, path: '/locations' },
  {
    text: 'Inventory',
    icon: <PartsIcon />,
    children: [
      { text: 'Parts', icon: <PartsIcon />, path: '/inventory/parts' },
      { text: 'Suppliers', icon: <SupplierIcon />, path: '/inventory/suppliers' },
      { text: 'Part Categories', icon: <CategoryIcon />, path: '/inventory/categories' },
      { text: 'Storage Locations', icon: <WarehouseIcon />, path: '/inventory/locations' },
    ],
  },
];

export const Layout: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuth();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [inventoryOpen, setInventoryOpen] = useState(location.pathname.startsWith('/inventory'));

  const handleDrawerToggle = () => {
    setMobileOpen(!mobileOpen);
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = () => {
    handleMenuClose();
    logout();
  };

  const drawer = (
    <div>
      <Toolbar>
        <Typography variant="h6" noWrap component="div">
          CMMS
        </Typography>
      </Toolbar>
      <Divider />
      <List>
        {menuItems.map((item) => (
          <React.Fragment key={item.text}>
            {item.children ? (
              <>
                <ListItem disablePadding>
                  <ListItemButton onClick={() => setInventoryOpen(!inventoryOpen)}>
                    <ListItemIcon>{item.icon}</ListItemIcon>
                    <ListItemText primary={item.text} />
                    {inventoryOpen ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                  </ListItemButton>
                </ListItem>
                <Collapse in={inventoryOpen} timeout="auto" unmountOnExit>
                  <List component="div" disablePadding>
                    {item.children.map((child) => (
                      <ListItem key={child.text} disablePadding>
                        <ListItemButton
                          sx={{ pl: 4 }}
                          selected={location.pathname === child.path}
                          onClick={() => {
                            navigate(child.path!);
                            setMobileOpen(false);
                          }}
                        >
                          <ListItemIcon>{child.icon}</ListItemIcon>
                          <ListItemText primary={child.text} />
                        </ListItemButton>
                      </ListItem>
                    ))}
                  </List>
                </Collapse>
              </>
            ) : (
              <ListItem disablePadding>
                <ListItemButton
                  selected={location.pathname === item.path}
                  onClick={() => {
                    navigate(item.path!);
                    setMobileOpen(false);
                  }}
                >
                  <ListItemIcon>{item.icon}</ListItemIcon>
                  <ListItemText primary={item.text} />
                </ListItemButton>
              </ListItem>
            )}
          </React.Fragment>
        ))}
      </List>
    </div>
  );

  return (
    <Box sx={{ display: 'flex' }}>
      <CssBaseline />
      <AppBar
        position="fixed"
        sx={{
          width: { sm: `calc(100% - ${drawerWidth}px)` },
          ml: { sm: `${drawerWidth}px` },
        }}
      >
        <Toolbar>
          <IconButton
            color="inherit"
            aria-label="open drawer"
            edge="start"
            onClick={handleDrawerToggle}
            sx={{ mr: 2, display: { sm: 'none' } }}
          >
            <MenuIcon />
          </IconButton>
          <Typography variant="h6" noWrap component="div" sx={{ flexGrow: 1 }}>
            {menuItems.find((item) => item.path === location.pathname)?.text || 'CMMS'}
          </Typography>
          <IconButton color="inherit" onClick={handleMenuOpen}>
            <Avatar sx={{ width: 32, height: 32, bgcolor: 'secondary.main' }}>
              {user?.firstName?.[0] || 'U'}
            </Avatar>
          </IconButton>
          <Menu
            anchorEl={anchorEl}
            open={Boolean(anchorEl)}
            onClose={handleMenuClose}
            anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
            transformOrigin={{ vertical: 'top', horizontal: 'right' }}
          >
            <MenuItem disabled>
              <AccountCircle sx={{ mr: 1 }} />
              {user?.fullName || user?.username}
            </MenuItem>
            <Divider />
            <MenuItem onClick={handleLogout}>
              <LogoutIcon sx={{ mr: 1 }} />
              Logout
            </MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>
      <Box
        component="nav"
        sx={{ width: { sm: drawerWidth }, flexShrink: { sm: 0 } }}
      >
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={handleDrawerToggle}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: 'block', sm: 'none' },
            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: drawerWidth },
          }}
        >
          {drawer}
        </Drawer>
        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', sm: 'block' },
            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: drawerWidth },
          }}
          open
        >
          {drawer}
        </Drawer>
      </Box>
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 3,
          width: { sm: `calc(100% - ${drawerWidth}px)` },
          mt: 8,
        }}
      >
        <Outlet />
      </Box>
    </Box>
  );
};
