mkdir Nuget


cd RIFF.Core
dotnet pack RIFF.Core.csproj -c Release --include-symbols --include-source -p:PackageVersion=3.0.0
cd..
move RIFF.Core\bin\Release\*.nupkg Nuget\

cd RIFF.Framework
dotnet pack RIFF.Framework.csproj -c Release --include-symbols --include-source -p:PackageVersion=3.0.0
cd..
move RIFF.Framework\bin\Release\*.nupkg Nuget\

rem cd RIFF.Core
rem ..\nuget pack RIFF.Core.csproj -IncludeReferencedProjects -Symbols -Version 2.2.11
rem cd..

cd RIFF.Console
..\nuget pack RIFF.Console.csproj -IncludeReferencedProjects -Symbols -Version 3.0.0
cd..

rem cd RIFF.Framework
rem ..\nuget pack RIFF.Framework.csproj -IncludeReferencedProjects -Symbols -Version 2.2.11
rem cd..

cd RIFF.Web.Core
..\nuget pack RIFF.Web.Core.csproj -IncludeReferencedProjects -Symbols -Version 3.0.0
..\nuget pack RIFF.Web.Core.nuspec -Version 3.0.0
cd ..
rem move RIFF.Core\*.nupkg Nuget\
move RIFF.Console\*.nupkg Nuget\
rem move RIFF.Framework\*.nupkg Nuget\
move RIFF.Web.Core\*.nupkg Nuget\
pause
