msbuild ../Mustache.cs.sln /p:Configuration=Release
nuget pack ../Mustache.cs/Mustache.cs.csproj -Properties Configuration=Release
nuget push *.nupkg
del *.nupkg