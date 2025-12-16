import "./BeautyNewBooking.css";
import { useEffect, useMemo, useState } from "react";
import { createReservation } from "../../../frontapi/reservationsApi";
import { getActiveServices, type CatalogItem } from "../../../frontapi/catalogApi";
import { fetchEmployees } from "../../../frontapi/employeesApi";
import { BeautySelect } from "../../../components/ui/BeautySelect";
import { BeautyDatePicker } from "../../../components/ui/BeautyDatePicker";
import { BeautyTimePicker } from "../../../components/ui/BeautyTimePicker";

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

    const quickDateLabel = useMemo(() => {
        if (!date) return "";
        const d = new Date(`${date}T00:00:00`);
        if (Number.isNaN(d.getTime())) return "";
        return d.toLocaleDateString([], { weekday: "long", year: "numeric", month: "long", day: "numeric" });
    }, [date]);

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

            <div className="new-booking-layout">
                {/* LEFT: details */}
                <div className="new-booking-card">
                    <div className="new-booking-grid">
                        <div className="nb-field">
                            <label>Client Name</label>
                            <input
                                type="text"
                                value={clientName}
                                onChange={(e) => setClientName(e.target.value)}
                                disabled={loading || saving}
                                placeholder="e.g. Ema Petrauskaitė"
                            />
                        </div>

                        <div className="nb-field">
                            <label>Phone</label>
                            <input
                                type="text"
                                value={phone}
                                onChange={(e) => setPhone(e.target.value)}
                                disabled={loading || saving}
                                placeholder="e.g. +370..."
                            />
                        </div>

                        <div className="nb-field full">
                            <BeautySelect
                                label="Service"
                                value={serviceId}
                                onChange={setServiceId}
                                disabled={loading || saving}
                                placeholder="Select service"
                                options={[
                                    { value: "", label: "Select service", subLabel: "Choose what to book" },
                                    ...services.map((s) => ({
                                        value: String(s.catalogItemId),
                                        label: s.name,
                                        subLabel: `€${s.basePrice.toFixed(2)} • ${s.defaultDurationMin ?? 0} min`,
                                    })),
                                ]}
                            />
                            {selectedService && (
                                <div className="muted" style={{ marginTop: 8 }}>
                                    Default duration: {selectedService.defaultDurationMin ?? 0} min
                                </div>
                            )}
                        </div>

                        <div className="nb-field full">
                            <BeautySelect
                                label="Employee (optional)"
                                value={employeeId}
                                onChange={setEmployeeId}
                                disabled={loading || saving}
                                placeholder="Select employee"
                                options={[
                                    { value: "", label: "Any employee", subLabel: "Auto-assign / optional" },
                                    ...employees.map((emp: any) => ({
                                        value: String(emp.employeeId ?? emp.id),
                                        label: String(emp.name ?? "Employee"),
                                        subLabel: "",
                                    })),
                                ]}
                            />
                        </div>

                        <div className="nb-field full">
                            <label>Notes</label>
                            <textarea
                                rows={3}
                                value={notes}
                                onChange={(e) => setNotes(e.target.value)}
                                disabled={loading || saving}
                                placeholder="Optional notes…"
                            />
                        </div>
                    </div>
                </div>

                {/* RIGHT: schedule */}
                <div className="new-booking-card schedule-card">
                    <div className="schedule-header">
                        <div>
                            <div className="schedule-title">Schedule</div>
                            <div className="muted">
                                {quickDateLabel ? quickDateLabel : "Pick date and time"}
                                {time ? ` • ${time}` : ""}
                            </div>
                        </div>
                        <div className="schedule-quick">
                            <button
                                className="btn btn-ghost"
                                disabled={loading || saving}
                                onClick={() => {
                                    const d = new Date();
                                    const yyyy = d.getFullYear();
                                    const mm = String(d.getMonth() + 1).padStart(2, "0");
                                    const dd = String(d.getDate()).padStart(2, "0");
                                    setDate(`${yyyy}-${mm}-${dd}`);
                                }}
                            >
                                Today
                            </button>
                            <button
                                className="btn btn-ghost"
                                disabled={loading || saving}
                                onClick={() => {
                                    const d = new Date();
                                    d.setDate(d.getDate() + 1);
                                    const yyyy = d.getFullYear();
                                    const mm = String(d.getMonth() + 1).padStart(2, "0");
                                    const dd = String(d.getDate()).padStart(2, "0");
                                    setDate(`${yyyy}-${mm}-${dd}`);
                                }}
                            >
                                Tomorrow
                            </button>
                        </div>
                    </div>

                    <div className="schedule-grid">
                        <BeautyDatePicker
                            label="Date"
                            value={date}
                            onChange={setDate}
                            disabled={loading || saving}
                            placeholder="Pick a date"
                        />

                        <BeautyTimePicker
                            label="Time"
                            value={time}
                            onChange={setTime}
                            disabled={loading || saving}
                            placeholder="Pick a time"
                        />
                    </div>

                    <div className="new-booking-actions">
                        <button className="btn btn-primary" onClick={save} disabled={loading || saving}>
                            {saving ? "Saving…" : "Save Booking"}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}
