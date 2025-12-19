# POS System (Beauty & Catering)

A multi-tenant Point of Sale (POS) system built for two business types:

- **Beauty**
- **Catering**

All tenants share a single **PostgreSQL** database, with strict data isolation by **Business/Tenant context** (every entity is scoped by `BusinessId`).

---

## Tech Stack

### Backend
- **C# / .NET**
- **REST API**

### Frontend
- **React**
- **TypeScript**
- **CSS**

### Database
- **PostgreSQL** (Docker)

### Payments
- **Stripe** (payments + refunds)
- **Gift Cards** (standalone or combined with Stripe)

---

## Core Features

### 1) Account System & Roles

Authentication and authorization uses **role-based access control**.

**Roles**
- Business Owner
- Manager
- Staff

> Note: **Super Admin** is not implemented.

**Business Creation**
- A **Business Owner** and their **Business** are created via the backend (initialization is server-side).

**Employee Management**
- Add employees with:
  - user accounts
  - assigned roles (Owner / Manager / Staff)

---

### 2) Catalog: Products & Services

**Products**
- Full CRUD:
  - create / edit / delete
- Assign:
  - name
  - price
  - inventory tracking (quantity)
  - tax class

> Pricing, tax, and discount calculations are implemented to ensure accurate totals.

---

### 3) Discounts

Discounts can be created as:
- **Order-level** (applies to the entire order)
- **Product-level** (applies to selected products)

Rules:
- Discounts are calculated **before tax**
- For product-level discounts, applicable products must be explicitly selected

---

### 4) Gift Cards

Gift cards act as stored-value (top-up) cards:
- create gift cards
- top-up balance
- use as payment:
  - gift card only
  - gift card + Stripe (partial payment)

---

### 5) Reservations (Beauty)

Reservations support:
- selecting a specific date
- selecting a specific time

Primarily intended for **Beauty** businesses.

---

### 6) Orders & Order Flow

**Order Management**
- Full lifecycle management (where applicable):
  - create / update / delete
- Add order lines consisting of:
  - services
  - catalog items (products)

**Order Statuses**
- `Open`
- `Closed`
- `Cancelled`
- `Refunded`

**Split / Move Order Lines (Bill Splitting)**
Orders support splitting by moving selected lines (or partial quantities) to another open order:
- Move one or more lines to a target order
- Move partial quantities (clone line into target, reduce qty in source)
- Validates inputs and prevents invalid operations (duplicates, exceeding available qty, etc.)

---

### 7) Payments & Refunds

**Payments**
- Processed via **Stripe**
- Combined payments supported:
  - Stripe + Gift Card

**Refunds**
- Full refund functionality implemented
- Refunded orders are reflected:
  - in **Stripe**
  - in system state (payment status + order status)
- Inventory is restored via **stock movements** when refunding (products only)

---

### 8) Dashboard

Dashboard displays:
- core business information
- high-level operational data

---

## Multi-Tenant Architecture

- Single shared PostgreSQL database
- Data isolation by **Business / Tenant** context
- Every business entity is scoped by a **Business identifier**

---

# Reasons for Deviating from the Original Design

### 1) Gift cards were not defined
The original design document did not specify a **GiftCard entity** or describe how gift cards should be stored/audited. Without a clear data model and business rules, gift card behavior would be ambiguous.

### 2) Missing auditability and snapshots
The design did not account for **immutable snapshots** of prices, taxes, or discounts. Real POS systems require historical accuracy, so values must be preserved as they were at transaction time.

### 3) Employee entity created a bottleneck
The original model over-centralized business logic around the **Employee** entity, which reduced flexibility for order and reservation management.

### 4) YAML contract not used due to different data model
Because the implemented data model diverged significantly, the original YAML was not used directly.

---

## Reasons for Deviating from our YAML

Our YAML defined order modification via a single `PATCH /orders/{id}` endpoint.

We split this into explicit endpoints for:
- adding lines
- updating lines
- removing lines

Because each operation has critical side-effects:
- inventory movements
- tax/discount snapshots
- deterministic validations

We also implemented a dedicated **move order lines** endpoint to support POS-style bill splitting, which was not defined in the YAML contract.

---

# Score for the Design Document: **5.5 / 10**

## Why
- Data model exists, but not deep enough (missing cardinalities, snapshot/audit rules, delete vs archive)
- No package diagram → architecture/module boundaries unclear
- No wireframes
- Endpoint behavior is underwritten → lacks error cases

Overall: it reads like a high-level overview, not a buildable spec.
