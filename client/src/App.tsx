import { useEffect, useState } from "react";
import reactLogo from "./assets/react.svg";
import viteLogo from "/vite.svg";
import "./App.css";
import BeautyDashboard from "./pages/BeautyIndustry/BeautyDashboard/BeautyDashboard";
import CateringDashboard from "./pages/CateringIndustry/CateringDashboard/CateringDashboard";

function App() {
    const [count, setCount] = useState(0);
    const [apiMessage, setApiMessage] = useState("Loading from API...");
    const [selectedOption, setSelectedOption] = useState("");
    const [isLoggedIn, setIsLoggedIn] = useState(false);

    const handleLogin = () => {
        if(!selectedOption) {
            alert('Please select an option first!');
            return;
        }
        //simulating login for easier design at this time
        setIsLoggedIn(true);
    }
    
    if(isLoggedIn){
        switch(selectedOption){
            case "beautyIndustry":
                return <BeautyDashboard />;
            case "cateringIndustry":
                return <CateringDashboard />;
            default:
                return <div>Invalid business type</div>
        }
    }
    
    return(
        <div className="content-box">{}
            <div className="top-bar">
                <h1 className="title">SuperApp</h1>
            </div>
            <div className="login">
                <h1 className="login-text">Login</h1>
                <select
                    id = "role-select"
                    value = {selectedOption}
                    onChange = {(e) => setSelectedOption(e.currentTarget.value)}
                    className = "dropdown"
                >
                    <option value="">Select account type</option>
                    <option value="beautyIndustry">Beauty Industry</option>
                    <option value="cateringIndustry">Catering Industry</option>
                </select>
                <button onClick={handleLogin} className="login-btn">
                    Log in with Google
                </button>
                <p className="login-info">
                    You will be redirected to Google 0Auth for authentication.
                </p>
            </div>
        </div>
    );
}

export default App;
