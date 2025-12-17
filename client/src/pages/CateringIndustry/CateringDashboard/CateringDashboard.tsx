import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import "./CateringDashboard.css";
import "../../BeautyIndustry/BeautyDashboard/BeautyDashboard.css";

import { getUserFromToken } from "../../../utils/auth";

import CateringReservations from "../CateringReservations/CateringReservations";
import CateringEmployees from "../CateringEmployees/CateringEmployees";
import CateringInventory from "../CateringInventory/CateringInventory";
import CateringPayments from "../CateringPayments/CateringPayments";
import CateringGiftCards from "../CateringGiftCards/CateringGiftCards";
import CateringSettings from "../CateringSettings/CateringSettings";
import CateringOrders from "../CateringOrders/CateringOrders";
import CateringOrderCreate from "../CateringOrders/CateringOrderCreate";
import CateringOrderDetails from "../CateringOrders/CateringOrderDetails";
import CateringNewReservation from "../CateringReservations/CateringNewReservation";

import BeautyOrderPayment from "../../BeautyIndustry/BeautyOrders/BeautyOrderPayment";

import { listPaymentsForBusiness, type PaymentHistoryItem } from "../../../frontapi/paymentApi";
import { listReservations, type ReservationSummary } from "../../../frontapi/reservationsApi";
import { listStockItems, type StockItemSummary } from "../../../frontapi/stockApi";

type Screen =
    | "dashboard"
    | "reservations"
    | "reservation-create"
    | "employees"
    | "inventory"
    | "payments"
    | "gift-cards"
    | "orders"
    | "order-create"
    | "order-detail"
    | "order-payment"
    | "settings";

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
    return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

export default function CateringDashboard() {
    const user = getUserFromToken();
    const role = user?.role ?? "";

    const [activeScreen, setActiveScreen] = useState<Screen>("dashboard");
    const [activeOrderId, setActiveOrderId] = useState<number | null>(null);
    const [activeTab, setActiveTab] = useState<DashboardTab>("upcoming");
    const [employeeCount, setEmployeeCount] = useState<number | null>(null);

    const businessId = Number(localStorage.getItem("businessId"));
    const token = localStorage.getItem("token");

    const [dashLoading, setDashLoading] = useState(true);
    const [dashError, setDashError] = useState<string | null>(null);
    const [payments, setPayments] = useState<PaymentHistoryItem[]>([]);
    const [reservations, setReservations] = useState<ReservationSummary[]>([]);
    const [stockItems, setStockItems] = useState<StockItemSummary[]>([]);

    const [currentMonth] = useState(new Date());

    const handleLogout = () => {
        localStorage.clear();
        window.location.reload();
    };

    useEffect(() => {
        const load = async () => {
            if (!businessId) {
                setDashError("Missing businessId");
                setDashLoading(false);
                return;
            }

            setDashLoading(true);
            setDashError(null);
            try {
                const from = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), 1, 0, 0, 0);
                const to = new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1, 0, 0, 0);

                const [payList, resList, stockList] = await Promise.all([
                    listPaymentsForBusiness(),
                    listReservations(businessId, { dateFrom: from.toISOString(), dateTo: to.toISOString() }),
                    listStockItems(businessId),
                ]);

                setPayments(Array.isArray(payList) ? payList : []);
                setReservations(Array.isArray(resList) ? resList : []);
                setStockItems(Array.isArray(stockList) ? stockList : []);
            } catch (e: any) {
                setDashError(e?.message || "Failed to load dashboard data");
                setPayments([]);
                setReservations([]);
                setStockItems([]);
            } finally {
                setDashLoading(false);
            }
        };

        load();
    }, [businessId, currentMonth]);

    useEffect(() => {
        if (role === "Staff") return;
        if (!token || !businessId) {
            setEmployeeCount(null);
            return;
        }

        fetch(`http://localhost:5269/api/businesses/${businessId}/employees`, {
            headers: { Authorization: `Bearer ${token}` },
        })
            .then((res) => {
                if (!res.ok) throw new Error();
                return res.json();
            })
            .then((data) => {
                const active = (Array.isArray(data) ? data : []).filter((e: any) => e.status === "Active");
                setEmployeeCount(active.length);
            })
            .catch(() => setEmployeeCount(null));
    }, [role, token, businessId]);

    const todayReservations = useMemo(() => {
        const today = new Date();
        return reservations.filter((r) => isSameDay(new Date(r.appointmentStart), today)).length;
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

    const lowStockCount = useMemo(() => stockItems.filter((s) => (s.qtyOnHand ?? 0) < 5).length, [stockItems]);

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

            <div className="navbar">
                <button className={`nav-btn ${activeScreen === "dashboard" ? "active" : ""}`} onClick={() => setActiveScreen("dashboard")}>
                    📊 Dashboard
                </button>

                <button className={`nav-btn ${activeScreen === "reservations" ? "active" : ""}`} onClick={() => setActiveScreen("reservations")}>
                    📅 Reservations
                </button>

                {role !== "Staff" && (
                    <button className={`nav-btn ${activeScreen === "employees" ? "active" : ""}`} onClick={() => setActiveScreen("employees")}>
                        👥 Employees
                    </button>
                )}

                <button className={`nav-btn ${activeScreen === "inventory" ? "active" : ""}`} onClick={() => setActiveScreen("inventory")}>
                    📦 Inventory
                </button>

                <button className={`nav-btn ${activeScreen === "payments" ? "active" : ""}`} onClick={() => setActiveScreen("payments")}>
                    💳 Payments
                </button>

                <button className={`nav-btn ${activeScreen === "gift-cards" ? "active" : ""}`} onClick={() => setActiveScreen("gift-cards")}>
                    🎁 Gift Cards
                </button>

                <button className={`nav-btn ${activeScreen === "orders" ? "active" : ""}`} onClick={() => setActiveScreen("orders")}>
                    🧾 Orders
                </button>

            </div>

            <div className="dashboard-container">
                {activeScreen === "dashboard" && (
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
                                <div className="stat-number">{dashLoading ? "…" : todayReservations}</div>
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
                                <div className="stat-number">{dashLoading ? "…" : lowStockCount}</div>
                                <div className="stat-label">Low Stock Items</div>
                            </div>
                        </div>

                        <div className="tabs">
                            <button className={`tab ${activeTab === "upcoming" ? "active" : ""}`} onClick={() => setActiveTab("upcoming")}>
                                Upcoming Reservations
                            </button>
                            <button className={`tab ${activeTab === "payments" ? "active" : ""}`} onClick={() => setActiveTab("payments")}>
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
                                    <div className="booking-item">No upcoming reservations</div>
                                ) : (
                                    upcomingReservations.map((r) => (
                                        <div className="booking-item" key={r.reservationId}>
                                            <div className="booking-details">
                                                <div className="detail-value">{new Date(r.appointmentStart).toLocaleString()}</div>
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
                                                    Order #{p.orderId} — {(p.amountCents / 100).toFixed(2)} {String(p.currency ?? "EUR").toUpperCase()}
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

                {activeScreen === "reservations" && <CateringReservations goToNewReservation={() => setActiveScreen("reservation-create")} />}
                {activeScreen === "reservation-create" && <CateringNewReservation goBack={() => setActiveScreen("reservations")} />}

                {activeScreen === "employees" && role !== "Staff" && <CateringEmployees />}
                {activeScreen === "inventory" && <CateringInventory />}
                {activeScreen === "payments" && <CateringPayments />}
                {activeScreen === "gift-cards" && <CateringGiftCards />}
                {activeScreen === "settings" && <CateringSettings />}

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
                        goBack={() => setActiveScreen("dashboard")}
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
                    <BeautyOrderPayment orderId={activeOrderId} onBack={() => setActiveScreen("order-detail")} />
                )}
            </div>
        </div>
    );
}