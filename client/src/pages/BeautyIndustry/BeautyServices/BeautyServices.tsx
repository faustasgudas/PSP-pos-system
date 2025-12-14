import { useEffect, useState } from "react";
import "./BeautyServices.css";
import {
    deactivateService,
    updateService,
    createService,
} from "../../../frontapi/catalogApi";

interface Service {
    catalogItemId: number;
    name: string;
    basePrice: number;
    status: string;
    defaultDurationMin: number;
}

export default function BeautyServices() {
    const [services, setServices] = useState<Service[]>([]);
    const [editing, setEditing] = useState<Service | null>(null);
    const [confirmDeactivate, setConfirmDeactivate] =
        useState<Service | null>(null);
    const [adding, setAdding] = useState(false);

    const [name, setName] = useState("");
    const [price, setPrice] = useState("");
    const [duration, setDuration] = useState("");
    const [status, setStatus] = useState("Active");

    const businessId = Number(localStorage.getItem("businessId"));
    const token = localStorage.getItem("token");

    useEffect(() => {
        if (!token || !businessId) return;

        fetch(
            `https://localhost:44317/api/businesses/${businessId}/catalog-items?type=Service&status=Active`,
            {
                headers: { Authorization: `Bearer ${token}` },
            }
        )
            .then((r) => r.json())
            .then(setServices);
    }, [businessId, token]);

    const saveEdit = async () => {
        if (!editing) return;

        const updated = await updateService(
            businessId,
            editing.catalogItemId,
            {
                name,
                basePrice: Number(price),
                defaultDurationMin: Number(duration),
                status,
            }
        );

        setServices((prev) =>
            prev.map((s) =>
                s.catalogItemId === updated.catalogItemId ? updated : s
            )
        );

        setEditing(null);
    };

    const confirmArchive = async () => {
        if (!confirmDeactivate) return;

        await deactivateService(
            businessId,
            confirmDeactivate.catalogItemId
        );

        setServices((prev) =>
            prev.filter(
                (s) =>
                    s.catalogItemId !==
                    confirmDeactivate.catalogItemId
            )
        );

        setConfirmDeactivate(null);
    };

    const saveNewService = async () => {
        const created = await createService(businessId, {
            name,
            code: name.toUpperCase().replace(/\s+/g, "_"),
            type: "Service",
            basePrice: Number(price),
            defaultDurationMin: Number(duration),
            taxClass: "Service",
            status,
        });

        setServices((prev) => [...prev, created]);
        setAdding(false);
    };

    return (
        <div className="services-page">
            <div className="services-header">
                <h1>Services</h1>
                <button
                    className="add-service-btn"
                    onClick={() => {
                        setName("");
                        setPrice("");
                        setDuration("");
                        setStatus("Active");
                        setAdding(true);
                    }}
                >
                    + Add Service
                </button>
            </div>

            <div className="services-grid">
                {services.map((s) => (
                    <div key={s.catalogItemId} className="service-card">
                        <div className="service-top">
                            <h2>{s.name}</h2>
                            <span className="service-price">
                                €{s.basePrice.toFixed(2)}
                            </span>
                        </div>


                        <div className="service-actions">
                            <button
                                className="edit-btn"
                                onClick={() => {
                                    setEditing(s);
                                    setName(s.name);
                                    setPrice(s.basePrice.toString());
                                    setDuration(
                                        s.defaultDurationMin.toString()
                                    );
                                    setStatus(s.status);
                                }}
                            >
                                Edit
                            </button>

                            <button
                                className="deactivate-btn"
                                onClick={() =>
                                    setConfirmDeactivate(s)
                                }
                            >
                                Deactivate
                            </button>
                        </div>
                    </div>
                ))}
            </div>

            {/* ADD / EDIT MODAL */}
            {(editing || adding) && (
                <div className="modal-overlay">
                    <div className="modal">
                        <h2>
                            {adding ? "Add Service" : "Edit Service"}
                        </h2>

                        <input
                            placeholder="Service name"
                            value={name}
                            onChange={(e) =>
                                setName(e.target.value)
                            }
                        />

                        <input
                            placeholder="Price (€)"
                            value={price}
                            onChange={(e) =>
                                setPrice(e.target.value)
                            }
                        />

                        <select
                            value={status}
                            onChange={(e) =>
                                setStatus(e.target.value)
                            }
                        >
                            <option value="Active">Active</option>
                            <option value="Archived">Archive</option>
                        </select>

                        <input
                            placeholder="Duration (minutes)"
                            value={duration}
                            onChange={(e) =>
                                setDuration(e.target.value)
                            }
                        />

                        <div className="modal-actions">
                            <button
                                onClick={() =>
                                    adding
                                        ? saveNewService()
                                        : saveEdit()
                                }
                            >
                                Save
                            </button>
                            <button
                                className="secondary"
                                onClick={() => {
                                    setEditing(null);
                                    setAdding(false);
                                }}
                            >
                                Cancel
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* DEACTIVATE CONFIRM */}
            {confirmDeactivate && (
                <div className="modal-overlay">
                    <div className="modal">
                        <h2>Deactivate Service</h2>
                        <p>
                            Are you sure you want to deactivate{" "}
                            <strong>
                                {confirmDeactivate.name}
                            </strong>
                            ?
                        </p>

                        <div className="modal-actions">
                            <button
                                className="danger"
                                onClick={confirmArchive}
                            >
                                Deactivate
                            </button>
                            <button
                                className="secondary"
                                onClick={() =>
                                    setConfirmDeactivate(null)
                                }
                            >
                                Cancel
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
