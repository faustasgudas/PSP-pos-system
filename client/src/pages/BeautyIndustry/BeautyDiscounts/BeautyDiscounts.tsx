import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import "./BeautyDiscounts.css";
import { BeautySelect } from "../../../components/ui/BeautySelect";
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
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(
        d.getHours()
    )}:${pad(d.getMinutes())}`;
}

export default function BeautyDiscounts() {
    const user = getUserFromToken();
    const role = user?.role ?? "";
    const canManage = role === "Owner" || role === "Manager";

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [items, setItems] = useState<DiscountSummary[]>([]);

    const [query, setQuery] = useState("");
    const [scopeFilter, setScopeFilter] = useState<string>("");

    // Create modal
    const [showCreate, setShowCreate] = useState(false);
    const [code, setCode] = useState("");
    const [type, setType] = useState<DiscountType>("Percent");
    const [scope, setScope] = useState<DiscountScope>("Line");
    const [value, setValue] = useState("10");
    const [startsAt, setStartsAt] = useState(toLocalDateTimeInputValue(new Date()));
    const [endsAt, setEndsAt] = useState(
        toLocalDateTimeInputValue(new Date(Date.now() + 1000 * 60 * 60 * 24 * 365))
    );
    const [status, setStatus] = useState("Active");

    // Manage modal (selected item)
    const [selected, setSelected] = useState<DiscountSummary | null>(null);

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
        if (!canManage || saving) return;
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

            setSaving(true);

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
        } finally {
            setSaving(false);
        }
    };

    const openManage = (d: DiscountSummary) => {
        setSelected(d);
        setError(null);
    };

    const doDelete = async (d: DiscountSummary) => {
        if (!canManage || saving) return;
        const ok = window.confirm(`Delete discount ${d.code}?`);
        if (!ok) return;

        setError(null);
        try {
            setSaving(true);
            await deleteDiscount(d.discountId);
            setSelected(null);
            await load();
        } catch (e: any) {
            setError(e?.message || "Delete failed");
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="page beauty-discounts">
            <div className="action-bar">
                <h2 className="section-title">Discounts</h2>

                <div className="action-bar__right">
                    <input
                        className="inventory-search"
                        placeholder="Search by code…"
                        value={query}
                        onChange={(e) => setQuery(e.target.value)}
                        disabled={loading}
                    />

                    <div style={{ minWidth: 180 }}>
                        <BeautySelect
                            value={scopeFilter}
                            onChange={setScopeFilter}
                            disabled={loading}
                            placeholder="All scopes"
                            options={[
                                { value: "", label: "All scopes" },
                                { value: "Order", label: "Order" },
                                { value: "Line", label: "Line" },
                            ]}
                        />
                    </div>

                    <button className="btn btn-ghost" onClick={load} disabled={loading}>
                        {loading ? "Refreshing…" : "Refresh"}
                    </button>

                    {canManage && (
                        <button className="btn btn-primary" onClick={openCreate} disabled={loading}>
                            + New Discount
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

            {/* ✅ TABLE */}
            <div className="inventory-table-wrap">
                <table className="inventory-table">
                    <thead>
                    <tr>
                        <th>Code</th>
                        <th>Status</th>
                        <th>Type</th>
                        <th>Scope</th>
                        <th className="right">Value</th>
                        <th>Active</th>
                        <th className="right">Actions</th>
                    </tr>
                    </thead>

                    <tbody>
                    {loading && (
                        <tr>
                            <td colSpan={7}>
                                <span className="muted">Loading discounts…</span>
                            </td>
                        </tr>
                    )}

                    {!loading && filtered.length === 0 && (
                        <tr>
                            <td colSpan={7}>
                                <span className="muted">No discounts found</span>
                            </td>
                        </tr>
                    )}

                    {!loading &&
                        filtered.map((d) => {
                            const statusLower = String(d.status || "").toLowerCase();
                            return (
                                <tr key={d.discountId} className="inventory-row">
                                    <td className="beauty-discounts__code">{d.code}</td>

                                    <td>
                      <span className={`beauty-discounts__status status-${statusLower}`}>
                        {d.status}
                      </span>
                                    </td>

                                    <td className="muted">{d.type}</td>
                                    <td className="muted">{d.scope}</td>
                                    <td className="right">{d.value}</td>

                                    <td className="muted">
                                        {new Date(d.startsAt).toLocaleDateString()} →{" "}
                                        {new Date(d.endsAt).toLocaleDateString()}
                                    </td>

                                    <td className="right">
                                        <button
                                            className="btn btn-ghost"
                                            type="button"
                                            onClick={() => openManage(d)}
                                            disabled={saving}
                                        >
                                            Manage
                                        </button>
                                    </td>
                                </tr>
                            );
                        })}
                    </tbody>
                </table>
            </div>

            {/* ✅ CREATE MODAL */}
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
                                <BeautySelect
                                    value={type}
                                    onChange={(v) => setType(v as any)}
                                    options={[
                                        { value: "Percent", label: "Percent" },
                                        { value: "Amount", label: "Amount" },
                                    ]}
                                />
                            </div>

                            <div className="modal-field">
                                <label>Scope</label>
                                <BeautySelect
                                    value={scope}
                                    onChange={(v) => setScope(v as any)}
                                    options={[
                                        { value: "Order", label: "Order" },
                                        { value: "Line", label: "Line" },
                                    ]}
                                />
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
                                <input
                                    type="datetime-local"
                                    value={startsAt}
                                    onChange={(e) => setStartsAt(e.target.value)}
                                />
                            </div>

                            <div className="modal-field">
                                <label>Ends at</label>
                                <input
                                    type="datetime-local"
                                    value={endsAt}
                                    onChange={(e) => setEndsAt(e.target.value)}
                                />
                            </div>

                            <div className="modal-field">
                                <label>Status</label>
                                <BeautySelect
                                    value={status}
                                    onChange={setStatus}
                                    options={[
                                        { value: "Active", label: "Active" },
                                        { value: "Inactive", label: "Inactive" },
                                    ]}
                                />
                            </div>
                        </div>

                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={() => setShowCreate(false)}>
                                Cancel
                            </button>
                            <button className="btn btn-success" onClick={doCreate} disabled={saving}>
                                {saving ? "Creating…" : "Create"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* ✅ MANAGE MODAL (details + delete) */}
            {selected && (
                <div className="modal-overlay" onClick={() => setSelected(null)}>
                    <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                        <h3 className="modal-title">Discount details</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Code</label>
                                <input value={selected.code} readOnly />
                            </div>

                            <div className="modal-field">
                                <label>Status</label>
                                <input value={String(selected.status)} readOnly />
                            </div>

                            <div className="modal-field">
                                <label>Type</label>
                                <input value={String(selected.type)} readOnly />
                            </div>

                            <div className="modal-field">
                                <label>Scope</label>
                                <input value={String(selected.scope)} readOnly />
                            </div>

                            <div className="modal-field">
                                <label>Value</label>
                                <input value={String(selected.value)} readOnly />
                            </div>

                            <div className="modal-field">
                                <label>Active window</label>
                                <input
                                    value={`${new Date(selected.startsAt).toLocaleString()} → ${new Date(
                                        selected.endsAt
                                    ).toLocaleString()}`}
                                    readOnly
                                />
                            </div>
                        </div>

                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={() => setSelected(null)} disabled={saving}>
                                Close
                            </button>
                            {canManage && (
                                <button className="btn btn-danger" onClick={() => doDelete(selected)} disabled={saving}>
                                    Delete
                                </button>
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
