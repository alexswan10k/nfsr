@echo off

set nfsrExePath=%AppData%\npm\node_modules\nfsr\bin\nfsr.exe

IF EXIST %nfsrExePath% (
	%nfsrExePath% %*
)

IF NOT EXIST %nfsrExePath% (
	IF EXIST "%ProgramFiles(x86)%\Microsoft SDKs\F#\4.1\Framework\v4.0" (
		set fsharppath="%ProgramFiles(x86)%\Microsoft SDKs\F#\4.1\Framework\v4.0"
	)
	IF EXIST "%ProgramFiles(x86)%\Microsoft SDKs\F#\3.0\Framework\v4.0\" (
		set fsharppath="%ProgramFiles(x86)%\Microsoft SDKs\F#\3.0\Framework\v4.0"
	)
	IF EXIST "%ProgramFiles(x86)%\Microsoft SDKs\F#\3.1\Framework\v4.0\" (
		set fsharppath="%ProgramFiles(x86)%\Microsoft SDKs\F#\3.1\Framework\v4.0"
	)
	IF EXIST "%ProgramFiles(x86)%\Microsoft SDKs\F#\4.0\Framework\v4.0\" (
		set fsharppath="%ProgramFiles(x86)%\Microsoft SDKs\F#\4.0\Framework\v4.0"
	)

	%fsharppath%\Fsi.exe %~dp0\nfsr.fsx %* 
)