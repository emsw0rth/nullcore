---
slug: from-sprite-sheet-to-prefab-pivots-and-pixels-per-unit
title: "From Sprite Sheet to Prefab: Pivots, Pixels Per Unit and Keeping the Player the Same Size"
excerpt: "Turning grids of art into a portal, a laser door and a brand new player. The numbers that matter most: pixels per unit, pivots, alpha thresholds, and why a collider doesn't update when you edit a sprite's physics shape."
category: devlog
date: 2026-10-30
series: nullcore
---

A surprising amount of game development isn't code at all. It's taking a picture of lots of little pictures and turning it into something that behaves. In nullcore I've done that for a teleporter, a laser door, a switch, a set of props, and (most recently) the player themself.

Each time I made the same handful of mistakes first, so this post is a list of what I now check before I trust a spritesheet.

## The grid is a lie (a small one)

Every sheet I make is a grid of cells: 128×128 for the old player, 256×256 for the newer art. It's tempting to treat the grid as the sprite, so that each cell becomes one sprite, and that is *sometimes* the right call (more on that below). But there are three separate things to decide for every sprite, and the grid only answers the first:

1. **The rectangle:** which pixels belong to it.
2. **The pivot:** which point of the rectangle sits at the object's position.
3. **Pixels per unit:** how big one world unit is, in pixels.

### Rectangles are measured from the *bottom*

Unity's texture coordinates start at the bottom-left; every image editor counts rows from the top. A piece that starts 197 pixels down from the top of a 256-pixel cell and is 59 pixels tall sits at `y = 256 - (197 + 59) = 0` in Unity. Nearly every slicing bug I've had was a vertical mirror of this.

### "Transparent" isn't empty

For tight crops I measure the alpha bounding box of each piece. The first number I got for the door's pillar was **212 pixels wide**. The visible pillar is **81**. There was nothing wrong with the art: the sheet has a scattering of pixels at an alpha of just a few out of 255, probably left over from a soft glow or an anti-aliased edge. They're invisible, but a bounding box of "any alpha above zero" sees them.

I measure with a threshold now (anything above about 8/255 counts as content) and it gives the number I mean. The battery cap on my HUD sheet was 86 pixels wide at threshold 0 and 19 at threshold 8.

## Building a teleporter from parts

The old teleporter was a single sprite with an animation. The new art arrived as three separate pieces on one sheet (a **base**, a **top** and a **pillar**) plus an animated **portal** effect: 48 frames, 256×256 each, read left to right and top to bottom.

I made each piece a prefab, and rebuilt the teleporter out of them (base, top and pillar) with the portal in the middle. The teleporter's controller already had a single "this object is visible when I'm on" reference. I pointed it at the **portal** instead of the old animation, and now the whole glowing effect appears when the teleporter powers up and disappears when it doesn't.

Two things I'm glad I did:

- **The light belongs to the effect.** The portal has a blue point light. Rather than have the teleporter script manage the light separately, the animated-decoration component got an optional list of lights that it turns on and off with itself. The teleporter only ever switches the portal object, and the light follows.
- **The old animation got deleted**, not just disabled: the controller, the clip and the sprite sheet it used. Leftover assets that nothing references are how projects get slowly confusing.

## The door: one frame, several sheets

The door has the same layout: a base, a top, a pillar for the background, and a laser that's an animation of 48 frames. What made this one click was noticing that every cell on both sheets is **256 pixels square, and the door's pieces are authored in the same frame**: the pillar fills the cell's full height, the base sits at the bottom, the top at the top, and the laser beam sits centred between them, running behind both plates.

At 128 pixels per unit that frame is exactly 2×2 world units. So if each piece is positioned at the top, bottom or centre of that frame, everything lines up with no measuring. And for the 48 laser frames I computed one rectangle that fits *all* of them (x 100 to 155, y 24 to 231) and used it for every frame, instead of tight-cropping each. A per-frame crop looks fine in a still, but in motion the beam would jitter by a pixel or two each frame as the crop box changed size.

The door behavior is as simple as it should be:

```csharp
private void UpdateVisual()
{
    if (laserObject != null)
        laserObject.SetActive(!on);

    if (col != null)
        col.enabled = !on; // open doors don't block movement; closed doors do
}
```

Open means the laser is hidden and the collider is off. The laser has its own red light, tied to the same on/off, so an open door goes dark.

## The collider that didn't update

I drew custom physics shapes for the teleporter and door pieces in Unity's sprite editor, saved, and... nothing changed. The collisions were the old ones.

A `PolygonCollider2D` **copies** the sprite's physics shape when you create it, and from then on it keeps its own points. Editing the sprite's outline afterwards doesn't touch existing colliders. When I compared them, the base had a 6-point collider against a 15-point shape on the sprite, and the top had 16 points against 31.

The editor's own answer is the component's "Reset" menu item, one collider at a time. I wrote a small script that rebuilds a collider from its sprite:

```csharp
col.pathCount = sprite.GetPhysicsShapeCount();
for (int i = 0; i < col.pathCount; i++)
{
    sprite.GetPhysicsShape(i, shape);
    col.SetPath(i, shape);
}
```

Prefabs that contain *other* prefabs pick up the change automatically from the source, so I only had to regenerate each piece once. The rule I took from it: **a sprite's physics shape is a starting point for a collider, not a link to one.**

## Swapping in a new player without changing the game

The hardest version of all of this was replacing the player's art. The new spritesheet has 256×256 cells, with a much more detailed robot: one idle frame, then 16 frames of walking, 16 of running and 16 of jumping, all facing right. I wanted all of that, and I wanted the player to be **exactly the same size** as before, so that every platform gap, doorway and jump I'd already balanced still worked.

The old character was 98 pixels tall inside a 128-pixel cell, at 128 pixels per unit: **0.766 world units**. The new one is 239 pixels tall. So:

```
pixels per unit = 128 × 239 / 98 ≈ 312
```

At 312 pixels per unit, the new character is 239 / 312 = 0.766 units tall. Same height to three decimal places, and the physics collider never needed to change.

The pivot was the other half. The player object's origin sits half a unit above its feet (the collider runs from -0.5 up), so each sprite's pivot has to place the feet 0.5 units below the origin. That's 0.5 × 312 = 156 pixels from the feet, which, for these cells, works out to a pivot 163 pixels up from the bottom of the cell for the idle frame, and 167 for the walk, run and jump rows (they stand a few pixels higher in their cells). I set one pivot per group, shared by every frame, so the artist's alignment within each cell is preserved rather than re-centred.

Because the pivot is the character's horizontal centre, mirroring the sprite to face left (`flipX`) flips around the right point, and the character doesn't slide sideways when they turn.

### Matching animation timing to the game

The old clips had a feel I didn't want to lose, so the new frame rates were chosen to keep the old *cycle times*:

| Animation | Frames | Rate | Duration |
|---|---|---|---|
| Walk | 16 | 20 fps | 0.80 s (as before) |
| Run | 16 | 28 fps | 0.57 s (as before) |
| Jump | 16 | 23 fps | 0.70 s |

The jump deserves a note. It's no longer a quick pose change; it's a full take-off-to-landing sequence. A jump with `jumpForce = 14` and a gravity scale of 4 hangs in the air for about 0.71 seconds (`2 × 14 / 39.2`), so a 16-frame clip at 23 fps ends about when the player lands. Two consequences:

- The "how long to show the jump animation" setting went from 0.25 seconds to the length of the clip.
- A **double jump** has to restart the animation. Without a restart, the second jump would pick up wherever the first one had got to. One line in `Jump()` does it: `animator.Play(JumpStateHash, 0, 0f)`.

### Rendering it is the only real test

For all of this, my biggest time-saver was a throwaway render: put the old character and the new one side by side, on a ground line, at in-game scale, with the old collider drawn as a translucent box. Feet on the line, head height matched, body inside the box. If a number like 312 is wrong you see it instantly, and no amount of staring at the inspector shows you the same thing.

## One more import trap

When you copy a big image into a Unity project while the editor is open, Unity imports it as soon as it notices the file, and it can notice a half-written copy. I had a 3 MB sheet import as an 884 KB corrupt file with a "file could not be read" error. Now I copy to a temporary name first and *move* it into place, so the file only appears when it's complete.

## What I'm taking away

- **For every sprite, decide three things:** rectangle, pivot, pixels per unit.
- **Measure with an alpha threshold,** not "anything above zero."
- **If you're replacing art, solve for pixels per unit.** It keeps your physics and your level design valid.
- **Pivot is where the sprite touches the world.** For characters, that's the feet.
- **Colliders copy shapes; they don't follow them.**
- **When one frame holds many pieces, use the frame.** A shared coordinate system beats measuring each part.
- **Render it and look at it.**

That's the first batch of posts from nullcore's development. There's more to come: the audio settings, getting the game onto itch.io, and the build-and-ship workflow.

[← Back to the devlog](https://bmgames.net/blog)
