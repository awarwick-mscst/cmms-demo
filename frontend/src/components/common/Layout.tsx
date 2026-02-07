import React, { useState, useEffect } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import {
  AppBar,
  Box,
  Chip,
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
  Engineering as MaintenanceIcon,
  Assignment as WorkOrderIcon,
  Schedule as ScheduleIcon,
  AdminPanelSettings as AdminIcon,
  People as PeopleIcon,
  Timer as TimerIcon,
  Help as HelpIcon,
  Print as PrintIcon,
  Label as LabelIcon,
  Assessment as ReportsIcon,
  Brightness4 as Brightness4Icon,
  Brightness7 as Brightness7Icon,
  Checklist as ChecklistIcon,
  QrCodeScanner as QrCodeScannerIcon,
  Backup as BackupIcon,
  Storage as StorageIcon,
} from '@mui/icons-material';
import { Collapse } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../../hooks/useAuth';
import { useThemeMode } from '../../hooks/useThemeMode';
import { workOrderService } from '../../services/workOrderService';

const drawerWidth = 240;

interface MenuItem {
  text: string;
  icon: React.ReactNode;
  path?: string;
  children?: MenuItem[];
}

const menuItems: MenuItem[] = [
  { text: 'Dashboard', icon: <DashboardIcon />, path: '/' },
  { text: 'Scanner', icon: <QrCodeScannerIcon />, path: '/scanner' },
  { text: 'Assets', icon: <InventoryIcon />, path: '/assets' },
  { text: 'Categories', icon: <CategoryIcon />, path: '/categories' },
  { text: 'Locations', icon: <LocationIcon />, path: '/locations' },
  {
    text: 'Maintenance',
    icon: <MaintenanceIcon />,
    children: [
      { text: 'My Work', icon: <TimerIcon />, path: '/maintenance/my-work' },
      { text: 'Work Orders', icon: <WorkOrderIcon />, path: '/maintenance/work-orders' },
      { text: 'PM Schedules', icon: <ScheduleIcon />, path: '/maintenance/pm-schedules' },
    ],
  },
  {
    text: 'Inventory',
    icon: <PartsIcon />,
    children: [
      { text: 'Receive', icon: <WarehouseIcon />, path: '/inventory/receive' },
      { text: 'Parts', icon: <PartsIcon />, path: '/inventory/parts' },
      { text: 'Suppliers', icon: <SupplierIcon />, path: '/inventory/suppliers' },
      { text: 'Part Categories', icon: <CategoryIcon />, path: '/inventory/categories' },
      { text: 'Storage Locations', icon: <WarehouseIcon />, path: '/inventory/locations' },
    ],
  },
  { text: 'Reports', icon: <ReportsIcon />, path: '/reports' },
  {
    text: 'Admin',
    icon: <AdminIcon />,
    children: [
      { text: 'Users', icon: <PeopleIcon />, path: '/admin/users' },
      { text: 'Task Templates', icon: <ChecklistIcon />, path: '/admin/task-templates' },
      { text: 'Label Printers', icon: <PrintIcon />, path: '/admin/printers' },
      { text: 'Label Templates', icon: <LabelIcon />, path: '/admin/label-templates' },
      { text: 'Backup', icon: <BackupIcon />, path: '/admin/backup' },
      { text: 'Database', icon: <StorageIcon />, path: '/admin/database' },
      { text: 'Help', icon: <HelpIcon />, path: '/admin/help' },
    ],
  },
];

export const Layout: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuth();
  const { resolvedMode, toggleMode } = useThemeMode();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [openMenus, setOpenMenus] = useState<Record<string, boolean>>({
    Inventory: location.pathname.startsWith('/inventory'),
    Maintenance: location.pathname.startsWith('/maintenance'),
    Admin: location.pathname.startsWith('/admin'),
  });
  const [elapsedTime, setElapsedTime] = useState(0);

  // Query for active work session
  const { data: sessionData } = useQuery({
    queryKey: ['myActiveSession'],
    queryFn: () => workOrderService.getMyActiveSession(),
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  const activeSession = sessionData?.data;

  // Update elapsed time for active session
  useEffect(() => {
    if (activeSession?.isActive) {
      const startTime = new Date(activeSession.startedAt).getTime();
      const updateTimer = () => {
        const now = Date.now();
        setElapsedTime(Math.floor((now - startTime) / 1000));
      };
      updateTimer();
      const interval = setInterval(updateTimer, 1000);
      return () => clearInterval(interval);
    } else {
      setElapsedTime(0);
    }
  }, [activeSession]);

  const formatElapsedTime = (seconds: number) => {
    const hrs = Math.floor(seconds / 3600);
    const mins = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    return `${hrs.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  const toggleMenu = (menuName: string) => {
    setOpenMenus((prev) => ({ ...prev, [menuName]: !prev[menuName] }));
  };

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
                  <ListItemButton onClick={() => toggleMenu(item.text)}>
                    <ListItemIcon>{item.icon}</ListItemIcon>
                    <ListItemText primary={item.text} />
                    {openMenus[item.text] ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                  </ListItemButton>
                </ListItem>
                <Collapse in={openMenus[item.text]} timeout="auto" unmountOnExit>
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
          {activeSession?.isActive && (
            <Chip
              icon={<TimerIcon />}
              label={`Working: ${formatElapsedTime(elapsedTime)}`}
              color="success"
              onClick={() => navigate(`/maintenance/work-orders/${activeSession.workOrderId}`)}
              sx={{
                mr: 2,
                fontFamily: 'monospace',
                cursor: 'pointer',
                '&:hover': { bgcolor: 'success.dark' },
              }}
            />
          )}
          <IconButton color="inherit" onClick={toggleMode} sx={{ mr: 1 }}>
            {resolvedMode === 'dark' ? <Brightness7Icon /> : <Brightness4Icon />}
          </IconButton>
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
          height: 'calc(100vh - 64px)',
          overflow: 'auto',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <Outlet />
      </Box>
    </Box>
  );
};
