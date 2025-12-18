import { useEffect, useMemo, useState } from "react";
import "./CateringReservations.css";
import { getUserFromToken } from "../../../utils/auth";
import {
    cancelReservation,
    getReservation,
    listReservations,
    updateReservation,
    type ReservationSummary,
    type ReservationDetail,
} from "../../../frontapi/reservationsApi";
import { listCatalogItems, type CatalogItem } from "../../../frontapi/catalogApi";
import { fetchEmployees } from "../../../frontapi/employeesApi";

export default function CateringReservations(props: { goToNewReservation: () => void }) {
    const user = getUserFromToken();
    const role = user?.role ?? "";
    const canManage = role === "Owner" || role === "Manager";

    const businessId = Number(localStorage.getItem("businessId"));

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [reservations, setReservations] = useState<ReservationSummary[]>([]);
    const [services, setServices] = useState<CatalogItem[]>([]);
    const [employees, setEmployees] = useState<any[]>([]);

    const [detailedReservations, setDetailedReservations] = useState<Map<number, ReservationDetail>>(new Map());
    const [loadingDetails, setLoadingDetails] = useState(false);

    const [selectedDate, setSelectedDate] = useState(new Date());
    const [currentMonth, setCurrentMonth] = useState(new Date());

    const year = currentMonth.getFullYear();
    const month = currentMonth.getMonth();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    const isSameDay = (a: Date, b: Date) =>
        a.getFullYear() === b.getFullYear() && 
        a.getMonth() === b.getMonth() &&
        a.getDate() === b.getDate();
    
    const monthLabel = currentMonth.toLocaleString("default", {
        month: "long",
        year: "numeric",
    });
    
    const load = async (monthDate: Date) => {
        if (!businessId) {
            setError("Missing businessId");
            setReservations([]);
            setLoading(false);
            return;
        }

        setLoading(true);
        setError(null);
        try {
            const from = new Date(monthDate.getFullYear(), monthDate.getMonth(), 1, 0, 0, 0);
            const to = new Date(monthDate.getFullYear(), monthDate.getMonth() + 1, 1, 0, 0, 0);

            const [resList, svcList, empList] = await Promise.all([
                listReservations(businessId, { dateFrom: from.toISOString(), dateTo: to.toISOString() }),
                listCatalogItems(businessId, { type: "Service" }).catch(() => []),
                fetchEmployees(businessId).catch(() => []),
            ]);

            setReservations(Array.isArray(resList) ? resList : []);
            setServices(Array.isArray(svcList) ? svcList : []);
            setEmployees(Array.isArray(empList) ? empList : []);
        } catch (e: any) {
            setError(e?.message || "Failed to load reservations");
            setReservations([]);
        } finally {
            setLoading(false);
        }
    };

    const loadReservationDetails = async (list: ReservationSummary[]) => {
        if (!businessId || list.length === 0) {
            setDetailedReservations(new Map());
            return;
        }

        setLoadingDetails(true);
        try {
            const details = await Promise.all(list.map((r) => getReservation(businessId, r.reservationId)));
            const m = new Map<number, ReservationDetail>();
            details.forEach((d) => m.set(d.reservationId, d));
            setDetailedReservations(m);
        } catch {
            // non-blocking
        } finally {
            setLoadingDetails(false);
        }
    };

    useEffect(() => {
        load(currentMonth);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [businessId]);

    useEffect(() => {
        load(currentMonth);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [currentMonth]);

    const reservationsForSelectedDate = useMemo(() => {
        return reservations.filter(r =>
            isSameDay(new Date(r.appointmentStart), selectedDate)
        );
    }, [reservations, selectedDate]);

    useEffect(() => {
        void loadReservationDetails(reservationsForSelectedDate);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedDate, reservations]);

    const reservedDates = useMemo(() => {
        const days = new Set<number>();
        reservations.forEach(r => {
            const d = new Date(r.appointmentStart);
            if (d.getFullYear() === year && d.getMonth() === month) {
                days.add(d.getDate());
            }
        });
        return Array.from(days);
    }, [reservations, year, month]);
    
    const serviceNameById = useMemo(() => {
        const m = new Map<number, string>();
        services.forEach((s) => m.set(s.catalogItemId, s.name));
        return m;
    }, [services]);

    const employeeNameById = useMemo(() => {
        const m = new Map<number, string>();
        employees.forEach((e: any) => {
            const id = Number(e.employeeId ?? e.id);
            if (id) m.set(id, e.name ?? `Employee ${id}`);
        });
        return m;
    }, [employees]);

    const doCancel = async (reservationId: number) => {
        if (!businessId) return;
        const ok = window.confirm(`Cancel reservation #${reservationId}?`);
        if (!ok) return;

        setError(null);
        try {
            const detail = await getReservation(businessId, reservationId);
            if (detail.orderId) {
                setError(
                    `Reservation #${reservationId} already has order #${detail.orderId}. Cancel the order instead.`
                );
                return;
            }
            await cancelReservation(businessId, reservationId);
            await load(currentMonth);
        } catch (e: any) {
            setError(e?.message || "Cancel failed");
        }
    };

    const markCompleted = async (reservationId: number) => {
        if (!businessId) return;
        setError(null);
        try {
            await updateReservation(businessId, reservationId, { status: "Completed" });
            await load(currentMonth);
        } catch (e: any) {
            setError(e?.message || "Update failed");
        }
    };
    
    return (
        <div className="reservations-container">
            <div className="action-bar">
                <h2 className="section-title">Reservation Management</h2>
                
                <button
                    className="btn btn-primary"
                    onClick={props.goToNewReservation}
                    disabled={!canManage}
                    title={!canManage ? "Manager/Owner only" : ""}
                >
                    <span>➕</span> New Reservation
                </button>
            </div>

            {error && (
                <div className="reservation-item" style={{ border: "1px solid rgba(214,40,40,0.3)", background: "rgba(214,40,40,0.08)" }}>
                    <div className="reservation-details">
                        <div className="detail-value" style={{ color: "#b01d1d" }}>
                            {error}
                        </div>
                    </div>
                </div>
            )}

            <div className="calendar-wrapper">
                <div className="calendar-header">
                    <button
                        className="cal-nav-btn"
                        onClick={() => setCurrentMonth(new Date(year, month - 1, 1 ))}
                    >
                        ◀ Prev
                    </button>
                    <div className="calendar-title">{monthLabel}</div>
                    <button 
                        className="cal-nav-btn"
                        onClick={() => setCurrentMonth(new Date(year, month + 1, 1 ))}
                    >
                        Next ▶
                    </button>
                </div>
                <div className="calendar-grid">
                    {Array.from({ length: daysInMonth }, (_, i) => {
                        const day = i + 1;
                        const thisDate = new Date(year, month, day);

                        const isSelected = isSameDay(thisDate, selectedDate);
                        const isReserved = reservedDates.includes(day);

                        return (
                            <div
                                key={day}
                                className={`calendar-day ${isSelected ? "selected" : ""}`}
                                onClick={() => setSelectedDate(thisDate)}
                            >
                                {day}
                                {isReserved && <span className="dot" />}
                            </div>
                        );
                    })}
                </div>
            </div>
            <h3 className="sub-title">
                Reservations for{" "}
                {selectedDate.toLocaleDateString([], {
                    year: "numeric",
                    month: "short",
                    day: "numeric",
                })}
            </h3>
            <div className="reservation-list">
                {loading || loadingDetails ? (
                    <div className="reservation-item">
                        <div className="reservation-details">
                            <div className="detail-value">Loading…</div>
                        </div>
                    </div>
                ) : reservationsForSelectedDate.length > 0 ? (
                    reservationsForSelectedDate.map(r => (
                        <div key={r.reservationId} className="reservation-item">
                            <div className="reservation-details">
                                <div className="detail-value">
                                    {new Date(r.appointmentStart).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })} —{" "}
                                    {serviceNameById.get(r.catalogItemId) ?? `Service ${r.catalogItemId}`}
                                </div>
                                <div className="muted">
                                    Employee: {employeeNameById.get(r.employeeId) ?? r.employeeId} • Status: {r.status}
                                </div>
                                <div className="muted">
                                    Table: {detailedReservations.get(r.reservationId)?.tableOrArea ?? "—"}
                                </div>
                            </div>

                            {canManage && (
                                <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
                                    {String(r.status) !== "Cancelled" && (
                                        <button className="btn btn-danger" onClick={() => doCancel(r.reservationId)}>
                                            Cancel
                                        </button>
                                    )}
                                    {String(r.status) === "Booked" && (
                                        <button className="btn" onClick={() => markCompleted(r.reservationId)}>
                                            Mark completed
                                        </button>
                                    )}
                                </div>
                            )}
                        </div>
                    ))
                ) : (
                    <div className="reservation-item">
                        <div className="reservation-details">
                            <div className="detail-value">
                                No reservations for this day
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}