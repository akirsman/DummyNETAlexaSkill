rm .\sampleFactCsharp.zip
dotnet clean DummyNETAlexaSkillLambda.sln
dotnet build -c debug DummyNETAlexaSkillLambda.sln
dotnet publish -c debug DummyNETAlexaSkillLambda.sln
Compress-Archive -Force -Path .\bin\Debug\netcoreapp2.1\publish\* -CompressionLevel Optimal -DestinationPath .\sampleFactCsharp.zip
aws lambda update-function-code --function-name sampleFactCsharp --zip-file fileb://sampleFactCsharp.zip
Get-Date
