import { useEffect, useState } from "react";
import reactLogo from "./assets/react.svg";
import viteLogo from "/vite.svg";
import "./App.css";
import MainBI from "./pages/BeautyIndustry/MainBI/BeautyMain";
import MainCI from "./pages/CateringIndustry/MainCI/CateringMain";

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
                return <MainBI />;
            case "cateringIndustry":
                return <MainCI />;
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
    

    /*
        useEffect(() => {
            fetch("https://localhost:44317/api/hello")
                .then(async (res) => {
                    if (!res.ok) throw new Error(`Status ${res.status}`);
                    const text = await res.text();   // 👈 read plain text, not JSON
                    setApiMessage(text);
                })
                .catch((err) => {
                    console.error("API error:", err);
                    setApiMessage("Error talking to API");
                });
        }, []);
    
        return (
            <>
                <div>
                    <a href="https://vite.dev" target="_blank">
                        <img src={viteLogo} className="logo" alt="Vite logo" />
                    </a>
                    <a href="https://react.dev" target="_blank">
                        <img src={reactLogo} className="logo react" alt="React logo" />
                    </a>
                </div>
    
                <h1>Vite + React + .NET API</h1>
    
                <div className="card">
                    <button onClick={() => setCount((count) => count + 1)}>
                        count is {count}
                    </button>
                    <p>Edit <code>src/App.tsx</code> and save to test HMR</p>
                </div>
    
                <div className="card">
                    <h2>API test</h2>
                    <p>{apiMessage}</p>
                </div>
    
                <p className="read-the-docs">
                    If this shows "Hello from .NET API", your frontend ↔ backend connection works 🎉
                </p>
            </>
        );*/
}

export default App;
