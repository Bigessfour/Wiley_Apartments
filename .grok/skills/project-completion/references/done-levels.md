# Done levels & market honesty

## D1 — Spec Done

- Requirements / task list accepted  
- Deployed or deployable  
- Formal acceptance (e.g. dual-clerk sign-off)  

**Not the same as** “every screen is pleasant and fully linked.”

## D2 — Daily-ops Ready (default finish bar)

An operator can complete **real jobs** on every inventory surface:

- Formatted (layout, controls, empty/loading/error)  
- Connected (services, IDs, cross-links, domain rules)  
- Usable (job story completes; no S0/S1)  

## D3 — Market-polished

Parity aspirations with commercial products in the same category
(e.g. property management: AppFolio, Buildium, Yardi Breeze, DoorLoop):

- Tenant portal, online pay, e-sign, mobile apps  
- Bulk workflows, packaged reporting studios  
- Uniform UX density and broad E2E automation  

**D3 is optional.** Hitting D2 for a single-portfolio / intranet app is a valid
ship. Do not treat missing D3 features as D2 failures unless the signed spec
required them.

## Honest comparison frame

| Dimension | Commercial SaaS | Purpose-built vertical (e.g. ClerkSuite) |
|-----------|-----------------|------------------------------------------|
| Scope | Multi-property, portal, GL | Narrow domain, operator-first |
| Hosting | Vendor cloud | Often self-hosted / NAS |
| UI kit | Proprietary DS | Chosen stack (e.g. Syncfusion) |
| QA automation | Broad E2E | Often unit-heavy; E2E thin |
| Fit | General market | Exact workflow + constraints |

**Question for D2:** Can operators stop using the spreadsheet for the jobs this
app claims to own?  
**Question for D3:** Would a stranger pick this over a SaaS trial?
