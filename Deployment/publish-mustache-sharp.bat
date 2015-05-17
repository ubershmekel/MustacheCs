msbuild ../mustache.cs.sln /p:Configuration=Release
nuget pack ../mustache.cs/mustache.cs.csproj -Properties Configuration=Release
nuget push *.nupkg
del *.nupkg