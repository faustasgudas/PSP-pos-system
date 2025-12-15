import { useEffect, useState } from "react";
import "./BeautyServices.css";
import { getUserFromToken } from "../../../utils/auth";

interface Service {
    catalogItemId: number;
    name: string;
    basePrice: number;
    status: string;
}

export default function BeautyServices() {
    const user = getUserFromToken();
    const role = user?.role;

    const token = localStorage.getItem("token");
    const businessId = localStorage.getItem("businessId");

    const [services, setServices] = useState<Service[]>([]);
    const [loading, setLoading] = useState(true);

    const [showAddModal, setShowAddModal] = useState(false);
    const [showEditModal, setShowEditModal] = useState<Service | null>(null);

    const [name, setName] = useState("");
    const [price, setPrice] = useState("");

    const fetchServices = async () => {
        try {
            const res = await fetch(
                `http://localhost:5269/api/businesses/${businessId}/catalog-items?type=Service`,
                {
                    headers: {
                        Authorization: `Bearer ${token}`,
                    },
                }
            );

            if (!res.ok) throw new Error();
            const data = await res.json();
            setServices(data);
        } catch {
            setServices([]);
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
        await fetch(
            `http://localhost:5269/api/businesses/${businessId}/catalog-items`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify({
                    name,
                    code: name.toUpperCase().replace(/\s+/g, "_"),
                    type: "Service",
                    basePrice: Number(price),
                    taxClass: "STANDARD",
                    defaultDurationMin: 30,
                }),
            }
        );

        setShowAddModal(false);
        setName("");
        setPrice("");
        fetchServices();
    };

    const handleDeactivate = async (id: number) => {
        await fetch(
            `http://localhost:5269//api/businesses/${businessId}/catalog-items/${id}/archive`,
            {
                method: "POST",
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );
        fetchServices();
    };

    const handleReactivate = async (id: number) => {
        await fetch(
            `http://localhost:5269/api/businesses/${businessId}/catalog-items/${id}`,
            {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify({ status: "Active" }),
            }
        );
        fetchServices();
    };

    const handleEditPrice = async () => {
        if (!showEditModal) return;

        await fetch(
            `http://localhost:5269/api/businesses/${businessId}/catalog-items/${showEditModal.catalogItemId}`,
            {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify({ basePrice: Number(price) }),
            }
        );

        setShowEditModal(null);
        setPrice("");
        fetchServices();
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
                                        setPrice(String(service.basePrice));
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
                            placeholder="Price"
                            type="number"
                            value={price}
                            onChange={e => setPrice(e.target.value)}
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
                        <h3>Edit Price</h3>

                        <input
                            type="number"
                            value={price}
                            onChange={e => setPrice(e.target.value)}
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
