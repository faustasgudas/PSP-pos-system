import { useEffect, useMemo, useRef, useState } from "react";

function pad2(n: number) {
    return String(n).padStart(2, "0");
}

function tryParseHHMM(input: string): string | null {
    const s = input.trim();
    const m = s.match(/^(\d{1,2}):(\d{2})$/);
    if (!m) return null;
    const hh = Number(m[1]);
    const mm = Number(m[2]);
    if (!Number.isFinite(hh) || !Number.isFinite(mm)) return null;
    if (hh < 0 || hh > 23) return null;
    if (mm < 0 || mm > 59) return null;
    return `${pad2(hh)}:${pad2(mm)}`;
}

export function BeautyTimePicker(props: {
    label?: string;
    value: string; // HH:mm
    onChange: (hhmm: string) => void;
    disabled?: boolean;
    placeholder?: string;
    allowTyping?: boolean;
    minuteStep?: number; // used for slot buttons
    minTime?: string; // HH:mm
    maxTime?: string; // HH:mm
}) {
    const {
        label,
        value,
        onChange,
        disabled,
        placeholder = "Select time",
        allowTyping = true,
        minuteStep = 15,
        minTime,
        maxTime,
    } = props;
    const [open, setOpen] = useState(false);
    const rootRef = useRef<HTMLDivElement | null>(null);
    const [draft, setDraft] = useState(value);
    const inputRef = useRef<HTMLInputElement | null>(null);

    useEffect(() => {
        const onDocMouseDown = (e: MouseEvent) => {
            const el = rootRef.current;
            if (!el) return;
            if (!el.contains(e.target as Node)) setOpen(false);
        };
        document.addEventListener("mousedown", onDocMouseDown);
        return () => document.removeEventListener("mousedown", onDocMouseDown);
    }, []);

    useEffect(() => {
        setDraft(value);
    }, [value, open]);

    const parsedDraft = useMemo(() => tryParseHHMM(draft), [draft]);
    const isValid = !!parsedDraft;

    const slots = useMemo(() => {
        const step = Math.max(1, Math.min(60, Math.floor(minuteStep || 15)));
        const min = minTime ? tryParseHHMM(minTime) : null;
        const max = maxTime ? tryParseHHMM(maxTime) : null;
        const minMinutes = min ? Number(min.slice(0, 2)) * 60 + Number(min.slice(3, 5)) : null;
        const maxMinutes = max ? Number(max.slice(0, 2)) * 60 + Number(max.slice(3, 5)) : null;

        const out: string[] = [];
        for (let hh = 0; hh <= 23; hh++) {
            for (let mm = 0; mm < 60; mm += step) {
                const t = `${pad2(hh)}:${pad2(mm)}`;
                const mins = hh * 60 + mm;
                if (minMinutes !== null && mins < minMinutes) continue;
                if (maxMinutes !== null && mins > maxMinutes) continue;
                out.push(t);
            }
        }
        return out;
    }, [minuteStep, minTime, maxTime]);

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
                    {value ? value : placeholder}
                </div>
                <span className="bpicker__icon" aria-hidden="true">🕒</span>
            </button>

            {open && !disabled && (
                <div className="bpicker__popover">
                    <div className="btime__title">Choose a time</div>

                    {allowTyping && (
                        <>
                            <div className="btime__manual">
                                <input
                                    ref={inputRef}
                                    className="btime__input"
                                    value={draft}
                                    onChange={(e) => setDraft(e.target.value)}
                                    placeholder="HH:MM (e.g. 09:30)"
                                    inputMode="numeric"
                                    autoFocus
                                    onKeyDown={(e) => {
                                        if (e.key === "Enter") {
                                            if (!parsedDraft) return;
                                            onChange(parsedDraft);
                                            setOpen(false);
                                        }
                                    }}
                                    onBlur={() => {
                                        // Don't auto-apply on blur; just keep draft.
                                    }}
                                />
                                <button
                                    type="button"
                                    className="bcal__btn"
                                    onClick={() => {
                                        if (!parsedDraft) {
                                            inputRef.current?.focus();
                                            return;
                                        }
                                        onChange(parsedDraft);
                                        setOpen(false);
                                    }}
                                    disabled={!isValid}
                                >
                                    Apply
                                </button>
                            </div>

                            {!isValid && (
                                <div className="muted" style={{ fontSize: 12 }}>
                                    Please type time in <strong>HH:MM</strong> format (00:00–23:59).
                                </div>
                            )}
                        </>
                    )}

                    <div
                        style={{
                            marginTop: allowTyping ? 10 : 0,
                            display: "grid",
                            gridTemplateColumns: "repeat(4, minmax(0, 1fr))",
                            gap: 8,
                            maxHeight: 260,
                            overflow: "auto",
                            paddingRight: 4,
                        }}
                    >
                        {slots.map((t) => (
                            <button
                                key={t}
                                type="button"
                                className={`btime__slot ${t === value ? "is-selected" : ""}`}
                                onClick={() => {
                                    onChange(t);
                                    setOpen(false);
                                }}
                            >
                                {t}
                            </button>
                        ))}
                    </div>

                    <div className="bcal__footer">
                        <button type="button" className="bcal__btn" onClick={() => setOpen(false)}>
                            Close
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}

