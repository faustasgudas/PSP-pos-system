import { useState } from "react";
import "./App.css";
import BeautyDashboard from "./pages/BeautyIndustry/BeautyDashboard/BeautyDashboard";
import CateringDashboard from "./pages/CateringIndustry/CateringDashboard/CateringDashboard";
import { useAuth } from "./contexts/AuthContext";

function App() {
    const { isAuthenticated, businessType, login, logout, loading } = useAuth();
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [loginError, setLoginError] = useState("");
    const [isLoggingIn, setIsLoggingIn] = useState(false);

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        setLoginError("");
        
        if (!email || !password) {
            setLoginError("Please enter both email and password");
            return;
        }

        setIsLoggingIn(true);
        try {
            await login(email, password);
            // Clear form on success
            setEmail("");
            setPassword("");
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : "Login failed. Please try again.";
            setLoginError(errorMessage);
            // Keep credentials in form so user can retry
        } finally {
            setIsLoggingIn(false);
        }
    };

    if (loading) {
        return (
            <div className="content-box">
                <div className="top-bar">
                    <h1 className="title">SuperApp</h1>
                </div>
                <div className="login">
                    <p>Loading...</p>
                </div>
            </div>
        );
    }

    if (isAuthenticated) {
        // Route to appropriate dashboard based on business type
        if (businessType === "Beauty" || businessType === "beautyIndustry") {
            return <BeautyDashboard />;
        } else if (businessType === "Catering" || businessType === "cateringIndustry") {
            return <CateringDashboard />;
        } else {
            // Default to Beauty if business type is not set
            return <BeautyDashboard />;
        }
    }

    return (
        <div className="content-box">
            <div className="top-bar">
                <h1 className="title">SuperApp</h1>
            </div>
            <div className="login">
                <h1 className="login-text">Login</h1>
                <form onSubmit={handleLogin}>
                    <div style={{ marginBottom: "1rem" }}>
                        <input
                            type="email"
                            placeholder="Email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            className="dropdown"
                            style={{ width: "100%", padding: "0.5rem", marginBottom: "0.5rem" }}
                            disabled={isLoggingIn}
                        />
                    </div>
                    <div style={{ marginBottom: "1rem" }}>
                        <input
                            type="password"
                            placeholder="Password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            className="dropdown"
                            style={{ width: "100%", padding: "0.5rem", marginBottom: "0.5rem" }}
                            disabled={isLoggingIn}
                        />
                    </div>
                    {loginError && (
                        <div style={{ color: "red", marginBottom: "1rem", fontSize: "0.9rem" }}>
                            {loginError}
                        </div>
                    )}
                    <button 
                        type="submit" 
                        className="login-btn"
                        disabled={isLoggingIn}
                    >
                        {isLoggingIn ? "Logging in..." : "Log In"}
                    </button>
                </form>
                <p className="login-info">
                    Enter your email and password to access your account.
                </p>
            </div>
        </div>
    );
}

export default App;
