import { useState } from "react";
import "./App.css";
import { login } from "./frontapi/authApi";
import BeautyDashboard from "./pages/BeautyIndustry/BeautyDashboard/BeautyDashboard";
import CateringDashboard from "./pages/CateringIndustry/CateringDashboard/CateringDashboard";
import { jwtDecode } from "jwt-decode";

interface JwtPayload {
    businessId: string;
    employeeId: string;
    role: string;
    email: string;
}

export default function MainApp() {
    const [isLoggedIn, setIsLoggedIn] = useState(!!localStorage.getItem("token"));
    const [businessType, setBusinessType] = useState<string | null>(
        localStorage.getItem("businessType")
    );
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    const handleLogin = async () => {
        try {
            const result = await login(email, password);
            const decoded = jwtDecode<JwtPayload>(result.token);

            localStorage.setItem("token", result.token);
            localStorage.setItem("businessId", decoded.businessId);
            localStorage.setItem("employeeId", decoded.employeeId);
            localStorage.setItem("role", decoded.role);
            localStorage.setItem("businessType", result.businessType);

            setBusinessType(result.businessType);
            setIsLoggedIn(true);
        } catch (err) {
            alert("Invalid email or password");
            console.error(err);
        }
    };

    if (!isLoggedIn) {
        return (
            <div className="content-box login-shell">
                <div className="top-bar">
                    <h1 className="title">SuperApp</h1>
                </div>

                <div className="login">
                    <h1 className="login-text">Welcome back</h1>
                    <p className="login-info">Sign in to access your business dashboard.</p>

                    <div className="login-form">
                        <label className="login-label">Email</label>
                        <input
                            className="dropdown"
                            placeholder="you@company.com"
                            value={email}
                            onChange={(e) => setEmail(e.currentTarget.value)}
                            autoComplete="username"
                        />

                        <label className="login-label">Password</label>
                        <input
                            className="dropdown"
                            placeholder="••••••••"
                            type="password"
                            value={password}
                            onChange={(e) => setPassword(e.currentTarget.value)}
                            autoComplete="current-password"
                            onKeyDown={(e) => e.key === "Enter" && handleLogin()}
                        />
                    </div>

                    <button type="button" className="login-btn" onClick={handleLogin}>
                        Log In
                    </button>
                </div>
            </div>
        );
    }

    if (businessType === "Beauty") return <BeautyDashboard />;
    if (businessType === "Catering") return <CateringDashboard />;

    return <div>Unknown business type</div>;
}
