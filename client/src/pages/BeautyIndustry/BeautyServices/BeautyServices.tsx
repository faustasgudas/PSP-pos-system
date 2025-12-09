import { useState } from "react";
import "./BeautyServices.css";

interface Service {
    id: number;
    name: string;
    basePrice: { amount: number; currency: string };
}

interface BeautyServicesProps {
    services: Service[];
}

export default function BeautyServices({ services }: BeautyServicesProps) {
    const [showModal, setShowModal] = useState(false);

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
                        <div key={service.id} className="service-card">
                            <div>
                                <div className="service-name">{service.name}</div>
                                <div className="service-price">
                                    {service.basePrice.amount} {service.basePrice.currency}
                                </div>
                            </div>

                            <div className="service-actions">
                                <button className="btn-small">Edit</button>
                                <button className="btn-small btn-danger">Deactivate</button>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="no-services">No services found</div>
                )}
            </div>

            {/* ✅ ADD SERVICE MODAL */}
            {showModal && (
                <div className="modal-overlay">
                    <div className="modal-card">
                        <h3 className="modal-title">Add Service</h3>

                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Service Name</label>
                                <input type="text" />
                            </div>

                            <div className="modal-field">
                                <label>Price</label>
                                <input type="number" />
                            </div>

                            <div className="modal-field">
                                <label>Currency</label>
                                <select>
                                    <option value="EUR">EUR</option>
                                </select>
                            </div>
                        </div>

                        <div className="modal-actions">
                            <button
                                className="btn btn-secondary"
                                onClick={() => setShowModal(false)}
                            >
                                Cancel
                            </button>

                            <button className="btn btn-success">
                                Save Service
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
