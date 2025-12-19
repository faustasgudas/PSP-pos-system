import { useEffect, useMemo, useState } from "react";
import "../../../App.css";
import { createReservation } from "../../../frontapi/reservationsApi";
import { createCatalogItem, listCatalogItems, updateCatalogItem, type CatalogItem } from "../../../frontapi/catalogApi";
import { fetchEmployees } from "../../../frontapi/employeesApi";
import { BeautySelect } from "../../../components/ui/BeautySelect";
import { getUserFromToken } from "../../../utils/auth";
import { BeautyDatePicker } from "../../../components/ui/BeautyDatePicker";
import { BeautyTimePicker } from "../../../components/ui/BeautyTimePicker";

export default function CateringNewReservation(props: { goBack: () => void }) {
    const businessId = Number(localStorage.getItem("businessId"));
    const user = getUserFromToken();
    const role = user?.role ?? "";
    const canManage = role === "Owner" || role === "Manager";

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [services, setServices] = useState<CatalogItem[]>([]);
    const [reservationCatalogItemId, setReservationCatalogItemId] = useState<number | null>(null);
    const [employees, setEmployees] = useState<any[]>([]);

    const [employeeId, setEmployeeId] = useState<string>("");
    const [tableOrArea, setTableOrArea] = useState<string>("");
    const [date, setDate] = useState<string>("");
    const [time, setTime] = useState<string>("");
    const [notes, setNotes] = useState<string>("");

    const selectedReservationService = useMemo(() => {
        const id = Number(reservationCatalogItemId);
        if (!id) return null;
        return services.find((s) => s.catalogItemId === id) ?? null;
    }, [reservationCatalogItemId, services]);

    useEffect(() => {
        if (!businessId) {
            setError("Missing businessId");
            setLoading(false);
            return;
        }

        const load = async () => {
            setLoading(true);
            setError(null);
            try {
                const [svc, emps] = await Promise.all([
                    listCatalogItems(businessId, { type: "Service" }),
                    fetchEmployees(businessId),
                ]);

                const allServices = Array.isArray(svc) ? svc : [];
                const activeServices = allServices.filter(
                    (s) => String(s.status).toLowerCase() === "active"
                );
                setServices(activeServices);
                setEmployees(Array.isArray(emps) ? emps : []);

                // Backend requires catalogItemId; we keep a single internal “Reservation” service.
                let reservationSvc =
                    allServices.find((s) => String(s.code).toUpperCase() === "RESERVATION") ??
                    allServices.find((s) => String(s.name).toLowerCase() === "reservation") ??
                    null;

                const REQUIRED_DURATION_MIN = 90;

                const reservationNeedsFix =
                    !!reservationSvc &&
                    (
                        String(reservationSvc.status).toLowerCase() !== "active" ||
                        Number(reservationSvc.defaultDurationMin ?? 0) <= 0
                    );

                // If the internal service exists but is inactive or has invalid duration (<= 0), fix it (Owner/Manager only).
                if (reservationSvc && reservationNeedsFix && canManage) {
                    try {
                        const updated = await updateCatalogItem(businessId, reservationSvc.catalogItemId, {
                            status: "Active",
                            defaultDurationMin: REQUIRED_DURATION_MIN,
                        });
                        reservationSvc = updated;
                        setServices((prev) =>
                            prev.map((x) => (x.catalogItemId === updated.catalogItemId ? updated : x))
                        );
                    } catch {
                        // If we can’t fix it, we’ll fall back below (or error).
                    }
                }

                if (!reservationSvc && canManage) {
                    try {
                        const created = await createCatalogItem(businessId, {
                            name: "Reservation",
                            code: "RESERVATION",
                            type: "Service",
                            basePrice: 0,
                            taxClass: "STANDARD",
                            defaultDurationMin: REQUIRED_DURATION_MIN,
                            status: "Active",
                        });
                        reservationSvc = created;
                        setServices((prev) => [created, ...prev]);
                    } catch {
                        // If we can’t create it, we’ll fall back below.
                    }
                }

                const firstValidServiceId =
                    activeServices.find((s) => Number(s.defaultDurationMin ?? 0) > 0)?.catalogItemId ?? null;

                const reservationIsUsable =
                    !!reservationSvc &&
                    String(reservationSvc.status).toLowerCase() === "active" &&
                    Number(reservationSvc.defaultDurationMin ?? 0) > 0;

                const fallbackId = (reservationIsUsable ? reservationSvc!.catalogItemId : null) ?? firstValidServiceId;
                setReservationCatalogItemId(fallbackId);

                if (!fallbackId) {
                    setError(
                        "Cannot create reservations: backend requires an Active Service with DefaultDurationMin > 0. " +
                        "Create a Service (or ensure RESERVATION service has a valid duration)."
                    );
                }
            } catch (e: any) {
                setError(e?.message || "Failed to load reservation data");
            } finally {
                setLoading(false);
            }
        };

        load();
    }, [businessId, canManage]);

    const save = async () => {
        if (!businessId) return setError("Missing businessId");
        if (saving) return;

        const catalogItemId = Number(reservationCatalogItemId);
        if (!catalogItemId) return setError("Internal reservation type is missing. Refresh and try again.");

        if (!tableOrArea.trim()) return setError("Table is required (e.g. T12)");

        // NOTE: backend rule: Staff can only create reservations for themselves.
        // So for Staff we always send null to let backend auto-assign callerEmployeeId.
        const empId = role === "Staff" ? null : employeeId ? Number(employeeId) : null;
        if (employeeId && role !== "Staff" && (!Number.isFinite(empId) || !empId)) return setError("Invalid employee selection");

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
                tableOrArea: tableOrArea.trim(),
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
                <div className="muted">Table</div>
                <input
                    className="dropdown"
                    value={tableOrArea}
                    onChange={(e) => setTableOrArea(e.target.value)}
                    disabled={saving}
                    placeholder="e.g. T12 / Patio"
                />

                {selectedReservationService && (
                    <div className="muted" style={{ marginBottom: 10 }}>
                        Reservation type: {selectedReservationService.name}
                    </div>
                )}

                <div className="muted">Employee (optional)</div>
                <BeautySelect
                    value={employeeId}
                    onChange={setEmployeeId}
                    disabled={loading || saving || role === "Staff"}
                    placeholder="Select employee"
                    options={[
                        { value: "", label: "Select employee" },
                        ...employees.map((e: any) => ({
                            value: String(e.employeeId ?? e.id),
                            label: String(e.name ?? "Employee"),
                        })),
                    ]}
                />
                {role === "Staff" && (
                    <div className="muted" style={{ marginTop: 8 }}>
                        Staff can only create reservations for themselves (employee is auto-assigned).
                    </div>
                )}

                <div style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>
                    <div style={{ flex: 1, minWidth: 220 }}>
                        <BeautyDatePicker
                            label="Date"
                            value={date}
                            onChange={setDate}
                            disabled={saving}
                        />
                    </div>
                    <div style={{ flex: 1, minWidth: 220 }}>
                        <BeautyTimePicker
                            label="Time"
                            value={time}
                            onChange={setTime}
                            disabled={saving}
                            allowTyping={false}
                            minuteStep={15}
                            minTime="09:00"
                            maxTime="18:00"
                        />
                    </div>
                </div>

                <div className="muted">Notes</div>
                <textarea
                    className="dropdown"
                    style={{ minHeight: 100 }}
                    placeholder="Optional: customer name, phone (+370...), allergies, special requests…"
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    disabled={saving}
                />

                <button className="btn btn-primary" onClick={save} disabled={saving || loading}>
                    {saving ? "Saving…" : "Create reservation"}
                </button>
            </div>
        </div>
    );
}


