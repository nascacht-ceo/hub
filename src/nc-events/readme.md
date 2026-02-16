# Event Clearinghouse (`events.nascacht.io`)

The Event Clearinghouse is a **Graph-Driven Event Architecture** designed to facilitate real-time notifications across disparate organizations. It maps complex relationships between **Properties**, **Loans**, and **Contacts**, ensuring that when a significant event occurs, all related stakeholders are notified regardless of their internal data silos.

---

## 1. Core Architecture
The system is built on a four-layer event pipeline:

1.  **Ingestion (EventBridge/Event Grid):** Receives multi-tenant events using the **CloudEvents 1.0** standard.
2.  **Transformation (C# Middleware):** Uses `IClientTransformer` logic to project client-specific schemas into a **Canonical Data Model**.
3.  **Entity Resolution (Identity Registry):** An `IResolver<T>` engine that maps messy source identifiers (SSNs, Normalized Addresses, Loan numbers) to time-ordered **UUID v7 Golden IDs**.
4.  **Relationship Graph (Amazon Neptune/GCP Spanner Graph):** A graph database that performs nth-degree traversals to find related parties (e.g., *"Find all clients linked to a property where a resident just defaulted on a loan"*).

---

## 2. Master API Specification

### Event Ingress
| Endpoint | Method | Purpose |
| :--- | :--- | :--- |
| `/v1/events` | POST | Ingest a single lifecycle event (CloudEvent format). |
| `/v1/events/batch` | POST | Bulk ingestion for portfolio-level updates. |
| `/v1/events/{id}` | GET | Retrieve processing and fan-out delivery status. |

### Identity & Registry
| Endpoint | Method | Purpose |
| :--- | :--- | :--- |
| `/v1/registry/resolve` | POST | Resolve "Hints" (TaxID, Address) to a **Golden ID**. |
| `/v1/registry/{id}` | GET | Reverse lookup of all cross-org identifiers for a Golden ID. |
| `/v1/registry/map` | POST | Manually link a private client ID to an existing Golden ID. |
| `/v1/registry/merge` | POST | Consolidate duplicate entities into a single Golden ID. |

### Domain Workflows (Orders & Results)
| Endpoint | Method | Action |
| :--- | :--- | :--- |
| `/v1/loans/{id}/default` | POST | Report a loan status change. |
| `/v1/properties/{id}/inspect` | POST | Trigger an `inspection.ordered` command. |
| `/v1/contacts/{id}/credit` | POST | Trigger a `credit.requested` command. |
| `/v1/results/{orderId}` | POST | Entry point for service providers to post results. |

### Subscriptions
| Endpoint | Method | Purpose |
| :--- | :--- | :--- |
| `/v1/subscriptions` | POST | Define filters (e.g., "Watch Property P1 for events"). |
| `/v1/webhooks` | POST | Register the endpoint URL for push notifications. |

---

## 3. Data Identification Standards
To maintain consistency across organizations, the system utilizes specific "Authority" keys:

* **Properties:** Normalized via **Google Place IDs** or **Overture Maps IDs**.
* **Loans:** Identified by **Lienholder + Loan Number** or agency-specific identifiers (Freddie/Fannie).
* **Contacts:** Resolved via **Country + TaxID (SSN)**.

---

## 4. Integration Requirements
* **Security:** All requests must include a `Bearer` token. Outbound webhooks are signed with an `X-nc-signature` (HMAC-SHA256).
* **Idempotency:** Use the `Idempotency-Key` header for all `POST` operations to prevent duplicate orders or charges.
* **Schema:** Payloads must adhere to the `application/json+cloudevents` content type.