import { useState } from 'react';
import "../../../App.css";
import "./CateringDashboard.css";

import CateringEmployees from "../CateringEmployees/CateringEmployees";
import CateringGiftCards from "../CateringGiftCards/CateringGiftCards";
import CateringInventory from "../CateringInventory/CateringInventory";
import CateringPayments from "../CateringPayments/CateringPayments";
import CateringProducts from "../CateringProducts/CateringProducts";
import CateringReservations from "../CateringReservations/CateringReservations";
import CateringSettings from "../CateringSettings/CateringSettings";
import CateringTables from "../CateringTables/CateringTables";

type Screen =
    | "dashboard"
    | "gift-cards"
    | "inventory"
    | "payments"
    | "products"
    | "reservations"
    | "employees"
    | "settings"
    | "tables";

interface Reservation {
    id: number;
    customerName: string;
    customerPhone: string;
    customerEmail: string;
    reservationStart: string;
    reservationEnd: string;
    status: string;
    employeeId: number;
    notes?: string;
}

interface Payment {
    id: number;
    reservationId: number;
    amount: { amount: number; currency: string} ;
    method: string;
    status: string;
}

interface Product {
    id: number;
    name: string;
    basePrice: {amount: number; currency: string};
}

interface Table {
    id: number;
    seats: number;
    status: string;
}

interface Employee {
    id: number;
    name: string;
    role: string;
    status: string;
}

interface StockItem {
    id: number;
    name: string;
    qtyOnHand: number;
    unit: string;
}

interface GiftCard {
    id: number;
    code: string;
    balance: { amount: number; currency: string };
    status: string;
}

function CateringMain(){
    const [activeScreen, setActiveScreen] = useState<Screen>("dashboard");
    
    const [reservations] = useState<Reservation[]>([]);
    const [payments] = useState<Payment[]>([]);
    const [products] = useState<Product[]>([]);
    const [tables] = useState<Table[]>([]);
    const [employees] = useState<Employee[]>([]);
    const [stockItems] = useState<StockItem[]>([]);
    const [giftCards] = useState<GiftCard[]>([]);
    
    const todayRevenue = payments.reduce((sum, p) => sum + p.amount.amount, 0);
    const availableTables = tables.filter(table => table.status = "Available").length;
    const activeEmployees = employees.filter(employee => employee.status = "Active").length;
    const lowStockItems = stockItems.filter(item => item.qtyOnHand < 5).length;
    
    return(
        <div className="content-box" id="dashboard">
            {/* Top Bar */}
            <div className="top-bar">
                <h1 className="title">SuperApp</h1>
                <div className="user-info">
                    <span>John Smith (Manager)</span>
                    <button 
                        className="nav-btn"
                        onClick={() => setActiveScreen("settings")}
                    >
                        <span>⚙️</span> Settings
                    </button>
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
                    className={`nav-btn ${activeScreen === "employees" ? "active" : ""}`}
                    onClick={() => setActiveScreen("employees")}
                >
                    <span>👥</span> Employees
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
                                <button 
                                    className="btn btn-primary"
                                >
                                    <span>➕</span> New Reservation
                                </button>
                                <button 
                                    className="btn btn-primary"
                                >
                                    <span>⚡</span> Quick Order
                                </button>
                            </div>
                        </div>
                        
                        <div className="stat-grid">
                            <div className="stat-card">
                                <div className="stat-number" id="today-revenue">€{todayRevenue}</div>
                                <div className="stat-label">Today's Revenue</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-number" id="available-tables">{availableTables}</div>
                                <div className="stat-label">Available Tables</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-number" id="active-employees">{activeEmployees}</div>
                                <div className="stat-label">Active Employees</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-number" id="low-stock-items">{lowStockItems}</div>
                                <div className="stat-label">Low Stock Items</div>
                            </div>
                        </div>
                    </>
                )}
                {activeScreen === "reservations" && (
                    <CateringReservations
                        reservations={reservations}
                        employees={employees}
                        tables={tables}
                    />
                )}
                {activeScreen === "employees" && (
                    <CateringEmployees
                        employees={employees}
                    />
                )}
                {activeScreen === "tables" && (
                    <CateringTables
                        tables={tables}
                    />
                )}
                {activeScreen === "products" && (
                    <CateringProducts
                        products={products}
                    />
                )}
                {activeScreen === "inventory" && (
                    <CateringInventory/>
                )}
                {activeScreen === "payments" && (
                    <CateringPayments
                        payments={payments}
                    />
                )}
                {activeScreen === "gift-cards" && (
                    <CateringGiftCards
                        giftCards={giftCards}
                    />
                )}
                {activeScreen === "settings" && (
                    <CateringSettings/>
                )}
            </div>
        </div>
    )
}

export default CateringMain;