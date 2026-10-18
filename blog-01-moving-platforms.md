---
slug: moving-platforms-without-the-jitter
title: "Moving Platforms Without the Jitter: Carrying the Player (and Not the Boxes)"
excerpt: "A moving platform is a ten-line script until you stand on it. How I made the player ride along smoothly, and how I stopped platforms from shoving every crate in the level."
category: devlog
date: 2026-10-17
series: nullcore
---

This is the first post in a new series about **nullcore**, the 2D robot platformer I'm building in Unity 6. The last series was about game feel in a much smaller game. nullcore is bigger: eight levels, enemy robots, teleporters, doors, lights, a health system. Along the way I've run into a lot of problems that only show up once you have *several* systems touching each other. This series is about those.

I'm starting with moving platforms, because they looked like the easiest thing on my list and turned into one of the most educational.

## The platform itself was easy

A platform that slides between two points is about as simple as a script gets. Mine has a `pointA`, a `pointB`, a `speed`, and a kinematic `Rigidbody2D`:

```csharp
Vector2 oldPos = rb.position;
Vector2 newPos = Vector2.MoveTowards(oldPos, currentTargetPos, speed * Time.fixedDeltaTime);
rb.MovePosition(newPos);
frameDelta = newPos - oldPos;
```

Two details in there are worth explaining, because I got both wrong at first.

**Kinematic, and `MovePosition`.** A moving platform shouldn't be pushed around by anything, but it does need to push *back* on things standing on it. That's what a kinematic body is for. And you move a kinematic body with `rb.MovePosition` in `FixedUpdate`, not by writing to `transform.position`. Writing to the transform teleports the collider and the physics engine doesn't know the platform has a velocity, which matters in a second.

**Cache the points once.** My first version had the platform chasing a target forever. The cause: `pointA` and `pointB` are *children* of the platform, so they move with it. Reading `pointB.position` every frame meant the "target" was always the same distance ahead of wherever I currently was. The fix is to read the world positions once in `Awake`, before anything moves:

```csharp
if (pointA != null) worldPointA = pointA.position;
if (pointB != null) worldPointB = pointB.position;
```

Children of a moving object are a really convenient way to place waypoints in the editor, and a really easy way to build a feedback loop.

## The problem: standing on it

With the platform moving, I stood on it. And slid off the back. The player doesn't move with the platform because the player isn't *attached* to it; it's just resting on it, and my player controller sets horizontal velocity directly every tick (`velocity.x = moveInput * speed`), which means "no input" is literally "stop moving sideways." The platform leaves, the player stays.

The obvious fixes:

1. **Parent the player to the platform.** I skipped this one: a dynamic rigidbody under a moving parent fights the parent's transform, and you have to un-parent at exactly the right moment on jump, on dash, on walking off the edge.
2. **Move the player by the platform's delta.** Every physics tick, add the platform's movement to the player's *position*.

I went with the second one. It sort of worked, but it also produced a faint, constant jitter, and the player would sometimes feel like they were ignoring input for a frame.

## Positions are for teleporting. Velocity is for moving.

The jitter came from repeatedly writing a position onto a dynamic body. The physics solver is already trying to decide where that body should be this tick, based on its velocity and whatever it's touching. If you also overwrite its position, you're two systems disagreeing about the same number every 20 milliseconds.

What fixed it was expressing the carry as **velocity**, and applying it *before* the physics step, in the same place the controller already sets velocity:

```csharp
MovingPlatformController platform = groundCollider.GetComponentInParent<MovingPlatformController>();
if (platform != null)
{
    rb.linearVelocity += new Vector2(platform.FrameDelta.x / Time.fixedDeltaTime, 0f);
}
```

`FrameDelta` is how far the platform moved last tick; dividing by `fixedDeltaTime` turns it into a speed. The player's own input velocity and the platform's velocity just add up, which also composes cleanly with jumping, because `Jump()` only overwrites the *y* component.

There's a subtlety I only understood after the fact: I'd originally tried doing the carry from a collision callback on the platform, which runs *after* the physics step. With `Rigidbody2D` interpolation on, that was a hard correction after the fact, and interpolation turned it into visible stutter. Doing it before the step makes it part of the tick's normal integration, so interpolation smooths it like everything else.

> **Takeaway:** if a dynamic body needs to move, change its velocity. Only write positions when you actually mean to teleport it.

## The second problem: the platform shoves everything

Once the player rode smoothly, I put a platform in a room with some crates. The platform pushed them off. Obviously, that's what a kinematic body does to dynamic things in its path.

My first reaction was to call it a bug. It isn't, really. It's the physics doing exactly what I asked. The real question was whether I wanted *this particular* platform to behave that way, and the answer was "sometimes." A platform that carries the player across a pit shouldn't also be a bulldozer for every prop in the level.

So it became an opt-in flag, `collideOnlyWithPlayer`, off by default. When it's on, the platform and its children ignore every collider that isn't the player's.

```csharp
int count = gameObject.scene.GetPhysicsScene2D().OverlapBox(
    bounds.center, bounds.size + Vector3.one * (margin * 2f), 0f,
    new ContactFilter2D().NoFilter(), overlapResults);

// ...for each non-player collider found:
Physics2D.IgnoreCollision(own, other, true);
```

Each physics tick it looks slightly *ahead* of the platform (a margin based on how far it'll move), finds anything that's about to be in the way, and tells the physics engine to ignore the pair. Doing it a little early means nothing gets nudged on first contact. I keep a `HashSet` of colliders already ignored so it isn't redoing the work, and prune destroyed ones every 250 ticks.

A trap I hit while testing this: my first test showed *no effect at all*. The cause was that I'd run it in an isolated preview scene, and the query `Physics2D.OverlapBox` talks to the **default** physics scene, not the preview scene's. The fix is to ask the object's own scene: `gameObject.scene.GetPhysicsScene2D()`. Worth knowing if you ever test physics in isolation.

## Two cousins: spring and rolling platforms

Once the moving platform was solid, two variations came cheap:

- **Spring platform.** Steps through a frame sequence and launches the player at `launchSpeed = 18` on a specific frame (`launchFrame = 5`). The part that took thought: the collider has to *grow* with the animation, so there's a `colliderTops[]` array, one collider height per frame. The solid part of the spring is only as tall as the spring is drawn. (That idea comes back in the next post, in a much less cheerful context.)
- **Rolling platform.** This one is moved by ordinary physics, so the script only has to make the *wheels* look right. If a wheel of radius `r` moves `dx` along the ground without slipping, it has rotated `dx / r` radians:

```csharp
float degrees = -dx / wheelRadius * Mathf.Rad2Deg;
```

That one line, using the platform's *actual* movement instead of a made-up spin speed, is why the wheels never look like they're skating.

## What I'm taking away

- A moving platform is two problems: **moving**, and **being stood on**. The second is much harder.
- Velocity for motion, positions for teleports, and apply it before the physics step.
- If something looks like a bug but the physics is doing exactly what you told it to, the fix is usually a *flag*, not a rewrite.
- When you test physics in a scratch scene, use *that scene's* physics scene.

Next up: enemy robots. How they patrol without walking off ledges, why a trigger collider made them see walls that weren't there, and the spike trap that kept swallowing them.

[← Back to the devlog](https://bmgames.net/blog)
