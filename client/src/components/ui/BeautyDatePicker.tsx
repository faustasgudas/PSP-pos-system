import { useEffect, useMemo, useRef, useState } from "react";

function pad2(n: number) {
    return String(n).padStart(2, "0");
}

function toYmd(d: Date) {
    return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`;
}

function fromYmd(ymd: string): Date | null {
    if (!ymd) return null;
    const d = new Date(`${ymd}T00:00:00`);
    return Number.isNaN(d.getTime()) ? null : d;
}

export function BeautyDatePicker(props: {
    label?: string;
    value: string; // YYYY-MM-DD
    onChange: (ymd: string) => void;
    disabled?: boolean;
    placeholder?: string;
}) {
    const { label, value, onChange, disabled, placeholder = "Select date" } = props;
    const [open, setOpen] = useState(false);
    const rootRef = useRef<HTMLDivElement | null>(null);

    const selectedDate = useMemo(() => fromYmd(value), [value]);
    const [viewMonth, setViewMonth] = useState<Date>(() => selectedDate ?? new Date());

    useEffect(() => {
        if (selectedDate) setViewMonth(selectedDate);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [value]);

    useEffect(() => {
        const onDocMouseDown = (e: MouseEvent) => {
            const el = rootRef.current;
            if (!el) return;
            if (!el.contains(e.target as Node)) setOpen(false);
        };
        document.addEventListener("mousedown", onDocMouseDown);
        return () => document.removeEventListener("mousedown", onDocMouseDown);
    }, []);

    const monthLabel = useMemo(
        () => viewMonth.toLocaleDateString([], { month: "long", year: "numeric" }),
        [viewMonth]
    );

    const grid = useMemo(() => {
        const year = viewMonth.getFullYear();
        const month = viewMonth.getMonth();
        const first = new Date(year, month, 1);
        const startDay = (first.getDay() + 6) % 7; // monday=0
        const daysInMonth = new Date(year, month + 1, 0).getDate();

        const cells: Array<{ ymd: string; day: number; inMonth: boolean }> = [];
        for (let i = 0; i < startDay; i++) cells.push({ ymd: "", day: 0, inMonth: false });
        for (let d = 1; d <= daysInMonth; d++) {
            const dt = new Date(year, month, d);
            cells.push({ ymd: toYmd(dt), day: d, inMonth: true });
        }
        // pad to full weeks
        while (cells.length % 7 !== 0) cells.push({ ymd: "", day: 0, inMonth: false });
        return cells;
    }, [viewMonth]);

    const display = useMemo(() => {
        if (!selectedDate) return "";
        return selectedDate.toLocaleDateString([], { weekday: "short", year: "numeric", month: "short", day: "numeric" });
    }, [selectedDate]);

    const todayYmd = useMemo(() => toYmd(new Date()), []);

    return (
        <div className={`bpicker ${disabled ? "is-disabled" : ""}`} ref={rootRef}>
            {label && <div className="bpicker__label">{label}</div>}

            <button
                type="button"
                className={`bpicker__trigger ${open ? "is-open" : ""} ${value ? "has-value" : ""}`}
                onClick={() => !disabled && setOpen((v) => !v)}
                disabled={disabled}
            >
                <div className="bpicker__value">
                    {value ? display : placeholder}
                </div>
                <span className="bpicker__icon" aria-hidden="true">📅</span>
            </button>

            {open && !disabled && (
                <div className="bpicker__popover">
                    <div className="bcal__header">
                        <button
                            type="button"
                            className="bcal__nav"
                            onClick={() => setViewMonth((d) => new Date(d.getFullYear(), d.getMonth() - 1, 1))}
                        >
                            ‹
                        </button>
                        <div className="bcal__title">{monthLabel}</div>
                        <button
                            type="button"
                            className="bcal__nav"
                            onClick={() => setViewMonth((d) => new Date(d.getFullYear(), d.getMonth() + 1, 1))}
                        >
                            ›
                        </button>
                    </div>

                    <div className="bcal__weekdays">
                        {["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((w) => (
                            <div key={w} className="bcal__wd">{w}</div>
                        ))}
                    </div>

                    <div className="bcal__grid">
                        {grid.map((c, idx) => {
                            const isSelected = c.ymd && c.ymd === value;
                            const isToday = c.ymd && c.ymd === todayYmd;
                            return (
                                <button
                                    key={idx}
                                    type="button"
                                    className={`bcal__day ${c.inMonth ? "" : "is-empty"} ${isToday ? "is-today" : ""} ${isSelected ? "is-selected" : ""}`}
                                    disabled={!c.inMonth}
                                    onClick={() => {
                                        onChange(c.ymd);
                                        setOpen(false);
                                    }}
                                >
                                    {c.inMonth ? c.day : ""}
                                </button>
                            );
                        })}
                    </div>

                    <div className="bcal__footer">
                        <button
                            type="button"
                            className="bcal__btn"
                            onClick={() => onChange(todayYmd)}
                        >
                            Today
                        </button>
                        <button
                            type="button"
                            className="bcal__btn"
                            onClick={() => setOpen(false)}
                        >
                            Close
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}

