import { useEffect, useMemo, useState } from "react";
import {
    listAllOrders,
    listMyOrders,
    type OrderSummary,
} from "../../../frontapi/orderApi";
import { getUserFromToken } from "../../../utils/auth";
import { logout } from "../../../frontapi/authApi";

import "../../../App.css";
import "./BeautyOrders.css";

type OrdersScope = "mine" | "all";

export default function BeautyOrders(props: {
    onOpenOrder: (orderId: number) => void;
    onNewOrder: () => void;
}) {
    const user = getUserFromToken();
    const role = user?.role ?? "";
    const employeeId = Number(localStorage.getItem("employeeId"));

    const [scope, setScope] = useState<OrdersScope>("mine");
    const [statusFilter, setStatusFilter] = useState<string>("");
    const [queryId, setQueryId] = useState<string>("");

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [orders, setOrders] = useState<OrderSummary[]>([]);

    const canListAll = role === "Owner" || role === "Manager";
    const authProblem =
        (error ?? "").toLowerCase().includes("unauthorized") ||
        (error ?? "").toLowerCase().includes("forbid") ||
        (error ?? "").includes("401") ||
        (error ?? "").includes("403");

    const load = async () => {
        setLoading(true);
        setError(null);
        try {
            let data: OrderSummary[] = [];

            if (scope === "all") {
                data = await listAllOrders({ status: statusFilter || undefined });
            } else {
                // Backend /orders/mine intentionally returns only OPEN orders.
                // For managers/owners, emulate "mine" for other statuses using listAllOrders + filter by employeeId.
                if (canListAll && employeeId) {
                    const all = await listAllOrders({ status: statusFilter || undefined });
                    data = (Array.isArray(all) ? all : []).filter((o) => o.employeeId === employeeId);
                } else {
                    data = await listMyOrders();
                }
            }

            setOrders(Array.isArray(data) ? data : []);
        } catch (e: any) {
            setError(e?.message || "Failed to load orders");
            setOrders([]);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        // If user isn't allowed to list all, force "mine".
        if (!canListAll && scope === "all") setScope("mine");
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [canListAll]);

    useEffect(() => {
        load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [scope, statusFilter]);

    const filtered = useMemo(() => {
        let data = orders;

        // Client-side filter (case-insensitive). For managers/owners scope=mine this is redundant,
        // but helpful for staff (mine endpoint is Open-only anyway).
        if (statusFilter) {
            const sf = statusFilter.toLowerCase();
            data = data.filter((o) => String(o.status).toLowerCase() === sf);
        }

        return [...data].sort(
            (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        );
    }, [orders, scope, statusFilter]);

    const openById = () => {
        const id = Number(queryId);
        if (!Number.isFinite(id) || id <= 0) {
            setError("Enter a valid Order ID");
            return;
        }
        props.onOpenOrder(id);
    };

    return (
        <div className="page beauty-orders">
            <div className="beauty-orders__header">
                <div>
                    <h2>Orders</h2>
                    <div className="muted">
                        View and manage orders (create, edit lines, split).
                    </div>
                </div>

                <div className="beauty-orders__header-actions">
                    <button className="btn" onClick={load} disabled={loading}>
                        {loading ? "Refreshing…" : "Refresh"}
                    </button>
                    <button className="btn btn-primary" onClick={props.onNewOrder}>
                        ➕ New Order
                    </button>
                </div>
            </div>

            <div className="beauty-orders__controls">
                <div className="control-group">
                    <label className="muted">Scope</label>
                    <div className="control-row">
                        <button
                            className={`btn ${scope === "mine" ? "btn-primary" : ""}`}
                            onClick={() => setScope("mine")}
                            disabled={loading}
                        >
                            My orders
                        </button>
                        <button
                            className={`btn ${scope === "all" ? "btn-primary" : ""}`}
                            onClick={() => setScope("all")}
                            disabled={loading || !canListAll}
                            title={!canListAll ? "Managers/Owners only" : ""}
                        >
                            All orders
                        </button>
                    </div>
                </div>

                <div className="control-group">
                    <label className="muted">Status</label>
                    <select
                        className="dropdown"
                        value={statusFilter}
                        onChange={(e) => setStatusFilter(e.target.value)}
                        disabled={loading}
                    >
                        <option value="">All</option>
                        <option value="Open">Open</option>
                        <option value="Closed">Closed</option>
                        <option value="Cancelled">Cancelled</option>
                    </select>
                    {scope === "mine" && !canListAll && (
                        <div className="muted" style={{ marginTop: 6 }}>
                            Note: “My orders” endpoint shows only <strong>Open</strong> orders for staff.
                        </div>
                    )}
                </div>

                <div className="control-group">
                    <label className="muted">Open by ID</label>
                    <div className="control-row">
                        <input
                            className="dropdown"
                            inputMode="numeric"
                            placeholder="Order ID"
                            value={queryId}
                            onChange={(e) => setQueryId(e.target.value)}
                            disabled={loading}
                        />
                        <button className="btn" onClick={openById} disabled={loading}>
                            Open
                        </button>
                    </div>
                </div>
            </div>

            {error && (
                <div className="beauty-orders__error">
                    <div>{error}</div>
                    {authProblem && (
                        <div style={{ marginTop: 10 }}>
                            <button
                                className="btn"
                                onClick={() => {
                                    logout();
                                    window.location.reload();
                                }}
                            >
                                Log out
                            </button>
                        </div>
                    )}
                </div>
            )}

            {loading ? (
                <div className="page">Loading orders…</div>
            ) : filtered.length === 0 ? (
                <div className="beauty-orders__empty">
                    No orders found.
                </div>
            ) : (
                <div className="beauty-orders__list">
                    {filtered.map((o) => (
                        <button
                            key={o.orderId}
                            className="beauty-orders__card"
                            onClick={() => props.onOpenOrder(o.orderId)}
                        >
                            <div className="beauty-orders__card-top">
                                <div className="beauty-orders__card-title">
                                    Order #{o.orderId}
                                </div>
                                <div className={`beauty-orders__status status-${String(o.status).toLowerCase()}`}>
                                    {o.status}
                                </div>
                            </div>

                            <div className="beauty-orders__card-meta">
                                <div>
                                    <span className="muted">Created:</span>{" "}
                                    {new Date(o.createdAt).toLocaleString()}
                                </div>
                                <div>
                                    <span className="muted">Employee:</span> {o.employeeId}
                                </div>
                            </div>
                        </button>
                    ))}
                </div>
            )}
        </div>
    );
}


