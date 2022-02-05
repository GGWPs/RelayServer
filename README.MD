# Handleiding prototype
Om een instantie van de server te draaien, dien je het te compileren naar een .exe bestand.

Om het te compileren in Ubuntu, zet alle .cs bestanden in een folder. In de folder gebruik de commando "mcs -out:RelayServer.exe -pkg:dotnet *.cs". Start daarna het programma op met mono RelayServer.exe

Als argument kun je een ander poortnummer meegeven.
