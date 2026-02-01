import api from './api';

export interface LdapStatusResponse {
  enabled: boolean;
  server: string | null;
  port: number;
  useSsl: boolean;
  useStartTls: boolean;
  authenticationMode: string | null;
  allowLocalFallback: boolean;
  syncUserAttributes: boolean;
  groupMappingsCount: number;
  defaultRoles: string[];
  configurationWarnings: string[];
}

export interface LdapTestConnectionResponse {
  success: boolean;
  message: string | null;
  serverInfo: string | null;
  responseTimeMs: number;
  testedAt: string;
}

export interface LdapTestUserRequest {
  username: string;
  password: string;
}

export interface LdapUserInfoDto {
  distinguishedName: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  phone: string | null;
  displayName: string | null;
}

export interface LdapTestUserResponse {
  success: boolean;
  message: string | null;
  userInfo: LdapUserInfoDto | null;
  groups: string[];
  mappedRoles: string[];
  testedAt: string;
}

export interface LdapLookupUserRequest {
  username: string;
}

export interface LdapLookupUserResponse {
  found: boolean;
  message: string | null;
  userInfo: LdapUserInfoDto | null;
  groups: string[];
  existsLocally: boolean;
  localUserId: number | null;
}

export interface LdapGroupMapping {
  ldapGroup: string;
  roleName: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string | null;
  errors: string[];
}

export const ldapService = {
  /**
   * Get current LDAP configuration status
   */
  getStatus: async (): Promise<ApiResponse<LdapStatusResponse>> => {
    const response = await api.get<ApiResponse<LdapStatusResponse>>('/admin/ldap/status');
    return response.data;
  },

  /**
   * Test LDAP server connection
   */
  testConnection: async (): Promise<ApiResponse<LdapTestConnectionResponse>> => {
    const response = await api.post<ApiResponse<LdapTestConnectionResponse>>('/admin/ldap/test-connection');
    return response.data;
  },

  /**
   * Test LDAP user authentication
   */
  testUser: async (request: LdapTestUserRequest): Promise<ApiResponse<LdapTestUserResponse>> => {
    const response = await api.post<ApiResponse<LdapTestUserResponse>>('/admin/ldap/test-user', request);
    return response.data;
  },

  /**
   * Look up a user in LDAP
   */
  lookupUser: async (request: LdapLookupUserRequest): Promise<ApiResponse<LdapLookupUserResponse>> => {
    const response = await api.post<ApiResponse<LdapLookupUserResponse>>('/admin/ldap/lookup-user', request);
    return response.data;
  },

  /**
   * Get configured group mappings
   */
  getGroupMappings: async (): Promise<ApiResponse<LdapGroupMapping[]>> => {
    const response = await api.get<ApiResponse<LdapGroupMapping[]>>('/admin/ldap/group-mappings');
    return response.data;
  },
};
