import { useState } from 'react';
import "../../../App.css";
import "./CateringDashboard.css";

interface Reservation {
    id: number;
    customerName: string;
    reservationStart: string;
    reservationEnd: string;
    status: string;
    services: number[];
    employeeId: number;
    notes?: string;
}

interface Payment {
    id: number;
    reservationId: number;
    amount: { amount: number; currency: string };
    method: string;
    status: string;
}

interface Employee {
    id: number;
    name: string;
    role: string;
}

interface StockItem {
    id: number;
    qtyOnHand: number;
}

function CateringMain(){
    const [activeScreen, setActiveScreen] = 
        useState<'dashboard' | 'reservations' | 'tables' | 'products' | 'inventory' | 'payments' | 'gift-cards' | 'new-reservation' | 'taxes' | 'quick-order' | 'settings' >('dashboard');
    
    const formatTime = (dateString: string): string => {
        return new Date(dateString).toLocaleDateString([], { hour: '2-digit', minute: '2-digit' });
    };
    
    return(
        <div className="content-box" id="dashboard">
            {/* Top Bar */}
            <div className="top-bar">
                <h1 className="title">SuperApp</h1>
                <div className="user-info">
                    <span>John Smith (Manager)</span>
                    <button className="nav-btn">Settings</button>
                </div>
            </div>

            {/* Navbar */}
            <div className="navbar">
                <button
                    className={`nav-btn ${activeScreen === "dashboard" ? "active" : ""}`}
                    onClick={() => setActiveScreen("dashboard")}
                >
                    <span>📊</span> Dashboard
                </button>
                
                <button
                    className={`nav-btn ${activeScreen === "reservations" ? "active" : ""}`}
                    onClick={() => setActiveScreen("reservations")}
                >
                    <span>📅</span> Reservations
                </button>

                <button
                    className={`nav-btn ${activeScreen === "tables" ? "active" : ""}`}
                    onClick={() => setActiveScreen("tables")}
                >
                    <span>🪑</span> Tables
                </button>
                
                <button 
                    className={`nav-btn ${activeScreen === "products" ? "active" : ""}`}
                    onClick={() => setActiveScreen("products")}
                >
                    <span>📋</span> Products
                </button>

                <button
                    className={`nav-btn ${activeScreen === "inventory" ? "active" : ""}`}
                    onClick={() => setActiveScreen("inventory")}
                >
                    <span>📦</span> Inventory
                </button>
                
                <button 
                    className={`nav-btn ${activeScreen === "payments" ? "active" : ""}`}
                    onClick={() => setActiveScreen("payments")}
                >
                    <span>💳</span> Payments
                </button>

                <button 
                    className={`nav-btn ${activeScreen === "gift-cards" ? "active" : ""}`}
                    onClick={() => setActiveScreen("gift-cards")}
                >
                    <span>🎁</span> Gift Cards
                </button>
            </div>

            {/* Screen Switch */}
            <div className="dashboard-container">
                {activeScreen === "dashboard" && (
                    <>
                        {/* Dashboard */}
                        <div className="action-bar">
                            <h2 className="section-title">Today's Overview</h2>
                            <div className="action-buttons">
                                <button className="btn btn-primary">
                                    <span>➕</span> New Reservation
                                </button>
                                <button className="btn btn-primary">
                                    <span>⚡</span> Quick Order
                                </button>
                            </div>
                        </div>
                        
                        <div className="stat-grid">
                            <div className="stat-card">
                                <div className="stat-number" id="today-reservations">0</div>
                                <div className="stat-label">Today's Reservations</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-number" id="today-revenue">€0</div>
                                <div className="stat-label">Today's Revenue</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-number" id="available-tables">0</div>
                                <div className="stat-label">Available Tables</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-number" id="low-stock-items">0</div>
                                <div className="stat-label">Low Stock Items</div>
                            </div>
                        </div>
                    </>
                )}
            </div>
        </div>
    )
}

export default CateringMain;