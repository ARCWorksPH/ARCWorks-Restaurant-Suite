# ARCWorks ROMS — Ideal Glass Design Specification

**Authoritative visual target for the Restaurant Suite**  
This document replaces previous glass instructions.  
It is derived directly from the approved Ideal Display mockups (Tables Overview, Order Editor, Kitchen Display, Component Sheet, Inventory).

Any AI agent, designer, or implementer **must** match the visual language shown in those ideal images.

---

## 1. Overall Feeling

The interface must feel like **premium frosted glass with colored status energy**.

- Deep dark navy / charcoal base
- Soft, luminous glass cards that appear to glow from within according to their status
- Colored borders + soft inner colored glow (not flat borders)
- High clarity and strong hierarchy
- Calm enough for long shifts, but status is immediately readable from a distance
- No flat solid cards. No heavy neon. No photographic backgrounds.

**Priority:**  
1. Instant status recognition  
2. Readability (especially KDS)  
3. Touch-friendly controls  
4. Premium glass polish  

---

## 2. Background

Use a deep, quiet dark background. Soft ambient light is allowed but must stay subtle.

```css
body {
    background: #0B0F14;
    /* Optional very soft ambient (keep extremely restrained) */
    background-image:
        radial-gradient(circle at 12% 18%, rgba(45, 212, 191, 0.07), transparent 40%),
        radial-gradient(circle at 88% 78%, rgba(139, 92, 246, 0.05), transparent 38%);
    background-attachment: fixed;
}
```

Do **not** make the background busy. The cards carry the visual interest.

---

## 3. Core Glass Card Language (Most Important)

Every major card (table cards, order panels, KDS tickets, metric cards, form sections) must use this treatment:

### Base Glass
```css
.glass-card {
    background: rgba(22, 28, 38, 0.72);
    backdrop-filter: blur(20px) saturate(140%);
    -webkit-backdrop-filter: blur(20px) saturate(140%);
    border-radius: 16px;
    border: 1px solid rgba(255, 255, 255, 0.06);
    box-shadow:
        0 8px 32px rgba(0, 0, 0, 0.35),
        inset 0 1px 0 rgba(255, 255, 255, 0.06);
}
```

### Status-Colored Glass (Tables & KDS)

The key difference from the current UI is that **status cards must have a soft colored fill + matching colored border + soft outer glow**.

| Status            | Border / Glow Color     | Soft Fill                          | Text Color   |
|-------------------|-------------------------|------------------------------------|---------------|
| Available         | `#2DD4BF`               | `rgba(45, 212, 191, 0.10)`         | `#5EEAD4`     |
| Occupied / New    | `#60A5FA`               | `rgba(96, 165, 250, 0.10)`         | `#93C5FD`     |
| Preparing         | `#F59E0B`               | `rgba(245, 158, 11, 0.12)`         | `#FBBF24`     |
| Ready to Serve    | `#4ADE80`               | `rgba(74, 222, 128, 0.12)`         | `#86EFAC`     |
| Pending Payment   | `#A78BFA`               | `rgba(167, 139, 250, 0.12)`        | `#C4B5FD`     |
| Locked            | `#64748B`               | `rgba(100, 116, 139, 0.10)`        | `#94A3B8`     |

Example for a Preparing table card:

```css
.table-card.status-preparing {
    background: linear-gradient(
        145deg,
        rgba(245, 158, 11, 0.13) 0%,
        rgba(22, 28, 38, 0.78) 45%
    );
    border: 1px solid rgba(245, 158, 11, 0.55);
    box-shadow:
        0 0 24px rgba(245, 158, 11, 0.18),
        0 8px 32px rgba(0, 0, 0, 0.35),
        inset 0 1px 0 rgba(255, 255, 255, 0.07);
}
```

**Critical:** The colored glow must feel soft and integrated, not like a hard neon outline.

---

## 4. Status Pills

Status pills in the ideal design are rounded, slightly translucent, and match the card color.

```css
.status-pill {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    padding: 0.3rem 0.75rem;
    border-radius: 999px;
    font-size: 0.72rem;
    font-weight: 750;
    letter-spacing: 0.04em;
    text-transform: uppercase;
    border: 1px solid transparent;
}
```

Examples:
- Available → cyan fill + cyan text
- Preparing → amber fill + amber text
- Ready → green fill + green text
- Pending Payment → purple fill + purple text

Include a small icon when space allows (person, clock, check, lock).

---

## 5. Tables Overview (Exact Target)

From the ideal mockup:

- Cards are arranged in a clean grid
- Each card has a strong status-colored glass treatment
- Top of card shows status pill + optional menu (⋯)
- Key info is large and clear: Table name, Waiter, Total / Time / Order count
- Available cards feel calm and open
- Preparing / Ready cards feel more energetic through color
- Locked cards are muted

Do **not** use the thin flat borders currently seen in production.  
Use the richer colored glass treatment shown in the ideal images.

---

## 6. Order Editor

Target characteristics:

- Left category rail (Mains / Drinks / Desserts) — simple, dark, clear active state
- Center: menu item grid with **food photography** (when available)
- Menu cards: dark glass, rounded, clear price, “Add to Order” button
- Right: Order Summary panel (slightly more opaque glass)
- Large, clear line items with quantity steppers
- Prominent gradient primary button (“Send to Kitchen” / “Mark served”)
- Status of the table shown clearly at the top of the order panel

The overall feeling is clean, modern, and appetizing.

---

## 7. Kitchen Display (Highest Readability Priority)

The ideal KDS is designed for distance viewing:

- Large table number
- Extremely large elapsed timer with soft glow matching status color
- Status badge in the top-right of each ticket (NEW / PREPARING / WAITING FOR WAITER)
- Ticket background uses the status-colored glass treatment
- Item list is high-contrast and large
- Action button at the bottom is solid and highly visible
- Oldest orders first

Timer colors:
- New → soft blue/cyan glow
- Preparing → strong amber/orange glow
- Waiting for Waiter → green glow

Never make the KDS overly transparent. Prefer higher opacity + strong color for safety.

---

## 8. Primary Buttons

Keep the brand gradient:

```css
.btn-primary {
    background: linear-gradient(135deg, #8B5CF6 0%, #38BDF8 55%, #2DD4BF 100%);
    color: white;
    border: none;
    border-radius: 12px;
    font-weight: 700;
    box-shadow: 0 4px 18px rgba(45, 212, 191, 0.25);
}
```

Secondary and quiet buttons use translucent dark glass with subtle borders.

Danger buttons stay solid red / red-bordered.

---

## 9. Typography & Spacing

- Font: Inter / system UI stack
- Page titles: large, weight 700–750
- Card titles: clear and bold
- Supporting text: muted but still readable (`rgba(200, 210, 220, 0.7)`)
- Generous padding inside cards (minimum 1rem–1.25rem)
- Consistent 16–18px border radius on major cards

---

## 10. Component Rules (from Ideal Component Sheet)

Agents must respect these states:

**Buttons**
- Primary gradient (default / hover / focus / busy / disabled)
- Secondary outline
- Quiet / ghost
- Danger

**Inputs**
- Dark translucent background
- Clear focus ring (cyan/blue)
- Inline validation (green valid, red invalid with message)

**Table Cards**
- Must show the full colored glass treatment for every status
- Include the soft inner glow

**KDS Tickets**
- Large timer
- Strong status header
- Clear action

**Alerts**
- Info (blue), Success (green), Warning (amber), Danger (red)
- Soft colored background + matching border

---

## 11. What the Current UI Is Missing (Fix These)

Compared to the ideal mockups, the current production UI still has:

1. Cards that are too flat / too evenly dark
2. Status color applied only as a thin border instead of a soft colored glass fill + glow
3. Weaker sense of depth and luminosity
4. Less refined status pills
5. KDS timers and status treatments that are not yet as bold and distance-readable as the ideal

The new target is the richer, softly glowing status glass shown in the Ideal Display images.

---

## 12. Implementation Guidance for AI Agents

When generating mockups, CSS, or new screens:

1. Always start from the deep dark background.
2. Make every major card a frosted glass surface.
3. Apply **status-colored glass** (soft fill + colored border + soft outer glow) to table cards and KDS tickets.
4. Keep the brand purple→cyan gradient only for primary actions and active nav.
5. Prefer clarity and status recognition over extreme transparency.
6. Match the density and spacing of the ideal mockups.
7. Food photography is allowed and encouraged on menu cards.
8. Never return to pure flat solid cards.

---

## 13. Final Visual Target

The finished product should look like the Ideal Display images:

- Softly glowing, status-colored glass table cards
- Clean, modern Order Editor with food images
- High-visibility Kitchen Display with large glowing timers
- Refined component states
- Premium dark glass aesthetic that still feels calm and operational

When there is any conflict between “more glass” and “clearer status / better readability”, choose clarity.

---

**End of Ideal Design Specification**
