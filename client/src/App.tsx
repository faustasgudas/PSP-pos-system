import { useEffect, useState } from "react";
import reactLogo from "./assets/react.svg";
import viteLogo from "/vite.svg";
import "./App.css";

function App() {
    const [count, setCount] = useState(0);
    const [apiMessage, setApiMessage] = useState("Loading from API...");

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
    );
}

export default App;
