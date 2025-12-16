import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import { createReservation } from "../../../frontapi/reservationsApi";
import { listCatalogItems, type CatalogItem } from "../../../frontapi/catalogApi";
import { fetchEmployees } from "../../../frontapi/employeesApi";
import { BeautySelect } from "../../../components/ui/BeautySelect";

export default function CateringNewReservation(props: { goBack: () => void }) {
    const businessId = Number(localStorage.getItem("businessId"));

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [services, setServices] = useState<CatalogItem[]>([]);
    const [employees, setEmployees] = useState<any[]>([]);

    const [serviceId, setServiceId] = useState<string>("");
    const [employeeId, setEmployeeId] = useState<string>("");
    const [tableOrArea, setTableOrArea] = useState<string>("");
    const [date, setDate] = useState<string>("");
    const [time, setTime] = useState<string>("");
    const [notes, setNotes] = useState<string>("");

    const selectedService = useMemo(() => {
        const id = Number(serviceId);
        if (!id) return null;
        return services.find((s) => s.catalogItemId === id) ?? null;
    }, [serviceId, services]);

    useEffect(() => {
        if (!businessId) {
            setError("Missing businessId");
            setLoading(false);
            return;
        }

        setLoading(true);
        setError(null);
        Promise.all([listCatalogItems(businessId, { type: "Service" }), fetchEmployees(businessId)])
            .then(([svc, emps]) => {
                setServices((Array.isArray(svc) ? svc : []).filter((s) => String(s.status).toLowerCase() === "active"));
                setEmployees(Array.isArray(emps) ? emps : []);
            })
            .catch((e: any) => setError(e?.message || "Failed to load reservation data"))
            .finally(() => setLoading(false));
    }, [businessId]);

    const save = async () => {
        if (!businessId) return setError("Missing businessId");
        if (saving) return;

        const catalogItemId = Number(serviceId);
        if (!catalogItemId) return setError("Select a service");

        const empId = employeeId ? Number(employeeId) : null;
        if (employeeId && (!Number.isFinite(empId) || !empId)) return setError("Invalid employee selection");

        if (!date || !time) return setError("Select date and time");
        const start = new Date(`${date}T${time}:00`);
        if (Number.isNaN(start.getTime())) return setError("Invalid date/time");

        setSaving(true);
        setError(null);
        try {
            await createReservation(businessId, {
                catalogItemId,
                employeeId: empId,
                appointmentStart: start.toISOString(),
                notes: notes.trim() || null,
                tableOrArea: tableOrArea.trim() || null,
            });
            props.goBack();
        } catch (e: any) {
            setError(e?.message || "Failed to create reservation");
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="page">
            <div className="action-bar">
                <h2 className="section-title">New Reservation</h2>
                <button className="btn" onClick={props.goBack} disabled={saving}>
                    ← Back
                </button>
            </div>

            {error && (
                <div className="card" style={{ borderColor: "rgba(214,40,40,0.3)", background: "rgba(214,40,40,0.08)", color: "#b01d1d", marginBottom: 12 }}>
                    {error}
                </div>
            )}

            <div className="card" style={{ textAlign: "left" }}>
                <div className="muted">Service</div>
                <BeautySelect
                    value={serviceId}
                    onChange={setServiceId}
                    disabled={loading || saving}
                    placeholder="Select service"
                    options={[
                        { value: "", label: "Select service" },
                        ...services.map((s) => ({
                            value: String(s.catalogItemId),
                            label: s.name,
                            subLabel: `€${Number(s.basePrice).toFixed(2)}`,
                        })),
                    ]}
                />
                {selectedService && (
                    <div className="muted" style={{ marginBottom: 10 }}>
                        Default duration: {selectedService.defaultDurationMin ?? 0} min
                    </div>
                )}

                <div className="muted">Employee (optional)</div>
                <BeautySelect
                    value={employeeId}
                    onChange={setEmployeeId}
                    disabled={loading || saving}
                    placeholder="Select employee"
                    options={[
                        { value: "", label: "Select employee" },
                        ...employees.map((e: any) => ({
                            value: String(e.employeeId ?? e.id),
                            label: String(e.name ?? "Employee"),
                        })),
                    ]}
                />

                <div className="muted">Table / Area (optional)</div>
                <input className="dropdown" value={tableOrArea} onChange={(e) => setTableOrArea(e.target.value)} disabled={saving} placeholder="e.g. T12 / Patio" />

                <div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>
                    <div style={{ flex: 1, minWidth: 220 }}>
                        <div className="muted">Date</div>
                        <input className="dropdown" type="date" value={date} onChange={(e) => setDate(e.target.value)} disabled={saving} />
                    </div>
                    <div style={{ flex: 1, minWidth: 220 }}>
                        <div className="muted">Time</div>
                        <input className="dropdown" type="time" value={time} onChange={(e) => setTime(e.target.value)} disabled={saving} />
                    </div>
                </div>

                <div className="muted">Notes</div>
                <textarea className="dropdown" style={{ minHeight: 100 }} value={notes} onChange={(e) => setNotes(e.target.value)} disabled={saving} />

                <button className="btn btn-primary" onClick={save} disabled={saving || loading}>
                    {saving ? "Saving…" : "Create reservation"}
                </button>
            </div>
        </div>
    );
}


