# LedgerFlow — Invoice Processing Pipeline

[![CI](https://github.com/NickZivkovic1993/invoice-processing-pipeline/actions/workflows/ci.yml/badge.svg)](https://github.com/NickZivkovic1993/invoice-processing-pipeline/actions/workflows/ci.yml)

An automated accounts-payable pipeline on Azure: supplier invoices are captured, read with
**Azure AI Document Intelligence**, reconciled against purchase orders and goods receipts by a
**three-way match**, and either posted straight to the ERP or held in a review queue for a human.
Everything inside tolerance flows through without manual typing; only exceptions surface.

> **Reference implementation.** This is real, working engineering written to demonstrate the
> architecture — not a client deliverable. The customer scenario and all figures are illustrative;
> the code, tests, and infrastructure are not. Processing live documents needs your own Azure
> resources (see [Configuration](#configuration)).

## Architecture

```
   mailbox / upload
        │  (drops a PDF in blob storage)
        ▼
  ┌────────────────┐   Service Bus    ┌────────────────────┐
  │ CaptureFunction │ ───────────────▶ │ ProcessInvoiceFunc │
  │  (blob trigger) │   invoices-in    │  (queue trigger)   │
  └────────────────┘                  └─────────┬──────────┘
                                                │  extract (Document Intelligence)
                                                ▼
                                      ┌────────────────────┐
                                      │   ThreeWayMatcher   │  invoice × PO × receipts
                                      └─────────┬──────────┘
                                   auto-post ◀──┴──▶ exception queue (Azure SQL)
                                      │                     │
                                   ERP API            LedgerFlow.Api  ◀── React review UI
```

The **three-way match** is the heart of the system and lives in
[`LedgerFlow.Core`](src/LedgerFlow.Core/Matching/ThreeWayMatcher.cs) — pure, dependency-free, and
covered by [unit tests](tests/LedgerFlow.Core.Tests/ThreeWayMatcherTests.cs). Every Azure boundary
(document extraction, messaging, ERP, reference data) sits behind an interface so the domain stays
testable and the cloud stays out of the core.

## Projects

| Project | Role |
| ------- | ---- |
| `LedgerFlow.Core` | Domain model + three-way matcher and tolerance rules. No I/O. |
| `LedgerFlow.Infrastructure` | EF Core (Azure SQL), Document Intelligence extractor, Service Bus, ERP + reference-data clients. |
| `LedgerFlow.Functions` | Isolated-worker Functions: blob capture → Service Bus → extract → match → post. |
| `LedgerFlow.Api` | Minimal API backing the exception queue (list / approve / reject) + `/api/analytics` aggregates. |
| `web/` | React + TypeScript + Vite review-queue UI. |
| `infra/` | Bicep for the whole estate; managed identity throughout, no secrets in app settings. |

## Build & test

```bash
dotnet test LedgerFlow.sln      # matcher + per-supplier policy + pipeline orchestration tests
cd web && npm ci && npm run build
```

The pipeline orchestration (extract → match → post-or-queue, including duplicate suppression) is
covered end-to-end in
[InvoiceProcessorTests](tests/LedgerFlow.Infrastructure.Tests/InvoiceProcessorTests.cs) against an
in-memory database and a fake ERP. Per-supplier tolerance overrides live in
[SupplierPolicies](src/LedgerFlow.Core/Matching/SupplierPolicies.cs) — a strategic partner can
carry a looser price band while a repeat over-biller gets zero headroom.

Both run in [CI](.github/workflows/ci.yml) on every push, along with `az bicep build` over the
infrastructure.

## Run locally

```bash
docker compose up -d                       # SQL Server + Azurite
dotnet run --project src/LedgerFlow.Api    # exception-queue API on :5080
cd web && npm run dev                      # review UI on :5173, proxied to the API
```

The Functions worker and live extraction need real Azure resources — see below.

## Configuration

`LedgerFlow.Functions` reads (via managed identity in Azure, `local.settings.json` locally):

| Setting | Purpose |
| ------- | ------- |
| `DocumentIntelligenceEndpoint` | Azure AI Document Intelligence resource URL |
| `ServiceBusConnection` | Service Bus namespace |
| `BlobServiceUri` | Storage account for the invoice inbox |
| `SqlConnectionString` | Azure SQL exception-queue database |
| `ErpBaseUrl` | ERP posting + reference-data API |

Provision everything with `az deployment group create -g <rg> -f infra/main.bicep`.

---

Design and code by Nick Zivkovic.
