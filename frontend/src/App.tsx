import React, { useMemo } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Provider } from 'react-redux';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';

import { store } from './store';
import { ThemeContextProvider } from './contexts/ThemeContext';
import { LicenseProvider } from './contexts/LicenseContext';
import { useThemeMode } from './hooks/useThemeMode';
import { Layout } from './components/common/Layout';
import { ProtectedRoute } from './components/common/ProtectedRoute';
import { LoginPage } from './components/auth/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { AssetsPage } from './pages/AssetsPage';
import { AssetDetailPage } from './pages/AssetDetailPage';
import { AssetFormPage } from './pages/AssetFormPage';
import { CategoriesPage } from './pages/CategoriesPage';
import { LocationsPage } from './pages/LocationsPage';
import {
  PartsPage,
  PartFormPage,
  PartDetailPage,
  SuppliersPage,
  PartCategoriesPage,
  StorageLocationsPage,
  ReceiveInventoryPage,
} from './pages/inventory';
import { WorkOrdersPage } from './pages/maintenance/WorkOrdersPage';
import { WorkOrderFormPage } from './pages/maintenance/WorkOrderFormPage';
import { WorkOrderDetailPage } from './pages/maintenance/WorkOrderDetailPage';
import { PreventiveMaintenancePage } from './pages/maintenance/PreventiveMaintenancePage';
import { PMScheduleFormPage } from './pages/maintenance/PMScheduleFormPage';
import { MyWorkOrdersPage } from './pages/maintenance/MyWorkOrdersPage';
import { UsersPage } from './pages/admin/UsersPage';
import { UserFormPage } from './pages/admin/UserFormPage';
import { HelpPage } from './pages/admin/HelpPage';
import { PrintersPage } from './pages/admin/PrintersPage';
import { LabelTemplatesPage } from './pages/admin/LabelTemplatesPage';
import { LabelTemplateFormPage } from './pages/admin/LabelTemplateFormPage';
import { TaskTemplatesPage } from './pages/admin/TaskTemplatesPage';
import { TaskTemplateFormPage } from './pages/admin/TaskTemplateFormPage';
import { BackupPage } from './pages/admin/BackupPage';
import { DatabaseConfigPage } from './pages/admin/DatabaseConfigPage';
import { IntegrationSettingsPage } from './pages/admin/IntegrationSettingsPage';
import { NotificationQueuePage } from './pages/admin/NotificationQueuePage';
import { NotificationPreferencesPage } from './pages/settings/NotificationPreferencesPage';
import { LicensePage } from './pages/admin/LicensePage';
import {
  ReportsPage,
  ReorderReportPage,
  OverdueMaintenancePage,
  MaintenancePerformedPage,
  InventoryValuationPage,
  PMCompliancePage,
  StockMovementPage,
  WorkOrderSummaryPage,
} from './pages/reports';
import { ScannerPage } from './pages/ScannerPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      retry: 1,
    },
  },
});

const getTheme = (mode: 'light' | 'dark') =>
  createTheme({
    palette: {
      mode,
      primary: {
        main: '#1976d2',
      },
      secondary: {
        main: '#dc004e',
      },
      ...(mode === 'light'
        ? {
            background: {
              default: '#f5f5f5',
            },
          }
        : {}),
    },
    typography: {
      fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
    },
  });

const ThemedApp: React.FC = () => {
  const { resolvedMode } = useThemeMode();
  const theme = useMemo(() => getTheme(resolvedMode), [resolvedMode]);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <BrowserRouter>
        <LicenseProvider>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route
                path="/"
                element={
                  <ProtectedRoute>
                    <Layout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<DashboardPage />} />
                <Route path="assets" element={<AssetsPage />} />
                <Route path="assets/new" element={<AssetFormPage />} />
                <Route path="assets/:id" element={<AssetDetailPage />} />
                <Route path="assets/:id/edit" element={<AssetFormPage />} />
                <Route path="categories" element={<CategoriesPage />} />
                <Route path="locations" element={<LocationsPage />} />
                <Route path="scanner" element={<ScannerPage />} />

                {/* Inventory Routes */}
                <Route path="inventory/receive" element={<ReceiveInventoryPage />} />
                <Route path="inventory/parts" element={<PartsPage />} />
                <Route path="inventory/parts/new" element={<PartFormPage />} />
                <Route path="inventory/parts/:id" element={<PartDetailPage />} />
                <Route path="inventory/parts/:id/edit" element={<PartFormPage />} />
                <Route path="inventory/suppliers" element={<SuppliersPage />} />
                <Route path="inventory/categories" element={<PartCategoriesPage />} />
                <Route path="inventory/locations" element={<StorageLocationsPage />} />

                {/* Maintenance Routes */}
                <Route path="maintenance/my-work" element={<MyWorkOrdersPage />} />
                <Route path="maintenance/work-orders" element={<WorkOrdersPage />} />
                <Route path="maintenance/work-orders/new" element={<WorkOrderFormPage />} />
                <Route path="maintenance/work-orders/:id" element={<WorkOrderDetailPage />} />
                <Route path="maintenance/work-orders/:id/edit" element={<WorkOrderFormPage />} />
                <Route path="maintenance/pm-schedules" element={<PreventiveMaintenancePage />} />
                <Route path="maintenance/pm-schedules/new" element={<PMScheduleFormPage />} />
                <Route path="maintenance/pm-schedules/:id/edit" element={<PMScheduleFormPage />} />

                {/* Reports Routes */}
                <Route path="reports" element={<ReportsPage />} />
                <Route path="reports/reorder" element={<ReorderReportPage />} />
                <Route path="reports/inventory-valuation" element={<InventoryValuationPage />} />
                <Route path="reports/stock-movement" element={<StockMovementPage />} />
                <Route path="reports/overdue-maintenance" element={<OverdueMaintenancePage />} />
                <Route path="reports/maintenance-performed" element={<MaintenancePerformedPage />} />
                <Route path="reports/pm-compliance" element={<PMCompliancePage />} />
                <Route path="reports/work-order-summary" element={<WorkOrderSummaryPage />} />

                {/* Admin Routes */}
                <Route path="admin/users" element={<UsersPage />} />
                <Route path="admin/users/new" element={<UserFormPage />} />
                <Route path="admin/users/:id/edit" element={<UserFormPage />} />
                <Route path="admin/task-templates" element={<TaskTemplatesPage />} />
                <Route path="admin/task-templates/new" element={<TaskTemplateFormPage />} />
                <Route path="admin/task-templates/:id/edit" element={<TaskTemplateFormPage />} />
                <Route path="admin/printers" element={<PrintersPage />} />
                <Route path="admin/label-templates" element={<LabelTemplatesPage />} />
                <Route path="admin/label-templates/new" element={<LabelTemplateFormPage />} />
                <Route path="admin/label-templates/:id/edit" element={<LabelTemplateFormPage />} />
                <Route path="admin/help" element={<HelpPage />} />
                <Route path="admin/backup" element={<BackupPage />} />
                <Route path="admin/database" element={<DatabaseConfigPage />} />
                <Route path="admin/integrations" element={<IntegrationSettingsPage />} />
                <Route path="admin/notification-queue" element={<NotificationQueuePage />} />
                <Route path="admin/license" element={<LicensePage />} />

                {/* Settings Routes */}
                <Route path="settings/notifications" element={<NotificationPreferencesPage />} />
              </Route>
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
        </LicenseProvider>
          </BrowserRouter>
    </ThemeProvider>
  );
};

const App: React.FC = () => {
  return (
    <Provider store={store}>
      <QueryClientProvider client={queryClient}>
        <ThemeContextProvider>
          <ThemedApp />
        </ThemeContextProvider>
      </QueryClientProvider>
    </Provider>
  );
};

export default App;
