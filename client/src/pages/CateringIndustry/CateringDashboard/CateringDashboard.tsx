import { useState } from "react";
import "../../../App.css";
import "./CateringDashboard.css";
import { getUserFromToken } from "../../../utils/auth";

import CateringEmployees from "../CateringEmployees/CateringEmployees";
import CateringGiftCards from "../CateringGiftCards/CateringGiftCards";
import CateringInventory from "../CateringInventory/CateringInventory";
import CateringPayments from "../CateringPayments/CateringPayments";
import CateringProducts from "../CateringProducts/CateringProducts";
import CateringReservations from "../CateringReservations/CateringReservations";
import CateringSettings from "../CateringSettings/CateringSettings";
import CateringTables from "../CateringTables/CateringTables";
import CateringOrders from "../CateringOrders/CateringOrders";
import CateringOrderCreate from "../CateringOrders/CateringOrderCreate";
import CateringOrderDetails from "../CateringOrders/CateringOrderDetails";
import CateringNewReservation from "../CateringReservations/CateringNewReservation";
import CateringCatalogItems from "../CateringCatalogItems/CateringCatalogItems";

type Screen =
    | "dashboard"
    | "gift-cards"
    | "inventory"
    | "payments"
    | "products"
    | "catalog"
    | "reservations"
    | "reservation-create"
    | "orders"
    | "order-create"
    | "order-detail"
    | "employees"
    | "settings"
    | "tables";

type DashboardTab = "upcoming" | "payments";

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
    const [activeTab, setActiveTab] = useState<DashboardTab>("upcoming");
    const [activeOrderId, setActiveOrderId] = useState<number | null>(null);
    
    // data arrays (empty)
    const [reservations] = useState<Reservation[]>([]);
    const [payments] = useState<Payment[]>([]);
    const [products] = useState<Product[]>([]);
    const [tables] = useState<Table[]>([]);
    const [employees] = useState<Employee[]>([]);
    const [stockItems] = useState<StockItem[]>([]);
    const [giftCards] = useState<GiftCard[]>([]);
    
    // dashboard tabs
    const todayRevenue = payments.reduce((sum, p) => sum + p.amount.amount, 0);
    const availableTables = tables.filter(table => table.status === "Available").length;
    const activeEmployees = employees.filter(employee => employee.status = "Active").length;
    const lowStockItems = stockItems.filter(item => item.qtyOnHand < 5).length;
    
    const upcomingReservations = reservations
        .filter(r => new Date(r.reservationStart) > new Date())
        .slice(0, 5);
    
    const recentPayments = payments.slice(0, 5);
    
    const user = getUserFromToken();

    const handleLogout = () => {
        localStorage.clear();
        window.location.reload();
    };
    
    return(
        <div className="content-box beauty-shell">
            {/* Top Bar */}
            <div className="top-bar">
                <div className="top-left">
                    <h1 className="title">SuperApp</h1>
                    <button className="logout-btn" onClick={handleLogout}>
                        🚪 Log out
                    </button>
                </div>
                <div className="user-info">
                    {user ? `${user.email} (${user.role})` : ""}
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
                    className={`nav-btn ${activeScreen === "catalog" ? "active" : ""}`}
                    onClick={() => setActiveScreen("catalog")}
                >
                    <span>🗂️</span> Catalog
                </button>

                <button
                    className={`nav-btn ${activeScreen === "orders" ? "active" : ""}`}
                    onClick={() => setActiveScreen("orders")}
                >
                    <span>🧾</span> Orders
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
                {/* Dashboard */}
                {activeScreen === "dashboard" && (
                    <>
                        <div className="action-bar">
                            <h2 className="section-title">Today's Overview</h2>
                            <div className="action-buttons">
                                <button 
                                    className="btn btn-primary"
                                    onClick={() => setActiveScreen("reservation-create")}
                                >
                                    <span>➕</span> New Reservation
                                </button>
                                <button 
                                    className="btn btn-primary"
                                    onClick={() => setActiveScreen("order-create")}
                                >
                                    <span>⚡</span> Quick Order
                                </button>
                            </div>
                        </div>
                        
                        <div className="stat-grid">
                            <div 
                                className="stat-card"
                                onClick={() => setActiveScreen("payments")}
                            >
                                <div className="stat-number" id="today-revenue">€{todayRevenue}</div>
                                <div className="stat-label">Today's Revenue</div>
                            </div>
                            <div 
                                className="stat-card"
                                onClick={() => setActiveScreen("tables")}
                            >
                                <div className="stat-number" id="available-tables">{availableTables}</div>
                                <div className="stat-label">Available Tables</div>
                            </div>
                            <div 
                                className="stat-card"
                                onClick={() => setActiveScreen("employees")}
                            >
                                <div className="stat-number" id="active-employees">{activeEmployees}</div>
                                <div className="stat-label">Active Employees</div>
                            </div>
                            <div 
                                className="stat-card"
                                onClick={() => setActiveScreen("inventory")}
                            >
                                <div className="stat-number" id="low-stock-items">{lowStockItems}</div>
                                <div className="stat-label">Low Stock Items</div>
                            </div>
                        </div>
                        
                        <div className="tabs">
                            <button
                                className={`tab ${activeTab === "upcoming" ? "active" : ""}`}
                                onClick={() => setActiveTab("upcoming")}
                            >
                                Upcoming Reservations
                            </button>
                            <button
                                className={`tab ${activeTab === "payments" ? "active" : ""}`}
                                onClick={() => setActiveTab("payments")}
                            >
                                Recent Payments
                            </button>
                        </div>
                        {activeTab === "upcoming" && (
                            <div className="booking-list">
                                {upcomingReservations.length > 0 ? (
                                    upcomingReservations.map(b => (
                                        <div key={b.id} className="booking-item">
                                            <div className="booking-details">
                                                <div className="detail-value">
                                                    {b.customerName}
                                                </div>
                                            </div>
                                        </div>    
                                    ))
                                ) : (
                                    <div className="booking-item">
                                        <div className="booking-details">
                                            <div className="detail-value">
                                                No upcoming reservations
                                            </div>
                                        </div>
                                    </div>
                                )}
                            </div>
                        )}
                        {activeTab === "payments" && (
                            <div className="booking-list">
                                {recentPayments.length > 0 ? (
                                    recentPayments.map(p => (
                                        <div key={p.id} className="booking-item">
                                            <div className="booking-details">
                                                <div className="detail-value">
                                                    €{p.amount.amount}
                                                </div>
                                            </div>
                                        </div>
                                    ))
                                ) : (
                                    <div className="booking-item">
                                        <div className="booking-details">
                                            <div className="detail-value">
                                                No recent payments
                                            </div>
                                        </div>
                                    </div>
                                )}
                            </div>
                        )}
                    </>
                )}    
                        
                {activeScreen === "reservations" && (
                    <CateringReservations
                        goToNewReservation={() => setActiveScreen("reservation-create")}
                    />
                )}

                {activeScreen === "reservation-create" && (
                    <CateringNewReservation goBack={() => setActiveScreen("reservations")} />
                )}

                {activeScreen === "orders" && (
                    <CateringOrders
                        onNewOrder={() => setActiveScreen("order-create")}
                        onOpenOrder={(orderId) => {
                            setActiveOrderId(orderId);
                            setActiveScreen("order-detail");
                        }}
                    />
                )}

                {activeScreen === "order-create" && (
                    <CateringOrderCreate
                        goBack={() => setActiveScreen("orders")}
                        onCreated={(orderId) => {
                            setActiveOrderId(orderId);
                            setActiveScreen("order-detail");
                        }}
                    />
                )}

                {activeScreen === "order-detail" && activeOrderId && (
                    <CateringOrderDetails
                        orderId={activeOrderId}
                        onBack={() => setActiveScreen("orders")}
                    />
                )}

                {activeScreen === "catalog" && <CateringCatalogItems />}
                {activeScreen === "employees" && (<CateringEmployees employees={employees}/>)}
                {activeScreen === "tables" && (<CateringTables tables={tables} />)}
                {activeScreen === "products" && (<CateringProducts products={products} />)}
                {activeScreen === "inventory" && (<CateringInventory stockItems={stockItems} />)}
                {activeScreen === "payments" && (<CateringPayments payments={payments} />)}
                {activeScreen === "gift-cards" && (<CateringGiftCards giftCards={giftCards} />)}
                {activeScreen === "settings" && <CateringSettings />}
            </div>
        </div>
    );
}

export default CateringMain;