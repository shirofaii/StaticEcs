# StaticEcs tests

Characterization tests for the core: they pin down what StaticEcs does today
(components, tags, enable/disable, batch operations, change tracking as
systems see it, hooks, copy/move/clone, world snapshots, destroying and
recreating a world, storage growth past 70,000 entities), so a refactor that
changes behaviour fails here.

```
cd Tests~
dotnet test -c Debug     # with StaticEcs's debug checks (FFS_ECS_DEBUG), as in the Unity editor
dotnet test -c Release   # the optimized paths
```

- The folder ends in `~`, so Unity ignores it, and its `bin/` and `obj/` stay
  inside it. Building `StaticEcs.csproj` at the package root instead leaves an
  `obj/` there that Unity compiles, which breaks the project.
- `Lib/FFS.StaticEcs.csproj` compiles `../Src` into its own assembly.
  StaticEcs must not be compiled into the test assembly: `RegisterAll` skips
  StaticEcs's own assembly, so it would skip the test types too.
- Tests run one at a time; each creates and destroys the static world `TW`.
- The tracking tests record what a probe system sees after each step. A change
  made outside systems shows up once, on the probe's next update; `Set` counts
  as added and changed, `Mut` as changed, `Ref` as nothing.
