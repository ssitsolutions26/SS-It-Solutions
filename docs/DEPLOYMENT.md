# Deployment Guide

The website is currently configured to deploy automatically via GitHub Pages.

## Manual Deployment
If you need to deploy manually:
1. Ensure all changes are committed and pushed to the main branch.
2. In GitHub, go to Settings -> Pages.
3. Select the main branch as the source.
4. The site will automatically build and deploy.

## CI/CD Pipeline
A GitHub Actions workflow (`.github/workflows/deploy.yml`) has been implemented to handle deployments automatically. Any pushes or merges into the `main` branch will trigger an automatic build and deployment to GitHub Pages.