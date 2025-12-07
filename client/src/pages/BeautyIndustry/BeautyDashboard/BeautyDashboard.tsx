import { useState } from 'react';
import "../../../App.css"
import "./BeautyDashboard.css"
import BeautyReservations from "../BeautyReservations/BeautyReservations";

// Type definitions
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
    qtyOnHand: number;
}

function BeautyMain() {
    const [activeScreen, setActiveScreen] =
        useState<'dashboard' | 'reservations' | 'employees'>('dashboard');

    // TODO: Replace with actual API calls
    const [reservations, setReservations] = useState<Booking[]>([]);
    const [payments, setPayments] = useState<Payment[]>([]);
    const [services, setServices] = useState<Service[]>([]);
    const [employees, setEmployees] = useState<Employee[]>([]);
    const [stockItems, setStockItems] = useState<StockItem[]>([]);

    const getServiceNames = (serviceIds: number[]): string => {
        return serviceIds
            .map(id => services.find(s => s.id === id)?.name || 'Unknown')
            .join(', ');
    };

    const getEmployeeName = (employeeId: number): string => {
        return employees.find(e => e.id === employeeId)?.name || 'Unknown';
    };

    const formatTime = (dateString: string): string => {
        return new Date(dateString).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    };

    const todayBookings = reservations.length;
    const todayRevenue = payments.reduce((sum, p) => sum + p.amount.amount, 0);
    const activeEmployees = employees.length;
    const lowStockItems = stockItems.filter(item => item.qtyOnHand < 5).length;

    const upcomingReservations = reservations
        .filter(r => new Date(r.appointmentStart) > new Date())
        .slice(0, 5);

    return (
        <div className="content-box">
            {/* Top Bar */}
            <div className="top-bar">
                <h1 className="title">SuperApp</h1>
                <div className="user-info">
                    <span>Jane Doe (Owner)</span>
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
                    className={`nav-btn ${activeScreen === "employees" ? "active" : ""}`}
                    onClick={() => setActiveScreen("employees")}
                >
                    <span>👥</span> Employees
                </button>


                <button className="nav-btn">
                    <span>📋</span> Services
                </button>

                <button className="nav-btn">
                    <span>📦</span> Inventory
                </button>

                <button className="nav-btn">
                    <span>💳</span> Payments
                </button>

                <button className="nav-btn">
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
                                    <span>➕</span> New Booking
                                </button>
                            </div>
                        </div>

                        <div className="stat-grid">
                            <div className="stat-card">
                                <div className="stat-number">{todayBookings}</div>
                                <div className="stat-label">Today's Reservations</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-number">€{todayRevenue}</div>
                                <div className="stat-label">Today's Revenue</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-number">{activeEmployees}</div>
                                <div className="stat-label">Active Employees</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-number">{lowStockItems}</div>
                                <div className="stat-label">Low Stock Items</div>
                            </div>
                        </div>

                        {/* Upcoming reservations */}
                        <h2 className="section-title">Upcoming Reservations</h2>
                        <div className="booking-list">
                            {upcomingReservations.length > 0 ? (
                                upcomingReservations.map(booking => (
                                    <div key={booking.id} className="booking-item">
                                        <div className="booking-header">
                                            <div className="booking-time">{formatTime(booking.appointmentStart)}</div>
                                            <div className="booking-status status-confirmed">Confirmed</div>
                                        </div>
                                        <div className="booking-details">
                                            <div className="detail-item">
                                                <div className="detail-label">Client</div>
                                                <div className="detail-value">{booking.customerName}</div>
                                            </div>
                                            <div className="detail-item">
                                                <div className="detail-label">Service</div>
                                                <div className="detail-value">{getServiceNames(booking.services)}</div>
                                            </div>
                                            <div className="detail-item">
                                                <div className="detail-label">Employee</div>
                                                <div className="detail-value">{getEmployeeName(booking.employeeId)}</div>
                                            </div>
                                        </div>
                                    </div>
                                ))
                            ) : (
                                <div className="booking-item">
                                    <div className="booking-details">
                                        <div className="detail-value">No upcoming bookings</div>
                                    </div>
                                </div>
                            )}
                        </div>
                    </>
                )}

                {activeScreen === "reservations" && (
                    <BeautyReservations
                        reservations={reservations}
                        services={services}
                        employees={employees}
                    />
                )}
            </div>
        </div>
    );
}

export default BeautyMain;
