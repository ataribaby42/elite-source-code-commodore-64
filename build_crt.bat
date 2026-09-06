call make variant=easyflash-pal encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes unbound=yes
if errorlevel 1 exit /b %errorlevel%
powershell.exe -NoProfile -NonInteractive -Command "Start-Sleep -Seconds 2"

call make variant=easyflash-ntsc encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes unbound=yes
if errorlevel 1 exit /b %errorlevel%
powershell.exe -NoProfile -NonInteractive -Command "Start-Sleep -Seconds 2"
