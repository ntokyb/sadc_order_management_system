import axios from 'axios';

export const AUTH_TOKEN_KEY = 'authToken';
export const AUTH_USERNAME_KEY = 'authUsername';
export const AUTH_ROLES_KEY = 'authRoles';

const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:8080';

export const apiClient = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem(AUTH_TOKEN_KEY);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      window.dispatchEvent(new CustomEvent('auth:unauthorized'));
    }
    return Promise.reject(error);
  },
);

export interface AuthSession {
  token: string;
  username: string;
  roles: string[];
}

export function getAuthToken(): string | null {
  return localStorage.getItem(AUTH_TOKEN_KEY);
}

export function getAuthUsername(): string | null {
  return localStorage.getItem(AUTH_USERNAME_KEY);
}

export function getAuthRoles(): string[] {
  const raw = localStorage.getItem(AUTH_ROLES_KEY);
  if (!raw) return [];
  try {
    const parsed = JSON.parse(raw) as unknown;
    return Array.isArray(parsed) ? parsed.filter((r): r is string => typeof r === 'string') : [];
  } catch {
    return [];
  }
}

export function isOrderAdmin(): boolean {
  return getAuthRoles().includes('OrderAdmin');
}

export function setAuthSession(session: AuthSession): void {
  localStorage.setItem(AUTH_TOKEN_KEY, session.token);
  localStorage.setItem(AUTH_USERNAME_KEY, session.username);
  localStorage.setItem(AUTH_ROLES_KEY, JSON.stringify(session.roles));
}

export function clearAuthSession(): void {
  localStorage.removeItem(AUTH_TOKEN_KEY);
  localStorage.removeItem(AUTH_USERNAME_KEY);
  localStorage.removeItem(AUTH_ROLES_KEY);
}

export function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const payload = error.response?.data as { error?: string } | undefined;
    return payload?.error ?? error.message;
  }
  return error instanceof Error ? error.message : 'Unknown error';
}
