msbuild ../MustacheCs.sln /p:Configuration=Release
nuget pack ../MustacheCs/MustacheCs.csproj -Properties Configuration=Release
nuget push *.nupkg
del *.nupkg