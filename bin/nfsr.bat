REM "C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\Fsi.exe" %*

IF EXIST "%ProgramFiles(x86)%\Microsoft SDKs\F#\4.0\Framework\v4.0\" (
	set fsharppath="%ProgramFiles(x86)%\Microsoft SDKs\F#\4.0\Framework\v4.0"
)
IF EXIST "%ProgramFiles(x86)%\Microsoft SDKs\F#\3.1\Framework\v4.0\" (
	set fsharppath="%ProgramFiles(x86)%\Microsoft SDKs\F#\3.1\Framework\v4.0"
)
IF EXIST "%ProgramFiles(x86)%\Microsoft SDKs\F#\3.0\Framework\v4.0\" (
	set fsharppath="%ProgramFiles(x86)%\Microsoft SDKs\F#\3.0\Framework\v4.0"
)

%fsharppath%\Fsi.exe %~dp0\nfsr.fsx %* --debug+