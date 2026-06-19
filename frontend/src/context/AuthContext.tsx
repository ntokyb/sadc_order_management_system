import axios from 'axios';
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from 'react';
import { useNavigate } from 'react-router-dom';
import { AUTH_ROLES_KEY, AUTH_TOKEN_KEY, AUTH_USERNAME_KEY } from '../api/client';

export interface AuthUser {
  username: string;
  roles: string[];
  isAdmin: boolean;
}

export interface AuthContextValue {
  user: AuthUser | null;
  token: string | null;
  isLoading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue>(null!);

function readStoredUser(): AuthUser | null {
  const username = localStorage.getItem(AUTH_USERNAME_KEY);
  const rawRoles = localStorage.getItem(AUTH_ROLES_KEY);
  if (!username) return null;

  try {
    const roles = JSON.parse(rawRoles ?? '[]') as unknown;
    const roleList = Array.isArray(roles)
      ? roles.filter((role): role is string => typeof role === 'string')
      : [];
    return {
      username,
      roles: roleList,
      isAdmin: roleList.includes('OrderAdmin'),
    };
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate();
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(AUTH_TOKEN_KEY));
  const [user, setUser] = useState<AuthUser | null>(() => readStoredUser());
  const [isLoading, setIsLoading] = useState(false);

  const logout = useCallback(() => {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    localStorage.removeItem(AUTH_USERNAME_KEY);
    localStorage.removeItem(AUTH_ROLES_KEY);
    setToken(null);
    setUser(null);
    navigate('/login', { replace: true });
  }, [navigate]);

  useEffect(() => {
    const handler = () => logout();
    window.addEventListener('auth:unauthorized', handler);
    return () => window.removeEventListener('auth:unauthorized', handler);
  }, [logout]);

  const login = async (username: string, password: string) => {
    setIsLoading(true);
    try {
      const { data } = await axios.post<{
        token: string;
        username: string;
        roles: string[];
      }>(
        `${import.meta.env.VITE_API_URL ?? 'http://localhost:8080'}/api/dev/login`,
        { username, password },
      );

      localStorage.setItem(AUTH_TOKEN_KEY, data.token);
      localStorage.setItem(AUTH_USERNAME_KEY, data.username);
      localStorage.setItem(AUTH_ROLES_KEY, JSON.stringify(data.roles));
      setToken(data.token);
      setUser({
        username: data.username,
        roles: data.roles,
        isAdmin: data.roles.includes('OrderAdmin'),
      });
      navigate('/customers', { replace: true });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AuthContext.Provider value={{ user, token, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  return useContext(AuthContext);
}
