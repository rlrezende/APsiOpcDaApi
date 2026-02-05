@echo off
echo Registrando bibliotecas OPC COM...

cd /d "%~dp0\Libs\Opc"

:: Registrar bibliotecas COM principais
regsvr32 /s OpcComRcw.dll
regsvr32 /s OpcNetApi.Com.dll
regsvr32 /s PureComServer.dll

:: Registrar bibliotecas OPC UA se necessário
regsvr32 /s Opc.Ua.ComInterop.dll

echo.
echo Bibliotecas OPC registradas!
echo Reinicie a aplicacao para aplicar as mudancas.
pause