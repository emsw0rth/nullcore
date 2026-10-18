---
slug: hit-points-are-not-lives-battery-health-system
title: "Hit Points Are Not Lives: Building a Battery-Style Health System"
excerpt: "nullcore started with lives and 'touch a hazard, respawn.' Then I wanted the player to survive a hit. How I split damage from death, avoided double hits, and built a battery HUD that grows as the player gains max health."
category: devlog
date: 2026-10-27
series: nullcore
---

For a long time, nullcore had one rule about danger: touch a hazard, die. You respawned at the start of the level and a counter in the corner went down. That counter was **lives**, you started with three, and for a while that was enough.

It stopped being enough once I had enemy robots and traps in the same rooms. Dying *and being sent back to the start* for brushing a spike is a harsh punishment for a mistake of a few pixels. I wanted the player to be able to take a hit and keep going.

The obvious move would have been to turn "lives" into "health." I didn't, and the reason is the most useful design decision in this whole feature.

## Two systems, not one

A hit and a death are different events with different costs:

- **Life points** are the player's *health*. Each hit costs one. They start at 3, the maximum can grow, and exhausted ones can be recharged.
- **Extra lives** are what you spend when your life points run out. They work like the old lives: three at the start of a run, and you lose one when you die.

So the sequence is: you take a hit, you lose a life point. You take three hits and you're dead, which costs an extra life, refills every life point, and puts you back at the spawn. Taking damage never teleports you anywhere, and that single difference is most of what makes hazards feel fair.

I renamed the old system throughout the code (`GameSession.Lives` became `GameSession.ExtraLives`) so the two couldn't be confused later. Renaming first is cheap. Discovering in a month that "lives" means two things is not.

## One static class holds the run

Both counters live in a small static class, `GameSession`, which already tracked the current level name and the level timer. Static data is what I want here: it survives loading the next scene, so your health and extra lives carry from level to level.

```csharp
public const int StartingMaxLifePoints = 3;
public static int MaxLifePoints { get; private set; } = StartingMaxLifePoints;
public static int LifePoints { get; private set; } = StartingMaxLifePoints;
public static event System.Action LifePointsChanged;

// Spends one life point. Returns true when that left the player with none (i.e. they died).
public static bool LoseLifePoint()
{
    if (LifePoints > 0)
    {
        LifePoints--;
        LifePointsChanged?.Invoke();
    }
    return LifePoints <= 0;
}

// Recharges one exhausted life point. Returns false (and does nothing) if they are all charged.
public static bool RestoreLifePoint()
{
    if (LifePoints >= MaxLifePoints) return false;
    LifePoints++;
    LifePointsChanged?.Invoke();
    return true;
}

// Raises the maximum; the new life points start out charged.
public static void IncreaseMaxLifePoints(int amount = 1) { /* ... */ }
```

The thing that makes static state safe is resetting it deliberately. A `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` method puts everything back to the starting values when the game boots, and the main menu's Start button resets again. Without that, "static" means "whatever the last session left behind," which is the sort of bug that only shows up when you press Play a second time.

The `LifePointsChanged` event is how the HUD finds out. The HUD never polls the number; it just redraws when told.

## Taking a hit, and not taking it twice

Every hazard in the game used to call `player.Respawn()`. They now call `player.TakeDamage()`:

```csharp
public void TakeDamage()
{
    if (IsInvulnerable)
        return;

    if (GameSession.LoseLifePoint())
    {
        Respawn();
        return;
    }

    invulnerableUntil = Time.time + invulnerabilityDuration;
    rb.linearVelocity = new Vector2(rb.linearVelocity.x, hurtBounceSpeed);
    isGrounded = false;
}
```

Three things in those few lines took thought.

### 1. Invulnerability is not optional

The first time I imagined this feature without an invulnerability window, I pictured the player overlapping a sawblade for a single physics frame and losing all three life points in 60 milliseconds. A hazard doesn't deal damage *once*; it deals damage on every tick the player is touching it. So after any hit (and after any respawn) the player is invulnerable for 1.5 seconds, and `TakeDamage` just returns if it's active.

I'd already met a version of this problem with death. A spike trap reports a touch on enter *and* on stay, and two hazards can overlap, so the same death could be reported twice in one frame and cost two lives. The fix there was a tiny cooldown (0.25 seconds) inside `Respawn`, so only the first report in a window counts. Different mechanism, same idea: *an event that can arrive several times needs a rule for which one counts.*

### 2. The hit has to be visible

Taking damage with no feedback feels like the game is broken. Two cheap things carry it:

- A small upward bounce (`hurtBounceSpeed = 8`). It reads as "something hit me" and it also gets the player out of whatever hurt them.
- A blink while invulnerable: the sprite's alpha drops to 0.3 and back every 0.1 seconds. I do it through the sprite's colour alpha rather than toggling `enabled` so it can't fight with anything else that wants to hide the sprite.

This is the same lesson as in my game-feel post: the feedback is a handful of lines and it's worth more than the mechanic it decorates.

### 3. Entering isn't the same as staying

Spikes and sawblades used to react only in `OnTriggerEnter2D`, which fires *once*, when you first overlap. That was fine when touching them killed you. With invulnerability, there's a hole: you walk onto a spike, take a hit, and the invulnerability runs out *while you're still standing on it*. No new "enter" ever fires, so you'd stand there for the rest of the level taking no damage at all. Those hazards now also react on `OnTriggerStay2D`, and `TakeDamage` quietly ignores it while you're invulnerable.

## The battery

I wanted the health display to be a battery: a left cap, a row of cells, a right cap. A charged cell is a green one, an exhausted cell is dark. The art lives as individual pieces on a spritesheet, each cut so they butt up against each other with no gaps: 19 pixels of left cap, 16 per cell, 26 of right cap, and a three-cell battery comes out exactly 93 pixels wide, the width of the full mock-up battery I'd drawn on the same sheet.

The HUD component builds it at runtime from those four sprites:

```csharp
public void Rebuild(int maxPoints, int points)
{
    // clear old pieces...
    CreatePiece("Left", leftSprite);
    for (int i = 0; i < maxPoints; i++)
        cells.Add(CreatePiece("Cell " + (i + 1), chargedSprite));
    CreatePiece("Right", rightSprite);
    ApplyCharge(points);
}

private void Refresh()
{
    if (builtMax != GameSession.MaxLifePoints)
    {
        Rebuild(GameSession.MaxLifePoints, GameSession.LifePoints);
        return;
    }
    ApplyCharge(GameSession.LifePoints);
}
```

The point of the rebuild-on-change design is that the battery **grows** when the max does. Taking a hit just swaps a sprite from charged to exhausted. Finding a "max up" pickup (when I make one) rebuilds the row with one more cell. A `HorizontalLayoutGroup` with zero spacing does all the positioning, and each piece's layout size is its sprite size times a `pixelScale` setting (1.5 at the moment), so I can resize the whole thing with one number. The HUD is anchored top-right, so the battery grows leftward and stays put against the extra-lives counter.

A bonus of putting the battery inside the **existing** HUD prefab, instead of making a new one: all eight levels already contained that prefab, so none of them needed to be edited. I changed the prefab once and every level picked it up.

## The electrical heart

The other half of "it should be possible to replenish exhausted points" is a pickup: a glowing electrical heart. It's a trigger; the player touches it; it recharges one cell and disappears.

The one design question was what to do when the player's already at full health. Two choices: eat the pickup and waste it, or leave it. I went with leaving it by default. The player can walk past, take a hit later, and come back. If a level ever wants a "use it or lose it" heart, there's a checkbox:

```csharp
if (!GameSession.RestoreLifePoint() && !collectWhenFull)
    return;

collected = true;
Destroy(gameObject);
```

`RestoreLifePoint` returns `false` when there's nothing to restore, so the pickup doesn't need to know anything about health; it just asks.

## Testing a game with no input

I tested the whole chain without playing it: the static class on its own (lose a point, restore one, restore when full, raise the max, hit three times, reset), then a player instance in a scratch scene taking damage with invulnerability turned off, to check that hit three costs an extra life, refills the battery and snaps the player back to spawn. And I rendered the battery at 3, 5 and 6 max points to look at the layout.

None of that replaces playing it, but it turned a lot of "I think this is right" into "I watched it do the right thing."

## What I'm taking away

- **Damage and death are different events.** Don't let one number carry both.
- **Anything that can fire repeatedly needs a rule for which one counts:** a cooldown, an invulnerability window, a `collected` flag.
- **"Enter" and "stay" are different.** If a state can *end* while something is still overlapping, you need the "stay" event.
- **Build UI from data.** A HUD that rebuilds from `Max` and `Current` grows for free when the design does.
- **Edit a shared prefab once, not eight scenes.**

Next up: the sprite pipeline. Turning a sheet of frames into a portal, a laser door and a brand new player, and why the most important number in the whole process was "pixels per unit."

[← Back to the devlog](https://bmgames.net/blog)
