@echo off
echo ===================================================
echo Auto Push Website Updates to GitHub
echo ===================================================
echo.
echo Fetching latest updates from GitHub...
git pull origin main --rebase --autostash
echo.
echo Adding changes to git...
git add -f assets/Solar_Quotation_Billing/*.exe
git add .
echo.
echo Committing changes...
    git commit -m "Auto-update: Solar ERP Version 1.4.0 and Website updates"
echo.
echo Pushing to GitHub...
git push origin main
echo.
echo ===================================================
echo Process Completed! Check the output above for any errors.
echo ===================================================
pause
