#!/bin/bash

dotnet build AlacrityLibrary.csproj -c Release
cp bin/Release/net4.6/AlacrityLibrary.dll ../Assets/Alacrity/AlacrityCore
