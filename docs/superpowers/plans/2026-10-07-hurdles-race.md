# Hurdles Race Implementation Plan

> **For agentic workers:** Implement these steps in the isolated `codex/hurdles-race` worktree and verify the generated scene in Unity.

**Goal:** Produce a first playable 100m hurdles race scene while the final art is being made.

**Architecture:** Duplicate `PrototypeRace` through the Unity Editor to retain wired race systems and UI. Replace the prototype course with one shared track, two visible lanes, paired hurdles, and one finish trigger. A lane setup component prevents cross-lane physical collisions. A small hurdle component owns impact and falling animation, with optional sprite frames for the incoming spritesheet.

**Tech Stack:** Unity 6000.5.9f1, C#, Unity 2D physics, Unity Editor.

---

### Task 1: Hurdle behavior

- [x] Create an Editor test that fails while `Hurdle` is absent and checks impact once, collider removal, and unchanged state on a second impact.
- [x] Run the test to see the expected failure.
- [x] Implement `Assets/Scripts/Minigames/Hurdle.cs` with optional four-frame sprite animation and a visual rotation fallback.
- [x] Re-run the test and confirm it passes.

### Task 2: Shared track scene and prefab

- [x] Use an Editor builder to load `PrototypeRace`, preserve the wired player, camera, countdown, UI, finish, and match objects, then save `HurdlesRace.unity`.
- [x] Replace prototype platforms and hazards with one stadium, one track, two visual lanes and equal starts.
- [x] Test then configure lane collision isolation so jumping players only interact with their own lane.
- [x] Create a hurdle prefab and place five hurdles per lane at identical X positions.
- [x] Resize the finish trigger to cover both lanes; raise the shared camera's maximum zoom to fit a separated field.
- [x] Include the new scene in Build Settings after the prototype scene.

### Task 3: Verification and art handoff

- [x] Run Unity batch import/compile and inspect logs for script errors.
- [x] Validate that the saved scene has two players, ten hurdles, a shared finish, and valid race/UI references.
- [ ] Play test in Unity for jump spacing, camera, collision, finish order, medal, and repeat round.
- [ ] Replace the placeholder hurdle visual with four aligned 256 px sprites when the art arrives.
