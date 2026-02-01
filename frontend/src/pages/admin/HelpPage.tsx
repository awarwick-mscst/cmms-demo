import React, { useEffect, useRef } from 'react';
import { useLocation } from 'react-router-dom';
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  Card,
  CardContent,
  Divider,
  Link,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  Security as SecurityIcon,
  Settings as SettingsIcon,
  Code as CodeIcon,
} from '@mui/icons-material';

export const HelpPage: React.FC = () => {
  const location = useLocation();
  const ldapSectionRef = useRef<HTMLDivElement>(null);

  // Scroll to LDAP section if navigated with state
  useEffect(() => {
    const state = location.state as { section?: string } | null;
    if (state?.section === 'ldap' && ldapSectionRef.current) {
      ldapSectionRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [location.state]);

  return (
    <Box>
      <Typography variant="h5" gutterBottom>
        System Administration Help
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Configuration guides and documentation for system administrators.
      </Typography>

      {/* LDAP Configuration Section */}
      <Card ref={ldapSectionRef} sx={{ mb: 3 }}>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
            <SecurityIcon color="primary" />
            <Typography variant="h6">LDAP / Active Directory Authentication</Typography>
          </Box>

          <Typography variant="body2" paragraph>
            CMMS supports optional LDAP/Active Directory authentication, allowing users to log in
            with their Windows domain credentials. When enabled, users are automatically created
            in the system on first login and their roles are mapped from AD group memberships.
          </Typography>

          <Alert severity="info" sx={{ mb: 3 }}>
            LDAP configuration is managed through server-side configuration files for security.
            Contact your system administrator to enable or modify LDAP settings.
          </Alert>

          <Accordion defaultExpanded>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <SettingsIcon fontSize="small" />
                <Typography fontWeight="medium">Configuration File Location</Typography>
              </Box>
            </AccordionSummary>
            <AccordionDetails>
              <Typography variant="body2" paragraph>
                LDAP settings are configured in the API server's <code>appsettings.json</code> file
                or through environment variables. The configuration file is located at:
              </Typography>
              <Paper sx={{ p: 2, bgcolor: 'grey.100', fontFamily: 'monospace', fontSize: '0.875rem' }}>
                src/CMMS.API/appsettings.json
              </Paper>
              <Typography variant="body2" sx={{ mt: 2 }}>
                For production deployments, sensitive values like <code>ServiceAccountPassword</code>{' '}
                should be stored in environment variables or a secrets manager, not in the config file.
              </Typography>
            </AccordionDetails>
          </Accordion>

          <Accordion>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <CodeIcon fontSize="small" />
                <Typography fontWeight="medium">Configuration Reference</Typography>
              </Box>
            </AccordionSummary>
            <AccordionDetails>
              <Typography variant="body2" paragraph>
                Add or modify the <code>LdapSettings</code> section in your configuration:
              </Typography>
              <Paper
                sx={{
                  p: 2,
                  bgcolor: 'grey.900',
                  color: 'grey.100',
                  fontFamily: 'monospace',
                  fontSize: '0.75rem',
                  overflow: 'auto',
                  maxHeight: 400,
                }}
              >
                <pre style={{ margin: 0 }}>
{`{
  "LdapSettings": {
    "Enabled": true,
    "Server": "dc.example.com",
    "Port": 389,
    "UseSsl": false,
    "UseStartTls": true,
    "BaseDn": "DC=example,DC=com",
    "UserSearchBase": "OU=Users,DC=example,DC=com",
    "UserSearchFilter": "(sAMAccountName={0})",
    "Domain": "EXAMPLE",
    "ServiceAccountDn": "CN=CMMS Service,OU=ServiceAccounts,DC=example,DC=com",
    "ServiceAccountPassword": "",
    "ConnectionTimeout": 30,
    "AuthenticationMode": "Mixed",
    "AllowLocalFallback": true,
    "SyncUserAttributes": true,
    "AttributeMappings": {
      "Username": "sAMAccountName",
      "Email": "mail",
      "FirstName": "givenName",
      "LastName": "sn",
      "Phone": "telephoneNumber",
      "DisplayName": "displayName",
      "MemberOf": "memberOf"
    },
    "GroupMappings": [
      {
        "LdapGroup": "CN=CMMS-Admins,OU=Groups,DC=example,DC=com",
        "RoleName": "Administrator"
      },
      {
        "LdapGroup": "CN=CMMS-Technicians,OU=Groups,DC=example,DC=com",
        "RoleName": "Technician"
      }
    ],
    "DefaultRoles": ["Viewer"]
  }
}`}
                </pre>
              </Paper>
            </AccordionDetails>
          </Accordion>

          <Accordion>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography fontWeight="medium">Configuration Options Explained</Typography>
            </AccordionSummary>
            <AccordionDetails>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell><strong>Setting</strong></TableCell>
                      <TableCell><strong>Description</strong></TableCell>
                      <TableCell><strong>Default</strong></TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    <TableRow>
                      <TableCell><code>Enabled</code></TableCell>
                      <TableCell>Enable or disable LDAP authentication</TableCell>
                      <TableCell><code>false</code></TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>Server</code></TableCell>
                      <TableCell>LDAP server hostname or IP address</TableCell>
                      <TableCell>—</TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>Port</code></TableCell>
                      <TableCell>LDAP port (389 for LDAP, 636 for LDAPS)</TableCell>
                      <TableCell><code>389</code></TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>UseSsl</code></TableCell>
                      <TableCell>Use SSL/TLS (LDAPS) for connection</TableCell>
                      <TableCell><code>false</code></TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>UseStartTls</code></TableCell>
                      <TableCell>Use StartTLS to upgrade connection to TLS</TableCell>
                      <TableCell><code>true</code></TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>BaseDn</code></TableCell>
                      <TableCell>Base Distinguished Name for the directory</TableCell>
                      <TableCell>—</TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>UserSearchBase</code></TableCell>
                      <TableCell>OU where users are located</TableCell>
                      <TableCell>—</TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>UserSearchFilter</code></TableCell>
                      <TableCell>LDAP filter for finding users ({'{0}'} = username)</TableCell>
                      <TableCell><code>(sAMAccountName={'{0}'})</code></TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>Domain</code></TableCell>
                      <TableCell>Windows domain name (e.g., EXAMPLE)</TableCell>
                      <TableCell>—</TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>ServiceAccountDn</code></TableCell>
                      <TableCell>DN of service account for LDAP queries</TableCell>
                      <TableCell>—</TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>ServiceAccountPassword</code></TableCell>
                      <TableCell>Password for service account (use secrets in production!)</TableCell>
                      <TableCell>—</TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>AuthenticationMode</code></TableCell>
                      <TableCell>
                        <strong>LdapOnly</strong>: Only LDAP auth allowed<br />
                        <strong>LocalOnly</strong>: Only local passwords<br />
                        <strong>Mixed</strong>: Both methods available
                      </TableCell>
                      <TableCell><code>Mixed</code></TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>AllowLocalFallback</code></TableCell>
                      <TableCell>Fall back to local auth if LDAP fails (Mixed mode)</TableCell>
                      <TableCell><code>true</code></TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>SyncUserAttributes</code></TableCell>
                      <TableCell>Update user details from LDAP on each login</TableCell>
                      <TableCell><code>true</code></TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>GroupMappings</code></TableCell>
                      <TableCell>Map LDAP groups to application roles</TableCell>
                      <TableCell><code>[]</code></TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>DefaultRoles</code></TableCell>
                      <TableCell>Roles assigned if no group mappings match</TableCell>
                      <TableCell><code>["Viewer"]</code></TableCell>
                    </TableRow>
                  </TableBody>
                </Table>
              </TableContainer>
            </AccordionDetails>
          </Accordion>

          <Accordion>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography fontWeight="medium">Authentication Modes</Typography>
            </AccordionSummary>
            <AccordionDetails>
              <Typography variant="subtitle2" gutterBottom>
                LdapOnly Mode
              </Typography>
              <Typography variant="body2" paragraph>
                All users must authenticate against LDAP. Local passwords are ignored. Users are
                automatically created on first successful LDAP login.
              </Typography>

              <Typography variant="subtitle2" gutterBottom>
                LocalOnly Mode
              </Typography>
              <Typography variant="body2" paragraph>
                Standard local authentication only. LDAP is completely disabled. This is the default
                when <code>Enabled</code> is <code>false</code>.
              </Typography>

              <Typography variant="subtitle2" gutterBottom>
                Mixed Mode (Recommended)
              </Typography>
              <Typography variant="body2" paragraph>
                Users can authenticate via either LDAP or local passwords. Each user's{' '}
                <code>AuthenticationType</code> determines their method:
              </Typography>
              <ul>
                <li>
                  <Typography variant="body2">
                    <strong>Local</strong>: User authenticates with local password only
                  </Typography>
                </li>
                <li>
                  <Typography variant="body2">
                    <strong>Ldap</strong>: User authenticates with LDAP only
                  </Typography>
                </li>
                <li>
                  <Typography variant="body2">
                    <strong>Both</strong>: Tries LDAP first, falls back to local if{' '}
                    <code>AllowLocalFallback</code> is enabled
                  </Typography>
                </li>
              </ul>
            </AccordionDetails>
          </Accordion>

          <Accordion>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography fontWeight="medium">Setting Up Group Mappings</Typography>
            </AccordionSummary>
            <AccordionDetails>
              <Typography variant="body2" paragraph>
                Group mappings allow you to automatically assign application roles based on Active
                Directory group membership. You'll need the full Distinguished Name (DN) of each AD
                group.
              </Typography>

              <Typography variant="subtitle2" gutterBottom>
                Finding AD Group DNs
              </Typography>
              <Typography variant="body2" paragraph>
                In PowerShell on a domain-joined machine:
              </Typography>
              <Paper sx={{ p: 2, bgcolor: 'grey.100', fontFamily: 'monospace', fontSize: '0.75rem', mb: 2 }}>
                Get-ADGroup -Identity "CMMS-Admins" | Select-Object DistinguishedName
              </Paper>

              <Typography variant="subtitle2" gutterBottom>
                Example Mappings
              </Typography>
              <Paper sx={{ p: 2, bgcolor: 'grey.100', fontFamily: 'monospace', fontSize: '0.75rem' }}>
{`"GroupMappings": [
  {
    "LdapGroup": "CN=CMMS-Admins,OU=Groups,DC=example,DC=com",
    "RoleName": "Administrator"
  },
  {
    "LdapGroup": "CN=CMMS-Technicians,OU=Groups,DC=example,DC=com",
    "RoleName": "Technician"
  },
  {
    "LdapGroup": "CN=CMMS-InventoryManagers,OU=Groups,DC=example,DC=com",
    "RoleName": "Inventory Manager"
  }
]`}
              </Paper>
            </AccordionDetails>
          </Accordion>

          <Accordion>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography fontWeight="medium">Service Account Setup</Typography>
            </AccordionSummary>
            <AccordionDetails>
              <Typography variant="body2" paragraph>
                The LDAP service account is used to search for users in the directory before
                authentication. It should have minimal read-only permissions.
              </Typography>

              <Typography variant="subtitle2" gutterBottom>
                Required Permissions
              </Typography>
              <ul>
                <li>
                  <Typography variant="body2">Read access to user objects in the UserSearchBase OU</Typography>
                </li>
                <li>
                  <Typography variant="body2">
                    Read access to group objects (for group membership queries)
                  </Typography>
                </li>
              </ul>

              <Alert severity="warning" sx={{ mt: 2 }}>
                <strong>Security Best Practice:</strong> In production, store the service account
                password in environment variables or a secrets manager:
                <Paper sx={{ p: 1, mt: 1, bgcolor: 'grey.100', fontFamily: 'monospace', fontSize: '0.75rem' }}>
                  set LdapSettings__ServiceAccountPassword=YourSecurePassword
                </Paper>
              </Alert>
            </AccordionDetails>
          </Accordion>

          <Accordion>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography fontWeight="medium">Testing Your Configuration</Typography>
            </AccordionSummary>
            <AccordionDetails>
              <Typography variant="body2" paragraph>
                After configuring LDAP, restart the API server and use the admin endpoints to test:
              </Typography>

              <Typography variant="subtitle2" gutterBottom>
                1. Test Connection
              </Typography>
              <Typography variant="body2" paragraph>
                Go to <strong>Admin → Users</strong> and expand the LDAP status card. Click{' '}
                <strong>"Test Connection"</strong> to verify the server is reachable and the
                service account credentials are valid.
              </Typography>

              <Typography variant="subtitle2" gutterBottom>
                2. Test User Login
              </Typography>
              <Typography variant="body2" paragraph>
                Try logging in with an AD user account. On successful authentication:
              </Typography>
              <ul>
                <li>
                  <Typography variant="body2">A local user record will be created automatically</Typography>
                </li>
                <li>
                  <Typography variant="body2">Roles will be assigned based on AD group membership</Typography>
                </li>
                <li>
                  <Typography variant="body2">User attributes (name, email, phone) will be synced from AD</Typography>
                </li>
              </ul>

              <Typography variant="subtitle2" gutterBottom sx={{ mt: 2 }}>
                API Endpoints for Testing
              </Typography>
              <TableContainer component={Paper} sx={{ mt: 1 }}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell><strong>Endpoint</strong></TableCell>
                      <TableCell><strong>Description</strong></TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    <TableRow>
                      <TableCell><code>GET /api/v1/admin/ldap/status</code></TableCell>
                      <TableCell>View current LDAP configuration status</TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>POST /api/v1/admin/ldap/test-connection</code></TableCell>
                      <TableCell>Test LDAP server connectivity</TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>POST /api/v1/admin/ldap/test-user</code></TableCell>
                      <TableCell>Test authentication for a specific user</TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell><code>POST /api/v1/admin/ldap/lookup-user</code></TableCell>
                      <TableCell>Look up user info in LDAP (without auth)</TableCell>
                    </TableRow>
                  </TableBody>
                </Table>
              </TableContainer>
            </AccordionDetails>
          </Accordion>

          <Accordion>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography fontWeight="medium">Troubleshooting</Typography>
            </AccordionSummary>
            <AccordionDetails>
              <Typography variant="subtitle2" gutterBottom>
                Connection Fails
              </Typography>
              <ul>
                <li>
                  <Typography variant="body2">
                    Verify the server hostname and port are correct
                  </Typography>
                </li>
                <li>
                  <Typography variant="body2">
                    Check firewall rules allow traffic to the LDAP port (389 or 636)
                  </Typography>
                </li>
                <li>
                  <Typography variant="body2">
                    If using SSL/TLS, ensure certificates are trusted
                  </Typography>
                </li>
              </ul>

              <Typography variant="subtitle2" gutterBottom sx={{ mt: 2 }}>
                Authentication Fails
              </Typography>
              <ul>
                <li>
                  <Typography variant="body2">
                    Verify the service account DN and password are correct
                  </Typography>
                </li>
                <li>
                  <Typography variant="body2">
                    Check the UserSearchBase contains the user accounts
                  </Typography>
                </li>
                <li>
                  <Typography variant="body2">
                    Ensure the UserSearchFilter is correct for your directory
                  </Typography>
                </li>
              </ul>

              <Typography variant="subtitle2" gutterBottom sx={{ mt: 2 }}>
                Users Not Getting Correct Roles
              </Typography>
              <ul>
                <li>
                  <Typography variant="body2">
                    Verify group DNs in GroupMappings exactly match AD
                  </Typography>
                </li>
                <li>
                  <Typography variant="body2">
                    Check that the role names match roles defined in the application
                  </Typography>
                </li>
                <li>
                  <Typography variant="body2">
                    Ensure users are direct members of the mapped groups (not nested)
                  </Typography>
                </li>
              </ul>

              <Typography variant="subtitle2" gutterBottom sx={{ mt: 2 }}>
                Check Server Logs
              </Typography>
              <Typography variant="body2">
                Detailed LDAP errors are logged on the server. Check the log files at:
              </Typography>
              <Paper sx={{ p: 1, mt: 1, bgcolor: 'grey.100', fontFamily: 'monospace', fontSize: '0.75rem' }}>
                logs/cmms-YYYY-MM-DD.log
              </Paper>
            </AccordionDetails>
          </Accordion>

          <Divider sx={{ my: 3 }} />

          <Typography variant="body2" color="text.secondary">
            For additional assistance, contact your system administrator or refer to the{' '}
            <Link href="https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/get-started/virtual-dc/active-directory-domain-services-overview" target="_blank" rel="noopener">
              Active Directory documentation
            </Link>
            .
          </Typography>
        </CardContent>
      </Card>
    </Box>
  );
};
