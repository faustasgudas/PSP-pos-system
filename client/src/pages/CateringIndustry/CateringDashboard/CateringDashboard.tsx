import { useCallback, useEffect, useMemo, useState } from "react";
import "../../../App.css";
import "./CateringDashboard.css";
import { getUserFromToken } from "../../../utils/auth";

import CateringEmployees from "../CateringEmployees/CateringEmployees";
import CateringGiftCards from "../CateringGiftCards/CateringGiftCards";
import CateringInventory from "../CateringInventory/CateringInventory";
import CateringPayments from "../CateringPayments/CateringPayments";
import CateringReservations from "../CateringReservations/CateringReservations";
import CateringSettings from "../CateringSettings/CateringSettings";
import CateringOrders from "../CateringOrders/CateringOrders";
import CateringOrderCreate from "../CateringOrders/CateringOrderCreate";
import CateringOrderDetails from "../CateringOrders/CateringOrderDetails";
import CateringNewReservation from "../CateringReservations/CateringNewReservation";
import CateringDiscounts from "../CateringDiscounts/CateringDiscounts";
import BeautyOrderPayment from "../../BeautyIndustry/BeautyOrders/BeautyOrderPayment";
import { listReservations, type ReservationSummary } from "../../../frontapi/reservationsApi";
import { listPaymentsForBusiness, type PaymentHistoryItem } from "../../../frontapi/paymentApi";
import { fetchEmployees } from "../../../frontapi/employeesApi";
import { listStockItems, type StockItemSummary } from "../../../frontapi/stockApi";
import { listAllOrders } from "../../../frontapi/orderApi";

type Screen =
    | "dashboard"
    | "gift-cards"
    | "inventory"
    | "payments"
    | "discounts"
    | "reservations"
    | "reservation-create"
    | "orders"
    | "order-create"
    | "order-detail"
    | "order-payment"
    | "employees"
    | "settings"
    ;

type DashboardTab = "upcoming" | "payments";

function isSameDay(a: Date, b: Date) {
    return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

function CateringMain(){
    const [activeScreen, setActiveScreen] = useState<Screen>("dashboard");
    const [activeTab, setActiveTab] = useState<DashboardTab>("upcoming");
    const [activeOrderId, setActiveOrderId] = useState<number | null>(null);

    const businessId = Number(localStorage.getItem("businessId"));

    const [dashLoading, setDashLoading] = useState(true);
    const [dashError, setDashError] = useState<string | null>(null);

    const [employees, setEmployees] = useState<any[]>([]);
    const [stockItems, setStockItems] = useState<StockItemSummary[]>([]);
    const [upcomingReservations, setUpcomingReservations] = useState<ReservationSummary[]>([]);
    const [recentPayments, setRecentPayments] = useState<PaymentHistoryItem[]>([]);
    const [openOrdersCount, setOpenOrdersCount] = useState<number>(0);
    
    const user = getUserFromToken();

    const handleLogout = () => {
        localStorage.clear();
        window.location.reload();
    };

    const loadDashboard = useCallback(async () => {
        if (!businessId) {
            setDashError("Missing businessId (try logging in again).");
            setDashLoading(false);
            return;
        }

        setDashLoading(true);
        setDashError(null);
        try {
            const now = new Date();
            const from = new Date(now.getTime() - 24 * 60 * 60 * 1000);
            const to = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000);

            const [resv, pays, emps, stock, openOrders] = await Promise.all([
                listReservations(businessId, { status: "Booked", dateFrom: from.toISOString(), dateTo: to.toISOString() }).catch(() => []),
                listPaymentsForBusiness().catch(() => []),
                fetchEmployees(businessId).catch(() => []),
                listStockItems(businessId).catch(() => []),
                listAllOrders({ status: "Open" }).catch(() => []),
            ]);

            setUpcomingReservations(Array.isArray(resv) ? resv : []);
            setRecentPayments(Array.isArray(pays) ? pays : []);
            setEmployees(Array.isArray(emps) ? emps : []);
            setStockItems(Array.isArray(stock) ? stock : []);
            setOpenOrdersCount(Array.isArray(openOrders) ? openOrders.length : 0);
        } catch (e: any) {
            setDashError(e?.message || "Failed to load dashboard data");
        } finally {
            setDashLoading(false);
        }
    }, [businessId]);

    // Initial load (and when business changes)
    useEffect(() => {
        void loadDashboard();
    }, [loadDashboard]);

    // Refresh when user returns to Dashboard screen
    useEffect(() => {
        if (activeScreen === "dashboard") {
            void loadDashboard();
        }
    }, [activeScreen, loadDashboard]);

    // Auto-refresh dashboard while it is visible
    useEffect(() => {
        if (activeScreen !== "dashboard") return;
        const id = window.setInterval(() => {
            void loadDashboard();
        }, 15000);
        return () => window.clearInterval(id);
    }, [activeScreen, loadDashboard]);

    const todayRevenueEur = useMemo(() => {
        const today = new Date();
        const cents = recentPayments
            .filter((p) => String(p.status).toLowerCase() === "success")
            .filter((p) => {
                const at = new Date(p.createdAt);
                return isSameDay(at, today);
            })
            .reduce((s, p) => s + (Number(p.amountCents) || 0), 0);
        return (cents / 100).toFixed(2);
    }, [recentPayments]);

    const activeEmployeesCount = useMemo(
        () => employees.filter((e: any) => String(e.status ?? "").toLowerCase() === "active").length,
        [employees]
    );

    const lowStockItemsCount = useMemo(() => stockItems.filter((s) => (Number(s.qtyOnHand) || 0) < 5).length, [stockItems]);

    const upcomingTop5 = useMemo(() => {
        return upcomingReservations
            .slice()
            .sort((a, b) => new Date(a.appointmentStart).getTime() - new Date(b.appointmentStart).getTime())
            .slice(0, 5);
    }, [upcomingReservations]);

    const recentTop5Payments = useMemo(() => {
        return recentPayments
            .slice()
            .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
            .slice(0, 5);
    }, [recentPayments]);
    
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

                {user?.role !== "Staff" && (
                    <button
                        className={`nav-btn ${activeScreen === "discounts" ? "active" : ""}`}
                        onClick={() => setActiveScreen("discounts")}
                    >
                        <span>🏷️</span> Discounts
                    </button>
                )}
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

                        {dashError && (
                            <div className="card" style={{ borderColor: "rgba(214,40,40,0.3)", background: "rgba(214,40,40,0.08)", color: "#b01d1d", marginBottom: 12 }}>
                                {dashError}
                            </div>
                        )}
                        
                        <div className="stat-grid">
                            <div 
                                className="stat-card"
                                onClick={() => setActiveScreen("payments")}
                            >
                                <div className="stat-number" id="today-revenue">€{todayRevenueEur}</div>
                                <div className="stat-label">Today's Revenue</div>
                            </div>
                            <div 
                                className="stat-card"
                                onClick={() => setActiveScreen("orders")}
                            >
                                <div className="stat-number" id="available-tables">{openOrdersCount}</div>
                                <div className="stat-label">Open Orders</div>
                            </div>
                            <div 
                                className="stat-card"
                                onClick={() => setActiveScreen("employees")}
                            >
                                <div className="stat-number" id="active-employees">{activeEmployeesCount}</div>
                                <div className="stat-label">Active Employees</div>
                            </div>
                            <div 
                                className="stat-card"
                                onClick={() => setActiveScreen("inventory")}
                            >
                                <div className="stat-number" id="low-stock-items">{lowStockItemsCount}</div>
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
                                {dashLoading ? (
                                    <div className="booking-item">
                                        <div className="booking-details">
                                            <div className="detail-value">Loading…</div>
                                        </div>
                                    </div>
                                ) : upcomingTop5.length > 0 ? (
                                    upcomingTop5.map((r) => (
                                        <div key={r.reservationId} className="booking-item">
                                            <div className="booking-details">
                                                <div className="detail-value">
                                                    {new Date(r.appointmentStart).toLocaleString()} • Reservation #{r.reservationId}
                                                </div>
                                                <div className="muted">
                                                    Employee #{r.employeeId} • Status: {r.status}
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
                                {dashLoading ? (
                                    <div className="booking-item">
                                        <div className="booking-details">
                                            <div className="detail-value">Loading…</div>
                                        </div>
                                    </div>
                                ) : recentTop5Payments.length > 0 ? (
                                    recentTop5Payments.map((p) => (
                                        <div key={p.paymentId} className="booking-item">
                                            <div className="booking-details">
                                                <div className="detail-value">
                                                    €{((Number(p.amountCents) || 0) / 100).toFixed(2)} • Order #{p.orderId}
                                                </div>
                                                <div className="muted">
                                                    {p.method} • {p.status} • {new Date(p.createdAt).toLocaleString()}
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
                        onPay={(orderId) => {
                            setActiveOrderId(orderId);
                            setActiveScreen("order-payment");
                        }}
                    />
                )}

                {activeScreen === "order-payment" && activeOrderId && (
                    <BeautyOrderPayment
                        orderId={activeOrderId}
                        onBack={() => setActiveScreen("order-detail")}
                    />
                )}
                {activeScreen === "employees" && (<CateringEmployees />)}
                {activeScreen === "inventory" && (<CateringInventory />)}
                {activeScreen === "payments" && (<CateringPayments />)}
                {activeScreen === "gift-cards" && (<CateringGiftCards />)}
                {activeScreen === "discounts" && (<CateringDiscounts />)}
                {activeScreen === "settings" && <CateringSettings onBack={() => setActiveScreen("dashboard")} />}
            </div>
        </div>
    );
}

export default CateringMain;