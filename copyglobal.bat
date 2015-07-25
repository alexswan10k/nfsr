rem Copying to global node-modules (debug purposes only)
set sourcePath=%~dp0
set targetPath=%appdata%\npm\node_modules\nfsr
robocopy %sourcePath% %targetPath% /COPYALL /E /Z /MIR /XD .git