import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { getAuthToken, setAuthToken, removeAuthToken, getBusinessId, setBusinessId, removeBusinessId } from '../config/api';
import * as authApi from '../services/authService';

interface AuthContextType {
    isAuthenticated: boolean;
    businessId: number | null;
    businessType: string | null;
    login: (email: string, password: string) => Promise<void>;
    logout: () => void;
    loading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};

interface AuthProviderProps {
    children: ReactNode;
}

export const AuthProvider = ({ children }: AuthProviderProps) => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [businessId, setBusinessIdState] = useState<number | null>(null);
    const [businessType, setBusinessType] = useState<string | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        // Check if user is already authenticated on mount
        const token = getAuthToken();
        const storedBusinessId = getBusinessId();
        
        if (token && storedBusinessId) {
            setIsAuthenticated(true);
            setBusinessIdState(storedBusinessId);
            // Try to get business type from token or fetch it
            // For now, we'll set it from localStorage if available
            const storedBusinessType = localStorage.getItem('businessType');
            if (storedBusinessType) {
                setBusinessType(storedBusinessType);
            }
        }
        setLoading(false);
    }, []);

    const login = async (email: string, password: string) => {
        try {
            const response = await authApi.login(email, password);
            setAuthToken(response.token);
            setIsAuthenticated(true);
            
            if (response.businessType) {
                setBusinessType(response.businessType);
                localStorage.setItem('businessType', response.businessType);
            }
            
            // Fetch business info to get businessId
            // The token is now set, so getCurrentBusiness will use it automatically
            try {
                const business = await authApi.getCurrentBusiness();
                if (business && business.length > 0) {
                    const bizId = business[0].businessId;
                    setBusinessId(bizId);
                    setBusinessIdState(bizId);
                } else {
                    throw new Error('Business information not found');
                }
            } catch (err) {
                console.error('Failed to fetch business info:', err);
                // If business fetch fails, we should still allow login
                // but the user won't be able to use features that require businessId
                // This is better than blocking login entirely
            }
        } catch (error) {
            throw error;
        }
    };

    const logout = () => {
        removeAuthToken();
        removeBusinessId();
        localStorage.removeItem('businessType');
        setIsAuthenticated(false);
        setBusinessIdState(null);
        setBusinessType(null);
    };

    return (
        <AuthContext.Provider value={{ isAuthenticated, businessId, businessType, login, logout, loading }}>
            {children}
        </AuthContext.Provider>
    );
};
