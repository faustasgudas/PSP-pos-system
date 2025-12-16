import { useState } from 'react';
import "../../../App.css";
import "./CateringDashboard.css";
import { getUserFromToken } from "../../../utils/auth"

import BeautyEmployees from "../../BeautyIndustry/BeautyEmployees/BeautyEmployees";
import BeautyInventory from "../../BeautyIndustry/BeautyInventory/BeautyInventory";
import BeautyPayments from "../../BeautyIndustry/BeautyPayments/BeautyPayments";
import BeautyGiftCards from "../../BeautyIndustry/BeautyGiftCards/BeautyGiftCards";
import BeautyDiscounts from "../../BeautyIndustry/BeautyDiscounts/BeautyDiscounts";
import CateringSettings from "../CateringSettings/CateringSettings";
import CateringTables from "../CateringTables/CateringTables";
import CateringOrders from "../CateringOrders/CateringOrders";
import CateringOrderCreate from "../CateringOrders/CateringOrderCreate";
import CateringOrderDetails from "../CateringOrders/CateringOrderDetails";
import CateringProducts from "../CateringProducts/CateringProducts";
import CateringStockMovements from "../CateringStockMovements/CateringStockMovements";
import BeautyOrderPayment from "../../BeautyIndustry/BeautyOrders/BeautyOrderPayment";

type Screen =
    | "dashboard"
    | "inventory"
    | "payments"
    | "gift-cards"
    | "discounts"
    | "products"
    | "stock-movements"
    | "orders"
    | "order-create"
    | "order-detail"
    | "order-payment"
    | "employees"
    | "settings"
    | "tables";

type DashboardTab = "payments";

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
    const [activeTab, setActiveTab] = useState<DashboardTab>("payments");
    const [activeOrderId, setActiveOrderId] = useState<number | null>(null);
    
    // data arrays (empty)
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
    
    const recentPayments = payments.slice(0, 5);
    
    const user = getUserFromToken();
    const role = user?.role ?? "";

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
                    <button className="nav-btn" onClick={() => setActiveScreen("settings")}>
                        ⚙️ Settings
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
                
                {role !== "Staff" && (
                    <button
                        className={`nav-btn ${activeScreen === "employees" ? "active" : ""}`}
                        onClick={() => setActiveScreen("employees")}
                    >
                        <span>👥</span> Employees
                    </button>
                )}

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

                <button
                    className={`nav-btn ${activeScreen === "stock-movements" ? "active" : ""}`}
                    onClick={() => setActiveScreen("stock-movements")}
                >
                    <span>📈</span> Stock Movements
                </button>

                {role !== "Staff" && (
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
                                onClick={() => (role !== "Staff" ? setActiveScreen("employees") : setActiveScreen("dashboard"))}
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
                            <button className="tab active" onClick={() => setActiveTab("payments")}>
                                Recent Payments
                            </button>
                        </div>
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
                    <BeautyOrderPayment orderId={activeOrderId} onBack={() => setActiveScreen("order-detail")} />
                )}

                {activeScreen === "employees" && role !== "Staff" && <BeautyEmployees />}
                {activeScreen === "tables" && <CateringTables />}
                {activeScreen === "products" && <CateringProducts products={products} />}
                {activeScreen === "inventory" && <BeautyInventory />}
                {activeScreen === "payments" && <BeautyPayments />}
                {activeScreen === "gift-cards" && <BeautyGiftCards />}
                {activeScreen === "stock-movements" && <CateringStockMovements />}
                {activeScreen === "discounts" && role !== "Staff" && <BeautyDiscounts />}
                {activeScreen === "settings" && <CateringSettings />}
            </div>
        </div>
    );
}

export default CateringMain;