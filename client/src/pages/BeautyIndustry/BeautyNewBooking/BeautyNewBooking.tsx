import "./BeautyNewBooking.css";
import { useEffect, useMemo, useState } from "react";
import { createReservation } from "../../../frontapi/reservationsApi";
import { getActiveServices, type CatalogItem } from "../../../frontapi/catalogApi";
import { fetchEmployees } from "../../../frontapi/employeesApi";

export default function BeautyNewBooking({
                                             goBack,
                                         }: {
    goBack: () => void;
}) {
    const businessId = Number(localStorage.getItem("businessId"));

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [saving, setSaving] = useState(false);

    const [services, setServices] = useState<CatalogItem[]>([]);
    const [employees, setEmployees] = useState<any[]>([]);

    // UI fields (backend only supports notes, so we embed these into notes for now)
    const [clientName, setClientName] = useState("");
    const [phone, setPhone] = useState("");

    const [serviceId, setServiceId] = useState<string>("");
    const [employeeId, setEmployeeId] = useState<string>("");
    const [date, setDate] = useState<string>("");
    const [time, setTime] = useState<string>("");
    const [notes, setNotes] = useState<string>("");

    const selectedService = useMemo(() => {
        const id = Number(serviceId);
        return services.find((s) => s.catalogItemId === id) ?? null;
    }, [services, serviceId]);

    useEffect(() => {
        const load = async () => {
            if (!businessId) {
                setError("Missing businessId");
                setLoading(false);
                return;
            }

            setLoading(true);
            setError(null);
            try {
                const [svc, emp] = await Promise.all([
                    getActiveServices(businessId),
                    fetchEmployees(businessId),
                ]);
                setServices(Array.isArray(svc) ? svc : []);
                setEmployees(Array.isArray(emp) ? emp : []);
            } catch (e: any) {
                setError(e?.message || "Failed to load booking data");
            } finally {
                setLoading(false);
            }
        };

        load();
    }, [businessId]);

    const save = async () => {
        if (saving) return;
        if (!businessId) {
            setError("Missing businessId");
            return;
        }

        const catalogItemId = Number(serviceId);
        if (!catalogItemId) {
            setError("Select a service");
            return;
        }

        if (!date || !time) {
            setError("Select date and time");
            return;
        }

        const appointmentStart = new Date(`${date}T${time}:00`);
        if (Number.isNaN(appointmentStart.getTime())) {
            setError("Invalid date/time");
            return;
        }

        // Notes embedding to preserve UI intent without changing backend schema.
        const parts: string[] = [];
        if (clientName.trim()) parts.push(`Client: ${clientName.trim()}`);
        if (phone.trim()) parts.push(`Phone: ${phone.trim()}`);
        if (notes.trim()) parts.push(notes.trim());
        const finalNotes = parts.join("\n");

        const empId = employeeId ? Number(employeeId) : null;

        setSaving(true);
        setError(null);
        try {
            await createReservation(businessId, {
                catalogItemId,
                employeeId: empId || null,
                appointmentStart: appointmentStart.toISOString(),
                notes: finalNotes || null,
                tableOrArea: null,
            });
            goBack();
        } catch (e: any) {
            setError(e?.message || "Failed to create reservation");
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="new-booking-container">
            <div className="action-bar">
                <h2 className="section-title">New Booking</h2>
                <button className="btn btn-secondary" onClick={goBack}>
                    Back to Reservations
                </button>
            </div>

            {error && (
                <div style={{ marginTop: 12 }} className="new-booking-card">
                    <div style={{ color: "#b01d1d" }}>{error}</div>
                </div>
            )}

            <div className="new-booking-card">
                <div className="new-booking-grid">
                    <div className="nb-field">
                        <label>Client Name</label>
                        <input
                            type="text"
                            value={clientName}
                            onChange={(e) => setClientName(e.target.value)}
                            disabled={loading || saving}
                        />
                    </div>

                    <div className="nb-field">
                        <label>Phone</label>
                        <input
                            type="text"
                            value={phone}
                            onChange={(e) => setPhone(e.target.value)}
                            disabled={loading || saving}
                        />
                    </div>

                    <div className="nb-field">
                        <label>Service</label>
                        <select
                            value={serviceId}
                            onChange={(e) => setServiceId(e.target.value)}
                            disabled={loading || saving}
                        >
                            <option value="">Select service</option>
                            {services.map((s) => (
                                <option key={s.catalogItemId} value={s.catalogItemId}>
                                    {s.name} — €{s.basePrice.toFixed(2)}
                                </option>
                            ))}
                        </select>
                        {selectedService && (
                            <div className="muted" style={{ marginTop: 6 }}>
                                Default duration: {selectedService.defaultDurationMin ?? 0} min
                            </div>
                        )}
                    </div>

                    <div className="nb-field">
                        <label>Employee</label>
                        <select
                            value={employeeId}
                            onChange={(e) => setEmployeeId(e.target.value)}
                            disabled={loading || saving}
                        >
                            <option value="">Select employee (optional)</option>
                            {employees.map((emp: any) => (
                                <option key={emp.employeeId ?? emp.id} value={emp.employeeId ?? emp.id}>
                                    {emp.name}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="nb-field">
                        <label>Date</label>
                        <input
                            type="date"
                            value={date}
                            onChange={(e) => setDate(e.target.value)}
                            disabled={loading || saving}
                        />
                    </div>

                    <div className="nb-field">
                        <label>Time</label>
                        <input
                            type="time"
                            value={time}
                            onChange={(e) => setTime(e.target.value)}
                            disabled={loading || saving}
                        />
                    </div>

                    <div className="nb-field full">
                        <label>Notes</label>
                        <textarea
                            rows={3}
                            value={notes}
                            onChange={(e) => setNotes(e.target.value)}
                            disabled={loading || saving}
                        />
                    </div>
                </div>

                <div className="new-booking-actions">
                    <button className="btn btn-primary" onClick={save} disabled={loading || saving}>
                        {saving ? "Saving…" : "Save Booking"}
                    </button>
                </div>
            </div>
        </div>
    );
}
