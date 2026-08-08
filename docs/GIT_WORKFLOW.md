# Professional Git Workflow

To ensure scalable, collaborative, and safe development, this project follows the standard **GitFlow** branching model.

## Core Branches
- `main`: The production-ready branch. Code here is always stable and deployable.
- `develop`: The active development branch. Features are merged here for integration testing before release.

## Temporary/Supporting Branches
- `feature/*`: Used for developing new features. Branch off from `develop` and merge back into `develop`.
- `release/*`: Used for final preparation of a new production release. Branch off from `develop` and merge into both `main` and `develop`.
- `hotfix/*`: Used to quickly patch production issues. Branch off from `main` and merge into both `main` and `develop`.

---

## Generated Git Commands

### 1. Initializing the Structure (Completed)
We have already created the `develop` branch from `main`:
```bash
git checkout main
git checkout -b develop
git push -u origin develop
```

### 2. Starting a New Feature
```bash
git checkout develop
git pull origin develop
git checkout -b feature/your-feature-name
```
*Work on your feature, commit changes, and push:*
```bash
git add .
git commit -m "feat: your descriptive message"
git push -u origin feature/your-feature-name
```
*(Open a Pull Request into `develop`)*

### 3. Preparing a Release
When `develop` has enough features for a release:
```bash
git checkout develop
git pull origin develop
git checkout -b release/v1.0.0
```
*Test, fix bugs, update version numbers, then merge:*
```bash
git checkout main
git merge release/v1.0.0
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin main --tags

git checkout develop
git merge release/v1.0.0
git push origin develop
```

### 4. Hotfixing Production
If a critical bug is found on the live site (`main`):
```bash
git checkout main
git pull origin main
git checkout -b hotfix/critical-bug-name
```
*Fix the bug, then merge into both branches:*
```bash
git checkout main
git merge hotfix/critical-bug-name
git tag -a v1.0.1 -m "Hotfix patch"
git push origin main --tags

git checkout develop
git merge hotfix/critical-bug-name
git push origin develop
```
