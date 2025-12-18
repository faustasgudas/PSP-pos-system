import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import "./BeautyDiscounts.css";
import {
    addEligibility,
    createDiscount,
    deleteDiscount,
    listDiscounts,
    listEligibilities,
    removeEligibility,
    type DiscountScope,
    type DiscountSummary,
    type DiscountType,
} from "../../../frontapi/discountsApi";
import { getUserFromToken } from "../../../utils/auth";
import { logout } from "../../../frontapi/authApi";
import { listCatalogItems, type CatalogItem } from "../../../frontapi/catalogApi";
import { BeautySelect } from "../../../components/ui/BeautySelect";

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
    const businessId = Number(localStorage.getItem("businessId") || 0);

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

    // Eligible items (Line discounts)
    const [catalog, setCatalog] = useState<CatalogItem[]>([]);
    const [catalogLoading, setCatalogLoading] = useState(false);
    const [catalogQuery, setCatalogQuery] = useState("");
    const [createEligibleIds, setCreateEligibleIds] = useState<number[]>([]);

    // Manage modal (selected item)
    const [selected, setSelected] = useState<DiscountSummary | null>(null);
    const [manageEligibleIds, setManageEligibleIds] = useState<Set<number>>(new Set());
    const [manageQuery, setManageQuery] = useState("");
    const [manageLoading, setManageLoading] = useState(false);

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
        setCatalogQuery("");
        setCreateEligibleIds([]);
        setError(null);
        setShowCreate(true);
    };

    const ensureCatalogLoaded = async () => {
        if (!businessId) throw new Error("Missing business context (businessId).");
        if (catalog.length > 0) return;
        setCatalogLoading(true);
        try {
            const data = await listCatalogItems(businessId, { status: "Active" });
            setCatalog(Array.isArray(data) ? data : []);
        } finally {
            setCatalogLoading(false);
        }
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

            // If Line -> at least 1 eligible item is required
            if (scope === "Line") {
                if (!createEligibleIds.length) {
                    throw new Error("For Line discount you must select at least 1 eligible item");
                }
            }

            setSaving(true);

            const created = await createDiscount({
                code: c,
                type: type as any,
                scope: scope as any,
                value: v,
                startsAt: s.toISOString(),
                endsAt: e.toISOString(),
                status: status.trim() || "Active",
            });

            // 🔥 Create eligibilities right after discount creation (only for Line)
            if (scope === "Line" && createEligibleIds.length) {
                for (const id of createEligibleIds) {
                    await addEligibility(created.discountId, id);
                }
            }

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
        setManageQuery("");

        if (String(d.scope) === "Line") {
            (async () => {
                try {
                    setManageLoading(true);
                    await ensureCatalogLoaded();
                    const list = await listEligibilities(d.discountId);
                    setManageEligibleIds(new Set((list ?? []).map((x) => x.catalogItemId)));
                } catch (e: any) {
                    setError(e?.message || "Failed to load eligibilities");
                } finally {
                    setManageLoading(false);
                }
            })();
        }
    };

    const toggleManageEligibility = async (discountId: number, catalogItemId: number, checked: boolean) => {
        if (!canManage || saving) return;
        setError(null);
        try {
            setSaving(true);
            if (checked) {
                await addEligibility(discountId, catalogItemId);
                setManageEligibleIds((prev) => new Set([...Array.from(prev), catalogItemId]));
            } else {
                await removeEligibility(discountId, catalogItemId);
                setManageEligibleIds((prev) => {
                    const next = new Set(prev);
                    next.delete(catalogItemId);
                    return next;
                });
            }
        } catch (e: any) {
            setError(e?.message || "Failed to update eligibility");
        } finally {
            setSaving(false);
        }
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

            {showCreate && (
                <div className="modal-overlay" onClick={() => setShowCreate(false)}>
                    <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                        <h3 className="modal-title">New Discount</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Code</label>
                                <input
                                    placeholder="e.g. SUMMER10"
                                    value={code}
                                    onChange={(e) => setCode(e.target.value)}
                                />
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
                                <select
                                    value={scope}
                                    onChange={(e) => {
                                        const next = e.target.value as any;
                                        setScope(next);
                                        if (next !== "Line") {
                                            setCreateEligibleIds([]);
                                            setCatalogQuery("");
                                        } else {
                                            void ensureCatalogLoaded();
                                        }
                                    }}
                                >
                                    <option value="Order">Order</option>
                                    <option value="Line">Line</option>
                                </select>
                            </div>

                            {/* Eligible items only for Line */}
                            {scope === "Line" && (
                                <div className="modal-field">
                                    <label>Eligible items</label>

                                    <div className="muted" style={{ fontSize: 12 }}>
                                        Choose which catalog items this Line discount applies to.
                                    </div>

                                    <button
                                        type="button"
                                        className="btn btn-secondary"
                                        onClick={ensureCatalogLoaded}
                                        disabled={catalogLoading}
                                        style={{ width: "fit-content" }}
                                    >
                                        {catalogLoading ? "Loading items…" : "Load items"}
                                    </button>

                                    <input
                                        value={catalogQuery}
                                        onChange={(e) => setCatalogQuery(e.target.value)}
                                        placeholder="Search items by name/code…"
                                    />

                                    <div className="elig-list">
                                        {(catalogQuery.trim()
                                            ? catalog.filter((c) => {
                                                  const q = catalogQuery.trim().toLowerCase();
                                                  return (
                                                      String(c.status).toLowerCase() === "active" &&
                                                      (c.name.toLowerCase().includes(q) || c.code.toLowerCase().includes(q))
                                                  );
                                              })
                                            : catalog.filter((c) => String(c.status).toLowerCase() === "active")
                                        )
                                            .slice(0, 120)
                                            .map((c) => {
                                                const checked = createEligibleIds.includes(c.catalogItemId);
                                                return (
                                                    <label key={c.catalogItemId} className="elig-item">
                                                        <input
                                                            type="checkbox"
                                                            checked={checked}
                                                            onChange={(e) => {
                                                                const next = e.target.checked;
                                                                setCreateEligibleIds((prev) =>
                                                                    next
                                                                        ? Array.from(new Set([...prev, c.catalogItemId]))
                                                                        : prev.filter((id) => id !== c.catalogItemId)
                                                                );
                                                            }}
                                                        />
                                                        <span className="elig-name">
                                                            {c.name} <span className="muted">({c.type} • {c.code})</span>
                                                        </span>
                                                    </label>
                                                );
                                            })}
                                    </div>
                                    <div className="muted" style={{ fontSize: 12 }}>
                                        Line discounts work only for eligible items.
                                    </div>
                                </div>
                            )}

                            <div className="modal-field">
                                <label>Value</label>
                                <input
                                    type="number"
                                    inputMode="decimal"
                                    placeholder={type === "Percent" ? "e.g. 10" : "e.g. 5.00"}
                                    value={value}
                                    onChange={(e) => setValue(e.target.value)}
                                />
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
                            <button className="btn btn-success" onClick={doCreate} disabled={saving}>
                                {saving ? "Creating…" : "Create"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

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

                            {String(selected.scope) === "Line" && (
                                <div className="modal-field">
                                    <label>Eligible items</label>
                                    <div className="muted" style={{ fontSize: 12 }}>
                                        {canManage
                                            ? "Select eligible catalog items for this discount."
                                            : "You can view eligible items, but only managers/owners can edit."}
                                    </div>

                                    <input
                                        value={manageQuery}
                                        onChange={(e) => setManageQuery(e.target.value)}
                                        placeholder="Search items…"
                                    />

                                    {manageLoading ? (
                                        <div className="muted">Loading eligibilities…</div>
                                    ) : (
                                        <div className="elig-list">
                                            {(manageQuery.trim()
                                                ? catalog.filter((c) => {
                                                      const q = manageQuery.trim().toLowerCase();
                                                      return (
                                                          String(c.status).toLowerCase() === "active" &&
                                                          (c.name.toLowerCase().includes(q) || c.code.toLowerCase().includes(q))
                                                      );
                                                  })
                                                : catalog.filter((c) => String(c.status).toLowerCase() === "active")
                                            )
                                                .slice(0, 200)
                                                .map((c) => {
                                                    const checked = manageEligibleIds.has(c.catalogItemId);
                                                    return (
                                                        <label key={c.catalogItemId} className="elig-item">
                                                            <input
                                                                type="checkbox"
                                                                checked={checked}
                                                                disabled={!canManage || saving}
                                                                onChange={(e) =>
                                                                    toggleManageEligibility(
                                                                        selected.discountId,
                                                                        c.catalogItemId,
                                                                        e.target.checked
                                                                    )
                                                                }
                                                            />
                                                            <span className="elig-name">
                                                                {c.name} <span className="muted">({c.type} • {c.code})</span>
                                                            </span>
                                                        </label>
                                                    );
                                                })}
                                        </div>
                                    )}
                                </div>
                            )}
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
