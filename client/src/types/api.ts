// TypeScript types matching backend contracts

// ========== AUTH ==========
export interface LoginRequest {
    email: string;
    password: string;
}

export interface LoginResponse {
    token: string;
    businessType?: string;
}

export interface RegisterBusinessRequest {
    businessName: string;
    address: string;
    phone: string;
    email: string;
    countryCode: string;
    priceIncludesTax: boolean;
    ownerName: string;
    ownerEmail: string;
    ownerPassword: string;
    businessType?: string;
}

export interface RegisterBusinessResponse {
    businessId: number;
    ownerEmployeeId: number;
    token: string;
    businessType?: string;
}

// ========== RESERVATIONS ==========
export interface ReservationSummaryResponse {
    reservationId: number;
    businessId: number;
    employeeId: number;
    catalogItemId: number;
    appointmentStart: string; // ISO datetime
    appointmentEnd: string; // ISO datetime
    status: string;
}

export interface ReservationDetailResponse extends ReservationSummaryResponse {
    bookedAt: string; // ISO datetime
    plannedDurationMin: number;
    notes?: string;
    tableOrArea?: string;
    orderId?: number;
}

export interface CreateReservationRequest {
    catalogItemId: number;
    employeeId?: number;
    appointmentStart: string; // ISO datetime
    appointmentEnd: string; // ISO datetime
    plannedDurationMin: number;
    notes?: string;
    tableOrArea?: string;
}

export interface UpdateReservationRequest {
    appointmentStart?: string;
    appointmentEnd?: string;
    employeeId?: number;
    notes?: string;
    tableOrArea?: string;
}

// ========== CATALOG ITEMS ==========
export interface CatalogItemSummaryResponse {
    catalogItemId: number;
    businessId: number;
    name: string;
    code: string;
    type: string;
    basePrice: number;
    status: string;
    taxClass: string;
}

export interface CatalogItemDetailResponse extends CatalogItemSummaryResponse {
    description?: string;
}

export interface CreateCatalogItemRequest {
    name: string;
    code: string;
    type: string;
    basePrice: number;
    taxClass: string;
    description?: string;
}

export interface UpdateCatalogItemRequest {
    name?: string;
    code?: string;
    type?: string;
    basePrice?: number;
    taxClass?: string;
    description?: string;
}

// ========== EMPLOYEES ==========
export interface EmployeeSummaryResponse {
    employeeId: number;
    businessId: number;
    name: string;
    email: string;
    role: string;
    status: string;
}

export interface CreateEmployeeRequest {
    name: string;
    email: string;
    password: string;
    role: string;
}

export interface UpdateEmployeeRequest {
    name?: string;
    email?: string;
    role?: string;
}

export interface DeactivateEmployeeRequest {
    reason: string;
}

// ========== ORDERS ==========
export interface OrderSummaryResponse {
    orderId: number;
    businessId: number;
    employeeId: number;
    reservationId?: number;
    status: string;
    tableOrArea?: string;
    createdAt: string; // ISO datetime
    closedAt?: string; // ISO datetime
    tipAmount: number;
    discountId?: number;
}

// ========== PAYMENTS ==========
export interface PaymentResponse {
    paymentId: number;
    paidByGiftCard: number; // in cents
    remainingForStripe: number; // in cents
    stripeUrl?: string;
    stripeSessionId?: string;
}

// ========== GIFT CARDS ==========
export interface GiftCardResponse {
    giftCardId: number;
    code: string;
    balance: number; // in cents
    status: string;
    expiresAt?: string; // ISO datetime
    issuedAt: string; // ISO datetime
}

export interface CreateGiftCardRequest {
    balance: number; // in cents
    expiresAt?: string; // ISO datetime
}

// ========== STOCK ITEMS ==========
export interface StockItemSummaryResponse {
    stockItemId: number;
    catalogItemId: number;
    unit: string;
    qtyOnHand: number;
}

// ========== BUSINESS ==========
export interface BusinessResponse {
    businessId: number;
    name: string;
    address: string;
    phone: string;
    email: string;
    countryCode: string;
    priceIncludesTax: boolean;
    businessStatus: string;
    businessType: string;
}

// ========== API ERROR ==========
export interface ApiErrorResponse {
    error?: string;
    message?: string;
}
