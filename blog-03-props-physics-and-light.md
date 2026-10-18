---
slug: decoration-props-physics-friction-and-light
title: "84 Props, One Component: Physics, Friction and Light for Level Decorations"
excerpt: "Three spritesheets became 84 placeable prefabs. How I made them solid on demand, gave them friction when the ground has none, and stopped 2D lights from lighting things they shouldn't."
category: devlog
date: 2026-10-23
series: nullcore
---

A level that's only platforms and enemies looks like a test level. So I built up a stock of set dressing: three big spritesheets of props: crates, barrels, lockers, pipes, fans, signs, graffiti, windows, trash, lamps. I sliced them into **84 prefabs** that I can drag into a level.

The slicing itself was a chore (more on that at the end). The interesting part was the question that came right after: *what should a crate do when the player runs into it?*

## Decoration vs. furniture

The answer is: it depends on the prop. A poster on the wall should never collide with anything. A crate probably should. A heavy locker should be an immovable wall. A barrel should maybe get pushed.

I didn't want four prefab variants of every prop. I wanted one switch I could flip *per instance* in the editor. So every decoration gets one small component, `Decoration`, with three settings:

| Setting | Meaning |
|---|---|
| **Collidable** (default off) | Is this solid at all? Off = pure background. |
| **Is Static** | Only when collidable. On = an immovable block, like a platform. Off = a physics object that falls and can be pushed. |
| **Friction** | How quickly a pushed prop slows down. |

The component's whole job is to make the rest of the object agree with those switches:

```csharp
private void Apply()
{
    foreach (var col in GetComponents<Collider2D>())
        col.enabled = collidable;

    body = GetComponent<Rigidbody2D>();
    if (body != null)
    {
        if (body.simulated != collidable)
            body.simulated = collidable;

        var bodyType = collidable && isStatic ? RigidbodyType2D.Static : RigidbodyType2D.Dynamic;
        if (body.bodyType != bodyType)
            body.bodyType = bodyType;
    }

    int groundLayer = LayerMask.NameToLayer("Ground");
    int layer = collidable && groundLayer >= 0 ? groundLayer : 0;
    if (gameObject.layer != layer)
        gameObject.layer = layer;
}
```

A few choices in there that I'd make again:

- **A non-collidable prop has no physics at all.** The collider is disabled *and* the rigidbody is set to `simulated = false`. A disabled collider on a still-simulated rigidbody is still work for the physics engine, times 84 props across a level.
- **A collidable prop moves to the `Ground` layer.** That's what the player's ground check and the robots' sensors already look for, so the player can stand on a crate and a Mini Driver treats a crate as a wall, with no changes to either.
- **The same code runs in `Awake` and `OnValidate`.** So flipping the checkbox in the editor updates the prop immediately; I don't have to press play to find out what a setting does.

### Gravity, again

There's a callback to the game-feel post here. Props have their own `Rigidbody2D`, and every one of them uses a `gravityScale` of 4, the same as the player. In that earlier post I wrote about an object falling at the wrong speed because a new rigidbody quietly used Unity's default of 1. This time I checked on the way in.

## Friction when the ground has none

I wanted pushed crates to slide a bit and then stop. That's friction, which Unity's physics normally handles through `PhysicsMaterial2D`, so my first instinct was to give crates a friction material.

That can't work in this project, because of a decision I made much earlier: the **ground** and the **player** both use a zero-friction material, so the player doesn't stick to walls and doesn't stutter along seams. When two materials touch, the physics engine combines their friction values; with the common "geometric mean" rule (the square root of the product), zero times anything is zero. Nothing I put on a crate would matter, because the ground, and the player pushing it, contribute zero.

I had two options: change the ground and player materials (and re-tune the player's movement and wall behavior, which I'd rather not touch) or make friction the crate's own job, in a script. I went with the script, and I like it more than I expected: a straight-line "Coulomb" friction, where the stopping force is proportional to weight.

```csharp
// While resting on something, slow horizontal motion (relative to whatever it's
// resting on) by friction * gravity each second.
float decel = friction * Mathf.Abs(Physics2D.gravity.y) * body.gravityScale * Time.fixedDeltaTime;
float vx = Mathf.MoveTowards(body.linearVelocity.x, supportVelocityX, decel);
body.linearVelocity = new Vector2(vx, body.linearVelocity.y);
```

Two details make it behave:

- **"Resting on something" only counts contacts along the underside.** The code finds the bottom of the prop's colliders and ignores any contact point higher than that, plus a small tolerance. Otherwise a crate pressed against a wall, or a smaller crate sitting on top of it, would count as "on the ground" and drag at the wrong moments.
- **Slow toward the *support's* velocity, not toward zero.** If the crate is sitting on a moving platform, the "stopped" speed is the platform's speed. Without that, a crate would be dragged back relative to the world instead of staying put on the platform.

And since it's a number on the component, a patch of ice is just `friction = 0.05`.

## Which one is in front?

Overlapping props needed an answer to "which is drawn on top," and that's what **Order in Layer** is for. I wanted a sensible default: props behind the player and enemies, but I still wanted control per prop. I set every decoration (and later every light fixture) to default to `-1`, and then override on the ones that need to sit in front.

I also gave each prop a **shading** slider, from 0 to 1, that darkens the sprite: 1 is full brightness, 0 is black. I'd already built the same slider for backgrounds, to push them back visually. The prop version tints every sprite on the prop, including child sprites, so a prop made of several sprites darkens as one piece. It keeps the alpha, so it never makes a sprite stranger, only dimmer.

## Lights, and the layer that shouldn't be lit

nullcore uses Unity's 2D renderer, so lights are `Light2D` objects, and they have a rule that surprised me: lights only affect the **sorting layers** you tell them to.

I have wall lamps, ceiling lights, desk lamps, floodlights, all prefabs with a `LightingFixture` component that owns an `on` flag, a `broken` flag, three sprites (on/off/broken) and the lights it controls. Turn it on: swap the sprite, enable the lights. Break it: sprite swaps, lights stay off, and `SetOn` is ignored.

The problem came from the backgrounds. I wanted *some* backgrounds lit by nearby lamps, to get that pool of light on the wall behind a lamp, and *some* backgrounds to stay at ambient light only, so a distant skyline wouldn't glow when you walked past a lamp. The way to split them is by sorting layer: a background marked **Lightable** sits on `Default`, a non-lightable one sits on its own `BackgroundNoFixtureLight` layer, and fixture lights simply never target that layer.

The catch is "simply never target that layer." Every light has its own target list, there were a lot of lights (some point lights, some freeform, laser-shot lights), and the moment one of them was set to "all layers," a skyline lit up. So the fixture now enforces the rule itself:

```csharp
private void ExcludeUnlitBackgroundLayer()
{
    int unlitId = SortingLayer.NameToID(BackgroundController.UnlitSortingLayerName);
    if (unlitId == 0 || !SortingLayer.IsValid(unlitId))
        return;

    foreach (var light in fixtureLights)
    {
        if (light == null) continue;

        var targets = light.targetSortingLayers;
        if (System.Array.IndexOf(targets, unlitId) < 0) continue;

        var filtered = new List<int>(targets);
        filtered.Remove(unlitId);
        light.targetSortingLayers = filtered.ToArray();
    }
}
```

It runs on `Awake` and `OnValidate`, so a lamp can't be left in a state where it lights the unlit layer.

The one gotcha in *fixing* this was a bug in my own fix script. I wrote a one-off editor script to clean all the existing prefabs and had it skip saving any prefab it hadn't "changed." But `OnValidate` had already applied the fix to the in-memory copies as they loaded, so the script saw "nothing to change" and saved nothing. The in-memory version was right; the file on disk was still wrong. I had to force-save every prefab. The lesson: if your editor-time code mutates things on load, "did it change?" is not a reliable question for a batch script.

## A word about slicing 84 props

I sliced each sheet by hand-defining rectangles from measured pixel bounds, tight to the artwork, because I wanted each prop to be placeable by its visible edges, not by a big padded cell. One thing I had to learn the hard way is that **texture coordinates in Unity start at the bottom-left**, while every image editor counts rows from the top. A rectangle that's at `y = 128` from the top of a 1,024-pixel-tall sheet is at `1024 - (128 + height)` in Unity. Every off-by-a-mirror slicing bug I had was that.

## What I'm taking away

- **One component with a few plainly-named switches beats variants of every prefab.**
- **If a physics value needs to be zero in one place, check what that does to every other place it's combined.** Zero friction on the ground meant no friction anywhere.
- **Make the rule live in the object that must obey it,** like the lamps excluding the unlit layer themselves, not in a checklist I have to remember.
- **Batch-fixing editor-time data: save unconditionally.**

Next up: health. nullcore began with lives, then I decided the player should be able to take a *hit* without dying, which turned out to need a whole second system.

[← Back to the devlog](https://bmgames.net/blog)
