@echo off

@echo Installing DotNet2Fox FoxCOM.exe...
REM VFP Runtime files are duplicated here. Otherwise, registration below fails when building container.
move "C:\install\DotNet2Fox" "C:\Program Files (x86)\DotNet2Fox"

@echo Registering FoxCOM.exe...
"C:\Program Files (x86)\DotNet2Fox\FoxCOM.exe" /regserver

@echo Setting DCOM permissions...
REM Registering FoxCOM.exe doesn't setup AppID required to set DCOM permissions, so we have to add
reg add "HKCR\AppId\{56458AED-AFB5-4F73-B399-70ABAB55DD57}" /v "(Default)" /t REG_SZ /d "FoxCOM.Application"
C:\install\DComPermEx -runas {56458AED-AFB5-4F73-B399-70ABAB55DD57} "user manager\containeradministrator" thereisnopassword

@echo DotNet2Fox FoxCOM.exe installation complete.

REM Dockerfile handles removing C:\install