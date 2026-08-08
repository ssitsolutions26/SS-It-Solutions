# Git Workflow & Branching Strategy

This repository follows the industry-standard **GitFlow** branching strategy to ensure code quality, stability, and seamless team collaboration.

## Branch Hierarchy

```
main (Production)
  └── develop (Integration)
        ├── feature/ui-enhancements
        ├── feature/seo-optimization
        ├── release/v1.0.0
        └── hotfix/bug-fix-name
```

## Branch Definitions

### 1. `main` (Production Branch)
- Contains official release history.
- Code on `main` is always production-ready and automatically deployed to GitHub Pages.
- Direct commits to `main` are strictly prohibited. All changes arrive via Pull Requests from `release/*` or `hotfix/*`.

### 2. `develop` (Integration Branch)
- Serves as the primary integration branch for ongoing development.
- Contains the latest delivered development features for the next release.

### 3. `feature/*` (Feature Branches)
- Used to develop new features or enhancements.
- Created from: `develop`
- Merged back into: `develop`
- Naming convention: `feature/short-description` (e.g., `feature/company-settings`)

### 4. `release/*` (Release Preparation)
- Used to prepare and polish a new production release (testing, bug fixes, metadata).
- Created from: `develop`
- Merged into: `main` and `develop`
- Naming convention: `release/vX.Y.Z`

### 5. `hotfix/*` (Urgent Production Fixes)
- Used to quickly patch critical production issues.
- Created from: `main`
- Merged into: `main` and `develop`
- Naming convention: `hotfix/short-description`

## Common Commands

```bash
# Start a new feature
git checkout develop
git checkout -b feature/my-new-feature

# Finish a feature
git checkout develop
git merge feature/my-new-feature
git branch -d feature/my-new-feature

# Create a release
git checkout -b release/v1.0.0 develop
```
