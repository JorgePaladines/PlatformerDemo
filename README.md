# Player Movement in Unity Game

This project contains a 2D player movement system built in Unity 2022 using C#. The `PlayerMovement` script handles character locomotion, including running, jumping, dashing, stomping, and ducking, with smooth deceleration mechanics and directional control.

## Controls

The player can control the character using the following inputs:

- **Movement**:
  - **WASD Keys**: Move the character.
    - `W` or `Up Arrow`: (Currently unused in horizontal movement, reserved for potential vertical actions).
    - `A` or `Left Arrow`: Move left.
    - `S` or `Down Arrow`: Duck (crouch) when grounded and speed is at or below crouch speed.
    - `D` or `Right Arrow`: Move right.
  - **Arrow Keys**: Alternative movement controls (same as WASD).

- **Jumping**:
  - **Space Key**: Jump when grounded. Double jump (mid-air).

- **Dashing**:
  - **Left Shift Key**: Dash in the direction the character is facing or the input direction if specified. Interrupts deceleration and can reverse direction instantly when turning.

- **Attacking**:
  - **Left Mouse Button**: Perform an attack (logic handled in `PlayerAttack` script, not detailed here).

## Mechanics

### Movement
- **Running**: Move at `runSpeed` when holding a horizontal input.
- **Sprinting**: After dashing or stomping, sprint at `dashSpeed` until interrupted (stopping, ducking, or turning).
- **Ducking**: Enter crouch state (`crouchSpeed` = 2f) when pressing down, but only if speed is ≤ `crouchSpeed`. Reduces collider size.
- **Deceleration**: Smoothly slow down when releasing input or turning (normal: 15f, sharp: 30f). Dashes can interrupt this.

### Dashing
- Triggered by Left Shift.
- Speed: `dashSpeed` for `dashTime` (0.1f), followed by a cooldown (`dashCooldownTime` = 0.25f).
- Interrupts deceleration:
  - If decelerating from a turn, dash reverses direction instantly.
  - If decelerating from any source, dash applies `dashSpeed` in the facing direction.
- When dashing while ducking, stops immediately after `dashTime` ends.

### Jumping
- Single jump when grounded.
- Double jump when mid-air, resetting vertical velocity first.

### Stomping
- Triggered by dashing downward (Down Arrow + Left Shift) mid-air.
- On landing, sprints if holding horizontal input.

## Advanced Mechanics

### Movement

- Sprint out of a Stomp by letting go of the down input before touching the ground and holding the direction you want to go
- Sprint out of a Wall Jump by holding the input direction fast enough after Wall Jumping

### Rhythm Mechanic
- **Success**: 4 consecutive attacks or stomps at the moment the tempo is played trigger a more powerful attack
- **Fail**: Miss the valid beat window or miss an entire beat