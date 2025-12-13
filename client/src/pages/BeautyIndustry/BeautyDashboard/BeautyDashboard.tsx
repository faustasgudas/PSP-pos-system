import { useState } from "react";
import "../../../App.css";
import "./BeautyDashboard.css";
import { getUserFromToken } from "../../../utils/auth"

import BeautyReservations from "../BeautyReservations/BeautyReservations";
import BeautyEmployees from "../BeautyEmployees/BeautyEmployees";
import BeautyServices from "../BeautyServices/BeautyServices";
import BeautyInventory from "../BeautyInventory/BeautyInventory";
import BeautyPayments from "../BeautyPayments/BeautyPayments";
import BeautyGiftCards from "../BeautyGiftCards/BeautyGiftCards";
import BeautySettings from "../BeautySettings/BeautySettings";
import BeautyNewBooking from "../BeautyNewBooking/BeautyNewBooking";

// ==== TYPES ====

interface Booking {
    id: number;
    customerName: string;
    customerPhone: string;
    customerEmail: string;
    appointmentStart: string;
    appointmentEnd: string;
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

interface Service {
    id: number;
    name: string;
    basePrice: { amount: number; currency: string };
}

interface Employee {
    id: number;
    name: string;
    role: string;
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

type Screen =
    | "dashboard"
    | "reservations"
    | "employees"
    | "services"
    | "inventory"
    | "payments"
    | "giftcards"
    | "settings"
    | "new-booking";

type DashboardTab = "upcoming" | "payments";

function BeautyMain() {
    const [activeScreen, setActiveScreen] = useState<Screen>("dashboard");
    const [activeTab, setActiveTab] = useState<DashboardTab>("upcoming");

    // DATA ARRAYS – TEMP EMPTY (BACKEND LATER)
    const [reservations] = useState<Booking[]>([]);
    const [payments] = useState<Payment[]>([]);
    const [services] = useState<Service[]>([]);
    const [employees] = useState<Employee[]>([]);
    const [stockItems] = useState<StockItem[]>([]);
    const [giftCards] = useState<GiftCard[]>([]);

    // DASHBOARD STATS
    const todayBookings = reservations.length;
    const todayRevenue = payments.reduce((sum, p) => sum + p.amount.amount, 0);
    const activeEmployees = employees.length;
    const lowStockItems = stockItems.filter(item => item.qtyOnHand < 5).length;

    const upcomingReservations = reservations
        .filter(r => new Date(r.appointmentStart) > new Date())
        .slice(0, 5);

    const recentPayments = payments.slice(0, 5);
    
    const user = getUserFromToken();

    return (
        <div className="content-box">
            {/* TOP BAR */}
            <div className="top-bar">
                <h1 className="title">SuperApp</h1>
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

            {/* NAVBAR */}
            <div className="navbar">
                <button
                    className={`nav-btn ${activeScreen === "dashboard" ? "active" : ""}`}
                    onClick={() => setActiveScreen("dashboard")}
                >
                    📊 Dashboard
                </button>

                <button
                    className={`nav-btn ${activeScreen === "reservations" ? "active" : ""}`}
                    onClick={() => setActiveScreen("reservations")}
                >
                    📅 Reservations
                </button>

                <button
                    className={`nav-btn ${activeScreen === "employees" ? "active" : ""}`}
                    onClick={() => setActiveScreen("employees")}
                >
                    👥 Employees
                </button>

                <button
                    className={`nav-btn ${activeScreen === "services" ? "active" : ""}`}
                    onClick={() => setActiveScreen("services")}
                >
                    📋 Services
                </button>

                <button
                    className={`nav-btn ${activeScreen === "inventory" ? "active" : ""}`}
                    onClick={() => setActiveScreen("inventory")}
                >
                    📦 Inventory
                </button>

                <button
                    className={`nav-btn ${activeScreen === "payments" ? "active" : ""}`}
                    onClick={() => setActiveScreen("payments")}
                >
                    💳 Payments
                </button>

                <button
                    className={`nav-btn ${activeScreen === "giftcards" ? "active" : ""}`}
                    onClick={() => setActiveScreen("giftcards")}
                >
                    🎁 Gift Cards
                </button>
            </div>

            {/* SCREEN CONTENT */}
            <div className="dashboard-container">
                {/* DASHBOARD OVERVIEW */}
                {activeScreen === "dashboard" && (
                    <>
                        <div className="action-bar">
                            <h2 className="section-title">Today's Overview</h2>
                        </div>

                        {/* STAT CARDS */}
                        <div className="stat-grid">
                            <div
                                className="stat-card"
                                onClick={() => setActiveScreen("reservations")}
                            >
                                <div className="stat-number">{todayBookings}</div>
                                <div className="stat-label">Today's Reservations</div>
                            </div>

                            <div
                                className="stat-card"
                                onClick={() => setActiveScreen("payments")}
                            >
                                <div className="stat-number">€{todayRevenue}</div>
                                <div className="stat-label">Today's Revenue</div>
                            </div>

                            <div
                                className="stat-card"
                                onClick={() => setActiveScreen("employees")}
                            >
                                <div className="stat-number">{activeEmployees}</div>
                                <div className="stat-label">Active Employees</div>
                            </div>

                            <div
                                className="stat-card"
                                onClick={() => setActiveScreen("inventory")}
                            >
                                <div className="stat-number">{lowStockItems}</div>
                                <div className="stat-label">Low Stock Items</div>
                            </div>
                        </div>

                        {/* TABS: UPCOMING / PAYMENTS */}
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

                        {/* TAB CONTENT */}
                        {activeTab === "upcoming" && (
                            <div className="booking-list">
                                {upcomingReservations.length > 0 ? (
                                    upcomingReservations.map(b => (
                                        <div key={b.id} className="booking-item">
                                            <div className="booking-header">
                                                <div className="booking-time">
                                                    {new Date(b.appointmentStart).toLocaleTimeString(
                                                        [],
                                                        { hour: "2-digit", minute: "2-digit" }
                                                    )}
                                                </div>
                                                <div className="booking-status status-confirmed">
                                                    {b.status || "Confirmed"}
                                                </div>
                                            </div>
                                            <div className="booking-details">
                                                <div className="detail-item">
                                                    <div className="detail-label">Client</div>
                                                    <div className="detail-value">
                                                        {b.customerName}
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    ))
                                ) : (
                                    <div className="booking-item">
                                        <div className="booking-details">
                                            <div className="detail-value">
                                                No upcoming bookings
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
                                            <div className="booking-header">
                                                <div className="booking-time">
                                                    €{p.amount.amount}
                                                </div>
                                                <div className="booking-status status-completed">
                                                    {p.method}
                                                </div>
                                            </div>
                                            <div className="booking-details">
                                                <div className="detail-item">
                                                    <div className="detail-label">Reservation</div>
                                                    <div className="detail-value">
                                                        #{p.reservationId}
                                                    </div>
                                                </div>
                                                <div className="detail-item">
                                                    <div className="detail-label">Status</div>
                                                    <div className="detail-value">
                                                        {p.status}
                                                    </div>
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

                {/* OTHER SCREENS */}
                {activeScreen === "reservations" && (
                    <BeautyReservations
                        reservations={reservations}
                        services={services}
                        employees={employees}
                        goToNewBooking={() => setActiveScreen("new-booking")}
                    />
                )}

                {activeScreen === "employees" && (
                    <BeautyEmployees employees={employees} />
                )}

                {activeScreen === "services" && (
                    <BeautyServices services={services} />
                )}

                {activeScreen === "inventory" && (
                    <BeautyInventory stockItems={stockItems} />
                )}

                {activeScreen === "payments" && (
                    <BeautyPayments payments={payments} />
                )}

                {activeScreen === "giftcards" && (
                    <BeautyGiftCards giftCards={giftCards} />
                )}

                {activeScreen === "settings" && <BeautySettings />}

                {activeScreen === "new-booking" && (
                    <BeautyNewBooking
                        goBack={() => setActiveScreen("reservations")}
                    />
                )}
            </div>
        </div>
    );
}

export default BeautyMain;
