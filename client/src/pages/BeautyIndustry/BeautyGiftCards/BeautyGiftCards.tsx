import { useEffect, useMemo, useState } from "react";
import "./BeautyGiftCards.css";
import { BeautySelect } from "../../../components/ui/BeautySelect";

import {
    createGiftCard,
    deactivateGiftCard,
    getGiftCardByCode,
    getGiftCardById,
    listGiftCards,
    redeemGiftCard,
    topUpGiftCard,
    type GiftCardResponse,
} from "../../../frontapi/giftCardApi";

function formatMoneyFromMinorUnits(value: number, currency: string = "EUR") {
    const major = value / 100;
    try {
        return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(major);
    } catch {
        return `${major.toFixed(2)} ${currency}`;
    }
}

function parseMoneyToMinorUnits(input: string): number | null {
    const n = Number(input);
    if (!Number.isFinite(n) || n < 0) return null;
    return Math.round(n * 100);
}

export default function BeautyGiftCards() {
    const businessId = Number(localStorage.getItem("businessId"));
    const isBusinessReady = Number.isFinite(businessId) && businessId > 0;

    const [giftCards, setGiftCards] = useState<GiftCardResponse[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [statusFilter, setStatusFilter] = useState<string>("");
    const [codeFilter, setCodeFilter] = useState<string>("");

    // Create modal
    const [showCreate, setShowCreate] = useState(false);
    const [newCode, setNewCode] = useState("");
    const [newInitialAmount, setNewInitialAmount] = useState("");
    const [newExpiresAt, setNewExpiresAt] = useState<string>("");
    const [creating, setCreating] = useState(false);

    // Manage modal
    const [selected, setSelected] = useState<GiftCardResponse | null>(null);
    const [manageTopUpAmount, setManageTopUpAmount] = useState("");
    const [manageRedeemAmount, setManageRedeemAmount] = useState("");
    const [managing, setManaging] = useState(false);

    // Lookup
    const [lookupCode, setLookupCode] = useState("");
    const [lookupResult, setLookupResult] = useState<GiftCardResponse | null>(null);
    const [lookingUp, setLookingUp] = useState(false);

    const load = async () => {
        if (!isBusinessReady) return;
        setLoading(true);
        setError(null);
        try {
            const data = await listGiftCards(businessId, {
                status: statusFilter || undefined,
                code: codeFilter || undefined,
            });
            setGiftCards(Array.isArray(data) ? data : []);
        } catch (e: any) {
            setError(e?.message ?? "Failed to load gift cards");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        void load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [businessId]);

    const visibleGiftCards = useMemo(() => giftCards, [giftCards]);

    const openCreate = () => {
        setNewCode("");
        setNewInitialAmount("");
        setNewExpiresAt("");
        setError(null);
        setShowCreate(true);
    };

    const handleCreate = async () => {
        if (!isBusinessReady) return setError("Missing businessId (re-login?)");

        const code = newCode.trim();
        if (!code) return setError("Code is required");

        const balance = parseMoneyToMinorUnits(newInitialAmount.trim());
        if (balance === null) return setError("Initial amount must be a valid number");

        setCreating(true);
        setError(null);
        try {
            const created = await createGiftCard(businessId, {
                code,
                balance,
                expiresAt: newExpiresAt ? new Date(newExpiresAt).toISOString() : null,
            });
            setShowCreate(false);
            setGiftCards((prev) => [created, ...prev]);
        } catch (e: any) {
            setError(e?.message ?? "Failed to create gift card");
        } finally {
            setCreating(false);
        }
    };

    const openManage = (card: GiftCardResponse) => {
        setSelected(card);
        setManageTopUpAmount("");
        setManageRedeemAmount("");
        setError(null);
    };

    const refreshSelected = async () => {
        if (!isBusinessReady || !selected) return;
        const fresh = await getGiftCardById(businessId, selected.giftCardId);
        setSelected(fresh);
        setGiftCards((prev) => prev.map((c) => (c.giftCardId === fresh.giftCardId ? fresh : c)));
    };

    const handleTopUp = async () => {
        if (!isBusinessReady || !selected) return;
        const amount = parseMoneyToMinorUnits(manageTopUpAmount.trim());
        if (amount === null || amount <= 0) return setError("Top-up amount must be greater than 0");

        setManaging(true);
        setError(null);
        try {
            await topUpGiftCard(businessId, selected.giftCardId, amount);
            await refreshSelected();
            setManageTopUpAmount("");
        } catch (e: any) {
            setError(e?.message ?? "Top up failed");
        } finally {
            setManaging(false);
        }
    };

    const handleRedeem = async () => {
        if (!isBusinessReady || !selected) return;
        const amount = parseMoneyToMinorUnits(manageRedeemAmount.trim());
        if (amount === null || amount <= 0) return setError("Redeem amount must be greater than 0");

        setManaging(true);
        setError(null);
        try {
            const res = await redeemGiftCard(businessId, selected.giftCardId, amount);
            const updated: GiftCardResponse = { ...selected, balance: res.remainingBalance };
            setSelected(updated);
            setGiftCards((prev) => prev.map((c) => (c.giftCardId === updated.giftCardId ? updated : c)));
            setManageRedeemAmount("");
        } catch (e: any) {
            setError(e?.message ?? "Redeem failed");
        } finally {
            setManaging(false);
        }
    };

    const handleDeactivate = async () => {
        if (!isBusinessReady || !selected) return;
        setManaging(true);
        setError(null);
        try {
            await deactivateGiftCard(businessId, selected.giftCardId);
            await refreshSelected();
        } catch (e: any) {
            setError(e?.message ?? "Deactivate failed");
        } finally {
            setManaging(false);
        }
    };

    const handleLookup = async () => {
        if (!isBusinessReady) return;
        const code = lookupCode.trim();
        if (!code) return;

        setLookingUp(true);
        setError(null);
        try {
            const card = await getGiftCardByCode(businessId, code);
            setLookupResult(card);
        } catch (e: any) {
            setLookupResult(null);
            setError(e?.message ?? "Lookup failed");
        } finally {
            setLookingUp(false);
        }
    };

    return (
        <div className="giftcards-container">
            <div className="action-bar">
                <h2 className="section-title">Gift Cards</h2>
                <div className="action-bar__right">
                    <button className="btn btn-ghost" onClick={load} disabled={loading || !isBusinessReady}>
                        {loading ? "Refreshing…" : "Refresh"}
                    </button>
                    <button className="btn btn-primary" onClick={openCreate} disabled={!isBusinessReady}>
                        + New Gift Card
                    </button>
                </div>
            </div>

            {!isBusinessReady && (
                <div className="no-giftcards">Missing business context (try logging in again).</div>
            )}

            {error && <div className="giftcards-error">{error}</div>}

            <div className="giftcards-filters">
                <input
                    className="giftcards-filter-input"
                    placeholder="Filter by code…"
                    value={codeFilter}
                    onChange={(e) => setCodeFilter(e.target.value)}
                />
                <div style={{ minWidth: 220 }}>
                    <BeautySelect
                        value={statusFilter}
                        onChange={setStatusFilter}
                        placeholder="All statuses"
                        options={[
                            { value: "", label: "All statuses" },
                            { value: "Active", label: "Active" },
                            { value: "Inactive", label: "Inactive" },
                        ]}
                    />
                </div>
                <button className="btn btn-secondary" onClick={load} disabled={loading || !isBusinessReady}>
                    Apply
                </button>
            </div>

            <div className="giftcards-lookup">
                <input
                    className="giftcards-filter-input"
                    placeholder="Lookup by exact code…"
                    value={lookupCode}
                    onChange={(e) => setLookupCode(e.target.value)}
                />
                <button className="btn btn-secondary" onClick={handleLookup} disabled={lookingUp || !isBusinessReady}>
                    {lookingUp ? "Looking up…" : "Lookup"}
                </button>
                {lookupResult && (
                    <button className="btn btn-primary" onClick={() => openManage(lookupResult)}>
                        Open
                    </button>
                )}
            </div>

            {/* ✅ TABLE (vietoj card list) */}
            <div className="inventory-table-wrap">
                <table className="inventory-table">
                    <thead>
                    <tr>
                        <th>Code</th>
                        <th>Status</th>
                        <th className="right">Balance</th>
                        <th>Expires</th>
                        <th className="right">Actions</th>
                    </tr>
                    </thead>

                    <tbody>
                    {loading && (
                        <tr>
                            <td colSpan={5}>
                                <span className="muted">Loading…</span>
                            </td>
                        </tr>
                    )}

                    {!loading && visibleGiftCards.length === 0 && (
                        <tr>
                            <td colSpan={5}>
                                <span className="muted">No gift cards found</span>
                            </td>
                        </tr>
                    )}

                    {!loading &&
                        visibleGiftCards.map((card) => (
                            <tr key={card.giftCardId} className="inventory-row">
                                <td className="giftcard-code">{card.code}</td>
                                <td>
                    <span className={`giftcard-status ${card.status === "Inactive" ? "blocked" : ""}`}>
                      {card.status}
                    </span>
                                </td>
                                <td className="right">{formatMoneyFromMinorUnits(card.balance, "EUR")}</td>
                                <td className="muted">
                                    {card.expiresAt ? new Date(card.expiresAt).toLocaleDateString() : "—"}
                                </td>
                                <td className="right">
                                    <button className="btn btn-ghost" onClick={() => openManage(card)} disabled={managing}>
                                        Manage
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            {/* ✅ CREATE MODAL */}
            {showCreate && (
                <div className="modal-overlay" onClick={() => setShowCreate(false)}>
                    <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                        <h3 className="modal-title">New Gift Card</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Code</label>
                                <input
                                    type="text"
                                    placeholder="e.g. GC-ABCD-1234"
                                    value={newCode}
                                    onChange={(e) => setNewCode(e.target.value)}
                                />
                            </div>

                            <div className="modal-field">
                                <label>Initial amount (EUR)</label>
                                <input
                                    type="number"
                                    inputMode="decimal"
                                    placeholder="e.g. 25.00"
                                    value={newInitialAmount}
                                    onChange={(e) => setNewInitialAmount(e.target.value)}
                                />
                            </div>

                            <div className="modal-field">
                                <label>Expires at (optional)</label>
                                <input type="date" value={newExpiresAt} onChange={(e) => setNewExpiresAt(e.target.value)} />
                            </div>
                        </div>

                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={() => setShowCreate(false)} disabled={creating}>
                                Cancel
                            </button>
                            <button className="btn btn-success" onClick={handleCreate} disabled={creating}>
                                {creating ? "Creating…" : "Create Gift Card"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* ✅ MANAGE MODAL (kaip buvo, tik iškviečiamas iš lentelės) */}
            {selected && (
                <div className="modal-overlay" onClick={() => setSelected(null)}>
                    <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                        <h3 className="modal-title">Manage Gift Card</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Code</label>
                                <input type="text" value={selected.code} readOnly />
                            </div>

                            <div className="modal-field">
                                <label>Balance</label>
                                <input type="text" value={formatMoneyFromMinorUnits(selected.balance, "EUR")} readOnly />
                            </div>

                            <div className="modal-field">
                                <label>Top up (EUR)</label>
                                <input
                                    type="number"
                                    inputMode="decimal"
                                    placeholder="e.g. 10.00"
                                    value={manageTopUpAmount}
                                    onChange={(e) => setManageTopUpAmount(e.target.value)}
                                    disabled={managing}
                                />
                                <button className="btn btn-success" onClick={handleTopUp} disabled={managing}>
                                    Top up
                                </button>
                            </div>

                            <div className="modal-field">
                                <label>Redeem (EUR)</label>
                                <input
                                    type="number"
                                    inputMode="decimal"
                                    placeholder="e.g. 5.00"
                                    value={manageRedeemAmount}
                                    onChange={(e) => setManageRedeemAmount(e.target.value)}
                                    disabled={managing}
                                />
                                <button className="btn btn-primary" onClick={handleRedeem} disabled={managing}>
                                    Redeem
                                </button>
                            </div>
                        </div>

                        <div className="modal-actions">
                            <button className="btn btn-secondary" onClick={() => setSelected(null)} disabled={managing}>
                                Close
                            </button>
                            <button className="btn btn-danger" onClick={handleDeactivate} disabled={managing}>
                                Deactivate
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
