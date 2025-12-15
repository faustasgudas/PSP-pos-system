import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import "./BeautyDashboard.css";
import { getUserFromToken } from "../../../utils/auth";

import BeautyReservations from "../BeautyReservations/BeautyReservations";
import BeautyEmployees from "../BeautyEmployees/BeautyEmployees";
import BeautyServices from "../BeautyServices/BeautyServices";
import BeautyInventory from "../BeautyInventory/BeautyInventory";
import BeautyPayments from "../BeautyPayments/BeautyPayments";
import BeautyGiftCards from "../BeautyGiftCards/BeautyGiftCards";
import BeautySettings from "../BeautySettings/BeautySettings";
import BeautyNewBooking from "../BeautyNewBooking/BeautyNewBooking";
import BeautyOrderCreate from "../BeautyOrders/BeautyOrderCreate";
import BeautyOrders from "../BeautyOrders/BeautyOrders";
import BeautyOrderDetails from "../BeautyOrders/BeautyOrderDetails";
import BeautyOrderPayment from "../BeautyOrders/BeautyOrderPayment";

type Screen =
    | "dashboard"
    | "reservations"
    | "employees"
    | "services"
    | "inventory"
    | "payments"
    | "giftcards"
    | "settings"
    | "new-booking"
    | "orders"
    | "order-create"
    | "order-detail"
    | "order-payment";

type DashboardTab = "upcoming" | "payments";

export default function BeautyDashboard() {
    const user = getUserFromToken();
    const role = user?.role ?? null;

    const [activeScreen, setActiveScreen] = useState<Screen>("dashboard");
    const [activeOrderId, setActiveOrderId] = useState<number | null>(null);
    const [activeTab, setActiveTab] = useState<DashboardTab>("upcoming");
    const [employeeCount, setEmployeeCount] = useState<number | null>(null);

    /* ---------------- TEMP PLACEHOLDERS ---------------- */
    const reservations: any[] = [];
    const payments: any[] = [];
    const services: any[] = [];
    const employees: any[] = [];
    const stockItems: any[] = [];
    const giftCards: any[] = [];

    const todayBookings = reservations.length;
    const todayRevenue = payments.reduce(
        (sum, p) => sum + (p?.amount?.amount ?? 0),
        0
    );

    const lowStockItems = stockItems.filter(
        (item) => (item?.qtyOnHand ?? 0) < 5
    ).length;

    const upcomingReservations = useMemo(
        () =>
            reservations
                .filter((r) => new Date(r.appointmentStart) > new Date())
                .slice(0, 5),
        [reservations]
    );

    const recentPayments = useMemo(
        () => payments.slice(0, 5),
        [payments]
    );

    const token = localStorage.getItem("token");
    const businessId = localStorage.getItem("businessId");

    /* ---------------- LOGOUT ---------------- */
    const handleLogout = () => {
        localStorage.clear();
        window.location.reload();
    };

    /* -------- ACTIVE EMPLOYEE COUNT ---------- */
    useEffect(() => {
        if (role === "Staff") return;
        if (!token || !businessId) {
            setEmployeeCount(null);
            return;
        }

        fetch(
            `http://localhost:5269/api/businesses/${businessId}/employees`,
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

    return (
        <div className="content-box">
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
                    className={`nav-btn ${
                        activeScreen === "dashboard" ? "active" : ""
                    }`}
                    onClick={() => setActiveScreen("dashboard")}
                >
                    📊 Dashboard
                </button>

                <button
                    className={`nav-btn ${
                        activeScreen === "reservations" ? "active" : ""
                    }`}
                    onClick={() => setActiveScreen("reservations")}
                >
                    📅 Reservations
                </button>

                {role !== "Staff" && (
                    <button
                        className={`nav-btn ${
                            activeScreen === "employees" ? "active" : ""
                        }`}
                        onClick={() => setActiveScreen("employees")}
                    >
                        👥 Employees
                    </button>
                )}

                <button
                    className={`nav-btn ${
                        activeScreen === "services" ? "active" : ""
                    }`}
                    onClick={() => setActiveScreen("services")}
                >
                    📋 Services
                </button>

                <button
                    className={`nav-btn ${
                        activeScreen === "inventory" ? "active" : ""
                    }`}
                    onClick={() => setActiveScreen("inventory")}
                >
                    📦 Inventory
                </button>

                <button
                    className={`nav-btn ${
                        activeScreen === "payments" ? "active" : ""
                    }`}
                    onClick={() => setActiveScreen("payments")}
                >
                    💳 Payments
                </button>

                <button
                    className={`nav-btn ${
                        activeScreen === "giftcards" ? "active" : ""
                    }`}
                    onClick={() => setActiveScreen("giftcards")}
                >
                    🎁 Gift Cards
                </button>

                <button
                    className={`nav-btn ${
                        activeScreen === "orders" ? "active" : ""
                    }`}
                    onClick={() => setActiveScreen("orders")}
                >
                    🧾 Orders
                </button>
            </div>

            {/* MAIN CONTENT */}
            <div className="dashboard-container">
                {activeScreen === "dashboard" && (
                    <>
                        <div className="action-bar">
                            <h2 className="section-title">
                                Today's Overview
                            </h2>
                            <button
                                className="btn btn-primary"
                                onClick={() =>
                                    setActiveScreen("order-create")
                                }
                            >
                                ➕ New Order
                            </button>
                        </div>

                        <div className="stat-grid">
                            <div
                                className="stat-card"
                                onClick={() =>
                                    setActiveScreen("reservations")
                                }
                            >
                                <div className="stat-number">
                                    {todayBookings}
                                </div>
                                <div className="stat-label">
                                    Today's Reservations
                                </div>
                            </div>

                            <div
                                className="stat-card"
                                onClick={() =>
                                    setActiveScreen("payments")
                                }
                            >
                                <div className="stat-number">
                                    €{todayRevenue}
                                </div>
                                <div className="stat-label">
                                    Today's Revenue
                                </div>
                            </div>

                            {role !== "Staff" && (
                                <div
                                    className="stat-card"
                                    onClick={() =>
                                        setActiveScreen("employees")
                                    }
                                >
                                    <div className="stat-number">
                                        {employeeCount ?? "—"}
                                    </div>
                                    <div className="stat-label">
                                        Active Employees
                                    </div>
                                </div>
                            )}

                            <div
                                className="stat-card"
                                onClick={() =>
                                    setActiveScreen("inventory")
                                }
                            >
                                <div className="stat-number">
                                    {lowStockItems}
                                </div>
                                <div className="stat-label">
                                    Low Stock Items
                                </div>
                            </div>
                        </div>

                        <div className="tabs">
                            <button
                                className={`tab ${
                                    activeTab === "upcoming" ? "active" : ""
                                }`}
                                onClick={() =>
                                    setActiveTab("upcoming")
                                }
                            >
                                Upcoming Reservations
                            </button>
                            <button
                                className={`tab ${
                                    activeTab === "payments" ? "active" : ""
                                }`}
                                onClick={() =>
                                    setActiveTab("payments")
                                }
                            >
                                Recent Payments
                            </button>
                        </div>

                        {activeTab === "upcoming" && (
                            <div className="booking-list">
                                {upcomingReservations.length === 0 && (
                                    <div className="booking-item">
                                        No upcoming appointments
                                    </div>
                                )}
                            </div>
                        )}

                        {activeTab === "payments" && (
                            <div className="booking-list">
                                {recentPayments.length === 0 && (
                                    <div className="booking-item">
                                        No recent payments
                                    </div>
                                )}
                            </div>
                        )}
                    </>
                )}

                {activeScreen === "employees" &&
                    role !== "Staff" && <BeautyEmployees />}

                {activeScreen === "services" && <BeautyServices />}

                {/* ✅ FIXED: Pass required props */}
                {activeScreen === "inventory" && <BeautyInventory stockItems={stockItems} />}
                {activeScreen === "payments" && <BeautyPayments payments={payments} />}
                {activeScreen === "giftcards" && <BeautyGiftCards giftCards={giftCards} />}

                {activeScreen === "settings" && <BeautySettings />}

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
                    <BeautyOrderPayment
                        orderId={activeOrderId}
                        onBack={() => setActiveScreen("order-detail")}
                    />
                )}

                {activeScreen === "reservations" && (
                    <BeautyReservations
                        reservations={reservations}
                        services={services}
                        employees={employees}
                        goToNewBooking={() =>
                            setActiveScreen("new-booking")
                        }
                    />
                )}

                {activeScreen === "new-booking" && (
                    <BeautyNewBooking
                        goBack={() =>
                            setActiveScreen("reservations")
                        }
                    />
                )}
            </div>
        </div>
    );
}