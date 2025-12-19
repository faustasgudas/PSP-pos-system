import { useCallback, useEffect, useMemo, useState } from "react";
import "../../../App.css";
import "./BeautyDashboard.css";
import { getUserFromToken } from "../../../utils/auth";

import BeautyReservations from "../BeautyReservations/BeautyReservations";
import BeautyEmployees from "../BeautyEmployees/BeautyEmployees";
import BeautyServices from "../BeautyServices/BeautyServices";
import BeautyInventory from "../BeautyInventory/BeautyInventory";
import BeautyPayments from "../BeautyPayments/BeautyPayments";
import BeautyGiftCards from "../BeautyGiftCards/BeautyGiftCards";
import BeautyDiscounts from "../BeautyDiscounts/BeautyDiscounts";
import BeautySettings from "../BeautySettings/BeautySettings";
import BeautyNewBooking from "../BeautyNewBooking/BeautyNewBooking";
import BeautyOrderCreate from "../BeautyOrders/BeautyOrderCreate";
import BeautyOrders from "../BeautyOrders/BeautyOrders";
import BeautyOrderDetails from "../BeautyOrders/BeautyOrderDetails";
import BeautyOrderPayment from "../BeautyOrders/BeautyOrderPayment";

import { listPaymentsForBusiness, type PaymentHistoryItem } from "../../../frontapi/paymentApi";
import { listReservations, type ReservationSummary } from "../../../frontapi/reservationsApi";
import { listStockItems, type StockItemSummary } from "../../../frontapi/stockApi";
import { fetchEmployees as fetchEmployeesApi } from "../../../frontapi/employeesApi";

type Screen =
    | "dashboard"
    | "reservations"
    | "employees"
    | "services"
    | "inventory"
    | "payments"
    | "giftcards"
    | "discounts"
    | "settings"
    | "new-booking"
    | "orders"
    | "order-create"
    | "order-detail"
    | "order-payment";

type DashboardTab = "upcoming" | "payments";

function startOfDay(d = new Date()) {
    const x = new Date(d);
    x.setHours(0, 0, 0, 0);
    return x;
}
function endOfDay(d = new Date()) {
    const x = new Date(d);
    x.setHours(23, 59, 59, 999);
    return x;
}
function isSameDay(a: Date, b: Date) {
    return (
        a.getFullYear() === b.getFullYear() &&
        a.getMonth() === b.getMonth() &&
        a.getDate() === b.getDate()
    );
}

export default function BeautyDashboard() {
    const user = getUserFromToken();
    const role = user?.role ?? null;

    const [activeScreen, setActiveScreen] = useState<Screen>(role === "Staff" ? "reservations" : "dashboard");
    const [activeOrderId, setActiveOrderId] = useState<number | null>(null);
    const [activeTab, setActiveTab] = useState<DashboardTab>("upcoming");
    const [employeeCount, setEmployeeCount] = useState<number | null>(null);

    const businessId = Number(localStorage.getItem("businessId"));

    // Dashboard data
    const [dashLoading, setDashLoading] = useState(true);
    const [dashError, setDashError] = useState<string | null>(null);

    const [payments, setPayments] = useState<PaymentHistoryItem[]>([]);
    const [reservations, setReservations] = useState<ReservationSummary[]>([]);
    const [stockItems, setStockItems] = useState<StockItemSummary[]>([]);

    // Month context for “upcoming” list (same as Reservations calendar style)
    const [currentMonth] = useState(new Date());

    /* ---------------- LOGOUT ---------------- */
    const handleLogout = () => {
        localStorage.clear();
        window.location.reload();
    };

    /* ---------------- LOAD DASHBOARD DATA ---------------- */
    const loadDashboard = useCallback(async () => {
        if (!businessId) {
            setDashError("Missing businessId");
            setDashLoading(false);
            setPayments([]);
            setReservations([]);
            setStockItems([]);
            setEmployeeCount(null);
            return;
        }

        setDashLoading(true);
        setDashError(null);

        try {
            // reservations for current month (so upcoming list works)
            const from = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), 1, 0, 0, 0);
            const to = new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1, 0, 0, 0);

            const [payList, resList, stockList, emps] = await Promise.all([
                listPaymentsForBusiness(),
                listReservations(businessId, { dateFrom: from.toISOString(), dateTo: to.toISOString() }),
                listStockItems(businessId),
                role === "Staff" ? Promise.resolve([]) : fetchEmployeesApi(businessId).catch(() => []),
            ]);

            setPayments(Array.isArray(payList) ? payList : []);
            setReservations(Array.isArray(resList) ? resList : []);
            setStockItems(Array.isArray(stockList) ? stockList : []);

            if (role === "Staff") {
                setEmployeeCount(null);
            } else {
                const active = (Array.isArray(emps) ? emps : []).filter((e: any) => String(e.status) === "Active");
                setEmployeeCount(active.length);
            }
        } catch (e: any) {
            setDashError(e?.message || "Failed to load dashboard data");
            setPayments([]);
            setReservations([]);
            setStockItems([]);
            setEmployeeCount(null);
        } finally {
            setDashLoading(false);
        }
    }, [businessId, currentMonth, role]);

    // Initial load
    useEffect(() => {
        void loadDashboard();
    }, [loadDashboard]);

    // Refresh immediately when switching back to dashboard screen
    useEffect(() => {
        if (activeScreen === "dashboard") {
            void loadDashboard();
        }
    }, [activeScreen, loadDashboard]);

    // Auto-refresh while dashboard is visible
    useEffect(() => {
        if (activeScreen !== "dashboard") return;
        const id = window.setInterval(() => {
            void loadDashboard();
        }, 15000);
        return () => window.clearInterval(id);
    }, [activeScreen, loadDashboard]);

    /* ---------------- METRICS ---------------- */
    const todayBookings = useMemo(() => {
        const today = new Date();
        return reservations.filter((r) => {
            const d = new Date(r.appointmentStart);
            return isSameDay(d, today);
        }).length;
    }, [reservations]);

    const todayRevenue = useMemo(() => {
        const from = startOfDay(new Date());
        const to = endOfDay(new Date());

        const cents = payments
            .filter((p) => p.status === "Success")
            .filter((p) => {
                const when = new Date((p.completedAt ?? p.createdAt) as any);
                return !isNaN(when.getTime()) && when >= from && when <= to;
            })
            .reduce((sum, p) => sum + (Number(p.amountCents) || 0), 0);

        return cents / 100;
    }, [payments]);

    const lowStockItems = useMemo(() => {
        return stockItems.filter((s) => (s.qtyOnHand ?? 0) < 5).length;
    }, [stockItems]);

    const upcomingReservations = useMemo(() => {
        const now = new Date();
        return reservations
            .filter((r) => new Date(r.appointmentStart) > now && r.status !== "Cancelled")
            .slice()
            .sort((a, b) => new Date(a.appointmentStart).getTime() - new Date(b.appointmentStart).getTime())
            .slice(0, 5);
    }, [reservations]);

    const recentPayments = useMemo(() => {
        return payments
            .slice()
            .sort((a, b) => {
                const da = new Date((a.completedAt ?? a.createdAt) as any).getTime();
                const db = new Date((b.completedAt ?? b.createdAt) as any).getTime();
                return (isNaN(db) ? 0 : db) - (isNaN(da) ? 0 : da);
            })
            .slice(0, 5);
    }, [payments]);

    return (
        <div className="content-box beauty-shell">
            {/* TOP BAR */}
            <div className="top-bar">
                <div className="top-left">
                    <h1 className="title">SuperApp</h1>
                    <button className="logout-btn" onClick={handleLogout}>
                        🚪 Log out
                    </button>
                </div>

                <div className="user-info">
                    {user ? `${user.email} (${user.role})` : ""}
                    <button className="nav-btn" onClick={() => setActiveScreen("settings")}>
                        ⚙️ Settings
                    </button>
                </div>
            </div>

            {/* NAVBAR */}
            <div className="navbar">
                {role !== "Staff" && (
                    <button
                        className={`nav-btn ${activeScreen === "dashboard" ? "active" : ""}`}
                        onClick={() => setActiveScreen("dashboard")}
                    >
                        📊 Dashboard
                    </button>
                )}

                <button
                    className={`nav-btn ${activeScreen === "reservations" ? "active" : ""}`}
                    onClick={() => setActiveScreen("reservations")}
                >
                    📅 Reservations
                </button>

                {role !== "Staff" && (
                    <button
                        className={`nav-btn ${activeScreen === "employees" ? "active" : ""}`}
                        onClick={() => setActiveScreen("employees")}
                    >
                        👥 Employees
                    </button>
                )}

                <button
                    className={`nav-btn ${activeScreen === "services" ? "active" : ""}`}
                    onClick={() => setActiveScreen("services")}
                >
                    📋 Services
                </button>

                {role !== "Staff" && (
                    <button
                        className={`nav-btn ${activeScreen === "inventory" ? "active" : ""}`}
                        onClick={() => setActiveScreen("inventory")}
                    >
                        📦 Inventory
                    </button>
                )}

                {role !== "Staff" && (
                    <button
                        className={`nav-btn ${activeScreen === "payments" ? "active" : ""}`}
                        onClick={() => setActiveScreen("payments")}
                    >
                        💳 Payments
                    </button>
                )}

                <button
                    className={`nav-btn ${activeScreen === "giftcards" ? "active" : ""}`}
                    onClick={() => setActiveScreen("giftcards")}
                >
                    🎁 Gift Cards
                </button>

                {role !== "Staff" && (
                    <button
                        className={`nav-btn ${activeScreen === "discounts" ? "active" : ""}`}
                        onClick={() => setActiveScreen("discounts")}
                    >
                        🏷️ Discounts
                    </button>
                )}

                <button
                    className={`nav-btn ${activeScreen === "orders" ? "active" : ""}`}
                    onClick={() => setActiveScreen("orders")}
                >
                    🧾 Orders
                </button>
            </div>

            {/* MAIN CONTENT */}
            <div className="dashboard-container">
                {activeScreen === "dashboard" && role !== "Staff" && (
                    <>
                        <div className="action-bar">
                            <h2 className="section-title">Today's Overview</h2>
                            <button className="btn btn-primary" onClick={() => setActiveScreen("order-create")}>
                                ➕ New Order
                            </button>
                        </div>

                        {dashError && (
                            <div className="booking-item" style={{ marginBottom: 10 }}>
                                <div className="booking-details">
                                    <div className="detail-value" style={{ color: "#b01d1d" }}>
                                        {dashError}
                                    </div>
                                </div>
                            </div>
                        )}

                        <div className="stat-grid">
                            <div className="stat-card" onClick={() => setActiveScreen("reservations")}>
                                <div className="stat-number">{dashLoading ? "…" : todayBookings}</div>
                                <div className="stat-label">Today's Reservations</div>
                            </div>

                            <div className="stat-card" onClick={() => setActiveScreen("payments")}>
                                <div className="stat-number">{dashLoading ? "…" : `€${todayRevenue.toFixed(2)}`}</div>
                                <div className="stat-label">Today's Revenue</div>
                            </div>

                            {role !== "Staff" && (
                                <div className="stat-card" onClick={() => setActiveScreen("employees")}>
                                    <div className="stat-number">{employeeCount ?? "—"}</div>
                                    <div className="stat-label">Active Employees</div>
                                </div>
                            )}

                            <div className="stat-card" onClick={() => setActiveScreen("inventory")}>
                                <div className="stat-number">{dashLoading ? "…" : lowStockItems}</div>
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
                                ) : upcomingReservations.length === 0 ? (
                                    <div className="booking-item">No upcoming appointments</div>
                                ) : (
                                    upcomingReservations.map((r) => (
                                        <div className="booking-item" key={r.reservationId}>
                                            <div className="booking-details">
                                                <div className="detail-value">
                                                    {new Date(r.appointmentStart).toLocaleString()}
                                                </div>
                                                <div className="muted">Status: {r.status}</div>
                                            </div>
                                        </div>
                                    ))
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
                                ) : recentPayments.length === 0 ? (
                                    <div className="booking-item">No recent payments</div>
                                ) : (
                                    recentPayments.map((p) => (
                                        <div className="booking-item" key={p.paymentId}>
                                            <div className="booking-details">
                                                <div className="detail-value">
                                                    Order #{p.orderId} — {(p.amountCents / 100).toFixed(2)}{" "}
                                                    {String(p.currency ?? "EUR").toUpperCase()}
                                                </div>
                                                <div className="muted">
                                                    {new Date((p.completedAt ?? p.createdAt) as any).toLocaleString()} • {p.status}
                                                </div>
                                            </div>
                                        </div>
                                    ))
                                )}
                            </div>
                        )}
                    </>
                )}

                {activeScreen === "employees" && role !== "Staff" && <BeautyEmployees />}
                {activeScreen === "services" && <BeautyServices />}
                {activeScreen === "inventory" && role !== "Staff" && <BeautyInventory />}
                {activeScreen === "payments" && role !== "Staff" && <BeautyPayments />}
                {activeScreen === "giftcards" && <BeautyGiftCards />}
                {activeScreen === "discounts" && role !== "Staff" && <BeautyDiscounts />}
                {activeScreen === "settings" && <BeautySettings onBack={() => setActiveScreen("dashboard")} />}

                {activeScreen === "orders" && (
                    <BeautyOrders
                        onNewOrder={() => setActiveScreen("order-create")}
                        onOpenOrder={(orderId) => {
                            setActiveOrderId(orderId);
                            setActiveScreen("order-detail");
                        }}
                    />
                )}

                {activeScreen === "order-create" && (
                    <BeautyOrderCreate
                        goBack={() => setActiveScreen("dashboard")}
                        onCreated={(orderId) => {
                            setActiveOrderId(orderId);
                            setActiveScreen("order-detail");
                        }}
                    />
                )}

                {activeScreen === "order-detail" && activeOrderId && (
                    <BeautyOrderDetails
                        orderId={activeOrderId}
                        onBack={() => setActiveScreen("orders")}
                        onPay={(orderId) => {
                            setActiveOrderId(orderId);
                            setActiveScreen("order-payment");
                        }}
                    />
                )}

                {activeScreen === "order-payment" && activeOrderId && (
                    <BeautyOrderPayment orderId={activeOrderId} onBack={() => setActiveScreen("order-detail")} />
                )}

                {activeScreen === "reservations" && (
                    <BeautyReservations goToNewBooking={() => setActiveScreen("new-booking")} />
                )}

                {activeScreen === "new-booking" && (
                    <BeautyNewBooking goBack={() => setActiveScreen("reservations")} />
                )}
            </div>
        </div>
    );
}
