# Landing-page branding assets

Runtime assets for the public landing / staff login page.

| File | Purpose | Origin |
|---|---|---|
| `chef-doys-wordmark.png` | Dominant Chef Doy's gold script identity, 1220 x 558 RGBA | Deterministic gold-channel extraction from the user-supplied wordmark; the baked checkerboard was removed and the result was visually checked over the real background |
| `arcworks-mark.png` | Secondary ARCWorks product endorsement, 600 x 277 RGBA | Deterministic extraction from the user-supplied ARCWorks mark |
| `landing-bg-desktop.png` / `.webp` | Desktop atmosphere background, 1920 x 1080 | Normalized from the user-supplied desktop background |
| `landing-bg-mobile.png` / `.webp` | Purpose-composed portrait atmosphere background, 900 x 1600 | Generated with OpenAI's built-in image generation using the supplied desktop background as the style reference |
| `chef-doys-gourmet-restaurant.png` | Earlier full-composition source retained | Original handoff asset |
| `restaurant-logo-placeholder.svg` | Safe fallback | Placeholder |
| `login-background-placeholder.svg` | Safe fallback | Placeholder |

Do not put credentials, QR codes, customer/staff photos, or unlicensed stock here.
The landing page must not fetch visual assets from external URLs.

The mobile-generation prompt was:

> Use case: ads-marketing. Asset type: portrait mobile background for a premium
> restaurant staff login screen. Create a purpose-composed 9:16 portrait
> companion to the supplied desktop atmosphere image. Preserve its luxurious
> black/navy environment, warm gold energy and particles on the left and upper
> center, and cool cyan/blue energy on the right and lower area. Keep the center
> visually calm and dark enough for a gold Chef Doy's wordmark above a
> translucent login card. Background art only: no words, letters, logos,
> interface panels, buttons, checkerboard, watermark, people, or food.
