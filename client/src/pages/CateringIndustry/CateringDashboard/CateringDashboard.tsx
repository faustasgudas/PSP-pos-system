import { useEffect, useMemo, useState } from 'react';
import "../../../App.css";
import "./CateringDashboard.css";
import { getUserFromToken } from "../../../utils/auth"

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

type DashboardTab = "upcoming" | "payments";

export default function CateringMain(){
    const user = getUserFromToken();
    const role = user?.role ?? null;

    const [showNewModal, setShowNewModal] = useState(false);
    const [showQuickModal, setShowQuickModal] = useState(false);
    const [activeScreen, setActiveScreen] = useState<Screen>("dashboard");
    const [activeTab, setActiveTab] = useState<DashboardTab>("upcoming");
    const [employeeCount, setEmployeeCount] = useState<number | null>(null);
    
    // data arrays (empty)
    const reservations: any[] = [];
    const payments: any[] = [];
    const products: any[] = [];
    const tables: any[] = [];
    const employees: any[] = [];
    const stockItems: any[] = [];
    const giftCards: any[] = [];
    
    // dashboard tabs
    const todayRevenue = payments.reduce((sum, p) => sum + (p?.amount?.amount ?? 0), 0);
    const availableTables = tables.filter(table => table.status = "Available").length;
    const activeEmployees = employees.filter(employee => employee.status = "Active").length;
    const lowStockItems = stockItems.filter(item => item.qtyOnHand < 5).length;
    
    const upcomingReservations = useMemo(
        () =>
            reservations
                .filter((r) => new Date(r.reservationStart) > new Date())
                .slice(0, 5),
        [reservations]
    );
    
    const recentPayments = useMemo(
        () => payments.slice(0, 5),
        [payments]
    );
    
    const token = localStorage.getItem("token");
    const businessId = localStorage.getItem("businessId");
    
    const handleLogout = () => {
        localStorage.clear();
        window.location.reload();
    };
    
    useEffect(() => {
        if (role === "Staff") return;
        if (!token || !businessId) {
            setEmployeeCount(null);
            return;
        }
        fetch(
            `https://localhost:44317/api/businesses/${businessId}/employees`,
            {
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        )
            .then((res) => {
                if (!res.ok) throw new Error();
                return res.json();
            })
            .then((data) => {
                const active = data.filter(
                    (e: any) => e.status === "Active"
                );
                setEmployeeCount(active.length);
            })
            .catch(() => setEmployeeCount(null));
    }, [role, token, businessId]);
    
    return(
        <div className="content-box">
            {/* Top bar */}
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
                        ⚙️ Settings
                    </button>
                </div>
            </div>

            {/* Nav bar */}
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

            {/* Dashboard - Main */}
            <div className="dashboard-container">
                {activeScreen === "dashboard" && (
                    <>
                        <div className="action-bar">
                            <h2 className="section-title">Today's Overview</h2>
                            <button 
                                className="btn btn-primary"
                            >
                                <span>➕</span> New Reservation
                                {/* todo - add modal */}
                            </button>
                            <button 
                                className="btn btn-primary"
                            >
                                <span>⚡</span> Quick Order
                                {/* todo - add modal */}
                            </button>
                        </div>
                        
                        <div className="stat-grid">
                            <div 
                                className="stat-card"
                                onClick={() => setActiveScreen("payments")}
                            >
                                <div className="stat-number">€{todayRevenue}</div>
                                <div className="stat-label">Today's Revenue</div>
                            </div>
                            <div 
                                className="stat-card"
                                onClick={() => setActiveScreen("tables")}
                            >
                                <div className="stat-number">{availableTables}</div>
                                <div className="stat-label">Available Tables</div>
                            </div>
                            {role !== "Staff" && (
                                <div
                                    className="stat-card"
                                    onClick={() => setActiveScreen("employees")}
                                >
                                    <div className="stat-number">{employeeCount ?? "—"}</div>
                                    <div className="stat-label">Active Employees</div>
                                </div>
                            )}
                            <div 
                                className="stat-card"
                                onClick={() => setActiveScreen("inventory")}
                            >
                                <div className="stat-number">{lowStockItems}</div>
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
                            <div className="reservation-list">
                                {upcomingReservations.length > 0 ? (
                                    upcomingReservations.map(b => (
                                        <div key={b.id} className="reservation-item">
                                            <div className="reservation-details">
                                                <div className="detail-value">
                                                    {b.customerName}
                                                </div>
                                            </div>
                                        </div>    
                                    ))
                                ) : (
                                    <div className="reservation-item">
                                        <div className="reservation-details">
                                            <div className="detail-value">
                                                No upcoming reservations
                                            </div>
                                        </div>
                                    </div>
                                )}
                            </div>
                        )}
                        {activeTab === "payments" && (
                            <div className="reservation-list">
                                {recentPayments.length > 0 ? (
                                    recentPayments.map(p => (
                                        <div key={p.id} className="reservation-item">
                                            <div className="reservation-details">
                                                <div className="detail-value">
                                                    €{p.amount.amount}
                                                </div>
                                            </div>
                                        </div>
                                    ))
                                ) : (
                                    <div className="reservation-item">
                                        <div className="reservation-details">
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
                        reservations={reservations}
                        employees={employees}
                        tables={tables}
                    />
                )}
                {activeScreen === "employees" && role !== "Staff" && (<CateringEmployees employees={employees}/>)}
                {activeScreen === "tables" && (<CateringTables tables={tables} />)}
                {activeScreen === "products" && (<CateringProducts products={products} />)}
                {activeScreen === "inventory" && (<CateringInventory stockItems={stockItems} />)}
                {activeScreen === "payments" && (<CateringPayments payments={payments} />)}
                {activeScreen === "gift-cards" && (<CateringGiftCards giftCards={giftCards} />)}
                {activeScreen === "settings" && <CateringSettings />}

                {/* todo - add new reservation and quick order modals */}
            </div>
        </div>
    );
}