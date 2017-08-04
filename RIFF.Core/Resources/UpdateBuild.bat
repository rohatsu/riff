@ECHO OFF
ECHO %date% >BuildDate.txt
SET /p CurrentBuild=<BuildNumber.txt
SET /a NewBuild=%CurrentBuild%+1
ECHO %NewBuild%>BuildNumber.txt
