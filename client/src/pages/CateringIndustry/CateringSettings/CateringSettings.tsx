import { useEffect, useMemo, useState } from "react";
import "./CateringSettings.css";
import { BeautySelect } from "../../../components/ui/BeautySelect";

export default function CateringSettings(props?: { onBack?: () => void }) {
    const businessId = Number(localStorage.getItem("businessId"));
    const keyPrefix = useMemo(() => `biz:${businessId || "unknown"}:settings:`, [businessId]);

    const [businessName, setBusinessName] = useState("Catering");
    const [country, setCountry] = useState("LT");
    const [taxCalc, setTaxCalc] = useState("PerLine");
    const [priceIncludesTax, setPriceIncludesTax] = useState("true");

    const [saving, setSaving] = useState(false);
    const [savedAt, setSavedAt] = useState<number | null>(null);

    useEffect(() => {
        const bn = localStorage.getItem(`${keyPrefix}businessName`);
        const c = localStorage.getItem(`${keyPrefix}country`);
        const tc = localStorage.getItem(`${keyPrefix}taxCalc`);
        const pit = localStorage.getItem(`${keyPrefix}priceIncludesTax`);

        if (bn) setBusinessName(bn);
        if (c) setCountry(c);
        if (tc) setTaxCalc(tc);
        if (pit) setPriceIncludesTax(pit);
    }, [keyPrefix]);

    const save = () => {
        setSaving(true);
        try {
            localStorage.setItem(`${keyPrefix}businessName`, businessName.trim() || "Catering");
            localStorage.setItem(`${keyPrefix}country`, country);
            localStorage.setItem(`${keyPrefix}taxCalc`, taxCalc);
            localStorage.setItem(`${keyPrefix}priceIncludesTax`, priceIncludesTax);
            setSavedAt(Date.now());
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="settings-container">
            <div className="action-bar">
                <h2 className="section-title">Business Configuration</h2>
                <button className="btn btn-secondary" onClick={props?.onBack}>
                    Back to Dashboard
                </button>
            </div>

            <div className="settings-panel">
                <h3 className="settings-card-title">Business Settings</h3>

                <div className="settings-form-grid">
                    <div className="settings-field">
                        <label>Business Name</label>
                        <input
                            type="text"
                            value={businessName}
                            onChange={(e) => setBusinessName(e.target.value)}
                            placeholder="e.g. My Catering"
                        />
                    </div>

                    <div className="settings-field">
                        <label>Country</label>
                        <BeautySelect value={country} onChange={setCountry} options={[{ value: "LT", label: "Lithuania" }]} />
                    </div>

                    <div className="settings-field">
                        <label>Tax Calculation</label>
                        <BeautySelect
                            value={taxCalc}
                            onChange={setTaxCalc}
                            options={[
                                { value: "PerLine", label: "Round tax per line" },
                                { value: "PerOrder", label: "Round tax per order" },
                            ]}
                        />
                    </div>

                    <div className="settings-field">
                        <label>Price Includes Tax</label>
                        <BeautySelect
                            value={priceIncludesTax}
                            onChange={setPriceIncludesTax}
                            options={[
                                { value: "true", label: "Yes" },
                                { value: "false", label: "No" },
                            ]}
                        />
                    </div>
                </div>

                <div className="settings-actions" style={{ display: "flex", gap: 10, alignItems: "center" }}>
                    <button className="btn btn-success" onClick={save} disabled={saving}>
                        {saving ? "Saving…" : "Save Settings"}
                    </button>
                    {savedAt && <span className="muted">Saved</span>}
                </div>
            </div>
        </div>
    );
}
