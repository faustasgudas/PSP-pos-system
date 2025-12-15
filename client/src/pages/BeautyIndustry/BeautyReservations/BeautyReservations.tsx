import { useEffect, useMemo, useState } from "react";
import "./BeautyReservations.css";
import {
    cancelReservation,
    getReservation,
    listReservations,
    updateReservation,
    type ReservationSummary,
} from "../../../frontapi/reservationsApi";
import { getActiveServices, type CatalogItem } from "../../../frontapi/catalogApi";
import { fetchEmployees } from "../../../frontapi/employeesApi";
import { getUserFromToken } from "../../../utils/auth";
import { logout } from "../../../frontapi/authApi";

interface ReservationsProps {
    goToNewBooking: () => void;
}

export default function BeautyReservations({
    goToNewBooking,
}: ReservationsProps) {
    const user = getUserFromToken();
    const role = user?.role ?? "";

    const businessId = Number(localStorage.getItem("businessId"));

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [reservations, setReservations] = useState<ReservationSummary[]>([]);

    const [services, setServices] = useState<CatalogItem[]>([]);
    const [employees, setEmployees] = useState<any[]>([]);

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

    const bookedDates = useMemo(() => {
        const days = new Set<number>();
        reservations.forEach(r => {
            const d = new Date(r.appointmentStart);
            if (d.getFullYear() === year && d.getMonth() === month) {
                days.add(d.getDate());
            }
        });
        return Array.from(days);
    }, [reservations, year, month]);

    const bookingsForSelectedDate = useMemo(() => {
        return reservations.filter(r =>
            isSameDay(new Date(r.appointmentStart), selectedDate)
        );
    }, [reservations, selectedDate]);

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

    const authProblem =
        (error ?? "").toLowerCase().includes("unauthorized") ||
        (error ?? "").toLowerCase().includes("forbid") ||
        (error ?? "").includes("401") ||
        (error ?? "").includes("403");

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
                listReservations(businessId, {
                    dateFrom: from.toISOString(),
                    dateTo: to.toISOString(),
                }),
                getActiveServices(businessId),
                fetchEmployees(businessId),
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

    useEffect(() => {
        load(currentMonth);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [businessId]);

    useEffect(() => {
        load(currentMonth);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [currentMonth]);

    const canManage = role === "Owner" || role === "Manager";

    const doCancel = async (reservationId: number) => {
        if (!businessId) return;
        const ok = window.confirm(`Cancel reservation #${reservationId}?`);
        if (!ok) return;

        setError(null);
        try {
            // Bulletproof rule: if there's already an order, cancel from the Order screen instead.
            const detail = await getReservation(businessId, reservationId);
            if (detail.orderId) {
                setError(
                    `Reservation #${reservationId} already has order #${detail.orderId}. Cancel the order to auto-cancel the reservation.`
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
                    onClick={() => {
                        console.log("✅ NEW BOOKING BUTTON CLICKED");
                        goToNewBooking(); // 🔥 THIS MUST FIRE
                    }}
                >
                    <span>➕</span> New Booking
                </button>
            </div>

            {error && (
                <div style={{ margin: "10px 0" }} className="booking-item">
                    <div className="booking-details">
                        <div className="detail-value">{error}</div>
                        {authProblem && (
                            <button
                                className="btn"
                                onClick={() => {
                                    logout();
                                    window.location.reload();
                                }}
                                style={{ marginTop: 10 }}
                            >
                                Log out
                            </button>
                        )}
                    </div>
                </div>
            )}

            <div className="calendar-wrapper">
                <div className="calendar-header">
                    <button
                        className="cal-nav-btn"
                        onClick={() =>
                            setCurrentMonth(new Date(year, month - 1, 1))
                        }
                    >
                        ◀ Prev
                    </button>

                    <div className="calendar-title">{monthLabel}</div>

                    <button
                        className="cal-nav-btn"
                        onClick={() =>
                            setCurrentMonth(new Date(year, month + 1, 1))
                        }
                    >
                        Next ▶
                    </button>
                </div>

                <div className="calendar-grid">
                    {Array.from({ length: daysInMonth }, (_, i) => {
                        const day = i + 1;
                        const thisDate = new Date(year, month, day);

                        const isSelected = isSameDay(thisDate, selectedDate);
                        const isBooked = bookedDates.includes(day);

                        return (
                            <div
                                key={day}
                                className={`calendar-day ${isSelected ? "selected" : ""}`}
                                onClick={() => setSelectedDate(thisDate)}
                            >
                                {day}
                                {isBooked && <span className="dot" />}
                            </div>
                        );
                    })}
                </div>
            </div>

            <h3 className="sub-title">
                Bookings for{" "}
                {selectedDate.toLocaleDateString([], {
                    year: "numeric",
                    month: "short",
                    day: "numeric",
                })}
            </h3>

            <div className="booking-list">
                {loading ? (
                    <div className="booking-item">
                        <div className="booking-details">
                            <div className="detail-value">Loading…</div>
                        </div>
                    </div>
                ) : bookingsForSelectedDate.length > 0 ? (
                    bookingsForSelectedDate
                        .slice()
                        .sort(
                            (a, b) =>
                                new Date(a.appointmentStart).getTime() -
                                new Date(b.appointmentStart).getTime()
                        )
                        .map((b) => (
                            <div key={b.reservationId} className="booking-item">
                                <div className="booking-details">
                                    <div className="detail-value">
                                        {new Date(b.appointmentStart).toLocaleTimeString([], {
                                            hour: "2-digit",
                                            minute: "2-digit",
                                        })}{" "}
                                        —{" "}
                                        {serviceNameById.get(b.catalogItemId) ?? `Service ${b.catalogItemId}`}
                                    </div>
                                    <div className="muted">
                                        Employee: {employeeNameById.get(b.employeeId) ?? b.employeeId} • Status:{" "}
                                        {b.status}
                                    </div>
                                </div>

                                <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
                                    {canManage && b.status !== "Cancelled" && (
                                        <button className="btn" onClick={() => doCancel(b.reservationId)}>
                                            Cancel
                                        </button>
                                    )}
                                    {canManage && b.status === "Booked" && (
                                        <button className="btn" onClick={() => markCompleted(b.reservationId)}>
                                            Mark completed
                                        </button>
                                    )}
                                </div>
                            </div>
                        ))
                ) : (
                    <div className="booking-item">
                        <div className="booking-details">
                            <div className="detail-value">
                                No bookings for this day
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}
