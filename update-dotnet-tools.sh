#!/usr/bin/env bash

dotnet tool update --global --all
dotnet workload update

cd Beepsky/
dotnet tool update dotnet-ef
dotnet tool update dotnet-rpm
dotnet tool update dotnet-deb
dotnet tool restore

