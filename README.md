# LedgerFlow — Invoice Processing Pipeline

A front-end **UI concept** for an automated invoice-processing platform: capture
supplier invoices, extract their fields with Azure AI Document Intelligence,
validate them against purchase orders, and post the clean ones straight to the
ERP — leaving only exceptions for a human to review.

> ⚠️ **Demo / mockup only.** This is a static, self-contained interface with
> illustrative data. There is no backend, no real document processing, and the
> numbers are fictional. It exists to show the shape of the product.

## Screens

| Page | What it shows |
| ---- | ------------- |
| `index.html` | Executive dashboard — KPIs, live pipeline stages, throughput, outcomes, activity feed |
| `queue.html` | Exception review queue with a side-by-side document / extracted-fields panel |
| `analytics.html` | Straight-through-processing trends, exception reasons, supplier volumes, reconciliation summary |

## Concept

- **Capture** — invoices arrive from a monitored mailbox or upload and land on a queue
- **Extract** — Azure AI Document Intelligence reads header + line-item fields, extended with custom fields
- **Validate** — 3-way match against open purchase orders and supplier master data
- **Post** — anything within tolerance posts to the ERP automatically; the rest is held for review
- **Learn** — every human correction feeds back into extraction accuracy

## Running it

It's plain HTML/CSS/JS — no build, no dependencies. Just open `index.html`,
or serve the folder:

```bash
python -m http.server 8080
# then visit http://localhost:8080
```

## Stack (as pictured)

`Azure AI Document Intelligence` · `Azure Functions (.NET 8)` · `Azure Service Bus`
· `Azure SQL` · static HTML/CSS/vanilla JS front-end · inline-SVG charts

---

Built as a portfolio interface concept. Design and code by Nick Zivkovic.
