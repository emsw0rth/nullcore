---
slug: patrolling-robots-and-spike-traps
title: "Robots That Patrol, Shoot, and Fall Through Spike Traps"
excerpt: "Giving enemy robots eyes: ground and wall sensors, a trigger collider that made them see phantom walls, a sensor that never fired, and a spike trap that dropped them out of the world."
category: devlog
date: 2026-10-20
series: nullcore
---

nullcore is a game about robots, so it was time to add some that weren't me. I wanted two enemies to start with:

- **The Mini Driver**: a little wheeled robot that patrols back and forth. Touching it hurts.
- **The Humanoid Shooter**: walks a patrol route, spots you, pulls out a weapon and fires a laser.

Neither is complicated as an idea. Both were full of small physics problems that only appear when the robots are placed in real levels, next to real doors and pickups and traps.

## Giving a robot eyes

A patrolling robot needs to answer two questions every physics tick: *is there a wall in front of me?* and *is there ground in front of me?* If either answer is wrong, it turns around.

The Mini Driver does both with raycasts:

```csharp
private bool ShouldTurnAround()
{
    float direction = facingRight ? 1f : -1f;
    float halfWidth = BodyHalfWidth;
    Vector2 wallOrigin = rb.position + Vector2.right * direction * halfWidth;

    if (Cast(wallOrigin, Vector2.right * direction, wallCheckDistance, obstacleLayer))
        return true;

    // Cast from foot height (not body center) so the ray actually reaches the ground plane.
    Vector2 edgeOrigin = new Vector2(groundCheck.position.x + direction * halfWidth, groundCheck.position.y);
    return !Cast(edgeOrigin, Vector2.down, edgeCheckDropDistance, groundLayer);
}
```

A wall check is a tiny ray out of the front of the body. A ledge check is a short ray *down* from just in front of the front wheel: if it doesn't hit ground, there's a drop and the robot turns. Simple, and it works, as long as you get four small things right.

### 1. The ledge ray has to start at the feet

It's tempting to cast the ledge ray from the body's *center*. The body is tall enough that a short downward ray from there never reaches the ground, so the robot would think it's permanently standing at an edge. There's a comment in my code reminding me to cast from foot height instead. The ray length (`edgeCheckDropDistance = 0.3`) is then "how big a drop do I tolerate."

### 2. Triggers are invisible walls

Here's the one that took me longest to notice. `Physics2D.Raycast` hits **trigger colliders by default**. My levels are full of triggers: ability pickups, the zone around a switch, the detection area of a teleporter. Robots started turning around in the middle of empty corridors, in front of things that weren't there.

The fix was to build the ray with a filter that refuses triggers:

```csharp
var filter = new ContactFilter2D();
filter.SetLayerMask(mask);
filter.useTriggers = false;
return Physics2D.Raycast(origin, direction, filter, RaycastResults, distance) > 0;
```

Also, obstacles like a closed door aren't necessarily on the `Ground` layer, so the wall check uses a separate, broader `obstacleLayer` mask than the ledge check does.

### 3. The sensor has to be measured, not guessed

I started with a hand-tuned constant for "half the robot's width" (`bodyHalfWidth = 0.35`). The moment I scaled a robot, the sensors were in the wrong place. The robot now reads its real width from `bodyCollider.bounds.extents.x`, and the constant is only a fallback.

### 4. The sensor that never fired

The Humanoid Shooter had a stranger bug: it walked straight into walls and never turned. The wall sensor was fine in principle, but I was measuring from the robot's root transform, and the **collider was offset from the root**. The ray started somewhere behind the front of the body and never reached the wall.

The fix: measure the front edge from the collider's actual bounds (its `max.x` or `min.x`, depending on facing) and its real center height, instead of assuming the collider sits on the transform. Same lesson as the width: *the transform is not the collider*.

## Teaching the shooter to shoot

The Humanoid is a small state machine:

```csharp
private enum State { Patrol, ArmingWeapon, Shooting, HolsteringWeapon }
```

It patrols, and while patrolling it checks a box in front of it (3 wide, 1 tall) for the player. When it spots you it stops, plays its weapon-draw animation, fires, holds the shot for a moment, holsters, and then either arms again (if you're still there) or goes back to patrolling. Each step is just a sprite sequence and a timer; the whole thing is a `switch` in `FixedUpdate`.

The laser it fires is its own object. A couple of details that mattered:

- It's fired left or right, but the art only faces right, so the sprite is flipped with `flipX`, and the little light rig attached to it is rotated 180° on the Z axis when it fires left.
- It destroys itself when it hits a wall layer **or when it leaves the camera's view**, so stray shots never pile up off-screen.

## Don't let the robots collide with each other

With several robots patrolling in the same area, I didn't want them shoving and blocking each other. I gave robots their own physics layer, `Robot`, and switched off `Robot`-versus-`Robot` in the Physics 2D collision matrix. They still collide with the world and with the player, but not with their own kind.

## The spike trap that swallowed robots

The retractable spike traps are a classic: flat, then spikes pop up, then retract. In one area of the level I had a few Mini Drivers rolling around, with a spike trap sitting in their path.

Then I watched a robot ride onto a trap just as it extended, drop through the floor, and fall out of the world.

The cause was a trick I'd used when I first wrote the trap. The trap's collider was a thin plate (0.27 units tall) that was **solid** while retracted, so you could stand on it, and became a **trigger** the moment the spikes started to come out, so it could kill the player on contact. A trigger isn't solid. Anything standing on the plate, which is the *floor* at that spot, suddenly had no floor. The player respawns when that happens, so I never noticed. A robot just falls.

The fix was to stop using "solid or trigger" as a state, and split the trap into three jobs:

1. **The plate** stays solid, always. It's the floor, and it never changes.
2. **A spike body**, a solid box that grows with the spikes. Standing on the trap when it extends now *lifts* you, because the box grows up under your feet. It's the "pushed up instead of swallowed" behavior.
3. **A kill zone**, a trigger box covering the spikes, that hurts the player.

```csharp
private void UpdateSpikeColliders()
{
    bool extended = frameIndex >= 0 && popOutTipY != null && frameIndex < popOutTipY.Length;

    if (spikeBody != null)
    {
        spikeBody.enabled = extended;
        if (extended) SetBox(spikeBody, spikeWidth, popOutTipY[frameIndex]);
    }

    if (killZone != null)
    {
        killZone.enabled = extended;
        if (extended) SetBox(killZone, killZoneWidth, popOutTipY[frameIndex]);
    }
}
```

The box height follows the sprite, frame by frame, using the same idea as the spring platform in the last post: an array with one measured height per animation frame.

Getting those heights right was its own small lesson. `Sprite.textureRect` for these frames is a *trimmed* rectangle, so measuring "how tall is the spike in this frame" from the rect gives numbers that don't line up with the sprite as drawn. I ended up reading the actual opaque pixels of each frame to find where the tips are: about -0.02, 0.14, and 0.37 units from the tile's center, against a retracted plate top at -0.23.

I tested it the cheap way: a throwaway scene with a gap in the floor, a trap in the gap, and a robot on top, stepping the physics manually through a full pop-out and retract. The robot's feet went from resting height up about 0.6 units as the spikes rose, held there, then came back down. It never went below the plate.

One side effect worth knowing: the extended spikes are now solid, so a robot that *walks into* an extended trap treats it as a wall and turns around. That turned out to be exactly the behavior I'd want anyway.

## What I'm taking away

- **The transform is not the collider.** Measure from collider bounds, not from where the object's origin happens to be.
- **Rays hit triggers by default.** If a patrolling AI sees walls that aren't there, check `useTriggers`.
- **Don't flip a floor between solid and trigger.** If one thing needs to be both "the ground" and "the danger," make it two colliders.
- **A throwaway test scene beats staring at the editor.** Putting the exact failure in an empty room, with the physics stepped by hand, turned an unreliable "sometimes they fall" into something I could watch.

Next time: decorating levels. I ended up with 84 prop prefabs, and what looked like a visual task turned into a collision, friction and lighting problem.

[← Back to the devlog](https://bmgames.net/blog)
