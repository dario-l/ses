del *.nupkg -y
nuget pack ..\src\Ses\Ses.csproj -Prop Configuration=Release
nuget pack ..\src\Ses.Abstracts\Ses.Abstracts.csproj -Prop Configuration=Release
nuget pack ..\src\Ses.Domain\Ses.Domain.csproj -Prop Configuration=Release
nuget pack ..\src\Ses.MsSql\Ses.MsSql.csproj -Prop Configuration=Release
nuget pack ..\src\Ses.Subscriptions\Ses.Subscriptions.csproj -Prop Configuration=Release
nuget pack ..\src\Ses.Subscriptions.MsSql\Ses.Subscriptions.MsSql.csproj -Prop Configuration=Release

nuget push SimpleEventStore.*.nupkg -ApiKey 440e1b00-71d6-4031-ba62-9e4af7ffa2b4
pause