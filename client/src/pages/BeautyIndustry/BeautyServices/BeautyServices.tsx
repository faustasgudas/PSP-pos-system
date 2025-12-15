import { useEffect, useState } from "react";
import "./BeautyServices.css";
import { getUserFromToken } from "../../../utils/auth";
import {
    archiveCatalogItem,
    createCatalogItem,
    listCatalogItems,
    updateCatalogItem,
    type CatalogItem,
} from "../../../frontapi/catalogApi";
import { logout } from "../../../frontapi/authApi";

export default function BeautyServices() {
    const user = getUserFromToken();
    const role = user?.role;

    const businessId = Number(localStorage.getItem("businessId"));

    const [services, setServices] = useState<CatalogItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [showAddModal, setShowAddModal] = useState(false);
    const [showEditModal, setShowEditModal] = useState<CatalogItem | null>(null);

    const [name, setName] = useState("");
    const [code, setCode] = useState("");
    const [price, setPrice] = useState("");
    const [taxClass, setTaxClass] = useState("STANDARD");
    const [durationMin, setDurationMin] = useState("30");

    const fetchServices = async () => {
        try {
            setLoading(true);
            setError(null);
            if (!businessId) throw new Error("Missing businessId");

            const data = await listCatalogItems(businessId, { type: "Service" });
            setServices(Array.isArray(data) ? data : []);
        } catch (e: any) {
            setServices([]);
            setError(e?.message || "Failed to load services");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchServices();
    }, []);

    const activeServices = services.filter(s => s.status === "Active");
    const archivedServices = services.filter(s => s.status === "Archived");

    const handleAddService = async () => {
        try {
            setError(null);
            if (!businessId) throw new Error("Missing businessId");
            if (!name.trim()) throw new Error("Service name is required");
            const p = Number(price);
            if (!Number.isFinite(p) || p < 0) throw new Error("Invalid price");

            const c = (code || name)
                .trim()
                .toUpperCase()
                .replace(/\s+/g, "_")
                .slice(0, 24);

            const d = Number(durationMin);
            if (!Number.isFinite(d) || d < 0) throw new Error("Invalid duration");

            await createCatalogItem(businessId, {
                name: name.trim(),
                code: c,
                type: "Service",
                basePrice: p,
                taxClass: taxClass.trim() || "STANDARD",
                defaultDurationMin: d,
                status: "Active",
            });

            setShowAddModal(false);
            setName("");
            setCode("");
            setPrice("");
            setTaxClass("STANDARD");
            setDurationMin("30");
            fetchServices();
        } catch (e: any) {
            setError(e?.message || "Failed to add service");
        }
    };

    const handleDeactivate = async (id: number) => {
        try {
            setError(null);
            if (!businessId) throw new Error("Missing businessId");
            await archiveCatalogItem(businessId, id);
            fetchServices();
        } catch (e: any) {
            setError(e?.message || "Failed to deactivate service");
        }
    };

    const handleReactivate = async (id: number) => {
        try {
            setError(null);
            if (!businessId) throw new Error("Missing businessId");
            await updateCatalogItem(businessId, id, { status: "Active" });
            fetchServices();
        } catch (e: any) {
            setError(e?.message || "Failed to reactivate service");
        }
    };

    const handleEditPrice = async () => {
        if (!showEditModal) return;

        try {
            setError(null);
            if (!businessId) throw new Error("Missing businessId");
            const p = Number(price);
            if (!Number.isFinite(p) || p < 0) throw new Error("Invalid price");

            const d = Number(durationMin);
            if (!Number.isFinite(d) || d < 0) throw new Error("Invalid duration");

            const c = (code || showEditModal.code || showEditModal.name)
                .trim()
                .toUpperCase()
                .replace(/\s+/g, "_")
                .slice(0, 24);

            await updateCatalogItem(businessId, showEditModal.catalogItemId, {
                name: name.trim() || showEditModal.name,
                code: c,
                basePrice: p,
                taxClass: taxClass.trim() || showEditModal.taxClass,
                defaultDurationMin: d,
            });

            setShowEditModal(null);
            setName("");
            setCode("");
            setPrice("");
            setTaxClass("STANDARD");
            setDurationMin("30");
            fetchServices();
        } catch (e: any) {
            setError(e?.message || "Failed to update service");
        }
    };

    if (loading) return <div>Loading services…</div>;

    return (
        <div className="services-page">
            <div className="services-header">
                <h2>Services</h2>

                {(role === "Owner" || role === "Manager") && (
                    <button className="btn btn-primary" onClick={() => setShowAddModal(true)}>
                        ➕ Add Service
                    </button>
                )}
            </div>

            {error && (
                <div style={{ margin: "10px 0" }} className="services-divider">
                    <div style={{ color: "#b01d1d" }}>{error}</div>
                    {String(error).toLowerCase().includes("forbid") ||
                    String(error).includes("401") ||
                    String(error).includes("403") ? (
                        <button
                            className="btn"
                            style={{ marginTop: 10 }}
                            onClick={() => {
                                logout();
                                window.location.reload();
                            }}
                        >
                            Log out
                        </button>
                    ) : null}
                </div>
            )}

            {/* ACTIVE */}
            <h3>Active Services</h3>
            <div className="services-grid">
                {activeServices.map(service => (
                    <div key={service.catalogItemId} className="service-card">
                        <div className="service-name">{service.name}</div>
                        <div className="service-price">€{service.basePrice}</div>

                        {(role === "Owner" || role === "Manager") && (
                            <div className="card-actions">
                                <button
                                    className="btn"
                                    onClick={() => {
                                        setShowEditModal(service);
                                        setName(service.name);
                                        setCode(service.code);
                                        setPrice(String(service.basePrice));
                                        setTaxClass(service.taxClass || "STANDARD");
                                        setDurationMin(String(service.defaultDurationMin ?? 30));
                                    }}
                                >
                                    ✏️ Edit
                                </button>
                                <button
                                    className="btn btn-danger"
                                    onClick={() => handleDeactivate(service.catalogItemId)}
                                >
                                    Deactivate
                                </button>
                            </div>
                        )}
                    </div>
                ))}
            </div>

            <hr className="services-divider" />

            {/* ARCHIVED */}
            <h3 className="muted">Deactivated Services</h3>
            <div className="services-grid archived">
                {archivedServices.map(service => (
                    <div key={service.catalogItemId} className="service-card archived">
                        <div className="service-name">{service.name}</div>
                        <div className="service-price">€{service.basePrice}</div>

                        {(role === "Owner" || role === "Manager") && (
                            <button
                                className="btn btn-success"
                                onClick={() => handleReactivate(service.catalogItemId)}
                            >
                                ♻️ Reactivate
                            </button>
                        )}
                    </div>
                ))}
            </div>

            {/* ADD MODAL */}
            {showAddModal && (
                <div className="modal-backdrop">
                    <div className="modal">
                        <h3>Add Service</h3>

                        <input
                            placeholder="Service name"
                            value={name}
                            onChange={e => setName(e.target.value)}
                        />

                        <input
                            placeholder="Code (optional)"
                            value={code}
                            onChange={(e) => setCode(e.target.value)}
                        />

                        <input
                            placeholder="Price"
                            type="number"
                            value={price}
                            onChange={e => setPrice(e.target.value)}
                        />

                        <input
                            placeholder="Tax class (e.g. STANDARD)"
                            value={taxClass}
                            onChange={(e) => setTaxClass(e.target.value)}
                        />

                        <input
                            placeholder="Default duration (min)"
                            type="number"
                            value={durationMin}
                            onChange={(e) => setDurationMin(e.target.value)}
                        />

                        <div className="modal-actions">
                            <button className="btn" onClick={() => setShowAddModal(false)}>
                                Cancel
                            </button>
                            <button className="btn btn-primary" onClick={handleAddService}>
                                Add
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* EDIT MODAL */}
            {showEditModal && (
                <div className="modal-backdrop">
                    <div className="modal">
                        <h3>Edit Service</h3>

                        <input
                            placeholder="Service name"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                        />

                        <input
                            placeholder="Code"
                            value={code}
                            onChange={(e) => setCode(e.target.value)}
                        />

                        <input
                            type="number"
                            value={price}
                            onChange={e => setPrice(e.target.value)}
                        />

                        <input
                            placeholder="Tax class"
                            value={taxClass}
                            onChange={(e) => setTaxClass(e.target.value)}
                        />

                        <input
                            placeholder="Default duration (min)"
                            type="number"
                            value={durationMin}
                            onChange={(e) => setDurationMin(e.target.value)}
                        />

                        <div className="modal-actions">
                            <button className="btn" onClick={() => setShowEditModal(null)}>
                                Cancel
                            </button>
                            <button className="btn btn-primary" onClick={handleEditPrice}>
                                Save
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
