import { useState } from "react";
import "./App.css";
import { login } from "./frontapi/authApi";
import BeautyDashboard from "./pages/BeautyIndustry/BeautyDashboard/BeautyDashboard";
import CateringDashboard from "./pages/CateringIndustry/CateringDashboard/CateringDashboard";

function App() {
    const [selectedOption, setSelectedOption] = useState("");
    const [isLoggedIn, setIsLoggedIn] = useState(false);
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    const handleLogin = async () => {
        try {
            const result = await login(email, password);

            localStorage.setItem("token", result.token);

            setSelectedOption(
                result.businessType === "Beauty"
                    ? "beautyIndustry"
                    : "cateringIndustry"
            );

            setIsLoggedIn(true);
        } catch (err) {
            alert("Invalid email or password");
            console.error(err);
        }
    };


    // ✅ DASHBOARD GATE (SINGLE POINT OF TRUTH)
    if (isLoggedIn) {
        if (selectedOption === "beautyIndustry") return <BeautyDashboard />;
        if (selectedOption === "cateringIndustry") return <CateringDashboard />;
        return <div>Invalid business type</div>;
    }

    return (
        <div className="content-box">
            <div className="top-bar">
                <h1 className="title">SuperApp</h1>
            </div>

            <div className="login">
                <h1 className="login-text">Select Industry & Log In</h1>

                {/* ✅ INDUSTRY PICKER */}
                <select
                    value={selectedOption}
                    onChange={(e) => setSelectedOption(e.currentTarget.value)}
                    className="dropdown"
                >
                    <option value="">Select account type</option>
                    <option value="beautyIndustry">Beauty Industry</option>
                    <option value="cateringIndustry">Catering Industry</option>
                </select>

                {/* ✅ LOGIN FORM */}
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

export default App;
