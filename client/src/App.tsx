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

function App() {
    const [isLoggedIn, setIsLoggedIn] = useState(
        !!localStorage.getItem("token")
    );
    const [businessType, setBusinessType] = useState<string | null>(
        localStorage.getItem("businessType")
    );
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    const handleLogin = async () => {
        console.log("LOGIN CLICKED", email, password);

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

    // 🔴 LOGIN SCREEN
    if (!isLoggedIn) {
        return (
            <div className="content-box">
                <div className="top-bar">
                    <h1 className="title">SuperApp</h1>
                </div>

                <div className="login">
                    <h1 className="login-text">Log In</h1>

                    <input
                        className="dropdown"
                        placeholder="Email"
                        value={email}
                        onChange={(e) => setEmail(e.currentTarget.value)}
                    />

                    <input
                        className="dropdown"
                        placeholder="Password"
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.currentTarget.value)}
                    />

                    <button
                        type="button"
                        className="login-btn"
                        onClick={handleLogin}
                    >
                        Log In
                    </button>

                    <p className="login-info">
                        Enter your credentials to access your business.
                    </p>
                </div>
            </div>
        );
    }

    // 🟢 DASHBOARD SWITCH
    if (businessType === "Beauty") {
        return <BeautyDashboard />;
    }

    if (businessType === "Catering") {
        return <CateringDashboard />;
    }

    return <div>Unknown business type</div>;
}

export default App;
