import { useState } from "react";
import "./App.css";
import { login } from "./frontapi/authApi";
import BeautyDashboard from "./pages/BeautyIndustry/BeautyDashboard/BeautyDashboard";
import CateringDashboard from "./pages/CateringIndustry/CateringDashboard/CateringDashboard";

function App() {
    const [isLoggedIn, setIsLoggedIn] = useState(false);
    const [businessType, setBusinessType] = useState<string | null>(null);
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    const handleLogin = async () => {
        try {
            const result = await login(email, password);

            localStorage.setItem("token", result.token);

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

                    <button className="login-btn" onClick={handleLogin}>
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
