import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import "./BeautyDiscounts.css";
import {
    createDiscount,
    deleteDiscount,
    listDiscounts,
    type DiscountScope,
    type DiscountSummary,
    type DiscountType,
} from "../../../frontapi/discountsApi";
import { getUserFromToken } from "../../../utils/auth";
import { logout } from "../../../frontapi/authApi";

function toLocalDateTimeInputValue(d: Date) {
    const pad = (n: number) => String(n).padStart(2, "0");
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export default function BeautyDiscounts() {
    const user = getUserFromToken();
    const role = user?.role ?? "";
    const canManage = role === "Owner" || role === "Manager";

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [items, setItems] = useState<DiscountSummary[]>([]);

    const [query, setQuery] = useState("");
    const [scopeFilter, setScopeFilter] = useState<string>("");

    const [showCreate, setShowCreate] = useState(false);
    const [code, setCode] = useState("");
    const [type, setType] = useState<DiscountType>("Percent");
    const [scope, setScope] = useState<DiscountScope>("Line");
    const [value, setValue] = useState("10");
    const [startsAt, setStartsAt] = useState(toLocalDateTimeInputValue(new Date()));
    const [endsAt, setEndsAt] = useState(toLocalDateTimeInputValue(new Date(Date.now() + 1000 * 60 * 60 * 24 * 365)));
    const [status, setStatus] = useState("Active");

    const authProblem =
        (error ?? "").toLowerCase().includes("unauthorized") ||
        (error ?? "").toLowerCase().includes("forbid") ||
        (error ?? "").includes("401") ||
        (error ?? "").includes("403");

    const load = async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await listDiscounts();
            setItems(Array.isArray(data) ? data : []);
        } catch (e: any) {
            setError(e?.message || "Failed to load discounts");
            setItems([]);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        load();
    }, []);

    const filtered = useMemo(() => {
        const q = query.trim().toLowerCase();
        return items
            .filter((d) => (scopeFilter ? d.scope === scopeFilter : true))
            .filter((d) => (q ? d.code.toLowerCase().includes(q) : true))
            .sort((a, b) => a.code.localeCompare(b.code));
    }, [items, query, scopeFilter]);

    const openCreate = () => {
        setCode("");
        setType("Percent");
        setScope("Line");
        setValue("10");
        setStartsAt(toLocalDateTimeInputValue(new Date()));
        setEndsAt(toLocalDateTimeInputValue(new Date(Date.now() + 1000 * 60 * 60 * 24 * 365)));
        setStatus("Active");
        setError(null);
        setShowCreate(true);
    };

    const doCreate = async () => {
        if (!canManage) return;
        setError(null);
        try {
            const c = code.trim();
            if (!c) throw new Error("Code is required");
            const v = Number(value);
            if (!Number.isFinite(v) || v <= 0) throw new Error("Value must be > 0");

            const s = new Date(startsAt);
            const e = new Date(endsAt);
            if (isNaN(s.getTime()) || isNaN(e.getTime())) throw new Error("Invalid start/end date");
            if (e <= s) throw new Error("EndsAt must be after StartsAt");

            await createDiscount({
                code: c,
                type: type as any,
                scope: scope as any,
                value: v,
                startsAt: s.toISOString(),
                endsAt: e.toISOString(),
                status: status.trim() || "Active",
            });

            setShowCreate(false);
            await load();
        } catch (e: any) {
            setError(e?.message || "Create failed");
        }
    };

    const doDelete = async (d: DiscountSummary) => {
        if (!canManage) return;
        const ok = window.confirm(`Delete discount ${d.code}?`);
        if (!ok) return;

        setError(null);
        try {
            await deleteDiscount(d.discountId);
            await load();
        } catch (e: any) {
            setError(e?.message || "Delete failed");
        }
    };

    return (
        <div className="page beauty-discounts">
            <div className="action-bar">
                <h2 className="section-title">Discounts</h2>
                <div style={{ display: "flex", gap: 10, alignItems: "center", flexWrap: "wrap" }}>
                    <input
                        className="dropdown"
                        placeholder="Search by code…"
                        value={query}
                        onChange={(e) => setQuery(e.target.value)}
                        disabled={loading}
                    />
                    <select
                        className="dropdown"
                        value={scopeFilter}
                        onChange={(e) => setScopeFilter(e.target.value)}
                        disabled={loading}
                    >
                        <option value="">All scopes</option>
                        <option value="Order">Order</option>
                        <option value="Line">Line</option>
                    </select>
                    <button className="btn" onClick={load} disabled={loading}>
                        {loading ? "Refreshing…" : "Refresh"}
                    </button>
                    {canManage && (
                        <button className="btn btn-primary" onClick={openCreate} disabled={loading}>
                            ➕ New Discount
                        </button>
                    )}
                </div>
            </div>

            {error && (
                <div className="beauty-discounts__error">
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
                <div className="page">Loading discounts…</div>
            ) : filtered.length === 0 ? (
                <div className="beauty-discounts__empty">No discounts found.</div>
            ) : (
                <div className="beauty-discounts__list">
                    {filtered.map((d) => (
                        <div key={d.discountId} className="beauty-discounts__card">
                            <div className="beauty-discounts__card-top">
                                <div className="beauty-discounts__code">{d.code}</div>
                                <div className={`beauty-discounts__status status-${String(d.status).toLowerCase()}`}>
                                    {d.status}
                                </div>
                            </div>
                            <div className="beauty-discounts__meta">
                                <div>
                                    <span className="muted">Type:</span> {d.type}
                                </div>
                                <div>
                                    <span className="muted">Scope:</span> {d.scope}
                                </div>
                                <div>
                                    <span className="muted">Value:</span> {d.value}
                                </div>
                                <div>
                                    <span className="muted">Active:</span>{" "}
                                    {new Date(d.startsAt).toLocaleDateString()} →{" "}
                                    {new Date(d.endsAt).toLocaleDateString()}
                                </div>
                            </div>
                            {canManage && (
                                <div className="beauty-discounts__actions">
                                    <button className="btn btn-danger" onClick={() => doDelete(d)}>
                                        Delete
                                    </button>
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            )}

            {showCreate && (
                <div className="modal-overlay" onClick={() => setShowCreate(false)}>
                    <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                        <h3 className="modal-title">New Discount</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Code</label>
                                <input value={code} onChange={(e) => setCode(e.target.value)} />
                            </div>

                            <div className="modal-field">
                                <label>Type</label>
                                <select value={type} onChange={(e) => setType(e.target.value as any)}>
                                    <option value="Percent">Percent</option>
                                    <option value="Amount">Amount</option>
                                </select>
                            </div>

                            <div className="modal-field">
                                <label>Scope</label>
                                <select value={scope} onChange={(e) => setScope(e.target.value as any)}>
                                    <option value="Order">Order</option>
                                    <option value="Line">Line</option>
                                </select>
                            </div>

                            <div className="modal-field">
                                <label>Value</label>
                                <input type="number" value={value} onChange={(e) => setValue(e.target.value)} />
                                <div className="muted" style={{ fontSize: 12 }}>
                                    Percent: e.g. 10 = 10%. Amount: depends on backend currency rules.
                                </div>
                            </div>

                            <div className="modal-field">
                                <label>Starts at</label>
                                <input type="datetime-local" value={startsAt} onChange={(e) => setStartsAt(e.target.value)} />
                            </div>

                            <div className="modal-field">
                                <label>Ends at</label>
                                <input type="datetime-local" value={endsAt} onChange={(e) => setEndsAt(e.target.value)} />
                            </div>

                            <div className="modal-field">
                                <label>Status</label>
                                <select value={status} onChange={(e) => setStatus(e.target.value)}>
                                    <option value="Active">Active</option>
                                    <option value="Inactive">Inactive</option>
                                </select>
                            </div>
                        </div>

                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={() => setShowCreate(false)}>
                                Cancel
                            </button>
                            <button className="btn btn-success" onClick={doCreate}>
                                Create
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}


