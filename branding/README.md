# Latrovu brand

## Idea
Two message bubbles, the seeker (lime) and Latrovu (outline), in conversation. Where they meet sits a keyhole: the match, the new home, and safety. Wordmark `latrovu.` ends with a lime full stop: found, done.

Pillars: effortless, for everyone, safe (Swiss hosting, no big tech, no tracking).

## Colors
| Name | Hex | Use |
|---|---|---|
| Ink | `#111418` | text, outline bubble, keyhole |
| Lime | `#B8E62E` | seeker bubble, accents on dark |
| Lime dark | `#9CC41F` | lime on white for small elements (full stop) |
| Canvas | `#F3F4F1` | page background |
| Muted | `#5E6660` | secondary text |

Never use lime for thin lines or small text on white.

## Files
| File | Use |
|---|---|
| `latrovu-symbol.svg` | symbol on light backgrounds |
| `latrovu-symbol-on-dark.svg` | symbol on dark backgrounds |
| `latrovu-symbol-mono.svg` | single color (print, embossing), keyhole cut out |
| `latrovu-logo.svg` | horizontal lockup, symbol and wordmark |
| `favicon.svg` | browser tab and app icon, simplified without tails on an ink tile |

The wordmark uses the Outfit font (SIL Open Font License); convert it to outlines before sending the logo to printers.

## Export PNGs (WSL)
```bash
sudo apt install -y librsvg2-bin
cd /mnt/c/dev/latrovu/branding
for size in 16 32 180 192 512; do rsvg-convert -w $size -h $size favicon.svg -o favicon-$size.png; done
rsvg-convert -h 120 latrovu-logo.svg -o latrovu-logo.png
```
