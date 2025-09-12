dotnet publish tests\CX.Container.Server.IntegrationTests\CX.Container.Server.IntegrationTests.csproj
dotnet test --collect:"XPlat Code Coverage;Format=json,lcov,cobertura" tests\CX.Container.Server.IntegrationTests\bin\Release\net8.0\publish\CX.Container.Server.IntegrationTests.dll

dotnet publish tests\CX.Container.Server.UnitTests\CX.Container.Server.UnitTests.csproj
dotnet test --collect:"XPlat Code Coverage;Format=json,lcov,cobertura" tests\CX.Container.Server.UnitTests\bin\Release\net8.0\publish\CX.Container.Server.UnitTests.dll

dotnet publish tests\CX.Container.Server.FunctionalTests\CX.Container.Server.FunctionalTests.csproj
dotnet test --collect:"XPlat Code Coverage;Format=json,lcov,cobertura" tests\CX.Container.Server.FunctionalTests\bin\Release\net8.0\publish\CX.Container.Server.FunctionalTests.dll
