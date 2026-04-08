Amp.Core is consumed by other service repositories as a **Git submodule**. This keeps the library versioned independently while allowing each service repo to pin to a specific commit.

### Add to a new project

Run this from the root of the consuming repository. The submodule is placed at `src/Amp.Core` by convention:

```bash
git submodule add https://github.com/your-org/amp-core.git src/Amp.Core
git commit -m "Add Amp.Core submodule"
```

Then add a project reference in each consuming `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\Amp.Core\src\Amp.Core.Common\Amp.Core.Common.csproj" />
  <ProjectReference Include="..\Amp.Core\src\Amp.Core.Services.Abstractions\Amp.Core.Services.Abstractions.csproj" />
  <ProjectReference Include="..\Amp.Core\src\Amp.Core.Services\Amp.Core.Services.csproj" />
  <ProjectReference Include="..\Amp.Core\src\Amp.Core.Extensions\Amp.Core.Extensions.csproj" />
  <ProjectReference Include="..\Amp.Core\src\Amp.Core.Middleware\Amp.Core.Middleware.csproj" />
</ItemGroup>
```

Only reference the projects you actually use — not all five are required in every service.

### Clone a repo that already has the submodule

The submodule directory will be empty after a plain `git clone`. Initialise it with:

```bash
# Option A — clone and initialise in one step
git clone --recurse-submodules https://github.com/your-org/my-service.git

# Option B — already cloned, initialise afterwards
git submodule update --init --recursive
```

### Update to the latest Amp.Core commit

```bash
# Pull the latest commit on the tracked branch
cd src/Amp.Core
git pull origin main
cd ../..

# Stage the new submodule pointer and commit
git add src/Amp.Core
git commit -m "Update Amp.Core submodule to latest"
```

To update all submodules in a repo at once:

```bash
git submodule update --remote --merge
git add .
git commit -m "Update all submodules"
```

### Pin to a specific commit or tag

```bash
cd src/Amp.Core
git checkout v1.4.0        # or a specific commit SHA
cd ../..
git add src/Amp.Core
git commit -m "Pin Amp.Core to v1.4.0"
```

### Pull changes including submodule updates (day-to-day)

After pulling the parent repo, the submodule pointer may have moved. Always sync it:

```bash
git pull
git submodule update --init --recursive
```

### Remove the submodule

```bash
# 1. Remove the submodule entry from .gitmodules
git submodule deinit -f src/Amp.Core

# 2. Remove from the git index
git rm -f src/Amp.Core

# 3. Remove the cached submodule data
rm -rf .git/modules/src/Amp.Core

# 4. Commit
git commit -m "Remove Amp.Core submodule"
```

### Common issues

| Problem | Cause | Fix |
|---------|-------|-----|
| `src/Amp.Core` directory is empty | Submodule not initialised | `git submodule update --init --recursive` |
| Build error: project not found | `.csproj` path wrong after update | Verify `<ProjectReference>` paths match the current directory layout |
| Detached HEAD in submodule | Normal — submodules check out a commit, not a branch | Expected; only change the pointer intentionally via the steps above |
| Merge conflict in submodule pointer | Two branches updated the submodule independently | Resolve by choosing the desired commit SHA in `.gitmodules` then `git add src/Amp.Core` |
