import { useState } from "react";
import "./BeautyServices.css";
import type { CatalogItemSummaryResponse } from "../../../types/api";
import * as catalogService from "../../../services/catalogService";

interface BeautyServicesProps {
    services: CatalogItemSummaryResponse[];
    businessId: number;
    onRefresh: () => void;
}

export default function BeautyServices({ services, businessId, onRefresh }: BeautyServicesProps) {
    const [showModal, setShowModal] = useState(false);
    const [name, setName] = useState("");
    const [code, setCode] = useState("");
    const [basePrice, setBasePrice] = useState("");
    const [taxClass, setTaxClass] = useState("Standard");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    return (
        <div className="services-container">
            <div className="action-bar">
                <h2 className="section-title">Services</h2>
                <button
                    className="btn btn-primary"
                    onClick={() => setShowModal(true)}
                >
                    ➕ Add Service
                </button>
            </div>

            <div className="services-list">
                {services.length > 0 ? (
                    services.map(service => (
                        <div key={service.catalogItemId} className="service-card">
                            <div>
                                <div className="service-name">{service.name}</div>
                                <div className="service-code">Code: {service.code}</div>
                                <div className="service-price">
                                    €{service.basePrice.toFixed(2)}
                                </div>
                                <div className="service-status">Status: {service.status}</div>
                            </div>

                            <div className="service-actions">
                                <button className="btn-small">Edit</button>
                                <button
                                    className="btn-small btn-danger"
                                    onClick={async () => {
                                        if (confirm(`Are you sure you want to archive ${service.name}?`)) {
                                            try {
                                                await catalogService.archiveCatalogItem(businessId, service.catalogItemId);
                                                onRefresh();
                                            } catch (err) {
                                                alert(err instanceof Error ? err.message : "Failed to archive service");
                                            }
                                        }
                                    }}
                                >
                                    Archive
                                </button>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="no-services">No services found</div>
                )}
            </div>

            {/* ✅ ADD SERVICE MODAL */}
            {showModal && (
                <div className="modal-overlay" onClick={() => setShowModal(false)}>
                    <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                        <h3 className="modal-title">Add Service</h3>

                        {error && (
                            <div style={{ color: "red", marginBottom: "1rem" }}>
                                {error}
                            </div>
                        )}

                        <form
                            onSubmit={async (e) => {
                                e.preventDefault();
                                setIsSubmitting(true);
                                setError(null);
                                try {
                                    await catalogService.createCatalogItem(businessId, {
                                        name,
                                        code,
                                        type: "Service",
                                        basePrice: parseFloat(basePrice),
                                        taxClass,
                                    });
                                    setName("");
                                    setCode("");
                                    setBasePrice("");
                                    setTaxClass("Standard");
                                    setShowModal(false);
                                    onRefresh();
                                } catch (err) {
                                    setError(err instanceof Error ? err.message : "Failed to create service");
                                } finally {
                                    setIsSubmitting(false);
                                }
                            }}
                        >
                            <div className="modal-form">
                                <div className="modal-field">
                                    <label>Service Name *</label>
                                    <input
                                        type="text"
                                        value={name}
                                        onChange={(e) => setName(e.target.value)}
                                        required
                                    />
                                </div>

                                <div className="modal-field">
                                    <label>Code *</label>
                                    <input
                                        type="text"
                                        value={code}
                                        onChange={(e) => setCode(e.target.value.toUpperCase())}
                                        required
                                    />
                                </div>

                                <div className="modal-field">
                                    <label>Price (EUR) *</label>
                                    <input
                                        type="number"
                                        step="0.01"
                                        min="0"
                                        value={basePrice}
                                        onChange={(e) => setBasePrice(e.target.value)}
                                        required
                                    />
                                </div>

                                <div className="modal-field">
                                    <label>Tax Class *</label>
                                    <select
                                        value={taxClass}
                                        onChange={(e) => setTaxClass(e.target.value)}
                                        required
                                    >
                                        <option value="Standard">Standard</option>
                                        <option value="Reduced">Reduced</option>
                                        <option value="Zero">Zero</option>
                                    </select>
                                </div>
                            </div>

                            <div className="modal-actions">
                                <button
                                    type="button"
                                    className="btn btn-secondary"
                                    onClick={() => setShowModal(false)}
                                    disabled={isSubmitting}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="btn btn-success"
                                    disabled={isSubmitting}
                                >
                                    {isSubmitting ? "Saving..." : "Save Service"}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}
