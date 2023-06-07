@echo off

GOTO :PROGRAM

:PROGRAM
echo.
echo Choice project:
echo.
echo     list                       List packages
echo.
echo     pack                       Pack `p1eXu5.FSharp.Ports`
echo.
echo.

set /p "id=Enter ID: "

IF        "%id%"=="list" (
    dotnet fsi .\pack.fsx "list-packages"
    GOTO :PROGRAM

) ELSE IF "%id%"=="pack" (
    dotnet fsi .\pack.fsx "pack-ports-result"
    GOTO :PROGRAM

) ELSE IF "%id%"=="e" (
    echo exit
    GOTO :EOF

) ELSE (
    echo unknown case
    GOTO :PROGRAM
)