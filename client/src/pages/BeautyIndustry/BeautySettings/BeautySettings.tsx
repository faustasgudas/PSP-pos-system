import { useEffect, useState } from "react";
import "./BeautySettings.css";
import { BeautySelect } from "../../../components/ui/BeautySelect";
import { getUserFromToken } from "../../../utils/auth";
import { getBusiness, updateBusiness, type Business } from "../../../frontapi/businessApi";

export default function BeautySettings(props?: { onBack?: () => void }) {
    const user = getUserFromToken();
    const role = user?.role ?? "";
    const isOwner = role === "Owner";
    
    const businessId = Number(localStorage.getItem("businessId"));

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [business, setBusiness] = useState<Business | null>(null);

    const [businessName, setBusinessName] = useState("");
    const [address, setAddress] = useState("");
    const [phone, setPhone] = useState("");
    const [email, setEmail] = useState("");
    const [country, setCountry] = useState("LT");
    const [priceIncludesTax, setPriceIncludesTax] = useState(true);
    const [businessType, setBusinessType] = useState("Beauty");

    const [saving, setSaving] = useState(false);
    const [savedAt, setSavedAt] = useState<number | null>(null);

    useEffect(() => {
        const load = async () => {
            if (!businessId) {
                setError("Missing businessId");
                setLoading(false);
                return;
            }

            try {
                const biz = await getBusiness(businessId);
                setBusiness(biz);
                setBusinessName(biz.name || "");
                setAddress(biz.address || "");
                setPhone(biz.phone || "");
                setEmail(biz.email || "");
                setCountry(biz.countryCode || "LT");
                setPriceIncludesTax(biz.priceIncludesTax ?? true);
                setBusinessType(biz.businessType || "Beauty");
            } catch (e: any) {
                setError(e?.message || "Failed to load business settings");
            } finally {
                setLoading(false);
            }
        };

        load();
    }, [businessId]);

    const save = async () => {
        if (!businessId || !isOwner) return;
        
        setSaving(true);
        setError(null);
        try {
            const updated = await updateBusiness(businessId, {
                name: businessName.trim() || "Beauty Salon",
                address: address.trim() || "",
                phone: phone.trim() || "",
                email: email.trim() || "",
                countryCode: country,
                priceIncludesTax,
                businessType,
            });
            setBusiness(updated);
            setSavedAt(Date.now());
        } catch (e: any) {
            setError(e?.message || "Failed to save settings");
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

            {!isOwner ? (
                <div className="settings-panel">
                    <div className="card" style={{ borderColor: "rgba(214,40,40,0.3)", background: "rgba(214,40,40,0.08)", color: "#b01d1d" }}>
                        Only the business owner can modify settings.
                    </div>
                </div>
            ) : loading ? (
                <div className="settings-panel">
                    <div className="muted">Loading settings...</div>
                </div>
            ) : (
                <>
                    {error && (
                        <div className="card" style={{ borderColor: "rgba(214,40,40,0.3)", background: "rgba(214,40,40,0.08)", color: "#b01d1d", marginBottom: 12 }}>
                            {error}
                        </div>
                    )}

                    <div className="settings-panel">
                        <h3 className="settings-card-title">Business Settings</h3>

                        <div className="settings-form-grid">
                            <div className="settings-field">
                                <label>Business Name</label>
                                <input
                                    type="text"
                                    value={businessName}
                                    onChange={(e) => setBusinessName(e.target.value)}
                                    placeholder="e.g. Beauty Salon"
                                    disabled={saving}
                                />
                            </div>

                            <div className="settings-field">
                                <label>Address</label>
                                <input
                                    type="text"
                                    value={address}
                                    onChange={(e) => setAddress(e.target.value)}
                                    placeholder="e.g. 123 Main St"
                                    disabled={saving}
                                />
                            </div>

                            <div className="settings-field">
                                <label>Phone</label>
                                <input
                                    type="text"
                                    value={phone}
                                    onChange={(e) => setPhone(e.target.value)}
                                    placeholder="e.g. +370..."
                                    disabled={saving}
                                />
                            </div>

                            <div className="settings-field">
                                <label>Email</label>
                                <input
                                    type="email"
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    placeholder="e.g. info@business.com"
                                    disabled={saving}
                                />
                            </div>

                            <div className="settings-field">
                                <label>Country</label>
                                <BeautySelect 
                                    value={country} 
                                    onChange={setCountry} 
                                    disabled={saving}
                                    options={[{ value: "LT", label: "Lithuania" }]} 
                                />
                            </div>

                            <div className="settings-field">
                                <label>Price Includes Tax</label>
                                <BeautySelect
                                    value={priceIncludesTax ? "true" : "false"}
                                    onChange={(val) => setPriceIncludesTax(val === "true")}
                                    disabled={saving}
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
                            {savedAt && <span className="muted">Saved at {new Date(savedAt).toLocaleTimeString()}</span>}
                        </div>
                    </div>
                </>
            )}
        </div>
    );
}
