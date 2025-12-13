import { useState, useEffect } from "react";
import "../../../App.css";
import "./BeautyDashboard.css";
import { useAuth } from "../../../contexts/AuthContext";

import BeautyReservations from "../BeautyReservations/BeautyReservations";
import BeautyEmployees from "../BeautyEmployees/BeautyEmployees";
import BeautyServices from "../BeautyServices/BeautyServices";
import BeautyInventory from "../BeautyInventory/BeautyInventory";
import BeautyPayments from "../BeautyPayments/BeautyPayments";
import BeautyGiftCards from "../BeautyGiftCards/BeautyGiftCards";
import BeautySettings from "../BeautySettings/BeautySettings";
import BeautyNewBooking from "../BeautyNewBooking/BeautyNewBooking";

import * as reservationService from "../../../services/reservationService";
import * as catalogService from "../../../services/catalogService";
import * as employeeService from "../../../services/employeeService";
import * as stockService from "../../../services/stockService";
import * as giftCardService from "../../../services/giftCardService";
import * as orderService from "../../../services/orderService";

import type { ReservationSummaryResponse } from "../../../types/api";
import type { CatalogItemSummaryResponse } from "../../../types/api";
import type { EmployeeSummaryResponse } from "../../../types/api";
import type { StockItemSummaryResponse } from "../../../types/api";
import type { GiftCardResponse } from "../../../types/api";
import type { OrderSummaryResponse } from "../../../types/api";

// ==== TYPES ====
// Using backend types directly, with local mappings where needed

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
    const { businessId, logout } = useAuth();
    const [activeScreen, setActiveScreen] = useState<Screen>("dashboard");
    const [activeTab, setActiveTab] = useState<DashboardTab>("upcoming");

    // ✅ DATA ARRAYS – FETCHED FROM API
    const [reservations, setReservations] = useState<ReservationSummaryResponse[]>([]);
    const [orders, setOrders] = useState<OrderSummaryResponse[]>([]);
    const [services, setServices] = useState<CatalogItemSummaryResponse[]>([]);
    const [employees, setEmployees] = useState<EmployeeSummaryResponse[]>([]);
    const [stockItems, setStockItems] = useState<StockItemSummaryResponse[]>([]);
    const [giftCards, setGiftCards] = useState<GiftCardResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // Fetch all data on mount
    useEffect(() => {
        if (!businessId) return;

        const fetchData = async () => {
            setLoading(true);
            setError(null);
            try {
                // Fetch all data in parallel
                const [reservationsData, servicesData, employeesData, stockData, giftCardsData, ordersData] = await Promise.all([
                    reservationService.getReservations(businessId),
                    catalogService.getCatalogItems(businessId, { type: "Service" }),
                    employeeService.getEmployees(businessId, { status: "Active" }),
                    stockService.getStockItems(),
                    giftCardService.getGiftCards(),
                    orderService.getMyOrders(),
                ]);

                setReservations(reservationsData);
                setServices(servicesData);
                setEmployees(employeesData);
                setStockItems(stockData);
                setGiftCards(giftCardsData);
                setOrders(ordersData);
            } catch (err) {
                setError(err instanceof Error ? err.message : "Failed to load data");
                console.error("Error fetching data:", err);
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, [businessId]);

    // Refresh data when returning to dashboard
    useEffect(() => {
        if (activeScreen === "dashboard" && businessId && !loading) {
            const refreshData = async () => {
                try {
                    const [reservationsData, ordersData] = await Promise.all([
                        reservationService.getReservations(businessId),
                        orderService.getMyOrders(),
                    ]);
                    setReservations(reservationsData);
                    setOrders(ordersData);
                } catch (err) {
                    console.error("Error refreshing data:", err);
                    // Don't set error state here as it's just a refresh
                }
            };
            refreshData();
        }
    }, [activeScreen, businessId, loading]);

    // === DASHBOARD STATS ===
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    const todayBookings = reservations.filter(r => {
        const start = new Date(r.appointmentStart);
        return start >= today && start < tomorrow;
    }).length;

    // Calculate revenue from orders (simplified - you may need to fetch order details)
    const todayRevenue = orders
        .filter(o => {
            const created = new Date(o.createdAt);
            return created >= today && created < tomorrow;
        })
        .reduce((sum, o) => sum + 0, 0); // TODO: Calculate actual revenue from order details

    const activeEmployeesCount = employees.length;
    const lowStockItems = stockItems.filter(item => item.qtyOnHand < 5).length;

    const upcomingReservations = reservations
        .filter(r => new Date(r.appointmentStart) > new Date())
        .sort((a, b) => new Date(a.appointmentStart).getTime() - new Date(b.appointmentStart).getTime())
        .slice(0, 5);

    const recentOrders = orders
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
        .slice(0, 5);

    return (
        <div className="content-box">
            {/* TOP BAR */}
            <div className="top-bar">
                <h1 className="title">SuperApp</h1>
                <div className="user-info">
                    {businessId && <span>Business ID: {businessId}</span>}
                    {!businessId && <span style={{ color: "orange" }}>Loading business info...</span>}
                    <button
                        className="nav-btn"
                        onClick={() => setActiveScreen("settings")}
                    >
                        ⚙️ Settings
                    </button>
                    <button
                        className="nav-btn"
                        onClick={logout}
                        style={{ marginLeft: "0.5rem" }}
                    >
                        Logout
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
                {loading && (
                    <div style={{ padding: "2rem", textAlign: "center" }}>
                        Loading...
                    </div>
                )}

                {error && (
                    <div style={{ padding: "2rem", color: "red", textAlign: "center" }}>
                        Error: {error}
                    </div>
                )}

                {!loading && !error && (
                    <>
                        {/* ✅ DASHBOARD (VIEW ONLY) */}
                        {activeScreen === "dashboard" && (
                            <>
                                <div className="action-bar">
                                    <h2 className="section-title">Today's Overview</h2>
                                </div>

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
                                        <div className="stat-number">€{todayRevenue.toFixed(2)}</div>
                                        <div className="stat-label">Today's Revenue</div>
                                    </div>

                                    <div
                                        className="stat-card"
                                        onClick={() => setActiveScreen("employees")}
                                    >
                                        <div className="stat-number">{activeEmployeesCount}</div>
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
                                            upcomingReservations.map(r => (
                                                <div key={r.reservationId} className="booking-item">
                                                    <div className="booking-details">
                                                        <div className="detail-value">
                                                            Reservation #{r.reservationId} - {new Date(r.appointmentStart).toLocaleString()}
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
                                        {recentOrders.length > 0 ? (
                                            recentOrders.map(o => (
                                                <div key={o.orderId} className="booking-item">
                                                    <div className="booking-details">
                                                        <div className="detail-value">
                                                            Order #{o.orderId} - {new Date(o.createdAt).toLocaleString()}
                                                        </div>
                                                    </div>
                                                </div>
                                            ))
                                        ) : (
                                            <div className="booking-item">
                                                <div className="booking-details">
                                                    <div className="detail-value">
                                                        No recent orders
                                                    </div>
                                                </div>
                                            </div>
                                        )}
                                    </div>
                                )}
                            </>
                        )}

                        {/* ✅ RESERVATIONS (ONLY PLACE WITH NEW BOOKING BUTTON) */}
                        {activeScreen === "reservations" && businessId && (
                            <BeautyReservations
                                reservations={reservations}
                                services={services}
                                employees={employees}
                                goToNewBooking={() => setActiveScreen("new-booking")}
                                businessId={businessId}
                                onRefresh={() => {
                                    reservationService.getReservations(businessId).then(setReservations);
                                }}
                            />
                        )}

                        {activeScreen === "employees" && businessId && (
                            <BeautyEmployees 
                                employees={employees}
                                businessId={businessId}
                                onRefresh={() => {
                                    employeeService.getEmployees(businessId, { status: "Active" }).then(setEmployees);
                                }}
                            />
                        )}
                        {activeScreen === "services" && businessId && (
                            <BeautyServices 
                                services={services}
                                businessId={businessId}
                                onRefresh={() => {
                                    catalogService.getCatalogItems(businessId, { type: "Service" }).then(setServices);
                                }}
                            />
                        )}
                        {activeScreen === "inventory" && (
                            <BeautyInventory 
                                stockItems={stockItems}
                                onRefresh={() => {
                                    stockService.getStockItems().then(setStockItems);
                                }}
                            />
                        )}
                        {activeScreen === "payments" && (
                            <BeautyPayments 
                                orders={orders}
                                onRefresh={() => {
                                    orderService.getMyOrders().then(setOrders);
                                }}
                            />
                        )}
                        {activeScreen === "giftcards" && (
                            <BeautyGiftCards 
                                giftCards={giftCards}
                                onRefresh={() => {
                                    giftCardService.getGiftCards().then(setGiftCards);
                                }}
                            />
                        )}
                        {activeScreen === "settings" && <BeautySettings />}

                        {/* ✅ NEW BOOKING PAGE */}
                        {activeScreen === "new-booking" && businessId && (
                            <BeautyNewBooking
                                goBack={() => setActiveScreen("reservations")}
                                services={services}
                                employees={employees}
                                businessId={businessId}
                                onBookingCreated={() => {
                                    reservationService.getReservations(businessId).then(setReservations);
                                    setActiveScreen("reservations");
                                }}
                            />
                        )}
                    </>
                )}
            </div>
        </div>
    );
}

export default BeautyMain;
