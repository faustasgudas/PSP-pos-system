POS System (Beauty & Catering)

This project is a Point of Sale (POS) system designed to support two business types:

Beauty (services, reservations)

Catering (products, orders, payments)

The system is built as a multi-tenant application where all businesses share a single PostgreSQL database, with data isolated by business (tenant) context.

Tech Stack

Backend

C# / .NET

REST API

Frontend

React

TypeScript

CSS

Database

PostgreSQL (running in Docker)

Payments

Stripe (payments and refunds)

Gift Cards (can be combined with Stripe)

Core Features
1. Account System & Roles

Authentication and authorization are implemented with role-based access control.

Available roles:

Business Owner

Manager

Staff

Note: Super Admin is not implemented.

Business Creation

A Business Owner and their business are created via the backend (business initialization is handled server-side).

Employee Management

Add new employees with:

user accounts

assigned roles (Owner / Manager / Staff)

2. Catalog: Products & Services
Products

The system supports full CRUD operations for products:

Create / edit / delete products

Assign:

name

price

inventory tracking (quantity)

tax class

Pricing, tax, and discount values are handled in a way that ensures accurate order calculations.

3. Discounts

Discounts can be created as:

Order-level discounts (applied to the entire order)

Product-level discounts (applied to selected products)

Discount rules

Discounts are calculated before tax.

When creating a product-level discount, applicable products must be explicitly selected.

4. Gift Cards

Gift cards act as stored-value (top-up) cards:

Gift cards can be created

Gift cards can be topped up with additional balance

Can be used as a payment method:

alone

or combined with Stripe for partial payments

5. Reservations

The system supports reservations by:

selecting a specific date

selecting a specific time

This functionality is primarily intended for Beauty businesses.

6. Orders & Order Flow
Order Management

Orders support full lifecycle management and editing:

Create / update / delete (where applicable)

Add order lines consisting of:

services

catalog items (products)

Order Statuses

An order can be in one of the following states:

open

closed

cancelled

refunded

7. Payments & Refunds
Payments

Payments are processed via Stripe

Combined payments are supported:

Stripe + Gift Card

Refunds

Refund functionality is implemented

Refunded orders are reflected both in Stripe and within the system state

8. Dashboard

The system provides a dashboard displaying:

core business information

high-level operational data
# Reasons for Deviating from the Original Design

### 1. Gift cards were not defined

The original design document did not specify a **GiftCard entity** or describe how gift cards should be stored, audited. Due to the lack of a clear data model and business rules, implementing gift cards would be ambiguous and unreliable.

### 2. Missing auditability and snapshots

The design did not account for **immutable snapshots** of prices, taxes, or discounts. In real-world POS systems, historical accuracy is critical, and values must be preserved as they were at the time of transaction.

### 3. Employee entity created a bottleneck

The original model over-centralized business logic around the **Employee** entity, which reduced flexibility for order and reservation management.

### Because we used different data model we did not use their yamal.

## Reasons for Deviating from our YAMAL:

Our YAML defines order modification via a single PATCH /orders/{id} endpoint, we split this into explicit endpoints for adding/updating/removing lines because each operation has critical side-effects (inventory movements, tax/discount snapshots) that must be validated deterministically. We also implemented a dedicated “move order lines” endpoint to support POS-style bill splitting, which is not defined in the YAML contract.



#  Score for the design document: 5.5 / 10

##  Why:

-  Data model exists, but not deep enough (missing cardinalities, snapshot/audit rules, delete vs archive).

-  No package diagram → architecture/module boundaries unclear (harder to map to code structure).

-  No wireframes.

-  Endpoint behavior is underwritten → lacks error cases.


Overall: reads like a high-level overview, not a “buildable spec.”
