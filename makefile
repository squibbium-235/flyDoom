.PHONY: run build test clean

run:
	dotnet run --project src/FlyDoom.App

build:
	dotnet build

test:
	dotnet test

clean:
	dotnet clean