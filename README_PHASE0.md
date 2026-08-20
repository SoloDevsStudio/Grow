# Unity 6.5 Procedural Planet — Phase 0

## Goal
Create the project foundation before terrain generation begins.

## Phase 0 approval gate
Phase 0 is approved only when:

- Unity opens with zero compile errors.
- `PlanetSettings` assets can be created.
- The custom `World Creator` window opens.
- Settings edited in the custom window persist after restarting Unity.
- The project can be committed to Git.
- A fresh clone opens successfully on another machine after Unity regenerates `Library`.
- No machine-specific generated folders are tracked.

## Recommended workflow
1. Copy the `Assets/WorldSystem` folder into the Unity project.
2. Put `.gitignore` at the Unity project root.
3. Open Unity and wait for compilation.
4. Open `Tools > Procedural Planet > World Creator`.
5. Create a settings asset.
6. Save the scene/project.
7. Commit Phase 0.
8. Clone on a second device and confirm the project opens correctly.

## Source control rule
Commit:
- Assets/
- Packages/
- ProjectSettings/
- .gitignore
- documentation

Do not commit:
- Library/
- Temp/
- Logs/
- UserSettings/
- Builds/
- IDE-generated solution/project files
